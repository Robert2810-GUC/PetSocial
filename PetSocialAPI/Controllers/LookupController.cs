using Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Application.Lookups.Queries;

namespace PetSocialAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LookupController : ControllerBase
{
    private readonly IMediator _mediator;

    public LookupController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("pet-types")]
    public async Task<IActionResult> GetPetTypes()
    {
        var response = await _mediator.Send(new GetPetTypesQuery());
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("breeds")]
    public async Task<IActionResult> GetBreeds(long petTypeId)
    {
        var response = await _mediator.Send(new GetBreedsQuery(petTypeId));
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("colors")]
    public async Task<IActionResult> GetColors()
    {
        var response = await _mediator.Send(new GetColorsQuery());
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("usertypes")]
    public async Task<IActionResult> GetUserTypes()
    {
        var response = await _mediator.Send(new GetUserTypesQuery());
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("food")]
    public async Task<IActionResult> GetFood()
    {
        var response = await _mediator.Send(new GetPetFoodsQuery());
        return StatusCode(response.StatusCode, response);
    }
}
