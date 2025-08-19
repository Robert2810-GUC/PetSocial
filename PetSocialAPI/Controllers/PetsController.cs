using Application.Common.Models;
using Application.Pets.Commands;
using Application.Pets.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PetSocialAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

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

}

