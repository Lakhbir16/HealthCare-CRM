using Microsoft.EntityFrameworkCore;
using HealthcareCRM.Application.Common;
using HealthcareCRM.Application.DTOs;
using HealthcareCRM.Application.Interfaces;
using HealthcareCRM.Domain.Entities.Auth;
using HealthcareCRM.Domain.Enums;

namespace HealthcareCRM.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;

    public AuditService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(Guid? userId, string action, string? entityName, string? entityId, string? details, string? ipAddress, string? userAgent)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            TimestampUtc = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<ApiResponse<PagedResult<AuditLog>>> GetLogsAsync(int page = 1, int pageSize = 20)
    {
        var query = _context.AuditLogs.Include(a => a.User).OrderByDescending(a => a.TimestampUtc);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return ApiResponse<PagedResult<AuditLog>>.Ok(new PagedResult<AuditLog>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }
}

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAuditService _auditService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuditService auditService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _auditService = auditService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null)
        {
            await _auditService.LogAsync(null, "LOGIN_FAILED", "User", null, $"Failed login attempt for unknown email: {email}", null, null);
            return ApiResponse<LoginResponseDto>.Fail("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            return ApiResponse<LoginResponseDto>.Fail("Account is inactive. Please contact your system administrator.");
        }

        var now = _dateTimeProvider.UtcNow;
        if (user.IsLockedOut && user.LockoutEndUtc.HasValue && user.LockoutEndUtc > now)
        {
            var remainingMinutes = (int)Math.Ceiling((user.LockoutEndUtc.Value - now).TotalMinutes);
            return ApiResponse<LoginResponseDto>.Fail($"Account is temporarily locked. Try again in {remainingMinutes} minutes.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= 5)
            {
                user.IsLockedOut = true;
                user.LockoutEndUtc = now.AddMinutes(15);
                await _auditService.LogAsync(user.Id, "ACCOUNT_LOCKED", "User", user.Id.ToString(), $"Account locked after 5 failed attempts", null, null);
            }
            await _context.SaveChangesAsync();
            return ApiResponse<LoginResponseDto>.Fail("Invalid email or password.");
        }

        // Reset failed counter
        user.AccessFailedCount = 0;
        user.IsLockedOut = false;
        user.LockoutEndUtc = null;

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        if (roles.Count == 0)
        {
            return ApiResponse<LoginResponseDto>.Fail("User has no assigned roles.");
        }

        // Determine active role: preferred role or first available role
        string activeRole = roles[0];
        if (user.PreferredRoleId.HasValue)
        {
            var preferred = user.UserRoles.FirstOrDefault(ur => ur.RoleId == user.PreferredRoleId.Value);
            if (preferred != null)
            {
                activeRole = preferred.Role.Name;
            }
        }

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles, activeRole);
        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(request.RememberMe ? 30 : 7)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        var profileDto = await MapUserProfileAsync(user, roles);

        await _auditService.LogAsync(user.Id, "LOGIN_SUCCESS", "User", user.Id.ToString(), $"Login successful with active role: {activeRole}", null, null);

        return ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAtUtc = now.AddHours(1),
            User = profileDto,
            Roles = roles,
            ActiveRole = activeRole
        }, "Login successful");
    }

    public async Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var tokenRecord = await _context.RefreshTokens
            .Include(r => r.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Include(r => r.User.Profile)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

        if (tokenRecord == null || !tokenRecord.IsActive)
        {
            return ApiResponse<LoginResponseDto>.Fail("Invalid or expired refresh token.");
        }

        var user = tokenRecord.User;
        var now = _dateTimeProvider.UtcNow;

        // Revoke current token and issue replacement (rotation)
        var newRefreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();
        tokenRecord.RevokedAtUtc = now;
        tokenRecord.ReplacedByToken = newRefreshTokenValue;

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshTokenValue,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(7)
        };
        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var activeRole = roles.FirstOrDefault() ?? "Patient";

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles, activeRole);
        var profileDto = await MapUserProfileAsync(user, roles);

        return ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresAtUtc = now.AddHours(1),
            User = profileDto,
            Roles = roles,
            ActiveRole = activeRole
        });
    }

    public async Task<ApiResponse<LoginResponseDto>> SwitchRoleAsync(Guid userId, string requestedRole)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return ApiResponse<LoginResponseDto>.Fail("User not found.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        // Security check: Never trust frontend - verify the user actually holds the requested role
        var matchedRole = user.UserRoles.FirstOrDefault(ur => ur.Role.Name.Equals(requestedRole, StringComparison.OrdinalIgnoreCase));
        if (matchedRole == null)
        {
            await _auditService.LogAsync(userId, "UNAUTHORIZED_ROLE_SWITCH", "User", userId.ToString(), $"Attempted unauthorized switch to role {requestedRole}", null, null);
            return ApiResponse<LoginResponseDto>.Fail("You are not assigned to this role.");
        }

        user.PreferredRoleId = matchedRole.RoleId;
        await _context.SaveChangesAsync();

        var activeRoleName = matchedRole.Role.Name;
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roles, activeRoleName);
        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();

        var profileDto = await MapUserProfileAsync(user, roles);

        await _auditService.LogAsync(userId, "ROLE_SWITCHED", "User", userId.ToString(), $"Switched active role to {activeRoleName}", null, null);

        return ApiResponse<LoginResponseDto>.Ok(new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAtUtc = _dateTimeProvider.UtcNow.AddHours(1),
            User = profileDto,
            Roles = roles,
            ActiveRole = activeRoleName
        }, $"Active role switched to {activeRoleName}");
    }

    public async Task<ApiResponse<bool>> LogoutAsync(Guid userId, string? refreshToken)
    {
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var tokenRecord = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken && r.UserId == userId);
            if (tokenRecord != null)
            {
                tokenRecord.RevokedAtUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        await _auditService.LogAsync(userId, "LOGOUT", "User", userId.ToString(), "User logged out", null, null);
        return ApiResponse<bool>.Ok(true, "Logged out successfully");
    }

    public async Task<ApiResponse<UserProfileDto>> GetCurrentUserProfileAsync(Guid userId, string? activeRole)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return ApiResponse<UserProfileDto>.Fail("User not found.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var dto = await MapUserProfileAsync(user, roles);
        return ApiResponse<UserProfileDto>.Ok(dto);
    }

    public async Task<ApiResponse<List<string>>> GetUserRolesAsync(Guid userId)
    {
        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        return ApiResponse<List<string>>.Ok(roles);
    }

    public async Task<ApiResponse<List<UserManagementDto>>> GetUsersAsync()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new UserManagementDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                IsLockedOut = u.IsLockedOut,
                Roles = u.UserRoles.Select(r => r.Role.Name).ToList(),
                CreatedAtUtc = u.CreatedAtUtc
            })
            .ToListAsync();

        return ApiResponse<List<UserManagementDto>>.Ok(users);
    }

    public async Task<ApiResponse<UserManagementDto>> CreateUserAsync(CreateUserRequestDto request)
    {
        var existing = await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());
        if (existing)
            return ApiResponse<UserManagementDto>.Fail("A user with this email address already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // Assign roles
        if (request.Roles != null && request.Roles.Count > 0)
        {
            var dbRoles = await _context.Roles.Where(r => request.Roles.Contains(r.Name)).ToListAsync();
            foreach (var r in dbRoles)
            {
                _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = r.Id, AssignedAtUtc = DateTime.UtcNow });
            }
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(null, "USER_CREATED", "User", user.Id.ToString(), $"Created user {user.Email}", null, null);

        return ApiResponse<UserManagementDto>.Ok(new UserManagementDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            Roles = request.Roles ?? new List<string>(),
            CreatedAtUtc = user.CreatedAtUtc
        }, "User created successfully");
    }

    public async Task<ApiResponse<bool>> UpdateUserRolesAsync(Guid userId, List<string> roles)
    {
        var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return ApiResponse<bool>.Fail("User not found.");

        _context.UserRoles.RemoveRange(user.UserRoles);

        var dbRoles = await _context.Roles.Where(r => roles.Contains(r.Name)).ToListAsync();
        foreach (var r in dbRoles)
        {
            _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = r.Id, AssignedAtUtc = DateTime.UtcNow });
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(null, "USER_ROLES_UPDATED", "User", userId.ToString(), $"Updated roles for {user.Email}", null, null);
        return ApiResponse<bool>.Ok(true, "Roles updated successfully.");
    }

    public async Task<ApiResponse<bool>> SetUserStatusAsync(Guid userId, bool isActive, bool isLockedOut)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return ApiResponse<bool>.Fail("User not found.");

        user.IsActive = isActive;
        user.IsLockedOut = isLockedOut;
        if (!isLockedOut)
        {
            user.LockoutEndUtc = null;
            user.AccessFailedCount = 0;
        }

        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "User status updated successfully.");
    }

    private async Task<UserProfileDto> MapUserProfileAsync(User user, List<string> roles)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
        var nurse = await _context.Nurses.FirstOrDefaultAsync(n => n.UserId == user.Id);

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.Profile?.AvatarUrl,
            DateOfBirth = user.Profile?.DateOfBirth,
            Gender = user.Profile?.Gender,
            Address = user.Profile?.Address,
            City = user.Profile?.City,
            State = user.Profile?.State,
            ZipCode = user.Profile?.ZipCode,
            TimeZone = user.Profile?.TimeZone ?? "America/New_York",
            Bio = user.Profile?.Bio,
            Roles = roles,
            PatientId = patient?.Id,
            DoctorId = doctor?.Id,
            NurseId = nurse?.Id
        };
    }
}
