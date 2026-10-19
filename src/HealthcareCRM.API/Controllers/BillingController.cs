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
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly IPatientService _patientService;
    private readonly ICurrentUserService _currentUserService;

    public BillingController(
        IBillingService billingService,
        IPatientService patientService,
        ICurrentUserService currentUserService)
    {
        _billingService = billingService;
        _patientService = patientService;
        _currentUserService = currentUserService;
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] Guid? patientId, [FromQuery] InvoiceStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (myProfile.Success)
            {
                patientId = myProfile.Data!.Id;
            }
        }

        var result = await _billingService.GetInvoicesAsync(patientId, status, page, pageSize);
        return Ok(result);
    }

    [HttpGet("invoices/{id}")]
    public async Task<IActionResult> GetInvoiceById(Guid id)
    {
        var result = await _billingService.GetInvoiceByIdAsync(id);
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

    [HttpPost("invoices")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Receptionist}")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto request)
    {
        var result = await _billingService.CreateInvoiceAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Data!.Id }, result);
    }

    [HttpPost("payments")]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentDto request)
    {
        var processedBy = _currentUserService.UserId ?? Guid.Empty;
        var result = await _billingService.RecordPaymentAsync(request, processedBy);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] Guid? invoiceId, [FromQuery] Guid? patientId)
    {
        if (_currentUserService.IsInRole(AppRoles.Patient))
        {
            var myProfile = await _patientService.GetPatientByUserIdAsync(_currentUserService.UserId!.Value);
            if (myProfile.Success)
            {
                patientId = myProfile.Data!.Id;
            }
        }

        var result = await _billingService.GetPaymentsAsync(invoiceId, patientId);
        return Ok(result);
    }
}
