using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthcareCRM.Application.Common;
using HealthcareCRM.Application.DTOs;
using HealthcareCRM.Application.Interfaces;
using HealthcareCRM.Domain.Enums;

namespace HealthcareCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPatientService _patientService;
    private readonly ICurrentUserService _currentUserService;

    public AppointmentsController(
        IAppointmentService appointmentService,
        IPatientService patientService,
        ICurrentUserService currentUserService)
    {
        _appointmentService = appointmentService;
        _patientService = patientService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAppointments([FromQuery] Guid? patientId, [FromQuery] Guid? doctorId, [FromQuery] DateTime? date, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (myProfile.Success)
            {
                patientId = myProfile.Data!.Id;
            }
        }

        var result = await _appointmentService.GetAppointmentsAsync(patientId, doctorId, date, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppointmentById(Guid id)
    {
        var result = await _appointmentService.GetAppointmentByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (myProfile.Success && result.Data!.PatientId != myProfile.Data.Id)
            {
                return Forbid();
            }
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto request)
    {
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (myProfile.Success)
            {
                request.PatientId = myProfile.Data!.Id;
            }
        }

        var result = await _appointmentService.CreateAppointmentAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetAppointmentById), new { id = result.Data!.Id }, result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] AppointmentStatus status, [FromQuery] string? cancellationReason)
    {
        var result = await _appointmentService.UpdateStatusAsync(id, status, cancellationReason);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id}/reschedule")]
    public async Task<IActionResult> Reschedule(Guid id, [FromQuery] DateTime newTimeUtc)
    {
        var result = await _appointmentService.RescheduleAppointmentAsync(id, newTimeUtc);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("doctors")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctors([FromQuery] Guid? departmentId, [FromQuery] Guid? specialtyId)
    {
        var result = await _appointmentService.GetAvailableDoctorsAsync(departmentId, specialtyId);
        return Ok(result);
    }
}
