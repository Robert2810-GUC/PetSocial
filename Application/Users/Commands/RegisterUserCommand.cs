using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Users.Commands;

public class RegisterUserCommand : IRequest<string> // Returns JWT token on success
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }      // Also used as username
    public string Gender { get; set; }
    public DateTime DOB { get; set; }
    public string Password { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public IFormFile Image { get; set; }
}
