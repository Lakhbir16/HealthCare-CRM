namespace HealthcareCRM.Application.DTOs;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public UserProfileDto User { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
    public string ActiveRole { get; set; } = string.Empty;
}

public class SwitchRoleRequestDto
{
    public string Role { get; set; } = string.Empty;
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ForgotPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordResponseDto
{
    public string Message { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; } = 30;
    public DateTime ExpiresAtUtc { get; set; }
}

public class VerifyOtpRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}

public class VerifyOtpResponseDto
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiresAtUtc { get; set; }
}

public class ResendOtpRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? TimeZone { get; set; }
    public string? Bio { get; set; }
    public List<string> Roles { get; set; } = new();
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? NurseId { get; set; }
}

public class DevMailMessageDto
{
    public Guid Id { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public string DeliveryStatus { get; set; } = "Delivered";
}

public class UserManagementDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateUserRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UpdateUserRolesDto
{
    public List<string> Roles { get; set; } = new();
}
