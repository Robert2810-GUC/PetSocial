using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence;

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
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Validate inputs
            if (string.IsNullOrWhiteSpace(request.Email))
                return ApiResponse<TokenResult>.Fail("Email is required.", 400);
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                return ApiResponse<TokenResult>.Fail("Phone is required.", 400);
            if (string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<TokenResult>.Fail("Name is required.", 400);
            if (string.IsNullOrWhiteSpace(request.Password))
                return ApiResponse<TokenResult>.Fail("Password is required.", 400);

            // 2. Check uniqueness
            var emailExists = await _userManager.FindByEmailAsync(request.Email) != null;
            var phoneExists = _userManager.Users.Any(u => u.PhoneNumber == request.PhoneNumber);
            if (emailExists)
                return ApiResponse<TokenResult>.Fail("Email already registered.", 409);
            if (phoneExists)
                return ApiResponse<TokenResult>.Fail("Phone already registered.", 409);

            var otpEntity = await _dbContext.UserOtps
                .Where(x => x.CountryCode == request.CountryCode && x.PhoneNumber == request.PhoneNumber && x.Otp == request.Otp && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(x => x.ExpiresAt)
                .FirstOrDefaultAsync();

            if (otpEntity == null)
                return ApiResponse<TokenResult>.Fail("Invalid or expired OTP.", 400);

            otpEntity.IsUsed = true;

            // 3. Create Identity User
            var identityUser = new IdentityUser
            {
                UserName = $"{request.CountryCode}{request.PhoneNumber}",
                Email = request.Email,
                PhoneNumber = $"{request.CountryCode}{request.PhoneNumber}"
            };

            var result = await _userManager.CreateAsync(identityUser, request.Password);
            if (!result.Succeeded)
                return ApiResponse<TokenResult>.Fail(
                    "Registration failed: " + string.Join(", ", result.Errors.Select(e => e.Description)), 400);



            // Now you can parallelize
            var addUserLoginTask = Task.Run(async () =>
            {
                var userProfile = new User
                {
                    IdentityId = identityUser.Id,
                    Name = request.Name,
                    PhoneNumber = request.PhoneNumber,
                    CountryCode = request.CountryCode,
                    Email = request.Email
                };
                _dbContext.Users.Add(userProfile);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var userLogins = new UserLogin
                {
                    UserId = userProfile.Id,
                    Password = request.Password,
                };
                _dbContext.UserLogins.Add(userLogins);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }, cancellationToken);

            var generateTokenTask = Task.Run(() =>
            {
                return _jwtTokenService.GenerateToken(identityUser.Id, identityUser.Email, identityUser.UserName);
            });

            await Task.WhenAll(addUserLoginTask, generateTokenTask);

            await transaction.CommitAsync(cancellationToken);

            var tokenResult = new TokenResult { Token = generateTokenTask.Result };

            return ApiResponse<TokenResult>.Success(tokenResult, "User registered successfully!", 201);

        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ApiResponse<TokenResult>.Fail("Registration error: " + ex.Message, 500);
        }
    }
}
