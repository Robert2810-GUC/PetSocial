using Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System.Linq;

namespace Application.Users.Commands;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ApiResponse<string>>
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _db;

    public ResetPasswordCommandHandler(UserManager<IdentityUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<ApiResponse<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var country = request.CountryCode?.Trim();
        var phone = request.PhoneNumber?.Trim();
        if (string.IsNullOrWhiteSpace(country) ||
            string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(request.Otp) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ApiResponse<string>.Fail("Country code, phone number, OTP and new password are required.", 400);
        }

        var phoneKey = $"{country}{phone}";
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneKey, cancellationToken);
        if (user == null)
            return ApiResponse<string>.Fail("User not found.", 404);

        var otpEntity = await _db.UserOtps
            .Where(x => x.CountryCode == country &&
                        x.PhoneNumber == phone &&
                        x.Otp == request.Otp &&
                        !x.IsUsed &&
                        x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (otpEntity == null)
            return ApiResponse<string>.Fail("Invalid or expired OTP.", 400);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
        {
            var error = string.Join(";", result.Errors.Select(e => e.Description));
            return ApiResponse<string>.Fail(error, 400);
        }

        otpEntity.IsUsed = true;
        await _db.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Success(null, "Password reset successful.");
    }
}

