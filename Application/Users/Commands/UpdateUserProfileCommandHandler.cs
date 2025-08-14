using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Users.Commands;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, ApiResponse<TokenResult>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IImageService _imageService;

    public UpdateUserProfileCommandHandler(ApplicationDbContext dbContext, IImageService imageService)
    {
        _dbContext = dbContext;
        _imageService = imageService;
    }

    public async Task<ApiResponse<TokenResult>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        string uploadedPublicId = null;
        using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await _dbContext.Users
                .Include(u => u.PetOwnerProfile)
                .FirstOrDefaultAsync(u => u.IdentityId == request.IdentityId, cancellationToken);

            if (user == null)
                return ApiResponse<TokenResult>.Fail("User not found.", 404);

            user.Name = request.Name;
            user.UpdatedAt = DateTime.Now;
            _dbContext.Users.Update(user);

            var profile = user.PetOwnerProfile;
            string imageUrl = profile?.ImagePath;

            if (request.Image != null)
            {
                var uploadResult = await _imageService.UploadImageAsync(request.Image);
                imageUrl = uploadResult.Url;
                uploadedPublicId = uploadResult.PublicId;
            }

            if (profile == null)
            {
                profile = new PetOwnerProfile
                {
                    UserId = user.Id,
                    Gender = request.Gender,
                    Bio = request.Bio,
                    ImagePath = imageUrl
                };
                await _dbContext.PetOwnerProfiles.AddAsync(profile, cancellationToken);
            }
            else
            {
                profile.Gender = request.Gender;
                profile.Bio = request.Bio;
                profile.ImagePath = imageUrl;
                profile.UpdatedAt = DateTime.Now;
                _dbContext.PetOwnerProfiles.Update(profile);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return ApiResponse<TokenResult>.Success(null, "Profile updated!", 200);
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
                catch { }
            }
            return ApiResponse<TokenResult>.Fail("Update error: " + ex.Message, 500);
        }
    }
}
