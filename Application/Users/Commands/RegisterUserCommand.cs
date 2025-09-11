using MediatR;
using Application.Common.Models;
using Domain.Enums;

namespace Application.Users.Commands;

public class RegisterUserCommand : IRequest<ApiResponse<TokenResult>>
{
    public string Name { get; set; }
    public string? Email { get; set; }
    public string CountryCode { get; set; }
    public string PhoneNumber { get; set; }
    public string Otp { get; set; }
    public string Password { get; set; }

    public bool? IsOtpVerified = false;
    public long UserTypeId { get; set; } = (int)UserTypeEnum.PetOwner;
}
