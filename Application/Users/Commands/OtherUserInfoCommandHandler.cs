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

        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.IdentityId == request.IdentityId, cancellationToken);
            if (user == null)
                return ApiResponse<TokenResult>.Fail("User not found.", 404);

            string imageUrl = user.ImagePath; 
            if (request.Image != null)
            {
                var uploadResult = await _imageService.UploadImageAsync(request.Image);
                imageUrl = uploadResult.Url;
                uploadedPublicId = uploadResult.PublicId;
            }

            // Update fields
            user.ImagePath = imageUrl;
            user.DOB = request.DOB;
            user.Gender = request.Gender;

            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiResponse<TokenResult>.Success(null, "User info updated!", 200);
        }
        catch (Exception ex)
        {
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
            return ApiResponse<TokenResult>.Fail("Update error: " + ex.Message, 500);
        }
    }
}
