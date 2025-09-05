namespace Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(string userId, string email, string role, string? userName = null);
}
