using Application.Common.Models;
using MediatR;

namespace Application.Users.Commands;

public class VerifyOtpCommand : IRequest<ApiResponse<string>>
{
    public string CountryCode { get; set; }
    public string PhoneNumber { get; set; }
    public string Otp { get; set; }
}
