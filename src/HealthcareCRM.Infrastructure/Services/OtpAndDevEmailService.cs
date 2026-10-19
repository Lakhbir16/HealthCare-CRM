using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HealthcareCRM.Application.Common;
using HealthcareCRM.Application.DTOs;
using HealthcareCRM.Application.Interfaces;
using HealthcareCRM.Domain.Entities.Auth;

namespace HealthcareCRM.Infrastructure.Services;

public class DevEmailService : IDevEmailService
{
    private static readonly ConcurrentBag<DevMailMessageDto> _messages = new();
    private readonly ILogger<DevEmailService> _logger;

    public DevEmailService(ILogger<DevEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendOtpEmailAsync(string recipientEmail, string otp, DateTime expiresAtUtc)
    {
        var msg = new DevMailMessageDto
        {
            Id = Guid.NewGuid(),
            RecipientEmail = recipientEmail,
            Subject = "Your Healthcare CRM Password Reset OTP",
            Content = $"Your one-time password (OTP) is: {otp}. This code is strictly confidential and expires in 30 seconds.",
            OtpCode = otp,
            SentAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
            DeliveryStatus = "Delivered"
        };

        _messages.Add(msg);
        _logger.LogInformation("Simulated email dispatched to DevMailbox for {Recipient}. Expires at: {ExpiresAtUtc}", recipientEmail, expiresAtUtc);
        return Task.CompletedTask;
    }

    public Task<List<DevMailMessageDto>> GetSimulatedMessagesAsync(string? recipientFilter = null)
    {
        var query = _messages.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(recipientFilter))
        {
            query = query.Where(m => m.RecipientEmail.Contains(recipientFilter, StringComparison.OrdinalIgnoreCase));
        }

        var results = query.OrderByDescending(m => m.SentAtUtc).Take(50).ToList();
        return Task.FromResult(results);
    }

    public Task<DevMailMessageDto?> GetMessageByIdAsync(Guid id)
    {
        var msg = _messages.FirstOrDefault(m => m.Id == id);
        return Task.FromResult(msg);
    }
}

