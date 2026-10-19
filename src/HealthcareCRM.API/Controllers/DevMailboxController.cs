using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthcareCRM.Application.Common;
using HealthcareCRM.Application.Interfaces;

namespace HealthcareCRM.API.Controllers;

[ApiController]
[Route("api/dev-mailbox")]
[AllowAnonymous]
public class DevMailboxController : ControllerBase
{
    private readonly IDevEmailService _devEmailService;
    private readonly IHostEnvironment _env;

    public DevMailboxController(IDevEmailService devEmailService, IHostEnvironment env)
    {
        _devEmailService = devEmailService;
        _env = env;
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages([FromQuery] string? recipient)
    {
        // Enforce Development environment guard
        if (!_env.IsDevelopment())
        {
            return NotFound(ApiResponse<object>.Fail("Development mailbox is only available in development mode."));
        }

        var messages = await _devEmailService.GetSimulatedMessagesAsync(recipient);
        return Ok(ApiResponse<object>.Ok(messages, "Retrieved simulated inbox messages"));
    }

    [HttpGet("messages/{id}")]
    public async Task<IActionResult> GetMessageById(Guid id)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound(ApiResponse<object>.Fail("Development mailbox is only available in development mode."));
        }

        var message = await _devEmailService.GetMessageByIdAsync(id);
        if (message == null)
            return NotFound(ApiResponse<object>.Fail("Message not found."));

        return Ok(ApiResponse<object>.Ok(message));
    }
}
