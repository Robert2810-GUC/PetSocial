using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Businesses.Queries;

public class GetPetBusinessProfileQuery : IRequest<ApiResponse<PetBusinessProfileDto>>
{
    public string IdentityId { get; set; }
}

public class PetBusinessProfileDto
{
    public string BusinessName { get; set; }
    public string? OwnerName { get; set; }
    public DateTime? BusinessStartDate { get; set; }
    public string Address { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string SecurityNumber { get; set; }
    public string SecurityType { get; set; }
    public int? NumberOfEmployees { get; set; }
    public string? BusinessType { get; set; }
    public double? GoogleRating { get; set; }
    public string? GoogleRatingLink { get; set; }
    public string? BannerImagePath { get; set; }
    public string? ProfileImagePath { get; set; }
}

public class GetPetBusinessProfileQueryHandler : IRequestHandler<GetPetBusinessProfileQuery, ApiResponse<PetBusinessProfileDto>>
{
    private readonly ApplicationDbContext _dbContext;

    public GetPetBusinessProfileQueryHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<PetBusinessProfileDto>> Handle(GetPetBusinessProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.PetBusinessProfile)
            .FirstOrDefaultAsync(u => u.IdentityId == request.IdentityId, cancellationToken);
        if (user == null || user.PetBusinessProfile == null)
            return ApiResponse<PetBusinessProfileDto>.Fail("Business profile not found.", 404);

        var profile = user.PetBusinessProfile;
        var dto = new PetBusinessProfileDto
        {
            BusinessName = profile.BusinessName,
            OwnerName = profile.OwnerName,
            BusinessStartDate = profile.BusinessStartDate,
            Address = profile.Address,
            PhoneNumber = profile.PhoneNumber,
            Email = profile.Email,
            SecurityNumber = profile.SecurityNumber,
            SecurityType = profile.SecurityType,
            NumberOfEmployees = profile.NumberOfEmployees,
            BusinessType = profile.BusinessType,
            GoogleRating = profile.GoogleRating,
            GoogleRatingLink = profile.GoogleRatingLink,
            BannerImagePath = profile.BannerImagePath,
            ProfileImagePath = profile.ProfileImagePath
        };

        return ApiResponse<PetBusinessProfileDto>.Success(dto);
    }
}
