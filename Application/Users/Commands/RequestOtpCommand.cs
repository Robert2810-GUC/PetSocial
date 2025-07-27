using Application.Common.Models;
using MediatR;

namespace Application.Users.Commands;

public class RequestOtpCommand : IRequest<ApiResponse<string>>
{
    public string CountryCode { get; set; }
    public string PhoneNumber { get; set; }
}
