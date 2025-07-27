namespace Application.Common.Models;

public class LoginRequest
{
    public string Email { get; set; }  // optional
    public string Phone { get; set; }  // optional
    public string Password { get; set; }  // required
}
