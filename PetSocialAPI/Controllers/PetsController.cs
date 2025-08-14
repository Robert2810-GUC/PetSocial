using Application.Common.Models;
using Application.Pets.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PetSocialAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] RegisterPetCommand command)
    {
        // Get the user ID from the JWT
        var userId =
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<string>.Fail("Invalid token.", 401));

        // Pass the user ID into the command
        command.IdentityId = userId;

        var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
    }

    [HttpPut("profile")]
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

 