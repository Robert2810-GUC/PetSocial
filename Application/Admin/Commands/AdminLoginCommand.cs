using Application.Common.Models;
using MediatR;

namespace Application.Admin.Commands;

internal class AdminLoginCommand : IRequest<ApiResponse<TokenResult>>
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}
