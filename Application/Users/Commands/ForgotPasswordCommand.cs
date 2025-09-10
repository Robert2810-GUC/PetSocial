using Application.Common.Models;
using MediatR;

namespace Application.Users.Commands;

public class ForgotPasswordCommand : IRequest<ApiResponse<string>>
{
    public string CountryCode { get; set; }
    public string PhoneNumber { get; set; }
    public bool SendToEmail { get; set; }
    public string? Email { get; set; }
}
