namespace Application.Users.Commands;

using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        if (request is null) return ApiResponse<TokenResult>.Fail("Wrong Call..", 400);
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.PhoneNumber))
            return ApiResponse<TokenResult>.Fail("Email or Phone is required.", 400);

        if (string.IsNullOrWhiteSpace(request.Password))
            return ApiResponse<TokenResult>.Fail("Password is required.", 400);

        IdentityUser identityUser = null;
        User userProfile = null;

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            identityUser = await _userManager.FindByEmailAsync(request.Email);
            if (identityUser != null)
            {
                userProfile = await _dbContext.Users.FirstOrDefaultAsync(u => u.IdentityId == identityUser.Id, cancellationToken);
            }
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

        if (!string.Equals(identityUser?.Email, "admin@petsocial.com", StringComparison.OrdinalIgnoreCase) && (identityUser == null || userProfile == null))
            return ApiResponse<TokenResult>.Fail("User not found.", 404);


        // Check password
        var isPasswordValid = await _userManager.CheckPasswordAsync(identityUser, request.Password);
        if (!isPasswordValid)
            return ApiResponse<TokenResult>.Fail("Invalid credentials.", 401);

        var isProfileUpdated = false;
        var isPetRegistered = await _dbContext.UserPets.AnyAsync(p => p.UserId == userProfile.Id); 
        string userName = !string.IsNullOrEmpty(userProfile!.Name) ? userProfile.Name : identityUser.UserName!;
        if (isPetRegistered)
        {
            isProfileUpdated = userProfile.UserTypeId switch
            {
                1 => await _dbContext.PetOwnerProfiles.AnyAsync(p => p.UserId == userProfile.Id),
                2 => await _dbContext.PetBusinessProfiles.AnyAsync(p => p.UserId == userProfile.Id),
                3 => await _dbContext.ContentCreatorProfiles.AnyAsync(p => p.UserId == userProfile.Id),
                _ => false
            };
        }

        var roles = await _userManager.GetRolesAsync(identityUser);
        var role = roles.FirstOrDefault() ?? "User";

        // Generate JWT
        var token = _jwtTokenService.GenerateToken(identityUser.Id, identityUser.Email ?? string.Empty, role, userName);
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

