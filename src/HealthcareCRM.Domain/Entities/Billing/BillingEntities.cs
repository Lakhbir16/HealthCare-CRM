using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HealthcareCRM.Domain.Common;
using HealthcareCRM.Domain.Entities.Appointments;
using HealthcareCRM.Domain.Entities.Auth;
using HealthcareCRM.Domain.Entities.Patients;
using HealthcareCRM.Domain.Enums;

namespace HealthcareCRM.Domain.Entities.Billing;

public class InsuranceProvider : BaseEntity
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string PayerId { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? ContactPhone { get; set; }

    [MaxLength(256)]
    public string? ContactEmail { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(250)]
    public string? ClaimsAddress { get; set; }

    public bool IsActive { get; set; } = true;
}

public class PatientInsurancePolicy : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid InsuranceProviderId { get; set; }
    public virtual InsuranceProvider InsuranceProvider { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string PolicyNumber { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? GroupNumber { get; set; }

    [MaxLength(100)]
    public string SubscriberName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string RelationshipToSubscriber { get; set; } = "Self";

    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CopayAmount { get; set; } = 25.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DeductibleAmount { get; set; } = 500.00m;

    public bool IsActive { get; set; } = true;
}

public class Invoice : BaseEntity
{
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid? AppointmentId { get; set; }
    public virtual Appointment? Appointment { get; set; }

    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime IssueDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueDateUtc { get; set; } = DateTime.UtcNow.AddDays(30);

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; } = 0.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal InsuranceCoveredAmount { get; set; } = 0.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PatientAmountDue { get; set; } = 0.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; } = 0.00m;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<InsuranceClaim> Claims { get; set; } = new List<InsuranceClaim>();
}

public class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public virtual Invoice Invoice { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string ServiceDescription { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ServiceCode { get; set; } // CPT Code

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; } = 0.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; } = 0.00m;
}

public class Payment : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public virtual Invoice Invoice { get; set; } = null!;

    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime PaymentDateUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "CreditCard"; // CreditCard, DebitCard, Cash, Insurance, Check, Online

    [MaxLength(100)]
    public string? TransactionReference { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Completed"; // Completed, Pending, Failed, Refunded

    public Guid? ProcessedByUserId { get; set; }
    public virtual User? ProcessedByUser { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class InsuranceClaim : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public virtual Invoice Invoice { get; set; } = null!;

    public Guid InsuranceProviderId { get; set; }
    public virtual InsuranceProvider InsuranceProvider { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string ClaimNumber { get; set; } = string.Empty;

    public DateTime FiledDateUtc { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ClaimAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ApprovedAmount { get; set; }

    public ClaimStatus Status { get; set; } = ClaimStatus.Submitted;

    [MaxLength(1000)]
    public string? DenialReason { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
