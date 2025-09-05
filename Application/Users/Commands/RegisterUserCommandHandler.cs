using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence;
using System.Linq;

namespace Application.Users.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, ApiResponse<TokenResult>>
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _config;
    private readonly IJwtTokenService _jwtTokenService;


    public RegisterUserCommandHandler(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext dbContext,
        IConfiguration config,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _config = config;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<ApiResponse<TokenResult>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        var rawPhone = request.PhoneNumber?.Trim();
        var countryCode = request.CountryCode?.Trim();
        var password = request.Password;
        var phoneKey = $"{countryCode}{rawPhone}";

        if (string.IsNullOrWhiteSpace(rawPhone)) return ApiResponse<TokenResult>.Fail("Phone is required.", 400);
        if (string.IsNullOrWhiteSpace(name)) return ApiResponse<TokenResult>.Fail("Name is required.", 400);
        if (string.IsNullOrWhiteSpace(password)) return ApiResponse<TokenResult>.Fail("Password is required.", 400);
        if (string.IsNullOrWhiteSpace(countryCode)) return ApiResponse<TokenResult>.Fail("Country code is required.", 400);


        var phoneExists = await _userManager.Users
            .AsNoTracking()
            .AnyAsync(u => u.PhoneNumber == phoneKey, cancellationToken);
        if (phoneExists)
            return ApiResponse<TokenResult>.Fail("Phone already registered.", 409);

        var otpEntity = await _dbContext.UserOtps
            .Where(x => x.CountryCode == countryCode
                     && x.PhoneNumber == rawPhone
                     && x.Otp == request.Otp
                     && !x.IsUsed
                     && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otpEntity == null)
            return ApiResponse<TokenResult>.Fail("Invalid or expired OTP.", 400);

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var identityUser = new IdentityUser
            {
                UserName = phoneKey,
                PhoneNumber = phoneKey
            };

            var createRes = await _userManager.CreateAsync(identityUser, password);
            if (!createRes.Succeeded)
            {
                var msg = string.Join(", ", createRes.Errors.Select(e => e.Description));
                return ApiResponse<TokenResult>.Fail("Registration failed: " + msg, 400);
            }

            const string assignedRole = "User";
            var roleRes = await _userManager.AddToRoleAsync(identityUser, assignedRole);
            if (!roleRes.Succeeded)
            {
                var msg = string.Join(", ", roleRes.Errors.Select(e => e.Description));
                return ApiResponse<TokenResult>.Fail("Registration failed: " + msg, 400);
            }

            otpEntity.IsUsed = true;

            var userProfile = new User
            {
                IdentityId = identityUser.Id,
                Name = name!,
                PhoneNumber = rawPhone!,
                CountryCode = countryCode!,
                UserTypeId = request.UserTypeId
            };
            _dbContext.Users.Add(userProfile);
            await _dbContext.SaveChangesAsync(cancellationToken);


            var userLogin = new UserLogin
            {
                UserId = userProfile.Id,
                Password = password
            };
            _dbContext.UserLogins.Add(userLogin);
            await _dbContext.SaveChangesAsync(cancellationToken);


            var jwt = _jwtTokenService.GenerateToken(
                identityUser.Id,
                identityUser.Email ?? string.Empty,
                assignedRole,
                identityUser.UserName
            );

            await tx.CommitAsync(cancellationToken);

            var tokenResult = new TokenResult
            {
                Token = jwt,
                IsPetRegistered = false,
                IsProfileUpdated = false,
                UserName = name!
            };

            return ApiResponse<TokenResult>.Success(tokenResult, "User registered successfully!", 201);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return ApiResponse<TokenResult>.Fail("Registration error: " + ex.Message, 500);
        }
    }
}
