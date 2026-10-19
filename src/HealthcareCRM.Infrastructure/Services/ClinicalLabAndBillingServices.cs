using Microsoft.EntityFrameworkCore;
using HealthcareCRM.Application.Common;
using HealthcareCRM.Application.DTOs;
using HealthcareCRM.Application.Interfaces;
using HealthcareCRM.Domain.Entities.Billing;
using HealthcareCRM.Domain.Entities.Laboratory;
using HealthcareCRM.Domain.Entities.Medications;
using HealthcareCRM.Domain.Entities.Patients;
using HealthcareCRM.Domain.Enums;

namespace HealthcareCRM.Infrastructure.Services;

public class ClinicalService : IClinicalService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public ClinicalService(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<VitalSignDto>> RecordVitalSignsAsync(Guid patientId, VitalSignDto dto, Guid recordedByUserId)
    {
        var vitals = new VitalSign
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            RecordedByUserId = recordedByUserId,
            RecordedAtUtc = DateTime.UtcNow,
            BloodPressureSystolic = dto.BloodPressureSystolic,
            BloodPressureDiastolic = dto.BloodPressureDiastolic,
            HeartRateBpm = dto.HeartRateBpm,
            TemperatureFahrenheit = dto.TemperatureFahrenheit,
            RespiratoryRateBpm = dto.RespiratoryRateBpm,
            OxygenSaturationPercent = dto.OxygenSaturationPercent,
            HeightInches = dto.HeightInches,
            WeightPounds = dto.WeightPounds,
            Notes = dto.Notes
        };

        if (dto.HeightInches.HasValue && dto.HeightInches > 0 && dto.WeightPounds.HasValue && dto.WeightPounds > 0)
        {
            // BMI formula: (weight (lbs) / [height (in)]^2) * 703
            var bmi = (dto.WeightPounds.Value / (dto.HeightInches.Value * dto.HeightInches.Value)) * 703;
            vitals.Bmi = Math.Round(bmi, 2);
        }

        _context.VitalSigns.Add(vitals);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(recordedByUserId, "VITALS_RECORDED", "VitalSign", vitals.Id.ToString(), $"Vitals recorded for patient {patientId}", null, null);

        dto.Id = vitals.Id;
        dto.RecordedAtUtc = vitals.RecordedAtUtc;
        dto.Bmi = vitals.Bmi;
        return ApiResponse<VitalSignDto>.Ok(dto, "Vital signs recorded successfully.");
    }

    public async Task<ApiResponse<ClinicalNoteDto>> AddClinicalNoteAsync(Guid patientId, ClinicalNoteDto dto, Guid doctorId)
    {
        var note = new ClinicalNote
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentId = dto.AppointmentId,
            NoteType = dto.NoteType,
            Subjective = dto.Subjective,
            Objective = dto.Objective,
            Assessment = dto.Assessment,
            Plan = dto.Plan,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ClinicalNotes.Add(note);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(null, "CLINICAL_NOTE_ADDED", "ClinicalNote", note.Id.ToString(), $"Clinical note added for patient {patientId}", null, null);

        dto.Id = note.Id;
        dto.CreatedAtUtc = note.CreatedAtUtc;
        return ApiResponse<ClinicalNoteDto>.Ok(dto, "Clinical note recorded successfully.");
    }

    public async Task<ApiResponse<DiagnosisDto>> AddDiagnosisAsync(Guid patientId, DiagnosisDto dto, Guid? doctorId)
    {
        var diag = new Diagnosis
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentId = dto.AppointmentId,
            Icd10Code = dto.Icd10Code,
            Description = dto.Description,
            Status = dto.Status,
            DiagnosedAtUtc = DateTime.UtcNow,
            Notes = dto.Notes
        };

        _context.Diagnoses.Add(diag);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(null, "DIAGNOSIS_ADDED", "Diagnosis", diag.Id.ToString(), $"Diagnosis {dto.Icd10Code} added for patient {patientId}", null, null);

        dto.Id = diag.Id;
        dto.DiagnosedAtUtc = diag.DiagnosedAtUtc;
        return ApiResponse<DiagnosisDto>.Ok(dto, "Diagnosis recorded successfully.");
    }

    public async Task<ApiResponse<PrescriptionDto>> CreatePrescriptionAsync(CreatePrescriptionDto dto, Guid doctorId)
    {
        var pres = new Prescription
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            DoctorId = doctorId,
            AppointmentId = dto.AppointmentId,
            PrescriptionNumber = $"RX-{DateTime.UtcNow.Year}-{new Random().Next(100000, 999999)}",
            PrescribedAtUtc = DateTime.UtcNow,
            ExpirationDateUtc = DateTime.UtcNow.AddMonths(6),
            Status = PrescriptionStatus.Active,
            PharmacyNotes = dto.PharmacyNotes
        };

        foreach (var item in dto.Items)
        {
            pres.Items.Add(new PrescriptionItem
            {
                Id = Guid.NewGuid(),
                PrescriptionId = pres.Id,
                MedicationName = item.MedicationName,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                DurationDays = item.DurationDays,
                Quantity = item.Quantity,
                RefillsAllowed = item.RefillsAllowed,
                RefillsRemaining = item.RefillsAllowed,
                Instructions = item.Instructions
            });
        }

        _context.Prescriptions.Add(pres);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(null, "PRESCRIPTION_CREATED", "Prescription", pres.Id.ToString(), $"Prescription {pres.PrescriptionNumber} created", null, null);

        var result = await GetPrescriptionsAsync(dto.PatientId, null);
        var created = result.Data?.FirstOrDefault(p => p.Id == pres.Id);
        return ApiResponse<PrescriptionDto>.Ok(created!, "Prescription created successfully.");
    }

    public async Task<ApiResponse<List<PrescriptionDto>>> GetPrescriptionsAsync(Guid? patientId, Guid? doctorId)
    {
        var query = _context.Prescriptions
            .Include(p => p.Patient).ThenInclude(pt => pt.User)
            .Include(p => p.Doctor).ThenInclude(d => d.User)
            .Include(p => p.Items)
            .AsQueryable();

        if (patientId.HasValue)
            query = query.Where(p => p.PatientId == patientId.Value);

        if (doctorId.HasValue)
            query = query.Where(p => p.DoctorId == doctorId.Value);

        var list = await query
            .OrderByDescending(p => p.PrescribedAtUtc)
            .Select(p => new PrescriptionDto
            {
                Id = p.Id,
                PatientId = p.PatientId,
                PatientName = $"{p.Patient.User.FirstName} {p.Patient.User.LastName}",
                DoctorId = p.DoctorId,
                DoctorName = $"Dr. {p.Doctor.User.FirstName} {p.Doctor.User.LastName}",
                AppointmentId = p.AppointmentId,
                PrescriptionNumber = p.PrescriptionNumber,
                PrescribedAtUtc = p.PrescribedAtUtc,
                ExpirationDateUtc = p.ExpirationDateUtc,
                Status = p.Status,
                PharmacyNotes = p.PharmacyNotes,
                Items = p.Items.Select(i => new PrescriptionItemDto
                {
                    Id = i.Id,
                    PrescriptionId = i.PrescriptionId,
                    MedicationName = i.MedicationName,
                    Dosage = i.Dosage,
                    Frequency = i.Frequency,
                    DurationDays = i.DurationDays,
                    Quantity = i.Quantity,
                    RefillsAllowed = i.RefillsAllowed,
                    RefillsRemaining = i.RefillsRemaining,
                    Instructions = i.Instructions
                }).ToList()
            })
            .ToListAsync();

        return ApiResponse<List<PrescriptionDto>>.Ok(list);
    }

    public async Task<ApiResponse<PrescriptionDto>> UpdatePrescriptionStatusAsync(Guid id, PrescriptionStatus status)
    {
        var pres = await _context.Prescriptions.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
        if (pres == null)
            return ApiResponse<PrescriptionDto>.Fail("Prescription not found.");

        pres.Status = status;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(null, "PRESCRIPTION_STATUS_UPDATED", "Prescription", id.ToString(), $"Status updated to {status}", null, null);

        var list = await GetPrescriptionsAsync(pres.PatientId, null);
        return ApiResponse<PrescriptionDto>.Ok(list.Data!.First(p => p.Id == id));
    }
}

