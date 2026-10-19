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
public class LaboratoryController : ControllerBase
{
    private readonly ILabService _labService;
    private readonly IPatientService _patientService;
    private readonly ICurrentUserService _currentUserService;

    public LaboratoryController(
        ILabService labService,
        IPatientService patientService,
        ICurrentUserService currentUserService)
    {
        _labService = labService;
        _patientService = patientService;
        _currentUserService = currentUserService;
    }

    [HttpPost("orders")]
    [Authorize(Roles = AppRoles.Doctor)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateLabOrderDto request, [FromQuery] Guid doctorId)
    {
        var result = await _labService.CreateLabOrderAsync(request, doctorId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] Guid? patientId, [FromQuery] Guid? doctorId, [FromQuery] LabOrderStatus? status)
    {
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (myProfile.Success)
            {
                patientId = myProfile.Data!.Id;
            }
        }

        var result = await _labService.GetLabOrdersAsync(patientId, doctorId, status);
        return Ok(result);
    }

    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        var result = await _labService.GetLabOrderByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost("items/{itemId}/results")]
    [Authorize(Roles = $"{AppRoles.Doctor},{AppRoles.Nurse},{AppRoles.Admin}")]
    public async Task<IActionResult> RecordResult(Guid itemId, [FromBody] CreateLabResultDto request)
    {
        var technicianId = _currentUserService.UserId ?? Guid.Empty;
        var result = await _labService.RecordLabResultAsync(itemId, request, technicianId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPatch("orders/{id}/status")]
    [Authorize(Roles = $"{AppRoles.Doctor},{AppRoles.Nurse},{AppRoles.Admin}")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] LabOrderStatus status)
    {
        var result = await _labService.UpdateOrderStatusAsync(id, status);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
