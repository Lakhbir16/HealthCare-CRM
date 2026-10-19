using Microsoft.EntityFrameworkCore;
using HealthcareCRM.Application.Interfaces;
using HealthcareCRM.Domain.Entities.Auth;
using HealthcareCRM.Domain.Entities.Patients;
using HealthcareCRM.Domain.Entities.Staff;
using HealthcareCRM.Domain.Entities.Appointments;
using HealthcareCRM.Domain.Entities.Medications;
using HealthcareCRM.Domain.Entities.Laboratory;
using HealthcareCRM.Domain.Entities.Billing;
using HealthcareCRM.Domain.Entities.System;

namespace HealthcareCRM.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OTPVerification> OTPVerifications => Set<OTPVerification>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<MedicalHistory> MedicalHistories => Set<MedicalHistory>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<VitalSign> VitalSigns => Set<VitalSign>();
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<MedicalDocument> MedicalDocuments => Set<MedicalDocument>();
    public DbSet<DocumentCategory> DocumentCategories => Set<DocumentCategory>();

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<ClinicLocation> ClinicLocations => Set<ClinicLocation>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Nurse> Nurses => Set<Nurse>();
    public DbSet<NursingTask> NursingTasks => Set<NursingTask>();

    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<PatientCheckIn> PatientCheckIns => Set<PatientCheckIn>();

    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<MedicationAdministration> MedicationAdministrations => Set<MedicationAdministration>();

    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<LabOrderItem> LabOrderItems => Set<LabOrderItem>();
    public DbSet<LabSample> LabSamples => Set<LabSample>();
    public DbSet<LabResult> LabResults => Set<LabResult>();

    public DbSet<InsuranceProvider> InsuranceProviders => Set<InsuranceProvider>();
    public DbSet<PatientInsurancePolicy> PatientInsurancePolicies => Set<PatientInsurancePolicy>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InsuranceClaim> InsuranceClaims => Set<InsuranceClaim>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Many-to-Many: UserRole
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(ur => ur.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // User indexes
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);

            entity.HasOne(u => u.Profile)
                  .WithOne(p => p.User)
                  .HasForeignKey<UserProfile>(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Patient indexes
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasIndex(p => p.MedicalRecordNumber).IsUnique();

            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.PrimaryCareDoctor)
                  .WithMany()
                  .HasForeignKey(p => p.PrimaryCareDoctorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Doctor indexes
        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasIndex(d => d.LicenseNumber).IsUnique();
            entity.HasIndex(d => d.NpiNumber).IsUnique();

            entity.HasOne(d => d.User)
                  .WithMany()
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Department)
                  .WithMany(dept => dept.Doctors)
                  .HasForeignKey(d => d.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Specialty)
                  .WithMany(spec => spec.Doctors)
                  .HasForeignKey(d => d.SpecialtyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Nurse indexes
        modelBuilder.Entity<Nurse>(entity =>
        {
            entity.HasIndex(n => n.LicenseNumber).IsUnique();

            entity.HasOne(n => n.User)
                  .WithMany()
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.Department)
                  .WithMany(dept => dept.Nurses)
                  .HasForeignKey(n => n.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Appointment configuration
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasIndex(a => a.ScheduledDateTimeUtc);
            entity.HasIndex(a => new { a.DoctorId, a.ScheduledDateTimeUtc });

            entity.HasOne(a => a.Patient)
                  .WithMany(p => p.Appointments)
                  .HasForeignKey(a => a.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Doctor)
                  .WithMany()
                  .HasForeignKey(a => a.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.ClinicLocation)
                  .WithMany()
                  .HasForeignKey(a => a.ClinicLocationId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.Department)
                  .WithMany()
                  .HasForeignKey(a => a.DepartmentId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.CheckInRecord)
                  .WithOne(c => c.Appointment)
                  .HasForeignKey<PatientCheckIn>(c => c.AppointmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Clinical records configuration
        modelBuilder.Entity<Diagnosis>(entity =>
        {
            entity.HasOne(d => d.Patient)
                  .WithMany(p => p.Diagnoses)
                  .HasForeignKey(d => d.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Doctor)
                  .WithMany()
                  .HasForeignKey(d => d.DoctorId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ClinicalNote>(entity =>
        {
            entity.HasOne(cn => cn.Patient)
                  .WithMany(p => p.ClinicalNotes)
                  .HasForeignKey(cn => cn.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cn => cn.Doctor)
                  .WithMany()
                  .HasForeignKey(cn => cn.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VitalSign>(entity =>
        {
            entity.HasOne(vs => vs.Patient)
                  .WithMany(p => p.VitalSigns)
                  .HasForeignKey(vs => vs.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Prescription configuration
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasIndex(p => p.PrescriptionNumber).IsUnique();

            entity.HasOne(p => p.Patient)
                  .WithMany()
                  .HasForeignKey(p => p.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Doctor)
                  .WithMany()
                  .HasForeignKey(p => p.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(p => p.Items)
                  .WithOne(pi => pi.Prescription)
                  .HasForeignKey(pi => pi.PrescriptionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Lab Order configuration
        modelBuilder.Entity<LabOrder>(entity =>
        {
            entity.HasIndex(lo => lo.OrderNumber).IsUnique();

            entity.HasOne(lo => lo.Patient)
                  .WithMany()
                  .HasForeignKey(lo => lo.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(lo => lo.Doctor)
                  .WithMany()
                  .HasForeignKey(lo => lo.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(lo => lo.Items)
                  .WithOne(li => li.LabOrder)
                  .HasForeignKey(li => li.LabOrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LabOrderItem>(entity =>
        {
            entity.HasMany(li => li.Results)
                  .WithOne(lr => lr.LabOrderItem)
                  .HasForeignKey(lr => lr.LabOrderItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(li => li.Samples)
                  .WithOne(ls => ls.LabOrderItem)
                  .HasForeignKey(ls => ls.LabOrderItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Invoices and Billing
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(i => i.InvoiceNumber).IsUnique();

            entity.HasOne(i => i.Patient)
                  .WithMany()
                  .HasForeignKey(i => i.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(i => i.Items)
                  .WithOne(ii => ii.Invoice)
                  .HasForeignKey(ii => ii.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(i => i.Payments)
                  .WithOne(p => p.Invoice)
                  .HasForeignKey(p => p.InvoiceId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(i => i.Claims)
                  .WithOne(c => c.Invoice)
                  .HasForeignKey(c => c.InvoiceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // OTP Verifications
        modelBuilder.Entity<OTPVerification>(entity =>
        {
            entity.HasIndex(o => new { o.Email, o.Purpose, o.ExpiresAtUtc });
        });

        // Refresh Tokens
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(r => r.Token).IsUnique();
        });
    }
}