public class LabService : ILabService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public LabService(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<LabOrderDto>> CreateLabOrderAsync(CreateLabOrderDto request, Guid doctorId)
    {
        var order = new LabOrder
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            DoctorId = doctorId,
            AppointmentId = request.AppointmentId,
            OrderNumber = $"LAB-{DateTime.UtcNow.Year}-{new Random().Next(100000, 999999)}",
            OrderedAtUtc = DateTime.UtcNow,
            Priority = request.Priority,
            Status = LabOrderStatus.Ordered,
            ClinicalIndication = request.ClinicalIndication
        };

        foreach (var item in request.Items)
        {
            order.Items.Add(new LabOrderItem
            {
                Id = Guid.NewGuid(),
                LabOrderId = order.Id,
                TestName = item.TestName,
                TestCode = item.TestCode,
                SpecimenType = item.SpecimenType,
                Status = "Pending"
            });
        }

        _context.LabOrders.Add(order);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(null, "LAB_ORDER_CREATED", "LabOrder", order.Id.ToString(), $"Lab order {order.OrderNumber} created", null, null);

        return await GetLabOrderByIdAsync(order.Id);
    }

    public async Task<ApiResponse<List<LabOrderDto>>> GetLabOrdersAsync(Guid? patientId, Guid? doctorId, LabOrderStatus? status)
    {
        var query = _context.LabOrders
            .Include(lo => lo.Patient).ThenInclude(p => p.User)
            .Include(lo => lo.Doctor).ThenInclude(d => d.User)
            .Include(lo => lo.Items).ThenInclude(i => i.Results)
            .AsQueryable();

        if (patientId.HasValue)
            query = query.Where(lo => lo.PatientId == patientId.Value);

        if (doctorId.HasValue)
            query = query.Where(lo => lo.DoctorId == doctorId.Value);

        if (status.HasValue)
            query = query.Where(lo => lo.Status == status.Value);

        var list = await query
            .OrderByDescending(lo => lo.OrderedAtUtc)
            .Select(lo => new LabOrderDto
            {
                Id = lo.Id,
                PatientId = lo.PatientId,
                PatientName = $"{lo.Patient.User.FirstName} {lo.Patient.User.LastName}",
                DoctorId = lo.DoctorId,
                DoctorName = $"Dr. {lo.Doctor.User.FirstName} {lo.Doctor.User.LastName}",
                OrderNumber = lo.OrderNumber,
                OrderedAtUtc = lo.OrderedAtUtc,
                Priority = lo.Priority,
                Status = lo.Status,
                ClinicalIndication = lo.ClinicalIndication,
                Items = lo.Items.Select(i => new LabOrderItemDto
                {
                    Id = i.Id,
                    LabOrderId = i.LabOrderId,
                    TestName = i.TestName,
                    TestCode = i.TestCode,
                    SpecimenType = i.SpecimenType,
                    Status = i.Status,
                    Results = i.Results.Select(r => new LabResultDto
                    {
                        Id = r.Id,
                        LabOrderItemId = r.LabOrderItemId,
                        PerformedAtUtc = r.PerformedAtUtc,
                        ParameterName = r.ParameterName,
                        Value = r.Value,
                        Unit = r.Unit,
                        ReferenceRange = r.ReferenceRange,
                        IsAbnormal = r.IsAbnormal,
                        Flag = r.Flag,
                        InterpretationNotes = r.InterpretationNotes
                    }).ToList()
                }).ToList()
            })
            .ToListAsync();

        return ApiResponse<List<LabOrderDto>>.Ok(list);
    }

    public async Task<ApiResponse<LabOrderDto>> GetLabOrderByIdAsync(Guid id)
    {
        var list = await GetLabOrdersAsync(null, null, null);
        var order = list.Data?.FirstOrDefault(o => o.Id == id);
        if (order == null)
            return ApiResponse<LabOrderDto>.Fail("Lab order not found.");
        return ApiResponse<LabOrderDto>.Ok(order);
    }

    public async Task<ApiResponse<LabResultDto>> RecordLabResultAsync(Guid labOrderItemId, CreateLabResultDto request, Guid technicianId)
    {
        var item = await _context.LabOrderItems.Include(i => i.LabOrder).FirstOrDefaultAsync(i => i.Id == labOrderItemId);
        if (item == null)
            return ApiResponse<LabResultDto>.Fail("Lab order item not found.");

        var result = new LabResult
        {
            Id = Guid.NewGuid(),
            LabOrderItemId = labOrderItemId,
            LabTechnicianId = technicianId,
            PerformedAtUtc = DateTime.UtcNow,
            ParameterName = request.ParameterName,
            Value = request.Value,
            Unit = request.Unit,
            ReferenceRange = request.ReferenceRange,
            IsAbnormal = request.IsAbnormal,
            Flag = request.Flag,
            InterpretationNotes = request.InterpretationNotes
        };

        item.Status = "Completed";
        item.LabOrder.Status = LabOrderStatus.Completed;

        _context.LabResults.Add(result);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(technicianId, "LAB_RESULT_RECORDED", "LabResult", result.Id.ToString(), $"Result recorded for {request.ParameterName}", null, null);

        return ApiResponse<LabResultDto>.Ok(new LabResultDto
        {
            Id = result.Id,
            LabOrderItemId = result.LabOrderItemId,
            PerformedAtUtc = result.PerformedAtUtc,
            ParameterName = result.ParameterName,
            Value = result.Value,
            Unit = result.Unit,
            ReferenceRange = result.ReferenceRange,
            IsAbnormal = result.IsAbnormal,
            Flag = result.Flag,
            InterpretationNotes = result.InterpretationNotes
        }, "Lab result recorded successfully.");
    }

    public async Task<ApiResponse<LabOrderDto>> UpdateOrderStatusAsync(Guid id, LabOrderStatus status)
    {
        var order = await _context.LabOrders.FindAsync(id);
        if (order == null)
            return ApiResponse<LabOrderDto>.Fail("Lab order not found.");

        order.Status = status;
        await _context.SaveChangesAsync();
        return await GetLabOrderByIdAsync(id);
    }
}

