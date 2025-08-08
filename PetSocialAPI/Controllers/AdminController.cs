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
                SortOrder = dto.SortOrder != 0 ? dto.SortOrder : (int)_dbContext.PetTypes.Max(m => m.Id),
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
        string? newUploadedPublicId = null;
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
            petType.SortOrder = Convert.ToInt32(dto.SortOrder);

            if (image != null)
            {
                // 🔁 Remove old image if it has a Cloudinary public ID
                if (!string.IsNullOrEmpty(petType.ImagePath) && petType.ImagePath.Contains("res.cloudinary.com"))
                {
                    try
                    {
                        // Extract public ID (depends on how you stored it)
                        var uri = new Uri(petType.ImagePath);
                        var segments = uri.AbsolutePath.Split('/');
                        var filename = segments.Last(); // e.g. abc123.png
                        var publicId = filename[..filename.LastIndexOf('.')]; // abc123
                        await _imageService.DeleteImageAsync(publicId);
                    }
                    catch (Exception ex)
                    {
                        // Optionally log error deleting old image
                    }
                }

                // ⬆️ Upload new image
                var uploadResult = await _imageService.UploadImageAsync(image);
                petType.ImagePath = uploadResult.Url;
                newUploadedPublicId = uploadResult.PublicId;
            }

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(petType);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            if (!string.IsNullOrEmpty(newUploadedPublicId))
            {
                try { await _imageService.DeleteImageAsync(newUploadedPublicId); } catch { }
            }

            return StatusCode(500, $"Failed to update pet type: {ex.Message}");
        }
    }
    [HttpDelete("pet-types/{id}")]
    public async Task<IActionResult> DeletePetType(long id)
    {
        using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var type = await _dbContext.PetTypes.FindAsync(id);
            if (type == null) return NotFound("Pet type not found.");
            if (string.Equals(type.Name, "other", StringComparison.OrdinalIgnoreCase)) return StatusCode(500, "Can't delete Other.");

            await _dbContext.UserPets
                .Where(p => p.PetTypeId == id)
                .ExecuteUpdateAsync(p => p.SetProperty(x => x.PetTypeId, 0));

            _dbContext.PetTypes.Remove(type);
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok("Pet type deleted and references reset.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, $"Failed to delete pet type: {ex.Message}");
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
            if (breed.SortOrder == 0)
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

    [HttpDelete("breeds/{id}")]
    public async Task<IActionResult> DeleteBreed(long id)
    {
        using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var breed = await _dbContext.PetBreeds.FindAsync(id);
            if (breed == null) return NotFound("Breed not found.");
            if (string.Equals(breed.Name, "other", StringComparison.OrdinalIgnoreCase)) return StatusCode(500, "Can't delete Other.");
            if (string.Equals(breed.Name, "mix breed", StringComparison.OrdinalIgnoreCase)) return StatusCode(500, "Can't delete Mix Breed.");

            await _dbContext.UserPets
                .Where(p => p.PetBreedId == id)
                .ExecuteUpdateAsync(p => p.SetProperty(x => x.PetBreedId, 0));

            _dbContext.PetBreeds.Remove(breed);
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok("Breed deleted and references reset.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, $"Failed to delete breed: {ex.Message}");
        }
    }




    // ---------------------------
    // 🔹 PET Colors
    // ---------------------------

    [HttpGet("colors")]
    public async Task<IActionResult> GetColors(string? search = null)
    {
        var query = _dbContext.PetColors.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.ToLower().Contains(search.Trim().ToLower()));

        var colors = await query
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return Ok(colors);
    }

    [HttpPost("colors")]
    public async Task<IActionResult> CreateColor([FromBody] PetColor color)
    {
        if (string.IsNullOrWhiteSpace(color.Name))
            return BadRequest("Color name is required.");

        bool exists = await _dbContext.PetColors.AnyAsync(c => c.Name.ToLower() == color.Name.Trim().ToLower());
        if (exists)
            return Conflict("A color with the same name already exists.");

        using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            if (color.SortOrder == 0)
            {
                int maxSort = await _dbContext.PetColors.MaxAsync(c => (int?)c.SortOrder) ?? 0;
                color.SortOrder = maxSort + 1;
            }
            color.Name = color.Name.Trim();

            _dbContext.PetColors.Add(color);
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(color);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, $"Failed to create color: {ex.Message}");
        }
    }

    [HttpPut("colors/{id}")]
    public async Task<IActionResult> UpdateColor(long id, [FromBody] PetColor model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest("Color name is required.");

        using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var color = await _dbContext.PetColors.FindAsync(id);
            if (color == null) return NotFound("Color not found.");

            bool exists = await _dbContext.PetColors.AnyAsync(c =>
                c.Name.ToLower() == model.Name.Trim().ToLower() && c.Id != id);

            if (exists)
                return Conflict("Another color with the same name exists.");

            color.Name = model.Name.Trim();
            color.SortOrder = model.SortOrder;

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(color);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, $"Failed to update color: {ex.Message}");
        }
    }
    [HttpDelete("colors/{id}")]
    public async Task<IActionResult> DeleteColor(long id)
    {
        using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var color = await _dbContext.PetColors.FindAsync(id);
            if (color == null) return NotFound("Color not found.");
            if (string.Equals(color.Name, "other", StringComparison.OrdinalIgnoreCase)) return StatusCode(500, "Can't delete Other.");
            if (string.Equals(color.Name, "mix color", StringComparison.OrdinalIgnoreCase)) return StatusCode(500, "Can't delete Mix Color.");

            // Get affected UserPetColor records
            var userColors = await _dbContext.UserPetColors
                .Where(c => c.PetColorId == id)
                .ToListAsync();

            foreach (var uc in userColors)
                uc.PetColorId = 0;

            _dbContext.PetColors.Remove(color);
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok("Color deleted and references reset.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, $"Failed to delete color: {ex.Message}");
        }
    }
    // ---------------------------
    // 🔹 PET FOODS
    // ---------------------------

    [HttpGet("foods")]
    public async Task<IActionResult> GetFoods(string? search = null)
    {
        var query = _dbContext.PetFoods.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Name.ToLower().Contains(search.Trim().ToLower()));

        var foods = await query
            .OrderBy(f => f.SortOrder)
            .ToListAsync();

        return Ok(foods);
    }

    [HttpPost("foods")]
    public async Task<IActionResult> CreateFood([FromBody] PetFood food)
    {
        if (string.IsNullOrWhiteSpace(food.Name))
            return BadRequest("Food name is required.");

        bool exists = await _dbContext.PetFoods.AnyAsync(f => f.Name.ToLower() == food.Name.Trim().ToLower());
        if (exists)
            return Conflict("A food with the same name already exists.");

        using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            if (food.SortOrder == 0)
            {
                int maxSort = await _dbContext.PetFoods.MaxAsync(f => (int?)f.SortOrder) ?? 0;
                food.SortOrder = maxSort + 1;
            }
            food.Name = food.Name.Trim();

            _dbContext.PetFoods.Add(food);
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(food);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, $"Failed to create food: {ex.Message}");
        }
    }

    [HttpPut("foods/{id}")]
    public async Task<IActionResult> UpdateFood(long id, [FromBody] PetFood model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest("Food name is required.");

        using var tx = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var food = await _dbContext.PetFoods.FindAsync(id);
            if (food == null) return NotFound("Food not found.");

            bool exists = await _dbContext.PetFoods.AnyAsync(f =>
                f.Name.ToLower() == model.Name.Trim().ToLower() && f.Id != id);

            if (exists)
                return Conflict("Another food with the same name exists.");

            food.Name = model.Name.Trim();
            food.SortOrder = model.SortOrder;

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(food);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, $"Failed to update food: {ex.Message}");
        }
    }

    [HttpDelete("foods/{id}")]
    public async Task<IActionResult> DeleteFood(long id)
    {
        using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var food = await _dbContext.PetFoods.FindAsync(id);
            if (food == null) return NotFound("Food not found.");
            if (string.Equals(food.Name, "other", StringComparison.OrdinalIgnoreCase))
                return StatusCode(500, "Can't delete 'Other'.");

            var pets = await _dbContext.UserPets
                .Where(p => p.PetFoodId == id)
                .ToListAsync();

            foreach (var pet in pets)
                pet.PetFoodId = null;

            _dbContext.PetFoods.Remove(food);
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok("Food deleted and references reset.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, $"Failed to delete food: {ex.Message}");
        }
    }

}
