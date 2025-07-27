using Application.Common.Models;
using MediatR;

namespace Application.Users.Commands;

public class LoginCommand : IRequest<ApiResponse<TokenResult>>
{
    public string? Email { get; set; }
    public string? CountryCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Password { get; set; }
}