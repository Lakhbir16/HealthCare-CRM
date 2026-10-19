using Microsoft.EntityFrameworkCore;
using HealthcareCRM.Application.Common;
using HealthcareCRM.Application.DTOs;
using HealthcareCRM.Application.Interfaces;
using HealthcareCRM.Domain.Entities.Appointments;
using HealthcareCRM.Domain.Entities.Auth;
using HealthcareCRM.Domain.Entities.Patients;
using HealthcareCRM.Domain.Enums;

namespace HealthcareCRM.Infrastructure.Services;

public class PatientService : IPatientService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public PatientService(IApplicationDbContext context, IPasswordHasher passwordHasher, IAuditService auditService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    public async Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(string? search, int page = 1, int pageSize = 10)
    {
        var query = _context.Patients
            .Include(p => p.User)
            .Include(p => p.User.Profile)
            .Include(p => p.PrimaryCareDoctor)
                .ThenInclude(d => d!.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                p.User.FirstName.ToLower().Contains(s) ||
                p.User.LastName.ToLower().Contains(s) ||
                p.MedicalRecordNumber.ToLower().Contains(s) ||
                p.User.Email.ToLower().Contains(s) ||
                (p.User.PhoneNumber != null && p.User.PhoneNumber.Contains(s)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                UserId = p.UserId,
                MedicalRecordNumber = p.MedicalRecordNumber,
                FirstName = p.User.FirstName,
                LastName = p.User.LastName,
                Email = p.User.Email,
                PhoneNumber = p.User.PhoneNumber,
                DateOfBirth = p.User.Profile != null ? p.User.Profile.DateOfBirth : null,
                Gender = p.User.Profile != null ? p.User.Profile.Gender : null,
                BloodGroup = p.BloodGroup,
                MaritalStatus = p.MaritalStatus,
                Address = p.User.Profile != null ? p.User.Profile.Address : null,
                City = p.User.Profile != null ? p.User.Profile.City : null,
                State = p.User.Profile != null ? p.User.Profile.State : null,
                ZipCode = p.User.Profile != null ? p.User.Profile.ZipCode : null,
                PrimaryCareDoctorId = p.PrimaryCareDoctorId,
                PrimaryCareDoctorName = p.PrimaryCareDoctor != null ? $"Dr. {p.PrimaryCareDoctor.User.FirstName} {p.PrimaryCareDoctor.User.LastName}" : null,
                CreatedAtUtc = p.CreatedAtUtc
            })
            .ToListAsync();

        return ApiResponse<PagedResult<PatientDto>>.Ok(new PagedResult<PatientDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id)
    {
        var p = await _context.Patients
            .Include(p => p.User)
            .Include(p => p.User.Profile)
            .Include(p => p.PrimaryCareDoctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (p == null)
            return ApiResponse<PatientDto>.Fail("Patient not found.");

        return ApiResponse<PatientDto>.Ok(MapPatient(p));
    }

    public async Task<ApiResponse<PatientDto>> GetPatientByUserIdAsync(Guid userId)
    {
        var p = await _context.Patients
            .Include(p => p.User)
            .Include(p => p.User.Profile)
            .Include(p => p.PrimaryCareDoctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (p == null)
            return ApiResponse<PatientDto>.Fail("Patient profile not found.");

        return ApiResponse<PatientDto>.Ok(MapPatient(p));
    }

    public async Task<ApiResponse<PatientDto>> CreatePatientAsync(CreatePatientDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _context.Users.AnyAsync(u => u.Email.ToLower() == email);
        if (existing)
            return ApiResponse<PatientDto>.Fail("A user with this email address already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwordHasher.HashPassword("Patient@12345"),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            Profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Address = request.Address,
                City = request.City,
                State = request.State,
                ZipCode = request.ZipCode,
                Country = "USA",
                TimeZone = "America/New_York"
            }
        };

        var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.Patient);
        if (patientRole != null)
        {
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = patientRole.Id, AssignedAtUtc = DateTime.UtcNow });
        }

        _context.Users.Add(user);

        var mrn = $"MRN-{DateTime.UtcNow.Year}-{new Random().Next(10000, 99999)}";
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            MedicalRecordNumber = mrn,
            BloodGroup = request.BloodGroup,
            MaritalStatus = request.MaritalStatus,
            PrimaryCareDoctorId = request.PrimaryCareDoctorId,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(request.EmergencyContactName))
        {
            patient.EmergencyContacts.Add(new EmergencyContact
            {
                Id = Guid.NewGuid(),
                PatientId = patient.Id,
                Name = request.EmergencyContactName,
                PhoneNumber = request.EmergencyContactPhone ?? "N/A",
                Relationship = request.EmergencyContactRelationship ?? "Family",
                IsPrimary = true
            });
        }

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(null, "PATIENT_CREATED", "Patient", patient.Id.ToString(), $"Registered patient {patient.MedicalRecordNumber}", null, null);

        return await GetPatientByIdAsync(patient.Id);
    }

    public async Task<ApiResponse<PatientDto>> UpdatePatientAsync(Guid id, CreatePatientDto request)
    {
        var p = await _context.Patients
            .Include(p => p.User)
            .Include(p => p.User.Profile)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (p == null)
            return ApiResponse<PatientDto>.Fail("Patient not found.");

        p.User.FirstName = request.FirstName.Trim();
        p.User.LastName = request.LastName.Trim();
        p.User.PhoneNumber = request.PhoneNumber;
        p.BloodGroup = request.BloodGroup;
        p.MaritalStatus = request.MaritalStatus;
        p.PrimaryCareDoctorId = request.PrimaryCareDoctorId;

        if (p.User.Profile == null)
        {
            p.User.Profile = new UserProfile { Id = Guid.NewGuid(), UserId = p.UserId };
        }

        p.User.Profile.DateOfBirth = request.DateOfBirth;
        p.User.Profile.Gender = request.Gender;
        p.User.Profile.Address = request.Address;
        p.User.Profile.City = request.City;
        p.User.Profile.State = request.State;
        p.User.Profile.ZipCode = request.ZipCode;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(null, "PATIENT_UPDATED", "Patient", id.ToString(), "Updated patient demographic records", null, null);

        return await GetPatientByIdAsync(id);
    }

    public async Task<ApiResponse<List<MedicalHistoryDto>>> GetMedicalHistoriesAsync(Guid patientId)
    {
        var list = await _context.MedicalHistories
            .Where(m => m.PatientId == patientId)
            .OrderByDescending(m => m.RecordedAtUtc)
            .Select(m => new MedicalHistoryDto
            {
                Id = m.Id,
                PatientId = m.PatientId,
                Condition = m.Condition,
                DiagnosedDate = m.DiagnosedDate,
                Status = m.Status,
                Notes = m.Notes
            })
            .ToListAsync();

        return ApiResponse<List<MedicalHistoryDto>>.Ok(list);
    }

    public async Task<ApiResponse<List<AllergyDto>>> GetAllergiesAsync(Guid patientId)
    {
        var list = await _context.Allergies
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.RecordedAtUtc)
            .Select(a => new AllergyDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                Allergen = a.Allergen,
                Severity = a.Severity,
                Reaction = a.Reaction,
                DiagnosedDate = a.DiagnosedDate,
                RecordedAtUtc = a.RecordedAtUtc
            })
            .ToListAsync();

        return ApiResponse<List<AllergyDto>>.Ok(list);
    }

    public async Task<ApiResponse<List<DiagnosisDto>>> GetDiagnosesAsync(Guid patientId)
    {
        var list = await _context.Diagnoses
            .Include(d => d.Doctor).ThenInclude(doc => doc!.User)
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.DiagnosedAtUtc)
            .Select(d => new DiagnosisDto
            {
                Id = d.Id,
                PatientId = d.PatientId,
                DoctorId = d.DoctorId,
                DoctorName = d.Doctor != null ? $"Dr. {d.Doctor.User.FirstName} {d.Doctor.User.LastName}" : null,
                AppointmentId = d.AppointmentId,
                Icd10Code = d.Icd10Code,
                Description = d.Description,
                Status = d.Status,
                DiagnosedAtUtc = d.DiagnosedAtUtc,
                Notes = d.Notes
            })
            .ToListAsync();

        return ApiResponse<List<DiagnosisDto>>.Ok(list);
    }

    public async Task<ApiResponse<List<VitalSignDto>>> GetVitalSignsAsync(Guid patientId)
    {
        var list = await _context.VitalSigns
            .Include(v => v.RecordedByUser)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.RecordedAtUtc)
            .Select(v => new VitalSignDto
            {
                Id = v.Id,
                PatientId = v.PatientId,
                RecordedByUserName = v.RecordedByUser != null ? $"{v.RecordedByUser.FirstName} {v.RecordedByUser.LastName}" : null,
                RecordedAtUtc = v.RecordedAtUtc,
                BloodPressureSystolic = v.BloodPressureSystolic,
                BloodPressureDiastolic = v.BloodPressureDiastolic,
                HeartRateBpm = v.HeartRateBpm,
                TemperatureFahrenheit = v.TemperatureFahrenheit,
                RespiratoryRateBpm = v.RespiratoryRateBpm,
                OxygenSaturationPercent = v.OxygenSaturationPercent,
                HeightInches = v.HeightInches,
                WeightPounds = v.WeightPounds,
                Bmi = v.Bmi,
                Notes = v.Notes
            })
            .ToListAsync();

        return ApiResponse<List<VitalSignDto>>.Ok(list);
    }

    public async Task<ApiResponse<List<ClinicalNoteDto>>> GetClinicalNotesAsync(Guid patientId)
    {
        var list = await _context.ClinicalNotes
            .Include(c => c.Doctor).ThenInclude(d => d.User)
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ClinicalNoteDto
            {
                Id = c.Id,
                PatientId = c.PatientId,
                DoctorId = c.DoctorId,
                DoctorName = $"Dr. {c.Doctor.User.FirstName} {c.Doctor.User.LastName}",
                AppointmentId = c.AppointmentId,
                NoteType = c.NoteType,
                Subjective = c.Subjective,
                Objective = c.Objective,
                Assessment = c.Assessment,
                Plan = c.Plan,
                CreatedAtUtc = c.CreatedAtUtc
            })
            .ToListAsync();

        return ApiResponse<List<ClinicalNoteDto>>.Ok(list);
    }

    public async Task<ApiResponse<List<TreatmentPlanDto>>> GetTreatmentPlansAsync(Guid patientId)
    {
        var list = await _context.TreatmentPlans
            .Include(t => t.Doctor).ThenInclude(d => d.User)
            .Where(t => t.PatientId == patientId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new TreatmentPlanDto
            {
                Id = t.Id,
                PatientId = t.PatientId,
                DoctorId = t.DoctorId,
                DoctorName = $"Dr. {t.Doctor.User.FirstName} {t.Doctor.User.LastName}",
                Title = t.Title,
                Description = t.Description,
                Goals = t.Goals,
                StartDateUtc = t.StartDateUtc,
                TargetEndDateUtc = t.TargetEndDateUtc,
                Status = t.Status
            })
            .ToListAsync();

        return ApiResponse<List<TreatmentPlanDto>>.Ok(list);
    }

    private static PatientDto MapPatient(Patient p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        MedicalRecordNumber = p.MedicalRecordNumber,
        FirstName = p.User.FirstName,
        LastName = p.User.LastName,
        Email = p.User.Email,
        PhoneNumber = p.User.PhoneNumber,
        DateOfBirth = p.User.Profile?.DateOfBirth,
        Gender = p.User.Profile?.Gender,
        BloodGroup = p.BloodGroup,
        MaritalStatus = p.MaritalStatus,
        Address = p.User.Profile?.Address,
        City = p.User.Profile?.City,
        State = p.User.Profile?.State,
        ZipCode = p.User.Profile?.ZipCode,
        PrimaryCareDoctorId = p.PrimaryCareDoctorId,
        PrimaryCareDoctorName = p.PrimaryCareDoctor != null ? $"Dr. {p.PrimaryCareDoctor.User.FirstName} {p.PrimaryCareDoctor.User.LastName}" : null,
        CreatedAtUtc = p.CreatedAtUtc
    };
}

