using System.ComponentModel.DataAnnotations;
using HealthcareCRM.Domain.Common;
using HealthcareCRM.Domain.Entities.Appointments;
using HealthcareCRM.Domain.Entities.Patients;
using HealthcareCRM.Domain.Entities.Staff;
using HealthcareCRM.Domain.Enums;

namespace HealthcareCRM.Domain.Entities.Medications;

public class Medication : BaseEntity
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? GenericName { get; set; }

    [MaxLength(150)]
    public string? BrandName { get; set; }

    [MaxLength(50)]
    public string Form { get; set; } = "Tablet"; // Tablet, Capsule, Liquid, Injection, Inhaler, Topical

    [MaxLength(50)]
    public string Strength { get; set; } = string.Empty; // e.g. "500mg", "10ml"

    [MaxLength(150)]
    public string? Manufacturer { get; set; }

    [MaxLength(500)]
    public string? StandardInstructions { get; set; }

    public bool IsControlledSubstance { get; set; } = false;
}

public class Prescription : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public virtual Doctor Doctor { get; set; } = null!;

    public Guid? AppointmentId { get; set; }
    public virtual Appointment? Appointment { get; set; }

    [Required]
    [MaxLength(50)]
    public string PrescriptionNumber { get; set; } = string.Empty;

    public DateTime PrescribedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpirationDateUtc { get; set; }

    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Active;

    [MaxLength(1000)]
    public string? PharmacyNotes { get; set; }

    public virtual ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}

public class PrescriptionItem : BaseEntity
{
    public Guid PrescriptionId { get; set; }
    public virtual Prescription Prescription { get; set; } = null!;

    public Guid? MedicationId { get; set; }
    public virtual Medication? Medication { get; set; }

    [Required]
    [MaxLength(150)]
    public string MedicationName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Dosage { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Frequency { get; set; } = string.Empty; // e.g. "Once daily", "Twice daily with meals"

    public int DurationDays { get; set; } = 30;
    public int Quantity { get; set; } = 30;
    public int RefillsAllowed { get; set; } = 0;
    public int RefillsRemaining { get; set; } = 0;

    [MaxLength(500)]
    public string? Instructions { get; set; }

    public virtual ICollection<MedicationAdministration> Administrations { get; set; } = new List<MedicationAdministration>();
}

public class MedicationAdministration : BaseEntity
{
    public Guid PrescriptionItemId { get; set; }
    public virtual PrescriptionItem PrescriptionItem { get; set; } = null!;

    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid? AdministeredByNurseId { get; set; }
    public virtual Nurse? AdministeredByNurse { get; set; }

    public DateTime AdministeredAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string DoseAdministered { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Status { get; set; } = "Administered"; // Administered, Refused, Missed

    [MaxLength(500)]
    public string? Notes { get; set; }
}
