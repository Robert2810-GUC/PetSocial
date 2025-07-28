
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
    public async Task<IActionResult> GetPetTypes() =>
        Ok(await _db.PetTypes
            .OrderBy(t => t.SortOrder)
            .Select(t => new { t.Id, t.Name, t.ImagePath })
            .ToListAsync());

    [HttpGet("breeds")]
    public async Task<IActionResult> GetBreeds(long petTypeId) =>
        Ok(await _db.PetBreeds
            .Where(b => b.PetTypeID == petTypeId)
            .OrderBy(b => b.SortOrder)
            .Select(b => new { b.Id, b.Name })
            .ToListAsync());

    [HttpGet("colors")]
    public async Task<IActionResult> GetColors(long? petTypeId = null) =>
        Ok(await _db.PetColors
            // (Optional) .Where(c => c.PetTypeId == petTypeId) if colors are type-specific
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync());
    [HttpGet("usertypes")]
    public async Task<IActionResult> GetUserTypes() =>
        Ok(await _db.UserTypes
            // (Optional) .Where(c => c.PetTypeId == petTypeId) if colors are type-specific
            .Select(c => new { c.Id, c.Name })
            .ToListAsync());
}

