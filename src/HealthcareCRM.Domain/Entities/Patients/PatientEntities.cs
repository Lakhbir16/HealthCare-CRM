using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HealthcareCRM.Domain.Common;
using HealthcareCRM.Domain.Entities.Auth;
using HealthcareCRM.Domain.Entities.Staff;
using HealthcareCRM.Domain.Entities.Appointments;

namespace HealthcareCRM.Domain.Entities.Patients;

public class Patient : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string MedicalRecordNumber { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? BloodGroup { get; set; }

    [MaxLength(20)]
    public string? MaritalStatus { get; set; }

    public Guid? PrimaryCareDoctorId { get; set; }
    public virtual Doctor? PrimaryCareDoctor { get; set; }

    // Navigation collections
    public virtual ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
    public virtual ICollection<MedicalHistory> MedicalHistories { get; set; } = new List<MedicalHistory>();
    public virtual ICollection<Allergy> Allergies { get; set; } = new List<Allergy>();
    public virtual ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();
    public virtual ICollection<VitalSign> VitalSigns { get; set; } = new List<VitalSign>();
    public virtual ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();
    public virtual ICollection<TreatmentPlan> TreatmentPlans { get; set; } = new List<TreatmentPlan>();
    public virtual ICollection<MedicalDocument> MedicalDocuments { get; set; } = new List<MedicalDocument>();
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}

public class EmergencyContact : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Relationship { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Email { get; set; }

    public bool IsPrimary { get; set; } = true;
}

public class MedicalHistory : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Condition { get; set; } = string.Empty;

    public DateTime? DiagnosedDate { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Resolved, Chronic

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Allergy : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string Allergen { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Severity { get; set; } = "Moderate"; // Mild, Moderate, Severe, Life-Threatening

    [MaxLength(200)]
    public string? Reaction { get; set; }

    public DateTime? DiagnosedDate { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Diagnosis : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid? DoctorId { get; set; }
    public virtual Doctor? Doctor { get; set; }

    public Guid? AppointmentId { get; set; }
    public virtual Appointment? Appointment { get; set; }

    [Required]
    [MaxLength(30)]
    public string Icd10Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Inactive, Resolved

    public DateTime DiagnosedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(1500)]
    public string? Notes { get; set; }
}

public class VitalSign : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid? RecordedByUserId { get; set; }
    public virtual User? RecordedByUser { get; set; }

    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? HeartRateBpm { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? TemperatureFahrenheit { get; set; }

    public int? RespiratoryRateBpm { get; set; }
    public int? OxygenSaturationPercent { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? HeightInches { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal? WeightPounds { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Bmi { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class ClinicalNote : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public virtual Doctor Doctor { get; set; } = null!;

    public Guid? AppointmentId { get; set; }
    public virtual Appointment? Appointment { get; set; }

    [MaxLength(50)]
    public string NoteType { get; set; } = "SOAP";

    [MaxLength(4000)]
    public string? Subjective { get; set; }

    [MaxLength(4000)]
    public string? Objective { get; set; }

    [MaxLength(4000)]
    public string? Assessment { get; set; }

    [MaxLength(4000)]
    public string? Plan { get; set; }
}

public class TreatmentPlan : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public virtual Doctor Doctor { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(2000)]
    public string? Goals { get; set; }

    public DateTime StartDateUtc { get; set; }
    public DateTime? TargetEndDateUtc { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Completed, Discontinued
}

public class DocumentCategory : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }
}

public class MedicalDocument : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid? CategoryId { get; set; }
    public virtual DocumentCategory? Category { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? FilePath { get; set; }

    public long FileSizeBytes { get; set; }

    [MaxLength(100)]
    public string ContentType { get; set; } = "application/pdf";

    public Guid? UploadedByUserId { get; set; }
    public virtual User? UploadedByUser { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
