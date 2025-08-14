using Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Users.Commands;

public class UpdateUserProfileCommand : IRequest<ApiResponse<TokenResult>>
{
    public string? IdentityId { get; set; }
    public string Name { get; set; }
    public string Gender { get; set; }
    public string? Bio { get; set; }
    public IFormFile? Image { get; set; }
}
