using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Users.Commands;
public class ValidateUniqueCommandHandler : IRequestHandler<ValidateUniqueCommand, ApiResponse<string>>
{
    private readonly ApplicationDbContext _db;
    public ValidateUniqueCommandHandler(ApplicationDbContext db) => _db = db;

    public async Task<ApiResponse<string>> Handle(ValidateUniqueCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Email))
        {
            var emailExists = await _db.Users.AnyAsync(x => x.Email == request.Email, cancellationToken);
            if (emailExists)
                return ApiResponse<string>.Fail("Email already registered.", 409);
        }
        var phoneExists = await _db.Users.AnyAsync(x => x.CountryCode == request.CountryCode && x.PhoneNumber == request.PhoneNumber, cancellationToken);
        if (phoneExists)
            return ApiResponse<string>.Fail("Phone number already registered.", 409);

        return ApiResponse<string>.Success(null, "Unique", 200);
    }
}