public class BillingService : IBillingService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public BillingService(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<PagedResult<InvoiceDto>>> GetInvoicesAsync(Guid? patientId, InvoiceStatus? status, int page = 1, int pageSize = 10)
    {
        var query = _context.Invoices
            .Include(i => i.Patient).ThenInclude(p => p.User)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .AsQueryable();

        if (patientId.HasValue)
            query = query.Where(i => i.PatientId == patientId.Value);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.IssueDateUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceDto
            {
                Id = i.Id,
                PatientId = i.PatientId,
                PatientName = $"{i.Patient.User.FirstName} {i.Patient.User.LastName}",
                AppointmentId = i.AppointmentId,
                InvoiceNumber = i.InvoiceNumber,
                IssueDateUtc = i.IssueDateUtc,
                DueDateUtc = i.DueDateUtc,
                TotalAmount = i.TotalAmount,
                InsuranceCoveredAmount = i.InsuranceCoveredAmount,
                PatientAmountDue = i.PatientAmountDue,
                PaidAmount = i.PaidAmount,
                Status = i.Status,
                Notes = i.Notes,
                Items = i.Items.Select(it => new InvoiceItemDto
                {
                    Id = it.Id,
                    ServiceDescription = it.ServiceDescription,
                    ServiceCode = it.ServiceCode,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    TotalPrice = it.TotalPrice
                }).ToList(),
                Payments = i.Payments.Select(p => new PaymentDto
                {
                    Id = p.Id,
                    InvoiceId = p.InvoiceId,
                    PatientId = p.PatientId,
                    Amount = p.Amount,
                    PaymentDateUtc = p.PaymentDateUtc,
                    PaymentMethod = p.PaymentMethod,
                    TransactionReference = p.TransactionReference,
                    Status = p.Status
                }).ToList()
            })
            .ToListAsync();

        return ApiResponse<PagedResult<InvoiceDto>>.Ok(new PagedResult<InvoiceDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<ApiResponse<InvoiceDto>> GetInvoiceByIdAsync(Guid id)
    {
        var inv = await _context.Invoices
            .Include(i => i.Patient).ThenInclude(p => p.User)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inv == null)
            return ApiResponse<InvoiceDto>.Fail("Invoice not found.");

        return ApiResponse<InvoiceDto>.Ok(new InvoiceDto
        {
            Id = inv.Id,
            PatientId = inv.PatientId,
            PatientName = $"{inv.Patient.User.FirstName} {inv.Patient.User.LastName}",
            AppointmentId = inv.AppointmentId,
            InvoiceNumber = inv.InvoiceNumber,
            IssueDateUtc = inv.IssueDateUtc,
            DueDateUtc = inv.DueDateUtc,
            TotalAmount = inv.TotalAmount,
            InsuranceCoveredAmount = inv.InsuranceCoveredAmount,
            PatientAmountDue = inv.PatientAmountDue,
            PaidAmount = inv.PaidAmount,
            Status = inv.Status,
            Notes = inv.Notes,
            Items = inv.Items.Select(it => new InvoiceItemDto
            {
                Id = it.Id,
                ServiceDescription = it.ServiceDescription,
                ServiceCode = it.ServiceCode,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice,
                TotalPrice = it.TotalPrice
            }).ToList(),
            Payments = inv.Payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId,
                PatientId = p.PatientId,
                Amount = p.Amount,
                PaymentDateUtc = p.PaymentDateUtc,
                PaymentMethod = p.PaymentMethod,
                TransactionReference = p.TransactionReference,
                Status = p.Status
            }).ToList()
        });
    }

    public async Task<ApiResponse<InvoiceDto>> CreateInvoiceAsync(CreateInvoiceDto request)
    {
        var inv = new Invoice
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId,
            InvoiceNumber = $"INV-{DateTime.UtcNow.Year}-{new Random().Next(100000, 999999)}",
            IssueDateUtc = DateTime.UtcNow,
            DueDateUtc = DateTime.UtcNow.AddDays(30),
            TotalAmount = request.TotalAmount,
            InsuranceCoveredAmount = request.InsuranceCoveredAmount,
            PatientAmountDue = request.PatientAmountDue,
            PaidAmount = 0m,
            Status = InvoiceStatus.Pending,
            Notes = request.Notes
        };

        foreach (var item in request.Items)
        {
            inv.Items.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = inv.Id,
                ServiceDescription = item.ServiceDescription,
                ServiceCode = item.ServiceCode,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.UnitPrice * item.Quantity
            });
        }

        _context.Invoices.Add(inv);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(null, "INVOICE_CREATED", "Invoice", inv.Id.ToString(), $"Created invoice {inv.InvoiceNumber} for {inv.TotalAmount}", null, null);

        return await GetInvoiceByIdAsync(inv.Id);
    }

    public async Task<ApiResponse<PaymentDto>> RecordPaymentAsync(RecordPaymentDto request, Guid processedByUserId)
    {
        var inv = await _context.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == request.InvoiceId);
        if (inv == null)
            return ApiResponse<PaymentDto>.Fail("Invoice not found.");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = inv.Id,
            PatientId = inv.PatientId,
            Amount = request.Amount,
            PaymentDateUtc = DateTime.UtcNow,
            PaymentMethod = request.PaymentMethod,
            TransactionReference = request.TransactionReference ?? $"TXN-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
            Status = "Completed",
            ProcessedByUserId = processedByUserId,
            Notes = request.Notes
        };

        inv.PaidAmount += request.Amount;
        if (inv.PaidAmount >= inv.PatientAmountDue)
        {
            inv.Status = InvoiceStatus.Paid;
        }
        else if (inv.PaidAmount > 0)
        {
            inv.Status = InvoiceStatus.PartiallyPaid;
        }

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(processedByUserId, "PAYMENT_RECORDED", "Payment", payment.Id.ToString(), $"Payment of {request.Amount:C} recorded on invoice {inv.InvoiceNumber}", null, null);

        return ApiResponse<PaymentDto>.Ok(new PaymentDto
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            PatientId = payment.PatientId,
            Amount = payment.Amount,
            PaymentDateUtc = payment.PaymentDateUtc,
            PaymentMethod = payment.PaymentMethod,
            TransactionReference = payment.TransactionReference,
            Status = payment.Status
        }, "Payment recorded successfully.");
    }

    public async Task<ApiResponse<List<PaymentDto>>> GetPaymentsAsync(Guid? invoiceId, Guid? patientId)
    {
        var query = _context.Payments.AsQueryable();
        if (invoiceId.HasValue) query = query.Where(p => p.InvoiceId == invoiceId.Value);
        if (patientId.HasValue) query = query.Where(p => p.PatientId == patientId.Value);

        var list = await query.OrderByDescending(p => p.PaymentDateUtc).Select(p => new PaymentDto
        {
            Id = p.Id,
            InvoiceId = p.InvoiceId,
            PatientId = p.PatientId,
            Amount = p.Amount,
            PaymentDateUtc = p.PaymentDateUtc,
            PaymentMethod = p.PaymentMethod,
            TransactionReference = p.TransactionReference,
            Status = p.Status
        }).ToListAsync();

        return ApiResponse<List<PaymentDto>>.Ok(list);
    }
}

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;

    public DashboardService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync(string role, Guid userId)
    {
        var today = DateTime.UtcNow.Date;
        var stats = new DashboardStatsDto { Role = role };

        stats.TotalPatients = await _context.Patients.CountAsync();
        stats.TotalDoctors = await _context.Doctors.CountAsync();
        stats.TotalStaff = await _context.Nurses.CountAsync() + await _context.UserRoles.CountAsync(ur => ur.Role.Name == AppRoles.Receptionist || ur.Role.Name == AppRoles.BillingStaff);
        stats.TodayAppointments = await _context.Appointments.CountAsync(a => a.ScheduledDateTimeUtc.Date == today);
        stats.PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Scheduled);
        stats.TotalRevenue = await _context.Payments.Where(p => p.Status == "Completed").SumAsync(p => p.Amount);
        stats.PendingInvoicesAmount = await _context.Invoices.Where(i => i.Status == InvoiceStatus.Pending).SumAsync(i => i.PatientAmountDue - i.PaidAmount);
        stats.PendingLabOrders = await _context.LabOrders.CountAsync(l => l.Status == LabOrderStatus.Ordered || l.Status == LabOrderStatus.InProcess);
        stats.ActivePrescriptions = await _context.Prescriptions.CountAsync(p => p.Status == PrescriptionStatus.Active);

        // Recent appointments
        stats.RecentAppointments = await _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Doctor).ThenInclude(d => d.User)
            .OrderByDescending(a => a.ScheduledDateTimeUtc)
            .Take(5)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = $"{a.Patient.User.FirstName} {a.Patient.User.LastName}",
                DoctorId = a.DoctorId,
                DoctorName = $"Dr. {a.Doctor.User.FirstName} {a.Doctor.User.LastName}",
                ScheduledDateTimeUtc = a.ScheduledDateTimeUtc,
                Status = a.Status,
                ReasonForVisit = a.ReasonForVisit
            })
            .ToListAsync();

        return ApiResponse<DashboardStatsDto>.Ok(stats);
    }
}
