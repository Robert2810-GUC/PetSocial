using Application.Common.Models;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;

namespace Application.Users.Commands;

public class RequestOtpCommandHandler : IRequestHandler<RequestOtpCommand, ApiResponse<string>>
{
    private readonly ApplicationDbContext _db;
    private readonly ISmsSender _smsSender;
    public RequestOtpCommandHandler(ApplicationDbContext db, ISmsSender smsSender)
    {
        _db = db;
        _smsSender = smsSender;
    }

    public async Task<ApiResponse<string>> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        // Validate input, check not already registered
        var userExists = await _db.Users.AnyAsync(x => x.CountryCode == request.CountryCode && x.PhoneNumber == request.PhoneNumber, cancellationToken);
        if (userExists)
            return ApiResponse<string>.Fail("Phone already registered.", 409);

        // Generate OTP
        var otp = new Random().Next(100000, 999999).ToString();
        var entity = new UserOtp
        {
            CountryCode = request.CountryCode,
            PhoneNumber = request.PhoneNumber,
            Otp = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false
        };
        _db.UserOtps.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var to = $"{request.CountryCode}{request.PhoneNumber}";
        await _smsSender.SendSmsAsync(to, $"Your OTP is {otp}");

        return ApiResponse<string>.Success(null, "OTP sent! Validate OTP in 5 minutes before it expires.");
    }

}
