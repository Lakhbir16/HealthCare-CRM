using HealthcareCRM.Application.Common;
using HealthcareCRM.Application.DTOs;
using HealthcareCRM.Domain.Entities.Auth;
using HealthcareCRM.Domain.Enums;

namespace HealthcareCRM.Application.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, string activeRole);
    string GenerateRefreshToken();
}

public interface IDevEmailService
{
    Task SendOtpEmailAsync(string recipientEmail, string otp, DateTime expiresAtUtc);
    Task<List<DevMailMessageDto>> GetSimulatedMessagesAsync(string? recipientFilter = null);
    Task<DevMailMessageDto?> GetMessageByIdAsync(Guid id);
}

public interface IOtpService
{
    Task<ForgotPasswordResponseDto> RequestOtpAsync(string email);
    Task<VerifyOtpResponseDto> VerifyOtpAsync(string email, string otp);
    Task<ForgotPasswordResponseDto> ResendOtpAsync(string email);
    Task<bool> ResetPasswordAsync(ResetPasswordRequestDto request);
}

public interface IAuthService
{
    Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<ApiResponse<LoginResponseDto>> SwitchRoleAsync(Guid userId, string requestedRole);
    Task<ApiResponse<bool>> LogoutAsync(Guid userId, string? refreshToken);
    Task<ApiResponse<UserProfileDto>> GetCurrentUserProfileAsync(Guid userId, string? activeRole);
    Task<ApiResponse<List<string>>> GetUserRolesAsync(Guid userId);
    Task<ApiResponse<List<UserManagementDto>>> GetUsersAsync();
    Task<ApiResponse<UserManagementDto>> CreateUserAsync(CreateUserRequestDto request);
    Task<ApiResponse<bool>> UpdateUserRolesAsync(Guid userId, List<string> roles);
    Task<ApiResponse<bool>> SetUserStatusAsync(Guid userId, bool isActive, bool isLockedOut);
}

public interface IPatientService
{
    Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(string? search, int page = 1, int pageSize = 10);
    Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id);
    Task<ApiResponse<PatientDto>> GetPatientByUserIdAsync(Guid userId);
    Task<ApiResponse<PatientDto>> CreatePatientAsync(CreatePatientDto request);
    Task<ApiResponse<PatientDto>> UpdatePatientAsync(Guid id, CreatePatientDto request);
    Task<ApiResponse<List<MedicalHistoryDto>>> GetMedicalHistoriesAsync(Guid patientId);
    Task<ApiResponse<List<AllergyDto>>> GetAllergiesAsync(Guid patientId);
    Task<ApiResponse<List<DiagnosisDto>>> GetDiagnosesAsync(Guid patientId);
    Task<ApiResponse<List<VitalSignDto>>> GetVitalSignsAsync(Guid patientId);
    Task<ApiResponse<List<ClinicalNoteDto>>> GetClinicalNotesAsync(Guid patientId);
    Task<ApiResponse<List<TreatmentPlanDto>>> GetTreatmentPlansAsync(Guid patientId);
}

public interface IAppointmentService
{
    Task<ApiResponse<PagedResult<AppointmentDto>>> GetAppointmentsAsync(Guid? patientId, Guid? doctorId, DateTime? date, int page = 1, int pageSize = 10);
    Task<ApiResponse<AppointmentDto>> GetAppointmentByIdAsync(Guid id);
    Task<ApiResponse<AppointmentDto>> CreateAppointmentAsync(CreateAppointmentDto request);
    Task<ApiResponse<AppointmentDto>> UpdateStatusAsync(Guid id, AppointmentStatus status, string? cancellationReason = null);
    Task<ApiResponse<AppointmentDto>> RescheduleAppointmentAsync(Guid id, DateTime newTimeUtc);
    Task<ApiResponse<bool>> CheckDoctorConflictAsync(Guid doctorId, DateTime scheduledTimeUtc, int durationMinutes, Guid? excludeAppointmentId = null);
    Task<ApiResponse<List<DoctorDto>>> GetAvailableDoctorsAsync(Guid? departmentId, Guid? specialtyId);
}

public interface IClinicalService
{
    Task<ApiResponse<VitalSignDto>> RecordVitalSignsAsync(Guid patientId, VitalSignDto dto, Guid recordedByUserId);
    Task<ApiResponse<ClinicalNoteDto>> AddClinicalNoteAsync(Guid patientId, ClinicalNoteDto dto, Guid doctorId);
    Task<ApiResponse<DiagnosisDto>> AddDiagnosisAsync(Guid patientId, DiagnosisDto dto, Guid? doctorId);
    Task<ApiResponse<PrescriptionDto>> CreatePrescriptionAsync(CreatePrescriptionDto dto, Guid doctorId);
    Task<ApiResponse<List<PrescriptionDto>>> GetPrescriptionsAsync(Guid? patientId, Guid? doctorId);
    Task<ApiResponse<PrescriptionDto>> UpdatePrescriptionStatusAsync(Guid id, PrescriptionStatus status);
}

public interface ILabService
{
    Task<ApiResponse<LabOrderDto>> CreateLabOrderAsync(CreateLabOrderDto request, Guid doctorId);
    Task<ApiResponse<List<LabOrderDto>>> GetLabOrdersAsync(Guid? patientId, Guid? doctorId, LabOrderStatus? status);
    Task<ApiResponse<LabOrderDto>> GetLabOrderByIdAsync(Guid id);
    Task<ApiResponse<LabResultDto>> RecordLabResultAsync(Guid labOrderItemId, CreateLabResultDto request, Guid technicianId);
    Task<ApiResponse<LabOrderDto>> UpdateOrderStatusAsync(Guid id, LabOrderStatus status);
}

public interface IBillingService
{
    Task<ApiResponse<PagedResult<InvoiceDto>>> GetInvoicesAsync(Guid? patientId, InvoiceStatus? status, int page = 1, int pageSize = 10);
    Task<ApiResponse<InvoiceDto>> GetInvoiceByIdAsync(Guid id);
    Task<ApiResponse<InvoiceDto>> CreateInvoiceAsync(CreateInvoiceDto request);
    Task<ApiResponse<PaymentDto>> RecordPaymentAsync(RecordPaymentDto request, Guid processedByUserId);
    Task<ApiResponse<List<PaymentDto>>> GetPaymentsAsync(Guid? invoiceId, Guid? patientId);
}

public interface IDashboardService
{
    Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync(string role, Guid userId);
}

public interface IAuditService
{
    Task LogAsync(Guid? userId, string action, string? entityName, string? entityId, string? details, string? ipAddress, string? userAgent);
    Task<ApiResponse<PagedResult<AuditLog>>> GetLogsAsync(int page = 1, int pageSize = 20);
}

public class CreateLabOrderDto
{
    public Guid PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public PriorityLevel Priority { get; set; } = PriorityLevel.Routine;
    public string? ClinicalIndication { get; set; }
    public List<CreateLabOrderItemDto> Items { get; set; } = new();
}

public class CreateLabOrderItemDto
{
    public string TestName { get; set; } = string.Empty;
    public string TestCode { get; set; } = string.Empty;
    public string SpecimenType { get; set; } = "Blood";
}

public class CreateLabResultDto
{
    public string ParameterName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public bool IsAbnormal { get; set; }
    public string? Flag { get; set; }
    public string? InterpretationNotes { get; set; }
}

public class CreateInvoiceDto
{
    public Guid PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal InsuranceCoveredAmount { get; set; }
    public decimal PatientAmountDue { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
}
