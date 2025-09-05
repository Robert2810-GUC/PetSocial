using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Persistence;
using Application.Common.Models;

namespace PetSocialAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DonationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DonationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    public class DonationRequest
    {
        public long PetId { get; set; }
        public decimal Amount { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Donate([FromBody] DonationRequest request)
    {
        if (request.Amount <= 0)
            return StatusCode(400, ApiResponse<string>.Fail("Amount must be greater than zero.", 400));

        var donation = new PetDonation
        {
            PetId = request.PetId,
            Amount = request.Amount
        };

        _db.PetDonations.Add(donation);
        await _db.SaveChangesAsync();
        return StatusCode(200, ApiResponse<PetDonation>.Success(donation));
    }
}
