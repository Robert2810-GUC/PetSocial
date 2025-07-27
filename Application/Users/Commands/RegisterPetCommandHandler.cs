using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Pets.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Domain.Entities;
using Application.Common;
using System.Text.Json;

namespace Application.Users.Commands;

public class RegisterPetCommandHandler : IRequestHandler<RegisterPetCommand, ApiResponse<long>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IImageService _imageService;

    public RegisterPetCommandHandler(ApplicationDbContext db, IImageService imageService)
    {
        _dbContext = db;
        _imageService = imageService;
    }

    public async Task<ApiResponse<long>> Handle(RegisterPetCommand request, CancellationToken cancellationToken)
    {
        string uploadedPublicId = null;
        using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Upload pet image
            string imageUrl = null;


            if (request.Image != null)
            {
                var uploadResult = await _imageService.UploadImageAsync(request.Image);
                imageUrl = uploadResult.Url;
                uploadedPublicId = uploadResult.PublicId;
            }

            // 2. Get user (use select only Id for efficiency, but full user is fine if needed)
            var user = await _dbContext.Users
                .Where(u => u.IdentityId == request.IdentityId)
                .Select(u => new { u.Id })
                .FirstOrDefaultAsync(cancellationToken);
            if (user == null)
                return ApiResponse<long>.Fail("User not found.", 404);

            // 3. Insert UserPet
            var pet = new UserPet
            {
                UserId = user.Id,
                PetTypeId = request.PetTypeId,
                CustomPetTypeName = request.PetTypeId == ReservedIds.PetTypeOther ? request.CustomPetTypeName : null,
                PetName = request.PetName,
                PetFoundAt = request.PetFoundAt,
                ImagePath = imageUrl ?? "/images/default-pet.jpg",
                Gender = request.Gender,
                DOB = request.DOB,
                PetBreedId = request.PetBreedId,
                CustomPetBreed = request.PetBreedId == ReservedIds.PetBreedOther ? request.CustomPetBreed : null,
                Food = request.Food,
                Weight = request.Weight,
                WeightUnit = request.WeightUnit,
                Character = request.Character
            };
            _dbContext.UserPets.Add(pet);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 4. Insert mix breed info if needed
            if (request.IsMixedBreed && !string.IsNullOrEmpty(request.FirstBreed) && !string.IsNullOrEmpty(request.SecondBreed))
            {
                _dbContext.UserPetOtherBreeds.Add(new UserPetOtherBreed
                {
                    UserPetId = pet.Id,
                    FirstBreed = request.FirstBreed,
                    SecondBreed = request.SecondBreed
                });
            }

            // 5. Insert color(s)
            var petColor = new UserPetColor
            {
                UserPetId = pet.Id,
                PetColorId = request.PetColorId
            };
            _dbContext.UserPetColors.Add(petColor);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 6. Insert mix colors if any
            if (request.PetColorId == ReservedIds.ColorMix && !string.IsNullOrEmpty(request.MixColors))
            {
                var mixColors = JsonSerializer.Deserialize<List<MixColorDto>>(
                    request.MixColors,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                if (mixColors != null && mixColors.Count > 0)
                {
                    foreach (var mix in mixColors)
                    {
                        _dbContext.UserPetMixColors.Add(new UserPetMixColor
                        {
                            UserPetColorId = petColor.Id,
                            Color = mix.Color,
                            Percentage = mix.Percentage
                        });
                    }

                }
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return ApiResponse<long>.Success(pet.Id, "Pet registered successfully!", 201);
        }

        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            if (!string.IsNullOrEmpty(uploadedPublicId))
            {
                try
                {
                    await _imageService.DeleteImageAsync(uploadedPublicId);
                }
                catch (Exception delEx)
                {
                    // Optionally log the cleanup failure, but don’t throw!
                }
            }
            return ApiResponse<long>.Fail($"Registration failed: {ex.Message}", 500);
        }
    }

}
