using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence;

namespace Application.Users.Commands;

public class OtherUserInfoCommandHandler : IRequestHandler<OtherUserInfoCommand, ApiResponse<TokenResult>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IImageService _imageService;


    public OtherUserInfoCommandHandler(ApplicationDbContext dbContext, IImageService imageService)
    {

        _dbContext = dbContext;
        _imageService = imageService;
    }

    public async Task<ApiResponse<TokenResult>> Handle(OtherUserInfoCommand request, CancellationToken cancellationToken)
    {
        string uploadedPublicId = null;
        using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Find the user
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.IdentityId == request.IdentityId, cancellationToken);

            if (user == null)
                return ApiResponse<TokenResult>.Fail("User not found.", 404);


            user.Email = request.Email;
            user.UpdatedAt = DateTime.Now;

            _dbContext.Users.Update(user);


            // 2. Try to get the PetOwnerProfile
            var profile = await _dbContext.PetOwnerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

            string imageUrl = profile?.ImagePath;

            // 3. Handle image upload
            if (request.Image != null)
            {
                var uploadResult = await _imageService.UploadImageAsync(request.Image);
                imageUrl = uploadResult.Url;
                uploadedPublicId = uploadResult.PublicId;
            }

            // 4. If no profile exists, create one
            if (profile == null)
            {
                profile = new PetOwnerProfile
                {
                    UserId = user.Id,
                    Gender = request.Gender,
                    DOB = request.DOB,
                    ImagePath = imageUrl                   
                };
                await _dbContext.PetOwnerProfiles.AddAsync(profile, cancellationToken);
            }
            else
            {
                // Update fields
                profile.Gender = request.Gender;
                profile.DOB = request.DOB;
                profile.ImagePath = imageUrl;
                profile.UpdatedAt = DateTime.Now;

                _dbContext.PetOwnerProfiles.Update(profile);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return ApiResponse<TokenResult>.Success(null, "User info updated!", 200);
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
                catch
                {
                    // Optionally log the cleanup failure
                }
            }
            return ApiResponse<TokenResult>.Fail("Update error: " + ex.Message, 500);
        }
    }
}
