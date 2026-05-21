using LoanSuperMarket.Application.Features.Auth.Commands.ConfirmEmail;
using LoanSuperMarket.Application.Features.Auth.Commands.ForgotPassword;
using LoanSuperMarket.Application.Features.Auth.Commands.Login;
using LoanSuperMarket.Application.Features.Auth.Commands.Logout;
using LoanSuperMarket.Application.Features.Auth.Commands.RefreshToken;
using LoanSuperMarket.Application.Features.Auth.Commands.Register;
using LoanSuperMarket.Application.Features.Auth.Commands.ResetPassword;
using LoanSuperMarket.Application.Features.Auth.Commands.TwoFactor;
using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<string>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.UserType,
            request.CompanyName);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.Email,
            request.Password,
            request.RememberMe,
            request.TotpCode);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<string>>> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(request.RefreshToken);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<string>>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(
            request.Email,
            request.Token,
            request.NewPassword);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("confirm-email")]
    public async Task<ActionResult<ApiResponse<string>>> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmEmailCommand(request.UserId, request.Token);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("2fa/setup")]
    public async Task<ActionResult<ApiResponse<TwoFactorSetupResponse>>> Setup2Fa(
        CancellationToken cancellationToken)
    {
        var command = new Setup2FaCommand();

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("2fa/verify")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> Verify2Fa(
        [FromBody] Verify2FaRequest request,
        CancellationToken cancellationToken)
    {
        var command = new Verify2FaCommand(request.Code);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("2fa/disable")]
    public async Task<ActionResult<ApiResponse<string>>> Disable2Fa(
        CancellationToken cancellationToken)
    {
        var command = new Disable2FaCommand();

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }
}
