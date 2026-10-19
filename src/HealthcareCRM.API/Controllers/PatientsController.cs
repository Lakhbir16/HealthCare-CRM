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
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly ICurrentUserService _currentUserService;

    public PatientsController(IPatientService patientService, ICurrentUserService currentUserService)
    {
        _patientService = patientService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Doctor},{AppRoles.Nurse},{AppRoles.Receptionist}")]
    public async Task<IActionResult> GetPatients([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _patientService.GetPatientsAsync(search, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatientById(Guid id)
    {
        // Patient ownership check
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (!myProfile.Success || myProfile.Data!.Id != id)
            {
                return Forbid();
            }
        }

        var result = await _patientService.GetPatientByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize(Roles = AppRoles.Patient)]
    public async Task<IActionResult> GetMyPatientProfile()
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var result = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId.Value);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.Admin},{AppRoles.Receptionist},{AppRoles.HealthcareManager}")]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto request)
    {
        var result = await _patientService.CreatePatientAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetPatientById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.Admin},{AppRoles.Receptionist},{AppRoles.Patient}")]
    public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] CreatePatientDto request)
    {
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (!myProfile.Success || myProfile.Data!.Id != id)
                return Forbid();
        }

        var result = await _patientService.UpdatePatientAsync(id, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (!myProfile.Success || myProfile.Data!.Id != id)
                return Forbid();
        }

        var result = await _patientService.GetMedicalHistoriesAsync(id);
        return Ok(result);
    }

    [HttpGet("{id}/allergies")]
    public async Task<IActionResult> GetAllergies(Guid id)
    {
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (!myProfile.Success || myProfile.Data!.Id != id)
                return Forbid();
        }

        var result = await _patientService.GetAllergiesAsync(id);
        return Ok(result);
    }

    [HttpGet("{id}/diagnoses")]
    public async Task<IActionResult> GetDiagnoses(Guid id)
    {
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (!myProfile.Success || myProfile.Data!.Id != id)
                return Forbid();
        }

        var result = await _patientService.GetDiagnosesAsync(id);
        return Ok(result);
    }

    [HttpGet("{id}/vitals")]
    public async Task<IActionResult> GetVitals(Guid id)
    {
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (!myProfile.Success || myProfile.Data!.Id != id)
                return Forbid();
        }

        var result = await _patientService.GetVitalSignsAsync(id);
        return Ok(result);
    }

    [HttpGet("{id}/clinical-notes")]
    [Authorize(Roles = $"{AppRoles.Doctor},{AppRoles.Nurse},{AppRoles.Admin}")]
    public async Task<IActionResult> GetClinicalNotes(Guid id)
    {
        var result = await _patientService.GetClinicalNotesAsync(id);
        return Ok(result);
    }

    [HttpGet("{id}/treatment-plans")]
    public async Task<IActionResult> GetTreatmentPlans(Guid id)
    {
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (!myProfile.Success || myProfile.Data!.Id != id)
                return Forbid();
        }

        var result = await _patientService.GetTreatmentPlansAsync(id);
        return Ok(result);
    }
}
