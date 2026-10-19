using System.ComponentModel.DataAnnotations;
using HealthcareCRM.Domain.Common;
using HealthcareCRM.Domain.Entities.Auth;

namespace HealthcareCRM.Domain.Entities.System;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Type { get; set; } = "Info"; // Info, Alert, Appointment, LabResult, Billing

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAtUtc { get; set; }

    [MaxLength(500)]
    public string? ActionUrl { get; set; }
}

public class NotificationPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public bool EmailAlerts { get; set; } = true;
    public bool SmsAlerts { get; set; } = false;
    public bool AppointmentReminders { get; set; } = true;
    public bool LabResultsReady { get; set; } = true;
    public bool BillingUpdates { get; set; } = true;
}

public class SystemSetting : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = "General";
}
