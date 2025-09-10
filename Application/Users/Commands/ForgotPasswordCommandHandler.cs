using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Users.Commands;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<string>>
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _db;

    public ForgotPasswordCommandHandler(UserManager<IdentityUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<ApiResponse<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var country = request.CountryCode?.Trim();
        var phone = request.PhoneNumber?.Trim();
        if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(phone))
            return ApiResponse<string>.Fail("Country code and phone number are required.", 400);

        var phoneKey = $"{country}{phone}";
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneKey, cancellationToken);
        if (user == null)
            return ApiResponse<string>.Fail("User not found.", 404);

        var otp = new Random().Next(100000, 999999).ToString();
        var entity = new UserOtp
        {
            CountryCode = country!,
            PhoneNumber = phone!,
            Otp = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false
        };
        _db.UserOtps.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        if (request.SendToEmail)
        {
            var email = request.Email ?? user.Email;
            if (string.IsNullOrWhiteSpace(email))
                return ApiResponse<string>.Fail("Email is required to send OTP.", 400);
            //await _emailSender.SendEmailAsync(email, "Reset Password", $"Your OTP is {otp}");
        }
        else
        {
            var to = phoneKey;
            //await _smsSender.SendSmsAsync(to, $"Your OTP is {otp}");
        }

        return ApiResponse<string>.Success(otp, "OTP sent! Validate OTP in 5 minutes before it expires.");
    }
}

