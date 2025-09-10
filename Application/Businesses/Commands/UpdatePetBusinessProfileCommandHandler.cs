using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Businesses.Commands;

public class UpdatePetBusinessProfileCommandHandler : IRequestHandler<UpdatePetBusinessProfileCommand, ApiResponse<long>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IImageService _imageService;
    private readonly IGoogleRatingService _googleRatingService;

    public UpdatePetBusinessProfileCommandHandler(ApplicationDbContext dbContext, IImageService imageService, IGoogleRatingService googleRatingService)
    {
        _dbContext = dbContext;
        _imageService = imageService;
        _googleRatingService = googleRatingService;
    }

    public async Task<ApiResponse<long>> Handle(UpdatePetBusinessProfileCommand request, CancellationToken cancellationToken)
    {
        string? bannerPublicId = null;
        string? profilePublicId = null;
        using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (string.IsNullOrWhiteSpace(request.BusinessName) ||
                string.IsNullOrWhiteSpace(request.PhoneNumber) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                return ApiResponse<long>.Fail("Business Name, Phone Number and Email are required.", 400);
            }

            var user = await _dbContext.Users
                .Include(u => u.PetBusinessProfile)
                .FirstOrDefaultAsync(u => u.IdentityId == request.IdentityId, cancellationToken);
            if (user == null)
                return ApiResponse<long>.Fail("User not found.", 404);

            var phoneTaken = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.PhoneNumber == request.PhoneNumber && u.Id != user.Id, cancellationToken);
            if (phoneTaken)
                return ApiResponse<long>.Fail("Phone number already in use.", 409);

            var emailTaken = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == request.Email && u.Id != user.Id, cancellationToken);
            if (emailTaken)
                return ApiResponse<long>.Fail("Email already in use.", 409);

            var profile = user.PetBusinessProfile;
            if (profile == null)
            {
                profile = new PetBusinessProfile { UserId = user.Id };
                _dbContext.PetBusinessProfiles.Add(profile);
            }

            string? bannerUrl = profile.BannerImagePath;
            if (request.BannerImage != null)
            {
                var upload = await _imageService.UploadImageAsync(request.BannerImage);
                bannerUrl = upload.Url;
                bannerPublicId = upload.PublicId;
            }

            string? profileUrl = profile.ProfileImagePath;
            if (request.ProfileImage != null)
            {
                var upload = await _imageService.UploadImageAsync(request.ProfileImage);
                profileUrl = upload.Url;
                profilePublicId = upload.PublicId;
            }

            double? rating = null;
            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                rating = await _googleRatingService.GetRatingAsync(request.BusinessName, request.Address, cancellationToken);
            }

            profile.BusinessName = request.BusinessName;
            profile.OwnerName = request.OwnerName;
            profile.BusinessStartDate = request.BusinessStartDate;
            profile.Address = request.Address;
            profile.PhoneNumber = request.PhoneNumber;
            profile.Email = request.Email;
            profile.SecurityNumber = request.SecurityNumber;
            profile.SecurityType = request.SecurityType;
            profile.NumberOfEmployees = request.NumberOfEmployees;
            profile.BusinessType = request.BusinessType;
            profile.ServicesOffered = request.ServicesOffered;
            profile.HasParking = request.HasParking;
            profile.WebsiteUrl = request.WebsiteUrl;
            profile.PaymentMethods = request.PaymentMethods;
            profile.GoogleRating = rating;
            profile.GoogleRatingLink = request.GoogleRatingLink;
            profile.BannerImagePath = bannerUrl;
            profile.ProfileImagePath = profileUrl;
            profile.UpdatedAt = DateTime.Now;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return ApiResponse<long>.Success(profile.Id, "Business profile updated!", 200);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            if (!string.IsNullOrEmpty(bannerPublicId))
            {
                try { await _imageService.DeleteImageAsync(bannerPublicId); } catch { }
            }
            if (!string.IsNullOrEmpty(profilePublicId))
            {
                try { await _imageService.DeleteImageAsync(profilePublicId); } catch { }
            }
            return ApiResponse<long>.Fail("Update failed: " + ex.Message, 500);
        }
    }
}
