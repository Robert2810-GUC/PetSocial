using Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace PetSocialAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LookupController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public LookupController(ApplicationDbContext db) { _db = db; }

    [HttpGet("pet-types")]
    public async Task<IActionResult> GetPetTypes()
    {
        var types = await _db.PetTypes
            .OrderBy(t => t.SortOrder)
            .Select(t => new { t.Id, t.Name, t.ImagePath })
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(types));
    }

    [HttpGet("breeds")]
    public async Task<IActionResult> GetBreeds(long petTypeId)
    {
        var breeds = await _db.PetBreeds
            .Where(b => b.PetTypeID == petTypeId)
            .OrderBy(b => b.SortOrder)
            .Select(b => new { b.Id, b.Name })
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(breeds));
    }

    [HttpGet("colors")]
    public async Task<IActionResult> GetColors(long? petTypeId = null)
    {
        var colors = await _db.PetColors
            //.Where(c => c.PetTypeId == petTypeId) // if needed
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(colors));
    }

    [HttpGet("usertypes")]
    public async Task<IActionResult> GetUserTypes()
    {
        var userTypes = await _db.UserTypes
            .Select(c => new { c.Id, c.Name, c.ImagePath, c.Description })
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(userTypes));
    }

    [HttpGet("food")]
    public async Task<IActionResult> GetFood()
    {
        var foods = await _db.PetFoods
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(foods));
    }
}
