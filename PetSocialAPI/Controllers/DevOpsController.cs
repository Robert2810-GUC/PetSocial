using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Models;

namespace PetSocialAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevOpsController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager) : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly UserManager<IdentityUser> _userManager = userManager;

    [HttpPost("seed-roles")]
    public async Task<IActionResult> SeedRoles([FromHeader(Name = "X-Admin-Secret")] string secret)
    {
        if (secret != "chotulaal@1234")
            return StatusCode(401, ApiResponse<string>.Fail("Unauthorized", 401));

        string[] roles = { "Admin", "User" };
        List<string> created = new();
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var res = await _roleManager.CreateAsync(new IdentityRole(role));
                if (res.Succeeded) created.Add(role);
            }
        }
        return StatusCode(200, ApiResponse<object>.Success(new { created }));
    }
    [HttpPost("seed-admin")]
    public async Task<IActionResult> SeedAdmin([FromHeader(Name = "X-Admin-Secret")] string secret)
    {
        if (secret != "chotulaal@1234")
            return StatusCode(401, ApiResponse<string>.Fail("Unauthorized", 401));

        var identityUser = new IdentityUser
        {
            UserName = "admin@petsocial.com",
            Email = "admin@petsocial.com"
        };

        var result = await _userManager.CreateAsync(identityUser, "inos@1234");
        if (!result.Succeeded)
            return StatusCode(400, ApiResponse<string>.Fail(
                "Registration failed: " + string.Join(", ", result.Errors.Select(e => e.Description)), 400));
        var roleResult = await _userManager.AddToRoleAsync(identityUser, "Admin");
        if (!roleResult.Succeeded)
            return StatusCode(400, ApiResponse<string>.Fail(
                "Failed to assign role: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)), 400));

        return StatusCode(200, ApiResponse<string>.Success("Admin Created"));
    }
}
