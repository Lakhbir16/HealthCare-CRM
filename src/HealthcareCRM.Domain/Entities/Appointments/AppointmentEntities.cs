using System.ComponentModel.DataAnnotations;
using HealthcareCRM.Domain.Common;
using HealthcareCRM.Domain.Entities.Auth;
using HealthcareCRM.Domain.Entities.Patients;
using HealthcareCRM.Domain.Entities.Staff;
using HealthcareCRM.Domain.Enums;

namespace HealthcareCRM.Domain.Entities.Appointments;

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public virtual Doctor Doctor { get; set; } = null!;

    public Guid? ClinicLocationId { get; set; }
    public virtual ClinicLocation? ClinicLocation { get; set; }

    public Guid? DepartmentId { get; set; }
    public virtual Department? Department { get; set; }

    public DateTime ScheduledDateTimeUtc { get; set; }
    public int DurationMinutes { get; set; } = 30;

    [MaxLength(50)]
    public string AppointmentType { get; set; } = "InPerson"; // InPerson, Telehealth, FollowUp, Consultation

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    [MaxLength(500)]
    public string ReasonForVisit { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateTime? CheckedInAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public virtual PatientCheckIn? CheckInRecord { get; set; }
}

public class PatientCheckIn : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public virtual Appointment Appointment { get; set; } = null!;

    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public DateTime CheckInTimeUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(20)]
    public string? QueueNumber { get; set; }

    [MaxLength(50)]
    public string? RoomAssigned { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Waiting"; // Waiting, InTriage, WithDoctor, Completed
}
