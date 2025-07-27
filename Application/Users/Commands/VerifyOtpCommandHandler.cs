using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Users.Commands;

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, ApiResponse<string>>
{
    private readonly ApplicationDbContext _db;
    public VerifyOtpCommandHandler(ApplicationDbContext db) => _db = db;

    public async Task<ApiResponse<string>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var otpEntity = await _db.UserOtps
            .Where(x => x.CountryCode == request.CountryCode
                     && x.PhoneNumber == request.PhoneNumber
                     && x.Otp == request.Otp
                     && !x.IsUsed
                     && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.ExpiresAt)
            .FirstOrDefaultAsync();
        if (otpEntity == null)
            return ApiResponse<string>.Fail("Invalid or expired OTP", 400);

        otpEntity.IsUsed = true;
        await _db.SaveChangesAsync();
        return ApiResponse<string>.Success(null, "OTP verified!");
    }
}
