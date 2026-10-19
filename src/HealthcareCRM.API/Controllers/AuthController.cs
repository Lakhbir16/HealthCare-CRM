using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthcareCRM.Application.Common;
using HealthcareCRM.Application.DTOs;
using HealthcareCRM.Application.Interfaces;

namespace HealthcareCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IOtpService _otpService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IAuthService authService, IOtpService otpService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _otpService = otpService;
        _currentUserService = currentUserService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("switch-role")]
    [Authorize]
    public async Task<IActionResult> SwitchRole([FromBody] SwitchRoleRequestDto request)
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

        var result = await _authService.SwitchRoleAsync(_currentUserService.UserId.Value, request.Role);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto? request)
    {
        if (_currentUserService.UserId.HasValue)
        {
            await _authService.LogoutAsync(_currentUserService.UserId.Value, request?.RefreshToken);
        }
        return Ok(ApiResponse<bool>.Ok(true, "Logged out successfully"));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(ApiResponse<object>.Fail("Email address is required."));

        var result = await _otpService.RequestOtpAsync(request.Email);
        return Ok(ApiResponse<ForgotPasswordResponseDto>.Ok(result, "OTP generated successfully."));
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
            return BadRequest(ApiResponse<object>.Fail("Email and OTP are required."));

        var result = await _otpService.VerifyOtpAsync(request.Email, request.Otp);
        if (!result.IsValid)
            return BadRequest(ApiResponse<VerifyOtpResponseDto>.Fail(result.Message, new List<string> { result.Message }));

        return Ok(ApiResponse<VerifyOtpResponseDto>.Ok(result, "OTP verified successfully."));
    }

    [HttpPost("resend-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(ApiResponse<object>.Fail("Email is required."));

        var result = await _otpService.ResendOtpAsync(request.Email);
        return Ok(ApiResponse<ForgotPasswordResponseDto>.Ok(result, "New OTP generated successfully."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(ApiResponse<object>.Fail("Passwords do not match."));

        var success = await _otpService.ResetPasswordAsync(request);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail("Invalid or expired password reset request."));

        return Ok(ApiResponse<bool>.Ok(true, "Password has been successfully reset. You may now log in with your new password."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

        var result = await _authService.GetCurrentUserProfileAsync(_currentUserService.UserId.Value, _currentUserService.ActiveRole);
        return Ok(result);
    }

    [HttpGet("my-roles")]
    [Authorize]
    public async Task<IActionResult> GetMyRoles()
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized"));

        var result = await _authService.GetUserRolesAsync(_currentUserService.UserId.Value);
        return Ok(result);
    }
}
