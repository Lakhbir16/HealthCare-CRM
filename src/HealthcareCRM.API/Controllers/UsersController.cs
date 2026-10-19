using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthcareCRM.Application.DTOs;
using HealthcareCRM.Application.Interfaces;
using HealthcareCRM.Domain.Enums;

namespace HealthcareCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _authService.GetUsersAsync();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request)
    {
        var result = await _authService.CreateUserAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id}/roles")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> UpdateRoles(Guid id, [FromBody] UpdateUserRolesDto request)
    {
        var result = await _authService.UpdateUserRolesAsync(id, request.Roles);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SetStatus(Guid id, [FromQuery] bool isActive, [FromQuery] bool isLockedOut)
    {
        var result = await _authService.SetUserStatusAsync(id, isActive, isLockedOut);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
