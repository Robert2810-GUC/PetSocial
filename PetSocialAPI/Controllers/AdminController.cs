using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Domain.Entities;
using Domain.DTOs;
using Application.Common.Interfaces;

namespace PetSocialAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IImageService _imageService;

    public AdminController(ApplicationDbContext db, IImageService imageService)
    {
        _dbContext = db;
        _imageService = imageService;
    }

    // ---------------------------
    // 🔹 PET TYPES
    // ---------------------------

    [HttpGet("pet-types")]
    public async Task<IActionResult> GetPetTypes()
    {
        var types = await _dbContext.PetTypes
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        return Ok(types);
    }

    [HttpPost("pet-types")]
    public async Task<IActionResult> CreatePetType([FromForm] PetTypeDto dto, IFormFile? image)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Pet type name is required.");

        bool exists = await _dbContext.PetTypes.AnyAsync(p =>
            p.Name.ToLower() == dto.Name.Trim().ToLower());

        if (exists)
            return Conflict("A pet type with the same name already exists.");

        string? uploadedPublicId = null;
        using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            string imageUrl = "/images/default-pet.jpg";
            if (image != null)
            {
                var uploadResult = await _imageService.UploadImageAsync(image);
                imageUrl = uploadResult.Url;
                uploadedPublicId = uploadResult.PublicId;
            }

            var petType = new PetType
            {
                Name = dto.Name.Trim(),
                ImagePath = imageUrl
            };

            _dbContext.PetTypes.Add(petType);
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(petType);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            if (!string.IsNullOrEmpty(uploadedPublicId))
            {
                try { await _imageService.DeleteImageAsync(uploadedPublicId); } catch { }
            }
            return StatusCode(500, $"Failed to create pet type: {ex.Message}");
        }
    }

    [HttpPut("pet-types/{id}")]
    public async Task<IActionResult> UpdatePetType(long id, [FromForm] PetTypeDto dto, IFormFile? image)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Pet type name is required.");

        string? uploadedPublicId = null;
        using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var petType = await _dbContext.PetTypes.FindAsync(id);
            if (petType == null)
                return NotFound("Pet type not found.");

            bool nameTaken = await _dbContext.PetTypes.AnyAsync(p =>
                p.Name.ToLower() == dto.Name.Trim().ToLower() && p.Id != id);

            if (nameTaken)
                return Conflict("Another pet type with the same name already exists.");

            petType.Name = dto.Name.Trim();

            if (image != null)
            {
                var uploadResult = await _imageService.UploadImageAsync(image);
                petType.ImagePath = uploadResult.Url;
                uploadedPublicId = uploadResult.PublicId;
            }

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(petType);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            if (!string.IsNullOrEmpty(uploadedPublicId))
            {
                try { await _imageService.DeleteImageAsync(uploadedPublicId); } catch { }
            }
            return StatusCode(500, $"Failed to update pet type: {ex.Message}");
        }
    }

    // ---------------------------
    // 🔹 PET BREEDS
    // ---------------------------

    [HttpGet("breeds")]
    public async Task<IActionResult> GetBreeds(long? petTypeId = null, string? search = null)
    {
        var query = _dbContext.PetBreeds
            .Include(b => b.PetType)
            .AsQueryable();

        if (petTypeId.HasValue)
            query = query.Where(b => b.PetTypeID == petTypeId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(b => b.Name.ToLower().Contains(search.Trim().ToLower()));

        var result = await (
            petTypeId.HasValue
                ? query.OrderBy(b => b.SortOrder)
                : query.OrderBy(b => b.PetTypeID).ThenBy(b => b.SortOrder)
        )
        .Select(b => new
        {
            b.Id,
            b.Name,
            b.SortOrder,
            b.PetTypeID,
            PetTypeName = b.PetType.Name
        })
        .ToListAsync();

        return Ok(result);
    }


    [HttpPost("breeds")]
    public async Task<IActionResult> CreateBreed([FromBody] PetBreed breed)
    {
        if (string.IsNullOrWhiteSpace(breed.Name) || breed.PetTypeID <= 0)
            return BadRequest("Breed name and PetTypeID are required.");

        bool exists = await _dbContext.PetBreeds.AnyAsync(b =>
            b.Name.ToLower() == breed.Name.Trim().ToLower() &&
            b.PetTypeID == breed.PetTypeID);

        if (exists)
            return Conflict("A breed with the same name already exists for this pet type.");

        using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            if(breed.SortOrder == 0)
            {
                int maxSortOrder = await _dbContext.PetBreeds
                    .Where(b => b.PetTypeID == breed.PetTypeID)
                    .MaxAsync(b => (int?)b.SortOrder) ?? 0;

                breed.SortOrder = maxSortOrder + 1;
            }
            breed.Name = breed.Name.Trim();
            _dbContext.PetBreeds.Add(breed);
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(breed);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, $"Failed to create breed: {ex.Message}");
        }
    }

    [HttpPut("breeds/{id}")]
    public async Task<IActionResult> UpdateBreed(long id, [FromBody] PetBreed model)
    {
        if (string.IsNullOrWhiteSpace(model.Name) || model.PetTypeID <= 0)
            return BadRequest("Breed name and PetTypeID are required.");

        using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var existing = await _dbContext.PetBreeds.FindAsync(id);
            if (existing == null)
                return NotFound("Breed not found.");

            bool nameConflict = await _dbContext.PetBreeds.AnyAsync(b =>
                b.Name.ToLower() == model.Name.Trim().ToLower() &&
                b.PetTypeID == model.PetTypeID &&
                b.Id != id);

            if (nameConflict)
                return Conflict("Another breed with the same name exists for this pet type.");

            existing.Name = model.Name.Trim();
            existing.SortOrder = model.SortOrder;
            existing.PetTypeID = model.PetTypeID;

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(existing);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, $"Failed to update breed: {ex.Message}");
        }
    }
}
