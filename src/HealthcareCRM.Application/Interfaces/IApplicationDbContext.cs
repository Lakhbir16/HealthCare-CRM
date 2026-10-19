using Microsoft.EntityFrameworkCore;
using HealthcareCRM.Domain.Entities.Auth;
using HealthcareCRM.Domain.Entities.Patients;
using HealthcareCRM.Domain.Entities.Staff;
using HealthcareCRM.Domain.Entities.Appointments;
using HealthcareCRM.Domain.Entities.Medications;
using HealthcareCRM.Domain.Entities.Laboratory;
using HealthcareCRM.Domain.Entities.Billing;
using HealthcareCRM.Domain.Entities.System;

namespace HealthcareCRM.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<OTPVerification> OTPVerifications { get; }
    DbSet<PasswordResetRequest> PasswordResetRequests { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DbSet<Patient> Patients { get; }
    DbSet<EmergencyContact> EmergencyContacts { get; }
    DbSet<MedicalHistory> MedicalHistories { get; }
    DbSet<Allergy> Allergies { get; }
    DbSet<Diagnosis> Diagnoses { get; }
    DbSet<VitalSign> VitalSigns { get; }
    DbSet<ClinicalNote> ClinicalNotes { get; }
    DbSet<TreatmentPlan> TreatmentPlans { get; }
    DbSet<MedicalDocument> MedicalDocuments { get; }
    DbSet<DocumentCategory> DocumentCategories { get; }

    DbSet<Department> Departments { get; }
    DbSet<Specialty> Specialties { get; }
    DbSet<ClinicLocation> ClinicLocations { get; }
    DbSet<Doctor> Doctors { get; }
    DbSet<Nurse> Nurses { get; }
    DbSet<NursingTask> NursingTasks { get; }

    DbSet<Appointment> Appointments { get; }
    DbSet<PatientCheckIn> PatientCheckIns { get; }

    DbSet<Medication> Medications { get; }
    DbSet<Prescription> Prescriptions { get; }
    DbSet<PrescriptionItem> PrescriptionItems { get; }
    DbSet<MedicationAdministration> MedicationAdministrations { get; }

    DbSet<LabOrder> LabOrders { get; }
    DbSet<LabOrderItem> LabOrderItems { get; }
    DbSet<LabSample> LabSamples { get; }
    DbSet<LabResult> LabResults { get; }

    DbSet<InsuranceProvider> InsuranceProviders { get; }
    DbSet<PatientInsurancePolicy> PatientInsurancePolicies { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<InsuranceClaim> InsuranceClaims { get; }

    DbSet<Notification> Notifications { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<SystemSetting> SystemSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
