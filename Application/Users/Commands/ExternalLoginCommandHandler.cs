using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
using Persistence;
using System.IdentityModel.Tokens.Jwt;
using Application.Users.Commands;
using System.Linq;

public class ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, ApiResponse<TokenResult>>
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;

    public ExternalLoginCommandHandler(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<ApiResponse<TokenResult>> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Provider) || string.IsNullOrEmpty(request.IdToken))
            return ApiResponse<TokenResult>.Fail("Provider and IdToken are required.", 400);

        var provider = request.Provider.Trim().ToLower();
        IdentityUser user = null;
        string email = null;
        string name = null;

        // GOOGLE
        if (provider == "google")
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
                email = payload.Email;
                name = payload.Name;
            }
            catch
            {
                return ApiResponse<TokenResult>.Fail("Invalid Google token.", 401);
            }
        }
        // FACEBOOK
        else if (provider == "facebook")
        {
            try
            {
                using var http = new HttpClient();
                var fbUri = $"https://graph.facebook.com/me?fields=id,name,email&access_token={request.IdToken}";
                var fbResp = await http.GetAsync(fbUri);
                if (!fbResp.IsSuccessStatusCode)
                    return ApiResponse<TokenResult>.Fail("Invalid Facebook token.", 401);

                var fbData = await fbResp.Content.ReadAsStringAsync();
                var fbObj = JObject.Parse(fbData);
                email = fbObj.Value<string>("email");
                name = fbObj.Value<string>("name");
                if (string.IsNullOrEmpty(email))
                    return ApiResponse<TokenResult>.Fail("Facebook did not return an email.", 401);
            }
            catch
            {
                return ApiResponse<TokenResult>.Fail("Error validating Facebook token.", 401);
            }
        }
        // APPLE
        else if (provider == "apple")
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(request.IdToken);
                email = jwt.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
                name = jwt.Claims.FirstOrDefault(x => x.Type == "name")?.Value;
                if (string.IsNullOrEmpty(email))
                    return ApiResponse<TokenResult>.Fail("Apple token did not provide email.", 401);

                // NOTE: For full Apple production security, validate signature as well!
            }
            catch
            {
                return ApiResponse<TokenResult>.Fail("Invalid Apple token.", 401);
            }
        }
        else
        {
            return ApiResponse<TokenResult>.Fail("Provider not supported.", 400);
        }

        // Try to find user by email
        user = await _userManager.FindByEmailAsync(email);

        // If not found, register new user and profile
        if (user == null)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email
                };
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                    return ApiResponse<TokenResult>.Fail(
                        "User creation failed: " + string.Join(", ", result.Errors.Select(e => e.Description)), 400);

                _dbContext.Users.Add(new User
                {
                    IdentityId = user.Id,
                    Name = name ?? email,
                    Email = email
                });
                await _dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync();
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResponse<TokenResult>.Fail("Registration error: " + ex.Message, 500);
            }
        }

        // Additional info
        var profile = _dbContext.Users.FirstOrDefault(u => u.IdentityId == user.Id);
        bool isPetRegistered = false;
        bool isProfileUpdated = false;
        string userName = profile?.Name ?? name ?? email;

        if (profile != null)
        {
            isPetRegistered = _dbContext.UserPets.Any(p => p.UserId == profile.Id);
            isProfileUpdated = isPetRegistered && _dbContext.PetOwnerProfiles.Any(p => p.UserId == profile.Id);
        }

        // Issue JWT
        var tokenResult = new TokenResult
        {
            Token = _jwtTokenService.GenerateToken(user.Id, user.Email, user.UserName),
            IsPetRegistered = isPetRegistered,
            IsProfileUpdated = isProfileUpdated,
            UserName = userName
        };
        return ApiResponse<TokenResult>.Success(tokenResult, "Login successful!", 200);
    }
}
