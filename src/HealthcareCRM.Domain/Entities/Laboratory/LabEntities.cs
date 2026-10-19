using System.ComponentModel.DataAnnotations;
using HealthcareCRM.Domain.Common;
using HealthcareCRM.Domain.Entities.Appointments;
using HealthcareCRM.Domain.Entities.Auth;
using HealthcareCRM.Domain.Entities.Patients;
using HealthcareCRM.Domain.Entities.Staff;
using HealthcareCRM.Domain.Enums;

namespace HealthcareCRM.Domain.Entities.Laboratory;

public class LabOrder : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public virtual Doctor Doctor { get; set; } = null!;

    public Guid? AppointmentId { get; set; }
    public virtual Appointment? Appointment { get; set; }

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    public DateTime OrderedAtUtc { get; set; } = DateTime.UtcNow;

    public PriorityLevel Priority { get; set; } = PriorityLevel.Routine;
    public LabOrderStatus Status { get; set; } = LabOrderStatus.Ordered;

    [MaxLength(1000)]
    public string? ClinicalIndication { get; set; }

    public virtual ICollection<LabOrderItem> Items { get; set; } = new List<LabOrderItem>();
}

public class LabOrderItem : BaseEntity
{
    public Guid LabOrderId { get; set; }
    public virtual LabOrder LabOrder { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string TestName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TestCode { get; set; } = string.Empty; // CPT or LOINC code

    [MaxLength(50)]
    public string SpecimenType { get; set; } = "Blood"; // Blood, Urine, Swab, Tissue

    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, InProcess, Completed, Cancelled

    public virtual ICollection<LabSample> Samples { get; set; } = new List<LabSample>();
    public virtual ICollection<LabResult> Results { get; set; } = new List<LabResult>();
}

public class LabSample : BaseEntity
{
    public Guid LabOrderItemId { get; set; }
    public virtual LabOrderItem LabOrderItem { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Barcode { get; set; } = string.Empty;

    [MaxLength(50)]
    public string SpecimenType { get; set; } = string.Empty;

    public DateTime CollectedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? CollectedByUserId { get; set; }
    public virtual User? CollectedByUser { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Collected"; // Collected, Received, Processed, Rejected
}

public class LabResult : BaseEntity
{
    public Guid LabOrderItemId { get; set; }
    public virtual LabOrderItem LabOrderItem { get; set; } = null!;

    public Guid? LabTechnicianId { get; set; }
    public virtual User? LabTechnician { get; set; }

    public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(100)]
    public string ParameterName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Unit { get; set; }

    [MaxLength(100)]
    public string? ReferenceRange { get; set; }

    public bool IsAbnormal { get; set; } = false;

    [MaxLength(20)]
    public string? Flag { get; set; } // Normal, High, Low, Critical

    [MaxLength(2000)]
    public string? InterpretationNotes { get; set; }
}
