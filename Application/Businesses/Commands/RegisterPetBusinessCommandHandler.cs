using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Businesses.Commands;

public class RegisterPetBusinessCommandHandler : IRequestHandler<RegisterPetBusinessCommand, ApiResponse<long>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IImageService _imageService;
    private readonly IGoogleRatingService _googleRatingService;

    public RegisterPetBusinessCommandHandler(ApplicationDbContext dbContext, IImageService imageService, IGoogleRatingService googleRatingService)
    {
        _dbContext = dbContext;
        _imageService = imageService;
        _googleRatingService = googleRatingService;
    }

    public async Task<ApiResponse<long>> Handle(RegisterPetBusinessCommand request, CancellationToken cancellationToken)
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
                .Where(u => u.IdentityId == request.IdentityId)
                .Select(u => new { u.Id })
                .FirstOrDefaultAsync(cancellationToken);
            if (user == null)
                return ApiResponse<long>.Fail("User not found.", 404);

            var existing = await _dbContext.PetBusinessProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);
            if (existing != null)
                return ApiResponse<long>.Fail("Business profile already exists.", 409);

            string? bannerUrl = null;
            if (request.BannerImage != null)
            {
                var upload = await _imageService.UploadImageAsync(request.BannerImage);
                bannerUrl = upload.Url;
                bannerPublicId = upload.PublicId;
            }

            string? profileUrl = null;
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

            var profile = new PetBusinessProfile
            {
                UserId = user.Id,
                BusinessName = request.BusinessName,
                OwnerName = request.OwnerName,
                BusinessStartDate = request.BusinessStartDate,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                SecurityNumber = request.SecurityNumber,
                SecurityType = request.SecurityType,
                NumberOfEmployees = request.NumberOfEmployees,
                BusinessType = request.BusinessType,
                ServicesOffered = request.ServicesOffered,
                HasParking = request.HasParking,
                WebsiteUrl = request.WebsiteUrl,
                PaymentMethods = request.PaymentMethods,
                GoogleRating = rating,
                GoogleRatingLink = request.GoogleRatingLink,
                BannerImagePath = bannerUrl,
                ProfileImagePath = profileUrl
            };

            _dbContext.PetBusinessProfiles.Add(profile);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return ApiResponse<long>.Success(profile.Id, "Business profile created!", 201);
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
            return ApiResponse<long>.Fail("Registration failed: " + ex.Message, 500);
        }
    }
}
