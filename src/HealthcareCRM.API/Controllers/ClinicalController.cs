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
public class ClinicalController : ControllerBase
{
    private readonly IClinicalService _clinicalService;
    private readonly IPatientService _patientService;
    private readonly ICurrentUserService _currentUserService;

    public ClinicalController(
        IClinicalService clinicalService,
        IPatientService patientService,
        ICurrentUserService currentUserService)
    {
        _clinicalService = clinicalService;
        _patientService = patientService;
        _currentUserService = currentUserService;
    }

    [HttpPost("vitals/{patientId}")]
    [Authorize(Roles = $"{AppRoles.Doctor},{AppRoles.Nurse}")]
    public async Task<IActionResult> RecordVitals(Guid patientId, [FromBody] VitalSignDto dto)
    {
        var result = await _clinicalService.RecordVitalSignsAsync(patientId, dto, _currentUserService.UserId ?? Guid.Empty);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("notes/{patientId}")]
    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<IActionResult> AddClinicalNote(Guid patientId, [FromBody] ClinicalNoteDto dto, [FromQuery] Guid doctorId)
    {
        var result = await _clinicalService.AddClinicalNoteAsync(patientId, dto, doctorId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("diagnoses/{patientId}")]
    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<IActionResult> AddDiagnosis(Guid patientId, [FromBody] DiagnosisDto dto, [FromQuery] Guid? doctorId)
    {
        var result = await _clinicalService.AddDiagnosisAsync(patientId, dto, doctorId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("prescriptions")]
    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<IActionResult> CreatePrescription([FromBody] CreatePrescriptionDto dto, [FromQuery] Guid doctorId)
    {
        var result = await _clinicalService.CreatePrescriptionAsync(dto, doctorId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("prescriptions")]
    public async Task<IActionResult> GetPrescriptions([FromQuery] Guid? patientId, [FromQuery] Guid? doctorId)
    {
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (myProfile.Success)
            {
                patientId = myProfile.Data!.Id;
            }
        }

        var result = await _clinicalService.GetPrescriptionsAsync(patientId, doctorId);
        return Ok(result);
    }

    [HttpPatch("prescriptions/{id}/status")]
    [Authorize(Roles = $"{AppRoles.Doctor},{AppRoles.Nurse},{AppRoles.Admin}")]
    public async Task<IActionResult> UpdatePrescriptionStatus(Guid id, [FromQuery] PrescriptionStatus status)
    {
        var result = await _clinicalService.UpdatePrescriptionStatusAsync(id, status);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
