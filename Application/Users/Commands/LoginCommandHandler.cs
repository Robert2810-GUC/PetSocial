namespace Application.Users.Commands;

using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System.Threading;
using System.Threading.Tasks;

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<TokenResult>>
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<ApiResponse<TokenResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if(request is null) return ApiResponse<TokenResult>.Fail("Wrong Call..", 400);
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.PhoneNumber))
            return ApiResponse<TokenResult>.Fail("Email or Phone is required.", 400);

        if (string.IsNullOrWhiteSpace(request.Password))
            return ApiResponse<TokenResult>.Fail("Password is required.", 400);

        IdentityUser identityUser = null;
        User userProfile = null;

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            identityUser = await _userManager.FindByEmailAsync(request.Email);
        }
        else if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            // Find custom User entity by phone, then get IdentityId
            userProfile = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber! && u.CountryCode == request.CountryCode);
            if (userProfile != null)
            {
                identityUser = await _userManager.FindByIdAsync(userProfile.IdentityId);
            }
        }

        if (identityUser == null)
            return ApiResponse<TokenResult>.Fail("User not found.", 404);

        // Check password
        var isPasswordValid = await _userManager.CheckPasswordAsync(identityUser, request.Password);
        if (!isPasswordValid)
            return ApiResponse<TokenResult>.Fail("Invalid credentials.", 401);

        if (userProfile == null)
        {
            userProfile = await _dbContext.Users.FirstOrDefaultAsync(u => u.IdentityId == identityUser.Id);
        }

        var isProfileUpdate = userProfile != null;
        var isPetRegistered = false;
        string userName = identityUser.UserName;

        if (isProfileUpdate)
        {
            userName = userProfile.Name;
            isPetRegistered = await _dbContext.UserPets.AnyAsync(p => p.UserId == userProfile.Id);
        }

        // Generate JWT
        var token = _jwtTokenService.GenerateToken(identityUser.Id, identityUser.Email, identityUser.UserName);
        var tokenResult = new TokenResult
        {
            Token = token,
            IsPetRegistered = isPetRegistered,
            IsProfileUpdated = isProfileUpdate,
            UserName = userName
        };

        return ApiResponse<TokenResult>.Success(tokenResult, "Login successful!", 200);
    }
}

