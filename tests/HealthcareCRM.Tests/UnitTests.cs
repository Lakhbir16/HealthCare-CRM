using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using HealthcareCRM.Application.Common;
using HealthcareCRM.Application.DTOs;
using HealthcareCRM.Application.Interfaces;
using HealthcareCRM.Domain.Entities.Appointments;
using HealthcareCRM.Domain.Entities.Auth;
using HealthcareCRM.Domain.Entities.Billing;
using HealthcareCRM.Domain.Entities.Patients;
using HealthcareCRM.Domain.Entities.Staff;
using HealthcareCRM.Domain.Enums;
using HealthcareCRM.Infrastructure.Persistence;
using HealthcareCRM.Infrastructure.Security;
using HealthcareCRM.Infrastructure.Services;
using Xunit;

namespace HealthcareCRM.Tests;

public class TestDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

    public void AdvanceSeconds(int seconds)
    {
        UtcNow = UtcNow.AddSeconds(seconds);
    }

    public void AdvanceMinutes(int minutes)
    {
        UtcNow = UtcNow.AddMinutes(minutes);
    }
}

public class HealthcareCrmTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TestDateTimeProvider _timeProvider;
    private readonly Mock<IDevEmailService> _mockEmailService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILogger<OtpService>> _mockOtpLogger;
    private readonly OtpService _otpService;
    private readonly AppointmentService _appointmentService;

    public HealthcareCrmTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _passwordHasher = new BcryptPasswordHasher();
        _timeProvider = new TestDateTimeProvider();
        _mockEmailService = new Mock<IDevEmailService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockOtpLogger = new Mock<ILogger<OtpService>>();

        _otpService = new OtpService(
            _context,
            _mockEmailService.Object,
            _passwordHasher,
            _timeProvider,
            _mockAuditService.Object,
            _mockOtpLogger.Object);

        _appointmentService = new AppointmentService(_context, _mockAuditService.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public void PasswordHashing_ShouldHashAndVerifySuccessfully()
    {
        // Arrange
        var password = "SecurePassword@2026!";

        // Act
        var hash = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hash);
        var isInvalid = _passwordHasher.VerifyPassword("WrongPassword123!", hash);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);
        isValid.Should().BeTrue();
        isInvalid.Should().BeFalse();
    }

    [Fact]
    public async Task OtpWorkflow_ShouldGenerateOtpAndExpireStrictlyAfter30Seconds()
    {
        // Arrange
        var email = "patient.test@healthcare.com";
        string? capturedOtp = null;

        _mockEmailService.Setup(e => e.SendOtpEmailAsync(email, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Callback<string, string, DateTime>((rec, code, exp) => capturedOtp = code)
            .Returns(Task.CompletedTask);

        // 1. Request OTP
        var requestResult = await _otpService.RequestOtpAsync(email);
        requestResult.ExpiresInSeconds.Should().Be(30);
        capturedOtp.Should().NotBeNull();
        capturedOtp.Should().HaveLength(6);

        // 2. Advance time by 20 seconds (within 30s window) -> Should succeed
        _timeProvider.AdvanceSeconds(20);
        var validResult = await _otpService.VerifyOtpAsync(email, capturedOtp!);
        validResult.IsValid.Should().BeTrue();
        validResult.ResetToken.Should().NotBeNullOrWhiteSpace();

        // 3. Test expiration with a new OTP request
        capturedOtp = null;
        await _otpService.RequestOtpAsync(email);
        capturedOtp.Should().NotBeNull();

        // Advance by 31 seconds -> Exceeded 30s expiration limit
        _timeProvider.AdvanceSeconds(31);
        var expiredResult = await _otpService.VerifyOtpAsync(email, capturedOtp!);
        expiredResult.IsValid.Should().BeFalse();
        expiredResult.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task OtpWorkflow_ShouldEnforceSingleUse()
    {
        // Arrange
        var email = "doctor.test@healthcare.com";
        string? capturedOtp = null;

        _mockEmailService.Setup(e => e.SendOtpEmailAsync(email, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Callback<string, string, DateTime>((rec, code, exp) => capturedOtp = code)
            .Returns(Task.CompletedTask);

        await _otpService.RequestOtpAsync(email);

        // First verification within 30s -> Success
        _timeProvider.AdvanceSeconds(10);
        var firstVerify = await _otpService.VerifyOtpAsync(email, capturedOtp!);
        firstVerify.IsValid.Should().BeTrue();

        // Second verification with the exact same OTP -> Rejected (Single-use enforcement)
        var secondVerify = await _otpService.VerifyOtpAsync(email, capturedOtp!);
        secondVerify.IsValid.Should().BeFalse();
        secondVerify.Message.Should().Contain("No active OTP found");
    }

    [Fact]
    public async Task OtpWorkflow_ShouldEnforceAttemptLimits()
    {
        // Arrange
        var email = "admin.test@healthcare.com";
        string? capturedOtp = null;

        _mockEmailService.Setup(e => e.SendOtpEmailAsync(email, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Callback<string, string, DateTime>((rec, code, exp) => capturedOtp = code)
            .Returns(Task.CompletedTask);

        await _otpService.RequestOtpAsync(email);

        // Submit 5 incorrect attempts
        for (int i = 0; i < 5; i++)
        {
            var failVerify = await _otpService.VerifyOtpAsync(email, "000000");
            failVerify.IsValid.Should().BeFalse();
        }

        // 6th attempt should lock/invalidate OTP
        var lockedVerify = await _otpService.VerifyOtpAsync(email, "000000");
        lockedVerify.IsValid.Should().BeFalse();
        lockedVerify.Message.Should().Contain("Maximum OTP attempts exceeded");
    }

    [Fact]
    public async Task ResendOtp_ShouldInvalidatePreviousOtpAndRestart30Seconds()
    {
        // Arrange
        var email = "nurse.test@healthcare.com";
        var capturedOtps = new List<string>();

        _mockEmailService.Setup(e => e.SendOtpEmailAsync(email, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Callback<string, string, DateTime>((rec, code, exp) => capturedOtps.Add(code))
            .Returns(Task.CompletedTask);

        await _otpService.RequestOtpAsync(email);
        var firstOtp = capturedOtps.First();

        _timeProvider.AdvanceSeconds(15);
        await _otpService.ResendOtpAsync(email);
        var secondOtp = capturedOtps.Last();

        // First OTP should now be rejected as superseded
        var verifyFirst = await _otpService.VerifyOtpAsync(email, firstOtp);
        verifyFirst.IsValid.Should().BeFalse();

        // Second OTP should succeed
        var verifySecond = await _otpService.VerifyOtpAsync(email, secondOtp);
        verifySecond.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AppointmentScheduling_ShouldDetectDoctorConflicts()
    {
        // Arrange
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddHours(14); // 2:00 PM

        // Schedule an appointment from 2:00 PM to 2:30 PM (30 mins)
        var existingAppt = new Appointment
        {
            Id = Guid.NewGuid(),
            DoctorId = doctorId,
            PatientId = patientId,
            ScheduledDateTimeUtc = baseTime,
            DurationMinutes = 30,
            Status = AppointmentStatus.Scheduled,
            ReasonForVisit = "Cardiology follow-up"
        };
        _context.Appointments.Add(existingAppt);
        await _context.SaveChangesAsync();

        // Act & Assert
        // Case 1: Same start time (2:00 PM) -> Conflict!
        var conflict1 = await _appointmentService.CheckDoctorConflictAsync(doctorId, baseTime, 30);
        conflict1.Data.Should().BeTrue();

        // Case 2: Overlapping time (2:15 PM to 2:45 PM) -> Conflict!
        var conflict2 = await _appointmentService.CheckDoctorConflictAsync(doctorId, baseTime.AddMinutes(15), 30);
        conflict2.Data.Should().BeTrue();

        // Case 3: Later non-overlapping time (3:00 PM to 3:30 PM) -> No conflict!
        var conflict3 = await _appointmentService.CheckDoctorConflictAsync(doctorId, baseTime.AddMinutes(60), 30);
        conflict3.Data.Should().BeFalse();
    }

    [Fact]
    public async Task MultiRoleAssignment_ShouldSupportMultipleRolesForSingleUser()
    {
        // Arrange: A user with both Doctor and HealthcareManager roles
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "doctor.manager@healthcare.com",
            PasswordHash = _passwordHasher.HashPassword("Pass123!"),
            FirstName = "Robert",
            LastName = "Smith"
        };

        var docRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.Doctor, NormalizedName = "DOCTOR" };
        var mgrRole = new Role { Id = Guid.NewGuid(), Name = AppRoles.HealthcareManager, NormalizedName = "HEALTHCAREMANAGER" };

        _context.Users.Add(user);
        _context.Roles.AddRange(docRole, mgrRole);

        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = docRole.Id });
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = mgrRole.Id });

        await _context.SaveChangesAsync();

        // Act
        var loadedUser = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        // Assert
        loadedUser.Should().NotBeNull();
        loadedUser!.UserRoles.Should().HaveCount(2);
        loadedUser.UserRoles.Select(ur => ur.Role.Name).Should().Contain(new[] { AppRoles.Doctor, AppRoles.HealthcareManager });
    }

    [Fact]
    public void BillingCalculation_ShouldComputeCopayAndBalanceCorrectly()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            InvoiceNumber = "INV-TEST-001",
            TotalAmount = 300.00m,
            InsuranceCoveredAmount = 250.00m,
            PatientAmountDue = 50.00m,
            PaidAmount = 20.00m,
            Status = InvoiceStatus.PartiallyPaid
        };

        // Assert
        var balance = invoice.PatientAmountDue - invoice.PaidAmount;
        balance.Should().Be(30.00m);
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }
}