public class AppointmentService : IAppointmentService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public AppointmentService(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<PagedResult<AppointmentDto>>> GetAppointmentsAsync(Guid? patientId, Guid? doctorId, DateTime? date, int page = 1, int pageSize = 10)
    {
        var query = _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Doctor.Specialty)
            .Include(a => a.Department)
            .Include(a => a.ClinicLocation)
            .AsQueryable();

        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);

        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);

        if (date.HasValue)
        {
            var targetDate = date.Value.Date;
            query = query.Where(a => a.ScheduledDateTimeUtc.Date == targetDate);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.ScheduledDateTimeUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapAppointment(a))
            .ToListAsync();

        return ApiResponse<PagedResult<AppointmentDto>>.Ok(new PagedResult<AppointmentDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<ApiResponse<AppointmentDto>> GetAppointmentByIdAsync(Guid id)
    {
        var a = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .Include(a => a.Doctor.Specialty)
            .Include(a => a.Department)
            .Include(a => a.ClinicLocation)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (a == null)
            return ApiResponse<AppointmentDto>.Fail("Appointment not found.");

        return ApiResponse<AppointmentDto>.Ok(MapAppointment(a));
    }

    public async Task<ApiResponse<bool>> CheckDoctorConflictAsync(Guid doctorId, DateTime scheduledTimeUtc, int durationMinutes, Guid? excludeAppointmentId = null)
    {
        var newStart = scheduledTimeUtc;
        var newEnd = scheduledTimeUtc.AddMinutes(durationMinutes);

        var hasConflict = await _context.Appointments
            .Where(a => a.DoctorId == doctorId &&
                        a.Status != AppointmentStatus.Cancelled &&
                        (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value))
            .AnyAsync(a => (newStart < a.ScheduledDateTimeUtc.AddMinutes(a.DurationMinutes)) &&
                           (newEnd > a.ScheduledDateTimeUtc));

        return ApiResponse<bool>.Ok(hasConflict);
    }

    public async Task<ApiResponse<AppointmentDto>> CreateAppointmentAsync(CreateAppointmentDto request)
    {
        // Conflict detection: prevent overlapping appointments for the same doctor
        var conflictCheck = await CheckDoctorConflictAsync(request.DoctorId, request.ScheduledDateTimeUtc, request.DurationMinutes);
        if (conflictCheck.Data)
        {
            return ApiResponse<AppointmentDto>.Fail("Doctor is not available at the selected date and time. Please choose another slot.");
        }

        var appt = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            DepartmentId = request.DepartmentId,
            ClinicLocationId = request.ClinicLocationId,
            ScheduledDateTimeUtc = request.ScheduledDateTimeUtc,
            DurationMinutes = request.DurationMinutes,
            AppointmentType = request.AppointmentType,
            ReasonForVisit = request.ReasonForVisit,
            Notes = request.Notes,
            Status = AppointmentStatus.Scheduled,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Appointments.Add(appt);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(null, "APPOINTMENT_SCHEDULED", "Appointment", appt.Id.ToString(), $"Scheduled appointment for patient {request.PatientId} with doctor {request.DoctorId}", null, null);

        return await GetAppointmentByIdAsync(appt.Id);
    }

    public async Task<ApiResponse<AppointmentDto>> UpdateStatusAsync(Guid id, AppointmentStatus status, string? cancellationReason = null)
    {
        var appt = await _context.Appointments.FindAsync(id);
        if (appt == null)
            return ApiResponse<AppointmentDto>.Fail("Appointment not found.");

        appt.Status = status;
        if (status == AppointmentStatus.Cancelled)
        {
            appt.CancellationReason = cancellationReason ?? "Cancelled by user/staff";
        }
        else if (status == AppointmentStatus.CheckedIn)
        {
            appt.CheckedInAtUtc = DateTime.UtcNow;
            if (appt.CheckInRecord == null)
            {
                appt.CheckInRecord = new PatientCheckIn
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appt.Id,
                    PatientId = appt.PatientId,
                    CheckInTimeUtc = DateTime.UtcNow,
                    QueueNumber = $"Q-{new Random().Next(10, 99)}",
                    Status = "Waiting"
                };
            }
        }
        else if (status == AppointmentStatus.Completed)
        {
            appt.CompletedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(null, "APPOINTMENT_STATUS_UPDATED", "Appointment", id.ToString(), $"Status updated to {status}", null, null);

        return await GetAppointmentByIdAsync(id);
    }

    public async Task<ApiResponse<AppointmentDto>> RescheduleAppointmentAsync(Guid id, DateTime newTimeUtc)
    {
        var appt = await _context.Appointments.FindAsync(id);
        if (appt == null)
            return ApiResponse<AppointmentDto>.Fail("Appointment not found.");

        var conflict = await CheckDoctorConflictAsync(appt.DoctorId, newTimeUtc, appt.DurationMinutes, appt.Id);
        if (conflict.Data)
        {
            return ApiResponse<AppointmentDto>.Fail("Doctor is unavailable at the newly selected time.");
        }

        appt.ScheduledDateTimeUtc = newTimeUtc;
        appt.Status = AppointmentStatus.Rescheduled;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(null, "APPOINTMENT_RESCHEDULED", "Appointment", id.ToString(), $"Rescheduled to {newTimeUtc}", null, null);

        return await GetAppointmentByIdAsync(id);
    }

    public async Task<ApiResponse<List<DoctorDto>>> GetAvailableDoctorsAsync(Guid? departmentId, Guid? specialtyId)
    {
        var query = _context.Doctors
            .Include(d => d.User)
            .Include(d => d.Department)
            .Include(d => d.Specialty)
            .Where(d => d.IsAcceptingPatients && d.User.IsActive);

        if (departmentId.HasValue)
            query = query.Where(d => d.DepartmentId == departmentId.Value);

        if (specialtyId.HasValue)
            query = query.Where(d => d.SpecialtyId == specialtyId.Value);

        var list = await query.Select(d => new DoctorDto
        {
            Id = d.Id,
            UserId = d.UserId,
            FullName = $"Dr. {d.User.FirstName} {d.User.LastName}",
            Email = d.User.Email,
            LicenseNumber = d.LicenseNumber,
            NpiNumber = d.NpiNumber,
            DepartmentId = d.DepartmentId,
            DepartmentName = d.Department.Name,
            SpecialtyId = d.SpecialtyId,
            SpecialtyName = d.Specialty.Name,
            ConsultationFee = d.ConsultationFee,
            IsAcceptingPatients = d.IsAcceptingPatients
        }).ToListAsync();

        return ApiResponse<List<DoctorDto>>.Ok(list);
    }

    private static AppointmentDto MapAppointment(Appointment a) => new()
    {
        Id = a.Id,
        PatientId = a.PatientId,
        PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
        PatientMrn = a.Patient.MedicalRecordNumber,
        DoctorId = a.DoctorId,
        DoctorName = $"Dr. {a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
        Specialty = a.Doctor.Specialty?.Name ?? "General Practice",
        DepartmentId = a.DepartmentId,
        DepartmentName = a.Department?.Name,
        ClinicLocationId = a.ClinicLocationId,
        ClinicLocationName = a.ClinicLocation?.Name,
        ScheduledDateTimeUtc = a.ScheduledDateTimeUtc,
        DurationMinutes = a.DurationMinutes,
        AppointmentType = a.AppointmentType,
        Status = a.Status,
        ReasonForVisit = a.ReasonForVisit,
        CancellationReason = a.CancellationReason,
        Notes = a.Notes,
        CheckedInAtUtc = a.CheckedInAtUtc,
        CompletedAtUtc = a.CompletedAtUtc
    };
}
