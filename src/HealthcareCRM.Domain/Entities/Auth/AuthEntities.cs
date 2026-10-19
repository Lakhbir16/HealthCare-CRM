using System.ComponentModel.DataAnnotations;
using HealthcareCRM.Domain.Common;

namespace HealthcareCRM.Domain.Entities.Auth;

public class User : BaseEntity
{
    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsLockedOut { get; set; } = false;

    public DateTime? LockoutEndUtc { get; set; }

    public int AccessFailedCount { get; set; } = 0;

    public Guid? PreferredRoleId { get; set; }

    // Navigation properties
    public virtual UserProfile? Profile { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<OTPVerification> OTPVerifications { get; set; } = new List<OTPVerification>();
    public virtual ICollection<PasswordResetRequest> PasswordResetRequests { get; set; } = new List<PasswordResetRequest>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public class Role : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NormalizedName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class UserRole
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? AssignedByUserId { get; set; }
}

public class UserProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? ZipCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; } = "USA";

    [MaxLength(50)]
    public string? TimeZone { get; set; } = "America/New_York";

    [MaxLength(1000)]
    public string? Bio { get; set; }
}

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    [MaxLength(500)]
    public string? ReplacedByToken { get; set; }

    public bool IsRevoked => RevokedAtUtc != null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsActive => !IsRevoked && !IsExpired;
}

public class OTPVerification : BaseEntity
{
    public Guid? UserId { get; set; }
    public virtual User? User { get; set; }

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string OtpHash { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Purpose { get; set; } = "PasswordReset";

    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAtUtc { get; set; }
    public int AttemptsCount { get; set; } = 0;
    public int MaxAttempts { get; set; } = 5;

    public bool IsExpired(DateTime currentUtc) => currentUtc >= ExpiresAtUtc;
}

public class PasswordResetRequest : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [Required]
    [MaxLength(256)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAtUtc { get; set; }
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public virtual User? User { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EntityName { get; set; }

    [MaxLength(100)]
    public string? EntityId { get; set; }

    [MaxLength(4000)]
    public string? Details { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
