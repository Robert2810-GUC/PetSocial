using Application.Common.Models;
using Application.Users.Queries;
using Application.Pets.Queries;
using Application.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Pets.Commands;

namespace PetSocialAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserProfile()
    {
        var identityId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(identityId))
            return Unauthorized(ApiResponse<string>.Fail("Invalid token.", 401));

        var result = await _mediator.Send(new GetUserProfileQuery { IdentityId = identityId });
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("user")]
    public async Task<IActionResult> UpdateUserProfile([FromForm] UpdateUserProfileCommand command)
    {
        var identityId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(identityId))
            return Unauthorized(ApiResponse<string>.Fail("Invalid token.", 401));

        command.IdentityId = identityId;
        var result = await _mediator.Send(command);
        return StatusCode(result.StatusCode, result);
    }
    [HttpGet("pet")]
    public async Task<IActionResult> GetPetProfile([FromQuery] long? petId)
    {
        var identityId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(identityId))
            return Unauthorized(ApiResponse<string>.Fail("Invalid token.", 401));

        var query = new GetPetProfileQuery { IdentityId = identityId, PetId = petId };
        var result = await _mediator.Send(query);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("pet")]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdatePetProfileCommand command)
    {
        var userId =
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<string>.Fail("Invalid token.", 401));

        command.IdentityId = userId;
        var result = await _mediator.Send(command);
        return StatusCode(result.StatusCode, result);
    }


}

