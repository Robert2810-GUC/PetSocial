using Application.Businesses.Commands;
using Application.Businesses.Queries;
using Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PetSocialAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BusinessController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] RegisterPetBusinessCommand command)
    {
        var identityId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(identityId))
            return Unauthorized(ApiResponse<string>.Fail("Invalid token.", 401));
        command.IdentityId = identityId;
        var result = await _mediator.Send(command);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromForm] UpdatePetBusinessProfileCommand command)
    {
        var identityId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(identityId))
            return Unauthorized(ApiResponse<string>.Fail("Invalid token.", 401));
        command.IdentityId = identityId;
        var result = await _mediator.Send(command);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var identityId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(identityId))
            return Unauthorized(ApiResponse<string>.Fail("Invalid token.", 401));
        var result = await _mediator.Send(new GetPetBusinessProfileQuery { IdentityId = identityId });
        return StatusCode(result.StatusCode, result);
    }
}
