using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthcareCRM.Application.Common;
using HealthcareCRM.Application.Interfaces;

namespace HealthcareCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(IDashboardService dashboardService, ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _currentUserService = currentUserService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var role = _currentUserService.ActiveRole ?? "Patient";
        var userId = _currentUserService.UserId ?? Guid.Empty;

        var result = await _dashboardService.GetDashboardStatsAsync(role, userId);
        return Ok(result);
    }
}
