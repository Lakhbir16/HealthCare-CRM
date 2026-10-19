namespace HealthcareCRM.Domain.Enums;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Nurse = "Nurse";
    public const string Receptionist = "Receptionist";
    public const string Patient = "Patient";
    public const string BillingStaff = "BillingStaff";
    public const string LabTechnician = "LabTechnician";
    public const string Pharmacist = "Pharmacist";
    public const string HealthcareManager = "HealthcareManager";
    public const string InsuranceCoordinator = "InsuranceCoordinator";
    public const string MedicalRecordsStaff = "MedicalRecordsStaff";

    public static readonly string[] All = new[]
    {
        SuperAdmin, Admin, Doctor, Nurse, Receptionist, Patient,
        BillingStaff, LabTechnician, Pharmacist, HealthcareManager,
        InsuranceCoordinator, MedicalRecordsStaff
    };
}

public enum AppointmentStatus
{
    Scheduled = 1,
    Confirmed = 2,
    CheckedIn = 3,
    InConsultation = 4,
    Completed = 5,
    Cancelled = 6,
    Rescheduled = 7,
    NoShow = 8
}

public enum PriorityLevel
{
    Routine = 1,
    Urgent = 2,
    Stat = 3
}

public enum LabOrderStatus
{
    Ordered = 1,
    SampleCollected = 2,
    InProcess = 3,
    Completed = 4,
    Cancelled = 5
}

public enum PrescriptionStatus
{
    Active = 1,
    Dispensed = 2,
    Completed = 3,
    Discontinued = 4,
    Expired = 5
}

public enum InvoiceStatus
{
    Draft = 1,
    Pending = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5,
    Overdue = 6
}

public enum ClaimStatus
{
    Submitted = 1,
    UnderReview = 2,
    Approved = 3,
    PartiallyApproved = 4,
    Denied = 5,
    Appealed = 6
}
