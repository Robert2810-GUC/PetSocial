using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Users.Queries;

public class GetUserProfileQuery : IRequest<ApiResponse<UserProfileDto>>
{
    public string IdentityId { get; set; }
}

public class UserProfileDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string CountryCode { get; set; }
    public string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public long UserTypeId { get; set; }
    public string? UserTypeName { get; set; }
    public PetOwnerProfileDto? OwnerProfile { get; set; }
    public PetBusinessProfileDto? BusinessProfile { get; set; }
    public ContentCreatorProfileDto? CreatorProfile { get; set; }
}

public class PetOwnerProfileDto
{
    public string Gender { get; set; }
    public DateTime? DOB { get; set; }
    public string ImagePath { get; set; }
}

public class PetBusinessProfileDto
{
    public string BusinessName { get; set; }
    public string Address { get; set; }
    public string WebsiteUrl { get; set; }
}

public class ContentCreatorProfileDto
{
    public string ChannelName { get; set; }
    public int FollowersCount { get; set; }
}

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, ApiResponse<UserProfileDto>>
{
    private readonly ApplicationDbContext _dbContext;

    public GetUserProfileQueryHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserType)
            .Include(u => u.PetOwnerProfile)
            .Include(u => u.PetBusinessProfile)
            .Include(u => u.ContentCreatorProfile)
            .FirstOrDefaultAsync(u => u.IdentityId == request.IdentityId, cancellationToken);

        if (user == null)
            return ApiResponse<UserProfileDto>.Fail("User not found.", 404);

        var dto = new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            CountryCode = user.CountryCode,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            UserTypeId = user.UserTypeId,
            UserTypeName = user.UserType?.Name,
            OwnerProfile = user.PetOwnerProfile == null ? null : new PetOwnerProfileDto
            {
                Gender = user.PetOwnerProfile.Gender,
                DOB = user.PetOwnerProfile.DOB,
                ImagePath = user.PetOwnerProfile.ImagePath
            },
            BusinessProfile = user.PetBusinessProfile == null ? null : new PetBusinessProfileDto
            {
                BusinessName = user.PetBusinessProfile.BusinessName,
                Address = user.PetBusinessProfile.Address,
                WebsiteUrl = user.PetBusinessProfile.WebsiteUrl
            },
            CreatorProfile = user.ContentCreatorProfile == null ? null : new ContentCreatorProfileDto
            {
                ChannelName = user.ContentCreatorProfile.ChannelName,
                FollowersCount = user.ContentCreatorProfile.FollowersCount
            }
        };

        return ApiResponse<UserProfileDto>.Success(dto);
    }
}

