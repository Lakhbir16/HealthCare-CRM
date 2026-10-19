using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HealthcareCRM.Application.Interfaces;
using HealthcareCRM.Domain.Entities.Appointments;
using HealthcareCRM.Domain.Entities.Auth;
using HealthcareCRM.Domain.Entities.Billing;
using HealthcareCRM.Domain.Entities.Laboratory;
using HealthcareCRM.Domain.Entities.Medications;
using HealthcareCRM.Domain.Entities.Patients;
using HealthcareCRM.Domain.Entities.Staff;
using HealthcareCRM.Domain.Entities.System;
using HealthcareCRM.Domain.Enums;
using HealthcareCRM.Infrastructure.Persistence;

namespace HealthcareCRM.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher hasher, ILogger logger)
    {
        try
        {
            if (context.Database.IsRelational())
            {
                await context.Database.MigrateAsync();
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
            }

            // 1. Seed Roles
            if (!await context.Roles.AnyAsync())
            {
                logger.LogInformation("Seeding all 12 Healthcare CRM roles...");
                var roles = AppRoles.All.Select(r => new Role
                {
                    Id = Guid.NewGuid(),
                    Name = r,
                    NormalizedName = r.ToUpperInvariant(),
                    Description = $"Role for {r}",
                    CreatedAtUtc = DateTime.UtcNow
                }).ToList();

                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            // 2. Seed Departments & Specialties & Locations
            if (!await context.Departments.AnyAsync())
            {
                logger.LogInformation("Seeding Departments, Specialties, and Clinic Locations...");
                var cardioDept = new Department { Id = Guid.NewGuid(), Name = "Cardiology", Code = "CARD", Location = "Building A, 3rd Floor" };
                var neuroDept = new Department { Id = Guid.NewGuid(), Name = "Neurology", Code = "NEUR", Location = "Building B, 2nd Floor" };
                var pedsDept = new Department { Id = Guid.NewGuid(), Name = "Pediatrics", Code = "PEDS", Location = "Building C, 1st Floor" };
                var intMedDept = new Department { Id = Guid.NewGuid(), Name = "Internal Medicine", Code = "INTM", Location = "Building A, 1st Floor" };
                await context.Departments.AddRangeAsync(cardioDept, neuroDept, pedsDept, intMedDept);

                var cardioSpec = new Specialty { Id = Guid.NewGuid(), Name = "Cardiovascular Disease", Code = "CARD-SPEC" };
                var neuroSpec = new Specialty { Id = Guid.NewGuid(), Name = "Neurological Medicine", Code = "NEUR-SPEC" };
                var genSpec = new Specialty { Id = Guid.NewGuid(), Name = "General Practice", Code = "GP-SPEC" };
                await context.Specialties.AddRangeAsync(cardioSpec, neuroSpec, genSpec);

                var loc1 = new ClinicLocation
                {
                    Id = Guid.NewGuid(),
                    Name = "Metropolitan Healthcare Pavilion",
                    Address = "100 Medical Center Blvd",
                    City = "New York",
                    State = "NY",
                    ZipCode = "10001",
                    Phone = "+1 (212) 555-0199",
                    Email = "info@metrohealth.org",
                    TimeZone = "America/New_York"
                };
                var loc2 = new ClinicLocation
                {
                    Id = Guid.NewGuid(),
                    Name = "Downtown Care Center",
                    Address = "450 Broadway Ave",
                    City = "New York",
                    State = "NY",
                    ZipCode = "10012",
                    Phone = "+1 (212) 555-0244",
                    Email = "downtown@metrohealth.org",
                    TimeZone = "America/New_York"
                };
                await context.ClinicLocations.AddRangeAsync(loc1, loc2);

                await context.SaveChangesAsync();
            }

            // 3. Seed Users and Profiles
            if (!await context.Users.AnyAsync())
            {
                logger.LogInformation("Seeding Users, Multi-role assignments, and Staff profiles...");
                var rolesMap = await context.Roles.ToDictionaryAsync(r => r.Name, r => r.Id);
                var cardioDept = await context.Departments.FirstAsync(d => d.Code == "CARD");
                var neuroDept = await context.Departments.FirstAsync(d => d.Code == "NEUR");
                var cardioSpec = await context.Specialties.FirstAsync(s => s.Code == "CARD-SPEC");
                var neuroSpec = await context.Specialties.FirstAsync(s => s.Code == "NEUR-SPEC");
                var defaultLoc = await context.ClinicLocations.FirstAsync();

                // SuperAdmin
                var superAdmin = CreateUser("superadmin@healthcare.com", hasher.HashPassword("Admin@12345"), "Alexander", "Vance", "+1 (555) 100-0001");
                AddRole(superAdmin, rolesMap[AppRoles.SuperAdmin]);
                AddRole(superAdmin, rolesMap[AppRoles.Admin]);

                // Admin
                var admin = CreateUser("admin@healthcare.com", hasher.HashPassword("Admin@12345"), "Elena", "Rostova", "+1 (555) 100-0002");
                AddRole(admin, rolesMap[AppRoles.Admin]);

                // Doctor 1: Dr. Robert Smith (Has both Doctor AND HealthcareManager roles!)
                var doc1User = CreateUser("doctor.smith@healthcare.com", hasher.HashPassword("Doctor@12345"), "Robert", "Smith", "+1 (555) 200-0001");
                AddRole(doc1User, rolesMap[AppRoles.Doctor]);
                AddRole(doc1User, rolesMap[AppRoles.HealthcareManager]);

                // Doctor 2: Dr. Emily Chen
                var doc2User = CreateUser("doctor.chen@healthcare.com", hasher.HashPassword("Doctor@12345"), "Emily", "Chen", "+1 (555) 200-0002");
                AddRole(doc2User, rolesMap[AppRoles.Doctor]);

                // Nurse 1: Sarah Jenkins
                var nurse1User = CreateUser("nurse.sarah@healthcare.com", hasher.HashPassword("Nurse@12345"), "Sarah", "Jenkins", "+1 (555) 300-0001");
                AddRole(nurse1User, rolesMap[AppRoles.Nurse]);

                // Nurse 2: John Miller
                var nurse2User = CreateUser("nurse.john@healthcare.com", hasher.HashPassword("Nurse@12345"), "John", "Miller", "+1 (555) 300-0002");
                AddRole(nurse2User, rolesMap[AppRoles.Nurse]);

                // Receptionist: Maria Garcia
                var recepUser = CreateUser("receptionist@healthcare.com", hasher.HashPassword("Recep@12345"), "Maria", "Garcia", "+1 (555) 400-0001");
                AddRole(recepUser, rolesMap[AppRoles.Receptionist]);

                // Billing Staff: David Wilson
                var billingUser = CreateUser("billing@healthcare.com", hasher.HashPassword("Billing@12345"), "David", "Wilson", "+1 (555) 500-0001");
                AddRole(billingUser, rolesMap[AppRoles.BillingStaff]);

                // Lab Technician: Alex Taylor
                var labUser = CreateUser("labtech@healthcare.com", hasher.HashPassword("Lab@12345"), "Alex", "Taylor", "+1 (555) 600-0001");
                AddRole(labUser, rolesMap[AppRoles.LabTechnician]);

                // Pharmacist: James Lee
                var pharmUser = CreateUser("pharmacist@healthcare.com", hasher.HashPassword("Pharmacy@12345"), "James", "Lee", "+1 (555) 700-0001");
                AddRole(pharmUser, rolesMap[AppRoles.Pharmacist]);

                // Insurance Coordinator: Laura Martinez
                var insUser = CreateUser("insurance@healthcare.com", hasher.HashPassword("Insur@12345"), "Laura", "Martinez", "+1 (555) 800-0001");
                AddRole(insUser, rolesMap[AppRoles.InsuranceCoordinator]);

                // Medical Records: Kevin Hall
                var medRecUser = CreateUser("records@healthcare.com", hasher.HashPassword("Record@12345"), "Kevin", "Hall", "+1 (555) 900-0001");
                AddRole(medRecUser, rolesMap[AppRoles.MedicalRecordsStaff]);

                // Patient 1: John Doe
                var pat1User = CreateUser("patient.john@healthcare.com", hasher.HashPassword("Patient@12345"), "John", "Doe", "+1 (555) 001-1001");
                AddRole(pat1User, rolesMap[AppRoles.Patient]);

                // Patient 2: Emma Watson
                var pat2User = CreateUser("patient.emma@healthcare.com", hasher.HashPassword("Patient@12345"), "Emma", "Watson", "+1 (555) 001-1002");
                AddRole(pat2User, rolesMap[AppRoles.Patient]);

                // Patient 3: Michael Brown
                var pat3User = CreateUser("patient.michael@healthcare.com", hasher.HashPassword("Patient@12345"), "Michael", "Brown", "+1 (555) 001-1003");
                AddRole(pat3User, rolesMap[AppRoles.Patient]);

                await context.Users.AddRangeAsync(
                    superAdmin, admin, doc1User, doc2User, nurse1User, nurse2User,
                    recepUser, billingUser, labUser, pharmUser, insUser, medRecUser,
                    pat1User, pat2User, pat3User);

                await context.SaveChangesAsync();

                // Seed Doctor and Nurse staff records
                var doc1 = new Doctor
                {
                    Id = Guid.NewGuid(),
                    UserId = doc1User.Id,
                    LicenseNumber = "MD-NY-98124",
                    NpiNumber = "1234567890",
                    DepartmentId = cardioDept.Id,
                    SpecialtyId = cardioSpec.Id,
                    OfficeNumber = "Suite 302",
                    ConsultationFee = 200.00m,
                    Bio = "Senior Cardiologist with 15 years experience in interventional cardiology and preventative cardiovascular health."
                };

                var doc2 = new Doctor
                {
                    Id = Guid.NewGuid(),
                    UserId = doc2User.Id,
                    LicenseNumber = "MD-NY-77231",
                    NpiNumber = "9876543210",
                    DepartmentId = neuroDept.Id,
                    SpecialtyId = neuroSpec.Id,
                    OfficeNumber = "Suite 210",
                    ConsultationFee = 220.00m,
                    Bio = "Board certified Neurologist specializing in neurodegenerative conditions and headache management."
                };

                var nurse1 = new Nurse
                {
                    Id = Guid.NewGuid(),
                    UserId = nurse1User.Id,
                    LicenseNumber = "RN-NY-44812",
                    DepartmentId = cardioDept.Id,
                    Shift = "Day"
                };

                var nurse2 = new Nurse
                {
                    Id = Guid.NewGuid(),
                    UserId = nurse2User.Id,
                    LicenseNumber = "RN-NY-55923",
                    DepartmentId = neuroDept.Id,
                    Shift = "Day"
                };

                await context.Doctors.AddRangeAsync(doc1, doc2);
                await context.Nurses.AddRangeAsync(nurse1, nurse2);

                // Seed Patients
                var patient1 = new Patient
                {
                    Id = Guid.NewGuid(),
                    UserId = pat1User.Id,
                    MedicalRecordNumber = "MRN-2026-10001",
                    BloodGroup = "O+",
                    MaritalStatus = "Married",
                    PrimaryCareDoctorId = doc1.Id,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var patient2 = new Patient
                {
                    Id = Guid.NewGuid(),
                    UserId = pat2User.Id,
                    MedicalRecordNumber = "MRN-2026-10002",
                    BloodGroup = "A+",
                    MaritalStatus = "Single",
                    PrimaryCareDoctorId = doc2.Id,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var patient3 = new Patient
                {
                    Id = Guid.NewGuid(),
                    UserId = pat3User.Id,
                    MedicalRecordNumber = "MRN-2026-10003",
                    BloodGroup = "B-",
                    MaritalStatus = "Single",
                    PrimaryCareDoctorId = doc1.Id,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await context.Patients.AddRangeAsync(patient1, patient2, patient3);
                await context.SaveChangesAsync();

                // Clinical Seed for Patient 1
                await context.Allergies.AddRangeAsync(
                    new Allergy { Id = Guid.NewGuid(), PatientId = patient1.Id, Allergen = "Penicillin", Severity = "Severe", Reaction = "Anaphylaxis and hives", DiagnosedDate = DateTime.UtcNow.AddYears(-5) },
                    new Allergy { Id = Guid.NewGuid(), PatientId = patient1.Id, Allergen = "Peanuts", Severity = "Moderate", Reaction = "Skin rash and itching", DiagnosedDate = DateTime.UtcNow.AddYears(-8) }
                );

                await context.MedicalHistories.AddRangeAsync(
                    new MedicalHistory { Id = Guid.NewGuid(), PatientId = patient1.Id, Condition = "Essential Hypertension", Status = "Active", Notes = "Controlled on daily ACE inhibitor", DiagnosedDate = DateTime.UtcNow.AddYears(-3) },
                    new MedicalHistory { Id = Guid.NewGuid(), PatientId = patient1.Id, Condition = "Hyperlipidemia", Status = "Active", Notes = "Diet and statin therapy", DiagnosedDate = DateTime.UtcNow.AddYears(-2) }
                );

                await context.Diagnoses.AddAsync(new Diagnosis
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient1.Id,
                    DoctorId = doc1.Id,
                    Icd10Code = "I10",
                    Description = "Essential (primary) hypertension",
                    Status = "Active",
                    DiagnosedAtUtc = DateTime.UtcNow.AddMonths(-6),
                    Notes = "Stage 1 primary hypertension diagnosed during routine wellness exam."
                });

                await context.VitalSigns.AddAsync(new VitalSign
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient1.Id,
                    RecordedByUserId = nurse1User.Id,
                    RecordedAtUtc = DateTime.UtcNow.AddDays(-2),
                    BloodPressureSystolic = 124,
                    BloodPressureDiastolic = 82,
                    HeartRateBpm = 72,
                    TemperatureFahrenheit = 98.6m,
                    RespiratoryRateBpm = 16,
                    OxygenSaturationPercent = 99,
                    HeightInches = 70.0m,
                    WeightPounds = 178.5m,
                    Bmi = 25.61m,
                    Notes = "Vitals stable, patient feeling well."
                });

                // Appointments
                var appt1 = new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient1.Id,
                    DoctorId = doc1.Id,
                    ClinicLocationId = defaultLoc.Id,
                    DepartmentId = cardioDept.Id,
                    ScheduledDateTimeUtc = DateTime.UtcNow.AddHours(2),
                    DurationMinutes = 30,
                    AppointmentType = "InPerson",
                    Status = AppointmentStatus.Scheduled,
                    ReasonForVisit = "Cardiovascular quarterly follow-up and blood pressure monitoring",
                    Notes = "Check recent ECG and metabolic panel.",
                    CreatedAtUtc = DateTime.UtcNow
                };

                var appt2 = new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient2.Id,
                    DoctorId = doc2.Id,
                    ClinicLocationId = defaultLoc.Id,
                    DepartmentId = neuroDept.Id,
                    ScheduledDateTimeUtc = DateTime.UtcNow.AddDays(1).AddHours(4),
                    DurationMinutes = 45,
                    AppointmentType = "InPerson",
                    Status = AppointmentStatus.Confirmed,
                    ReasonForVisit = "Chronic migraine consultation and preventative treatment evaluation",
                    CreatedAtUtc = DateTime.UtcNow
                };

                await context.Appointments.AddRangeAsync(appt1, appt2);

                // Prescription
                var rx = new Prescription
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient1.Id,
                    DoctorId = doc1.Id,
                    PrescriptionNumber = "RX-2026-449120",
                    PrescribedAtUtc = DateTime.UtcNow.AddDays(-10),
                    ExpirationDateUtc = DateTime.UtcNow.AddMonths(6),
                    Status = PrescriptionStatus.Active,
                    PharmacyNotes = "Dispense 90-day supply with 3 refills."
                };

                rx.Items.Add(new PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = rx.Id,
                    MedicationName = "Lisinopril 10mg Oral Tablet",
                    Dosage = "10mg",
                    Frequency = "Once daily in the morning",
                    DurationDays = 90,
                    Quantity = 90,
                    RefillsAllowed = 3,
                    RefillsRemaining = 3,
                    Instructions = "Take 1 tablet by mouth daily. Monitor blood pressure."
                });

                await context.Prescriptions.AddAsync(rx);

                // Lab Order
                var labOrder = new LabOrder
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient1.Id,
                    DoctorId = doc1.Id,
                    OrderNumber = "LAB-2026-881230",
                    OrderedAtUtc = DateTime.UtcNow.AddDays(-3),
                    Priority = PriorityLevel.Routine,
                    Status = LabOrderStatus.Completed,
                    ClinicalIndication = "Routine monitoring for hypertension and statin therapy."
                };

                var labItem = new LabOrderItem
                {
                    Id = Guid.NewGuid(),
                    LabOrderId = labOrder.Id,
                    TestName = "Comprehensive Metabolic Panel (CMP)",
                    TestCode = "80053",
                    SpecimenType = "Blood",
                    Status = "Completed"
                };

                labItem.Results.Add(new LabResult
                {
                    Id = Guid.NewGuid(),
                    LabOrderItemId = labItem.Id,
                    LabTechnicianId = labUser.Id,
                    PerformedAtUtc = DateTime.UtcNow.AddDays(-2),
                    ParameterName = "Serum Potassium",
                    Value = "4.2",
                    Unit = "mmol/L",
                    ReferenceRange = "3.5 - 5.0",
                    IsAbnormal = false,
                    Flag = "Normal"
                });

                labItem.Results.Add(new LabResult
                {
                    Id = Guid.NewGuid(),
                    LabOrderItemId = labItem.Id,
                    LabTechnicianId = labUser.Id,
                    PerformedAtUtc = DateTime.UtcNow.AddDays(-2),
                    ParameterName = "eGFR (Kidney Function)",
                    Value = "98",
                    Unit = "mL/min/1.73m2",
                    ReferenceRange = "> 60",
                    IsAbnormal = false,
                    Flag = "Normal"
                });

                labOrder.Items.Add(labItem);
                await context.LabOrders.AddAsync(labOrder);

                // Invoice
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient1.Id,
                    AppointmentId = appt1.Id,
                    InvoiceNumber = "INV-2026-99201",
                    IssueDateUtc = DateTime.UtcNow.AddDays(-5),
                    DueDateUtc = DateTime.UtcNow.AddDays(25),
                    TotalAmount = 250.00m,
                    InsuranceCoveredAmount = 210.00m,
                    PatientAmountDue = 40.00m,
                    PaidAmount = 40.00m,
                    Status = InvoiceStatus.Paid,
                    Notes = "Specialist Consultation and in-office vitals check."
                };

                invoice.Items.Add(new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    ServiceDescription = "Office Consultation - Established Patient (CPT 99214)",
                    ServiceCode = "99214",
                    Quantity = 1,
                    UnitPrice = 200.00m,
                    TotalPrice = 200.00m
                });

                invoice.Items.Add(new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    ServiceDescription = "ECG/EKG Rhythm Strip (CPT 93000)",
                    ServiceCode = "93000",
                    Quantity = 1,
                    UnitPrice = 50.00m,
                    TotalPrice = 50.00m
                });

                invoice.Payments.Add(new Payment
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    PatientId = patient1.Id,
                    Amount = 40.00m,
                    PaymentDateUtc = DateTime.UtcNow.AddDays(-4),
                    PaymentMethod = "CreditCard",
                    TransactionReference = "TXN-COPAY-88219",
                    Status = "Completed",
                    Notes = "Patient Copay paid at check-in."
                });

                await context.Invoices.AddAsync(invoice);

                // Insurance provider
                var insProvider = new InsuranceProvider
                {
                    Id = Guid.NewGuid(),
                    Name = "BlueCross BlueShield Healthcare",
                    PayerId = "BCBS-NY-001",
                    ContactPhone = "+1 (800) 555-2277",
                    ContactEmail = "claims@bcbs.com",
                    Address = "100 Blue Shield Way, Albany, NY",
                    IsActive = true
                };

                await context.InsuranceProviders.AddAsync(insProvider);

                var policy = new PatientInsurancePolicy
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient1.Id,
                    InsuranceProviderId = insProvider.Id,
                    PolicyNumber = "BCBS-991204882",
                    GroupNumber = "GRP-8812",
                    SubscriberName = "John Doe",
                    RelationshipToSubscriber = "Self",
                    EffectiveDate = DateTime.UtcNow.AddYears(-1),
                    CopayAmount = 40.00m,
                    DeductibleAmount = 500.00m,
                    IsActive = true
                };

                await context.PatientInsurancePolicies.AddAsync(policy);

                await context.SaveChangesAsync();
                logger.LogInformation("Database seeded successfully with test patients, doctors, nurses, appointments, vitals, prescriptions, lab orders, and invoices.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during database seeding.");
            throw;
        }
    }

    private static User CreateUser(string email, string passwordHash, string firstName, string lastName, string phone)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phone,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            Profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                DateOfBirth = new DateTime(1985, 5, 20),
                Gender = "Other",
                Address = "123 Medical Way",
                City = "New York",
                State = "NY",
                ZipCode = "10001",
                Country = "USA",
                TimeZone = "America/New_York"
            }
        };
    }

    private static void AddRole(User user, Guid roleId)
    {
        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = roleId,
            AssignedAtUtc = DateTime.UtcNow
        });
    }
}