public class OtpService : IOtpService
{
    private readonly IApplicationDbContext _context;
    private readonly IDevEmailService _devEmailService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IApplicationDbContext context,
        IDevEmailService devEmailService,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IAuditService auditService,
        ILogger<OtpService> logger)
    {
        _context = context;
        _devEmailService = devEmailService;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ForgotPasswordResponseDto> RequestOtpAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var now = _dateTimeProvider.UtcNow;
        var expiresAtUtc = now.AddSeconds(30);

        // Find user if exists
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        // Invalidate prior active OTPs for this email and purpose
        var oldOtps = await _context.OTPVerifications
            .Where(o => o.Email.ToLower() == normalizedEmail && !o.IsUsed && o.Purpose == "PasswordReset")
            .ToListAsync();

        foreach (var old in oldOtps)
        {
            old.IsUsed = true;
            old.UsedAtUtc = now;
        }

        // Generate 6-digit cryptographically secure OTP
        var rawOtp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");

        // Hash the OTP with SHA256 before storing (Never store raw OTP)
        var otpHash = HashOtp(rawOtp);

        var otpEntity = new OTPVerification
        {
            Id = Guid.NewGuid(),
            UserId = user?.Id,
            Email = normalizedEmail,
            OtpHash = otpHash,
            Purpose = "PasswordReset",
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc,
            IsUsed = false,
            AttemptsCount = 0,
            MaxAttempts = 5
        };

        _context.OTPVerifications.Add(otpEntity);
        await _context.SaveChangesAsync();

        // Send simulated email
        await _devEmailService.SendOtpEmailAsync(normalizedEmail, rawOtp, expiresAtUtc);

        await _auditService.LogAsync(user?.Id, "OTP_REQUESTED", "OTPVerification", otpEntity.Id.ToString(), $"OTP requested for {normalizedEmail}", null, null);

        return new ForgotPasswordResponseDto
        {
            Message = "If an account exists for this email address, password reset instructions have been generated.",
            ExpiresInSeconds = 30,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public async Task<VerifyOtpResponseDto> VerifyOtpAsync(string email, string otp)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var now = _dateTimeProvider.UtcNow;

        var otpRecord = await _context.OTPVerifications
            .Where(o => o.Email.ToLower() == normalizedEmail && o.Purpose == "PasswordReset" && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (otpRecord == null)
        {
            return new VerifyOtpResponseDto
            {
                IsValid = false,
                Message = "No active OTP found. Please request a new code."
            };
        }

        // Strict server-side expiration validation
        if (now >= otpRecord.ExpiresAtUtc)
        {
            otpRecord.IsUsed = true; // Mark as invalidated
            await _context.SaveChangesAsync();
            return new VerifyOtpResponseDto
            {
                IsValid = false,
                Message = "OTP has expired. Please request a new 30-second code."
            };
        }

        // Attempt counter check
        otpRecord.AttemptsCount++;
        if (otpRecord.AttemptsCount > otpRecord.MaxAttempts)
        {
            otpRecord.IsUsed = true; // Invalidate on too many attempts
            await _context.SaveChangesAsync();
            return new VerifyOtpResponseDto
            {
                IsValid = false,
                Message = "Maximum OTP attempts exceeded. Please request a new code."
            };
        }

        // Verify OTP hash
        var inputHash = HashOtp(otp);
        if (otpRecord.OtpHash != inputHash)
        {
            await _context.SaveChangesAsync();
            var remaining = otpRecord.MaxAttempts - otpRecord.AttemptsCount;
            return new VerifyOtpResponseDto
            {
                IsValid = false,
                Message = $"Invalid OTP code. {remaining} attempt(s) remaining."
            };
        }

        // Mark OTP as single-use verified
        otpRecord.IsUsed = true;
        otpRecord.UsedAtUtc = now;

        // Generate short-lived password reset token (5 minutes validity)
        var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var resetTokenHash = HashOtp(resetToken);

        if (otpRecord.UserId.HasValue)
        {
            var resetRequest = new PasswordResetRequest
            {
                Id = Guid.NewGuid(),
                UserId = otpRecord.UserId.Value,
                TokenHash = resetTokenHash,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(5),
                IsUsed = false
            };
            _context.PasswordResetRequests.Add(resetRequest);
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(otpRecord.UserId, "OTP_VERIFIED", "OTPVerification", otpRecord.Id.ToString(), $"OTP verified successfully for {normalizedEmail}", null, null);

        return new VerifyOtpResponseDto
        {
            IsValid = true,
            Message = "OTP verified successfully.",
            ResetToken = resetToken,
            ResetTokenExpiresAtUtc = now.AddMinutes(5)
        };
    }

    public async Task<ForgotPasswordResponseDto> ResendOtpAsync(string email)
    {
        // Resend generates a brand new 30-second OTP
        return await RequestOtpAsync(email);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var now = _dateTimeProvider.UtcNow;
        var tokenHash = HashOtp(request.ResetToken);

        var user = await _context.Users
            .Include(u => u.PasswordResetRequests)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null)
            return false;

        var resetRequest = user.PasswordResetRequests
            .Where(r => r.TokenHash == tokenHash && !r.IsUsed)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefault();

        if (resetRequest == null || now >= resetRequest.ExpiresAtUtc)
        {
            return false;
        }

        // Hash new password securely
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAtUtc = now;
        user.AccessFailedCount = 0;
        user.IsLockedOut = false;
        user.LockoutEndUtc = null;

        // Invalidate reset token
        resetRequest.IsUsed = true;
        resetRequest.UsedAtUtc = now;

        // Revoke active refresh tokens
        foreach (var rt in user.RefreshTokens.Where(rt => rt.IsActive))
        {
            rt.RevokedAtUtc = now;
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(user.Id, "PASSWORD_RESET_SUCCESS", "User", user.Id.ToString(), "Password successfully reset via OTP workflow", null, null);

        return true;
    }

    private static string HashOtp(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
