using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HealthcareCRM.Domain.Common;
using HealthcareCRM.Domain.Entities.Auth;
using HealthcareCRM.Domain.Entities.Patients;

namespace HealthcareCRM.Domain.Entities.Staff;

public class Department : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    public virtual ICollection<Nurse> Nurses { get; set; } = new List<Nurse>();
}

public class Specialty : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}

public class ClinicLocation : BaseEntity
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(50)]
    public string State { get; set; } = string.Empty;

    [MaxLength(20)]
    public string ZipCode { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string TimeZone { get; set; } = "America/New_York";

    public bool IsActive { get; set; } = true;
}

public class Doctor : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string NpiNumber { get; set; } = string.Empty; // National Provider Identifier (10-digit US)

    public Guid DepartmentId { get; set; }
    public virtual Department Department { get; set; } = null!;

    public Guid SpecialtyId { get; set; }
    public virtual Specialty Specialty { get; set; } = null!;

    [MaxLength(50)]
    public string? OfficeNumber { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ConsultationFee { get; set; } = 150.00m;

    [MaxLength(1000)]
    public string? Bio { get; set; }

    public bool IsAcceptingPatients { get; set; } = true;
}

public class Nurse : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }
    public virtual Department Department { get; set; } = null!;

    [MaxLength(50)]
    public string Shift { get; set; } = "Day"; // Day, Evening, Night, Rotating

    public bool IsActive { get; set; } = true;
}

public class NursingTask : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid? NurseId { get; set; }
    public virtual Nurse? Nurse { get; set; }

    [Required]
    [MaxLength(300)]
    public string TaskDescription { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Priority { get; set; } = "Routine"; // Routine, Urgent, Stat

    public DateTime DueTimeUtc { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Cancelled

    public DateTime? CompletedAtUtc { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
