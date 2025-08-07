using MediatR;
using Application.Common.Models;
using Microsoft.AspNetCore.Http;

namespace Application.Users.Commands;

public class OtherUserInfoCommand : IRequest<ApiResponse<TokenResult>>
{
    public string? IdentityId { get; set; } 
    public string Gender { get; set; } 
    public string Email { get; set; } 
    public IFormFile Image { get; set; }
    public DateTime DOB { get; set; }
}
