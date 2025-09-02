using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Pets.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Domain.Entities;
using Application.Common;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace Application.Pets.Commands;

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
        string? uploadedPublicId = null;
        using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Upload pet image
            string? imageUrl = null;


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

            if (!string.IsNullOrEmpty(request.PetUserName))
            {
                var userpetExists = await _dbContext.UserPets
                    .AsNoTracking()
                    .AnyAsync(up => up.PetUserName.ToLower() == request.PetUserName.Trim().ToLower(), cancellationToken);
                return ApiResponse<long>.Fail("User Name already taken.", 404);

            }
            else
            {

                request.PetUserName = getUniquePetUserName(request.PetName);
            }
                // 3. Insert UserPet
                var pet = new UserPet
                {
                    UserId = user.Id,
                    PetTypeId = request.PetTypeId,
                    CustomPetTypeName = request.PetTypeId == ReservedIds.PetTypeOther ? request.CustomPetTypeName?.Trim() : null,
                    PetName = request.PetName.Trim(),
                    PetFoundAt = request.PetFoundAt.Trim(),
                    ImagePath = imageUrl ?? "/images/default-pet.jpg",
                    Gender = request.Gender,
                    DOB = request.DOB,
                    PetBreedId = request.PetBreedId,
                    CustomPetBreed = request.PetBreedId == ReservedIds.PetBreedOther ? request.CustomPetBreed?.Trim() : null,
                    Food = request.Food,
                    Weight = request.Weight,
                    WeightUnit = request.WeightUnit,
                    Character = request.Character?.Trim(),
                    PetFoodId = request.PetFoodId,
                    CustomFood = (request.PetFoodId == ReservedIds.FoodOther) ? request.CustomFood?.Trim() : null,
                    IsGoldPaw = request.PetFoundAt.Contains("shelter", StringComparison.OrdinalIgnoreCase),
                    PetUserName = string.IsNullOrWhiteSpace(request.PetUserName) ? null : request.PetUserName.Trim().ToLower().Replace(" ", "")


                };
            _dbContext.UserPets.Add(pet);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var anyDBQuery = false;
            // 4. Insert mix breed info if needed
            if (request.IsMixedBreed && !string.IsNullOrEmpty(request.FirstBreed) && !string.IsNullOrEmpty(request.SecondBreed))
            {
                _dbContext.UserPetOtherBreeds.Add(new UserPetOtherBreed
                {
                    UserPetId = pet.Id,
                    FirstBreed = request.FirstBreed,
                    SecondBreed = request.SecondBreed
                });
                anyDBQuery = true;
            }

            UserPetColor? petColor = null;
            // 5. Insert color(s)
            if (request.PetColorId.HasValue)
            {
                petColor = new UserPetColor
                {
                    UserPetId = pet.Id,
                    PetColorId = request.PetColorId.Value
                };
                _dbContext.UserPetColors.Add(petColor);
                await _dbContext.SaveChangesAsync(cancellationToken);


                // 6. Insert mix colors if any
                if (request.PetColorId == ReservedIds.ColorMix && !string.IsNullOrEmpty(request.MixColors))
                {
                    try
                    {
                        var mixColors = JsonSerializer.Deserialize<List<MixColorDto>>(
                            request.MixColors,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (mixColors is { Count: > 0 })
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
                            anyDBQuery = true;
                        }
                    }
                    catch (JsonException je)
                    {
                        await tx.RollbackAsync(cancellationToken);
                        return ApiResponse<long>.Fail($"Your Sent MixColorJSON:{request.MixColors} \nInvalid MixColors JSON: {je.Message}", 400);
                    }
                }
            }
            if (anyDBQuery)
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
                catch (Exception)
                {
                    // Optionally log the cleanup failure, but don’t throw!
                }
            }
            return ApiResponse<long>.Fail($"Registration failed: {ex.Message}", 500);
        }
    }

    private string? getUniquePetUserName(string petName)
    {
        var baseUserName = petName.Trim().ToLower().Replace(" ", "");
        var uniqueUserName = baseUserName;
        uniqueUserName = $"{baseUserName}1234";
        while (_dbContext.UserPets.Any(up => up.PetUserName.ToLower() == uniqueUserName))
        {
            uniqueUserName = $"{baseUserName}{new Random().Next(0000, 9999)}";
        }
        return uniqueUserName;
    }
}
