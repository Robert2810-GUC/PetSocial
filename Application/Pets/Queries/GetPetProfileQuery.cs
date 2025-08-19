using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Pets.Queries;

public class GetPetProfileQuery : IRequest<ApiResponse<PetProfileResult>>
{
    public string IdentityId { get; set; }
    public long? PetId { get; set; }
}

public class PetProfileResult
{
    public PetDetailDto Pet { get; set; }
    public List<PetSummaryDto> Pets { get; set; }
}

public class PetSummaryDto
{
    public long Id { get; set; }
    public string PetName { get; set; }

    public bool IsGoldPaw {  get; set; }
}

public class PetDetailDto
{
    public long Id { get; set; }
    public long PetTypeId { get; set; }
    public string? PetTypeName { get; set; }
    public string? CustomPetTypeName { get; set; }
    public string PetName { get; set; }
    public string PetFoundAt { get; set; }
    public string ImagePath { get; set; }
    public string? Gender { get; set; }
    public DateTime? DOB { get; set; }
    public long? PetBreedId { get; set; }
    public string? PetBreedName { get; set; }
    public string? CustomPetBreed { get; set; }
    public bool IsMixedBreed { get; set; }
    public string? FirstBreed { get; set; }
    public string? SecondBreed { get; set; }
    public long? PetColorId { get; set; }
    public string? PetColorName { get; set; }
    public List<MixColorDetailDto> MixColors { get; set; } = new();
    public string? Food { get; set; }
    public long? PetFoodId { get; set; }
    public string? PetFoodName { get; set; }
    public string? CustomFood { get; set; }
    public decimal? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public string? Character { get; set; }
    public bool? IsGoldPaw { get; set; } = false;
}

public class MixColorDetailDto
{
    public string Color { get; set; }
    public decimal Percentage { get; set; }
}

public class GetPetProfileQueryHandler : IRequestHandler<GetPetProfileQuery, ApiResponse<PetProfileResult>>
{
    private readonly ApplicationDbContext _dbContext;

    public GetPetProfileQueryHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<PetProfileResult>> Handle(GetPetProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.IdentityId == request.IdentityId, cancellationToken);
        if (user == null)
            return ApiResponse<PetProfileResult>.Fail("User not found.", 404);

        var pets = await _dbContext.UserPets
            .Where(p => p.UserId == user.Id)
            .Select(p => new PetSummaryDto { Id = p.Id, PetName = p.PetName, IsGoldPaw = (p.IsGoldPaw.HasValue) ? p.IsGoldPaw.Value : false })
            .ToListAsync(cancellationToken);

        if (!pets.Any())
            return ApiResponse<PetProfileResult>.Fail("No pets found.", 404);

        var selectedPetId = request.PetId ?? pets.First().Id;

        var pet = await _dbContext.UserPets
            .AsNoTracking()
            .Include(p => p.PetType)
            .Include(p => p.PetBreed)
            .Include(p => p.PetFood)
            .Include(p => p.UserPetColors)
                .ThenInclude(upc => upc.PetColor)
            .Include(p => p.UserPetColors)
                .ThenInclude(upc => upc.UserPetMixColors)
            .Include(p => p.UserPetOtherBreeds)
            .FirstOrDefaultAsync(p => p.Id == selectedPetId && p.UserId == user.Id, cancellationToken);

        if (pet == null)
            return ApiResponse<PetProfileResult>.Fail("Pet not found.", 404);

        var upc = pet.UserPetColors?.FirstOrDefault();

        var detail = new PetDetailDto
        {
            Id = pet.Id,
            PetTypeId = pet.PetTypeId,
            PetTypeName = pet.PetType?.Name,
            CustomPetTypeName = pet.CustomPetTypeName,
            PetName = pet.PetName,
            PetFoundAt = pet.PetFoundAt,
            ImagePath = pet.ImagePath,
            Gender = pet.Gender,
            DOB = pet.DOB,
            PetBreedId = pet.PetBreedId,
            PetBreedName = pet.PetBreed?.Name,
            CustomPetBreed = pet.CustomPetBreed,
            Food = pet.Food,
            PetFoodId = pet.PetFoodId,
            PetFoodName = pet.PetFood?.Name,
            CustomFood = pet.CustomFood,
            Weight = pet.Weight,
            WeightUnit = pet.WeightUnit,
            Character = pet.Character,
            IsGoldPaw = pet.IsGoldPaw,

            // other-breed flags
            IsMixedBreed = pet.UserPetOtherBreeds != null && pet.UserPetOtherBreeds.Any(),
            FirstBreed = pet.UserPetOtherBreeds?.FirstOrDefault()?.FirstBreed,
            SecondBreed = pet.UserPetOtherBreeds?.FirstOrDefault()?.SecondBreed,

            // color (from the master PetColor via UserPetColor)
            PetColorId = upc?.PetColorId,
            PetColorName = upc?.PetColor?.Name,

            // mix colors (from UserPetMixColors under UserPetColor)
            // NOTE: column is Name, not Color
            MixColors = pet.UserPetColors?
                .SelectMany(c => c.UserPetMixColors ?? Enumerable.Empty<UserPetMixColor>())
                .Select(mc => new MixColorDetailDto { Color = mc.Color, Percentage = mc.Percentage })
                .ToList() ?? new List<MixColorDetailDto>()
        };



        var result = new PetProfileResult
        {
            Pet = detail,
            Pets = pets
        };

        return ApiResponse<PetProfileResult>.Success(result);
    }
}

