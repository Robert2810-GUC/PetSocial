using Application.Common.Models;
using Application.Users.Commands;
using Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace PetSocialAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ProfiledControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] RegisterUserCommand command, IFormFile image)
    {
        try
        {
            command.Image = image;
            var token = await _mediator.Send(command);

            var response = ApiResponse<object>.Success(new { token }, "User registered successfully.", 200);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var response = ApiResponse<object>.Fail(ex.Message, 400);
            return BadRequest(response);
        }
    }
}
