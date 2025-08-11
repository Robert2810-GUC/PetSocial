namespace Application.Users.Commands;

using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence;
using System.Linq;
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
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.PhoneNumber))
            return ApiResponse<TokenResult>.Fail("Email or Phone is required.", 400);

        if (string.IsNullOrWhiteSpace(request.Password))
            return ApiResponse<TokenResult>.Fail("Password is required.", 400);

        IdentityUser identityUser = null;

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            identityUser = await _userManager.FindByEmailAsync(request.Email);
        }
        else if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            // Find custom User entity by phone, then get IdentityId
            var userProfile = _dbContext.Users.FirstOrDefault(u => u.PhoneNumber == request.PhoneNumber && u.CountryCode == request.CountryCode);
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

        // Additional info
        var user = _dbContext.Users.FirstOrDefault(u => u.IdentityId == identityUser.Id);
        bool isPetRegistered = false;
        bool isProfileUpdated = false;
        string userName = user != null ? user?.Name != null ? user.Name.Trim() : "User" : string.Empty;

        if (user != null)
        {
            isPetRegistered = _dbContext.UserPets.Any(p => p.UserId == user.Id);
            isProfileUpdated = isPetRegistered && _dbContext.PetOwnerProfiles.Any(p => p.UserId == user.Id);
        }


        var roles = await _userManager.GetRolesAsync(identityUser);
        var role = roles.FirstOrDefault() ?? string.Empty;
        // Generate JWT
        var token = _jwtTokenService.GenerateToken(identityUser.Id, identityUser.Email, role, identityUser.UserName);
        var tokenResult = new TokenResult
        {
            Token = token,
            IsPetRegistered = isPetRegistered,
            IsProfileUpdated = isProfileUpdated,
            UserName = userName
        };

        return ApiResponse<TokenResult>.Success(tokenResult, "Login successful!", 200);
    }
}

