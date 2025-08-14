using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Application.Pets.Commands;

public class UpdatePetProfileCommandHandler : IRequestHandler<UpdatePetProfileCommand, ApiResponse<long>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IImageService _imageService;

    public UpdatePetProfileCommandHandler(ApplicationDbContext dbContext, IImageService imageService)
    {
        _dbContext = dbContext;
        _imageService = imageService;
    }

    public async Task<ApiResponse<long>> Handle(UpdatePetProfileCommand request, CancellationToken cancellationToken)
    {
        string uploadedPublicId = null;
        using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await _dbContext.Users
                .Where(u => u.IdentityId == request.IdentityId)
                .Select(u => new { u.Id })
                .FirstOrDefaultAsync(cancellationToken);
            if (user == null)
                return ApiResponse<long>.Fail("User not found.", 404);

            var pet = await _dbContext.UserPets
                .Include(p => p.UserPetOtherBreeds)
                .Include(p => p.UserPetColors)!.ThenInclude(pc => pc.UserPetMixColors)
                .FirstOrDefaultAsync(p => p.Id == request.PetId && p.UserId == user.Id, cancellationToken);
            if (pet == null)
                return ApiResponse<long>.Fail("Pet not found.", 404);

            string imageUrl = pet.ImagePath;
            if (request.Image != null)
            {
                var uploadResult = await _imageService.UploadImageAsync(request.Image);
                imageUrl = uploadResult.Url;
                uploadedPublicId = uploadResult.PublicId;
            }

            pet.PetTypeId = request.PetTypeId;
            pet.CustomPetTypeName = request.PetTypeId == ReservedIds.PetTypeOther ? request.CustomPetTypeName?.Trim() : null;
            pet.PetName = request.PetName.Trim();
            pet.PetFoundAt = request.PetFoundAt.Trim();
            pet.ImagePath = imageUrl;
            pet.Gender = request.Gender;
            pet.DOB = request.DOB;
            pet.PetBreedId = request.PetBreedId;
            pet.CustomPetBreed = request.PetBreedId == ReservedIds.PetBreedOther ? request.CustomPetBreed?.Trim() : null;
            pet.Food = request.Food;
            pet.Weight = request.Weight;
            pet.WeightUnit = request.WeightUnit;
            pet.Character = request.Character?.Trim();
            pet.PetFoodId = request.PetFoodId;
            pet.CustomFood = request.PetFoodId == ReservedIds.FoodOther ? request.CustomFood?.Trim() : null;
            pet.UpdatedAt = DateTime.Now;

            if (pet.UserPetOtherBreeds != null && pet.UserPetOtherBreeds.Any())
                _dbContext.UserPetOtherBreeds.RemoveRange(pet.UserPetOtherBreeds);

            if (pet.UserPetColors != null && pet.UserPetColors.Any())
            {
                foreach (var color in pet.UserPetColors)
                {
                    if (color.UserPetMixColors != null && color.UserPetMixColors.Any())
                        _dbContext.UserPetMixColors.RemoveRange(color.UserPetMixColors);
                }
                _dbContext.UserPetColors.RemoveRange(pet.UserPetColors);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            if (request.IsMixedBreed && !string.IsNullOrEmpty(request.FirstBreed) && !string.IsNullOrEmpty(request.SecondBreed))
            {
                _dbContext.UserPetOtherBreeds.Add(new UserPetOtherBreed
                {
                    UserPetId = pet.Id,
                    FirstBreed = request.FirstBreed,
                    SecondBreed = request.SecondBreed
                });
            }

            UserPetColor petColor = null;
            if (request.PetColorId.HasValue)
            {
                petColor = new UserPetColor
                {
                    UserPetId = pet.Id,
                    PetColorId = request.PetColorId.Value
                };
                _dbContext.UserPetColors.Add(petColor);
                await _dbContext.SaveChangesAsync(cancellationToken);

                if (request.PetColorId == ReservedIds.ColorMix && !string.IsNullOrEmpty(request.MixColors))
                {
                    var mixColors = JsonSerializer.Deserialize<List<MixColorDto>>(request.MixColors,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (mixColors != null)
                    {
                        foreach (var mix in mixColors)
                        {
                            _dbContext.UserPetMixColors.Add(new UserPetMixColor
                            {
                                UserPetColorId = petColor.Id,
                                Color = mix.Color.Trim(),
                                Percentage = mix.Percentage
                            });
                        }
                    }
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return ApiResponse<long>.Success(pet.Id, "Pet updated successfully!", 200);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            if (!string.IsNullOrEmpty(uploadedPublicId))
            {
                try { await _imageService.DeleteImageAsync(uploadedPublicId); } catch { }
            }
            return ApiResponse<long>.Fail($"Update failed: {ex.Message}", 500);
        }
    }
}
