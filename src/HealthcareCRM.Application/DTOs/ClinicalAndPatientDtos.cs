using HealthcareCRM.Domain.Enums;

namespace HealthcareCRM.Application.DTOs;

public class PatientDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? BloodGroup { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public Guid? PrimaryCareDoctorId { get; set; }
    public string? PrimaryCareDoctorName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreatePatientDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? BloodGroup { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public Guid? PrimaryCareDoctorId { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }
}

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? ClinicLocationId { get; set; }
    public string? ClinicLocationName { get; set; }
    public DateTime ScheduledDateTimeUtc { get; set; }
    public int DurationMinutes { get; set; }
    public string AppointmentType { get; set; } = "InPerson";
    public AppointmentStatus Status { get; set; }
    public string ReasonForVisit { get; set; } = string.Empty;
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }
    public DateTime? CheckedInAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class CreateAppointmentDto
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ClinicLocationId { get; set; }
    public DateTime ScheduledDateTimeUtc { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string AppointmentType { get; set; } = "InPerson";
    public string ReasonForVisit { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class VitalSignDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string? RecordedByUserName { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? HeartRateBpm { get; set; }
    public decimal? TemperatureFahrenheit { get; set; }
    public int? RespiratoryRateBpm { get; set; }
    public int? OxygenSaturationPercent { get; set; }
    public decimal? HeightInches { get; set; }
    public decimal? WeightPounds { get; set; }
    public decimal? Bmi { get; set; }
    public string? Notes { get; set; }
}

public class ClinicalNoteDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public Guid? AppointmentId { get; set; }
    public string NoteType { get; set; } = "SOAP";
    public string? Subjective { get; set; }
    public string? Objective { get; set; }
    public string? Assessment { get; set; }
    public string? Plan { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class DiagnosisDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public Guid? AppointmentId { get; set; }
    public string Icd10Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime DiagnosedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public class AllergyDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string Allergen { get; set; } = string.Empty;
    public string Severity { get; set; } = "Moderate";
    public string? Reaction { get; set; }
    public DateTime? DiagnosedDate { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}

public class MedicalHistoryDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateTime? DiagnosedDate { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
}

public class TreatmentPlanDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Goals { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime? TargetEndDateUtc { get; set; }
    public string Status { get; set; } = "Active";
}

public class PrescriptionDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public Guid? AppointmentId { get; set; }
    public string PrescriptionNumber { get; set; } = string.Empty;
    public DateTime PrescribedAtUtc { get; set; }
    public DateTime? ExpirationDateUtc { get; set; }
    public PrescriptionStatus Status { get; set; }
    public string? PharmacyNotes { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
}

public class PrescriptionItemDto
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public int Quantity { get; set; }
    public int RefillsAllowed { get; set; }
    public int RefillsRemaining { get; set; }
    public string? Instructions { get; set; }
}

public class CreatePrescriptionDto
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string? PharmacyNotes { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
}

public class LabOrderDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderedAtUtc { get; set; }
    public PriorityLevel Priority { get; set; }
    public LabOrderStatus Status { get; set; }
    public string? ClinicalIndication { get; set; }
    public List<LabOrderItemDto> Items { get; set; } = new();
}

public class LabOrderItemDto
{
    public Guid Id { get; set; }
    public Guid LabOrderId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string TestCode { get; set; } = string.Empty;
    public string SpecimenType { get; set; } = "Blood";
    public string Status { get; set; } = "Pending";
    public List<LabResultDto> Results { get; set; } = new();
}

public class LabResultDto
{
    public Guid Id { get; set; }
    public Guid LabOrderItemId { get; set; }
    public DateTime PerformedAtUtc { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public bool IsAbnormal { get; set; }
    public string? Flag { get; set; }
    public string? InterpretationNotes { get; set; }
}

public class InvoiceDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid? AppointmentId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDateUtc { get; set; }
    public DateTime DueDateUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal InsuranceCoveredAmount { get; set; }
    public decimal PatientAmountDue { get; set; }
    public decimal PaidAmount { get; set; }
    public InvoiceStatus Status { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
}

public class InvoiceItemDto
{
    public Guid Id { get; set; }
    public string ServiceDescription { get; set; } = string.Empty;
    public string? ServiceCode { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid PatientId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDateUtc { get; set; }
    public string PaymentMethod { get; set; } = "CreditCard";
    public string? TransactionReference { get; set; }
    public string Status { get; set; } = "Completed";
}

public class RecordPaymentDto
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "CreditCard";
    public string? TransactionReference { get; set; }
    public string? Notes { get; set; }
}

public class DoctorDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string NpiNumber { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public Guid SpecialtyId { get; set; }
    public string SpecialtyName { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public bool IsAcceptingPatients { get; set; }
}

public class DashboardStatsDto
{
    public string Role { get; set; } = string.Empty;
    public int TotalPatients { get; set; }
    public int TodayAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalStaff { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PendingInvoicesAmount { get; set; }
    public int PendingLabOrders { get; set; }
    public int ActivePrescriptions { get; set; }
    public List<AppointmentDto> RecentAppointments { get; set; } = new();
    public List<VitalSignDto> RecentVitals { get; set; } = new();
    public List<PrescriptionDto> RecentPrescriptions { get; set; } = new();
    public List<LabOrderDto> RecentLabOrders { get; set; } = new();
    public List<InvoiceDto> RecentInvoices { get; set; } = new();
}
