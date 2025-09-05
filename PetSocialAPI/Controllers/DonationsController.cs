using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Persistence;

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
            return BadRequest("Amount must be greater than zero.");

        var donation = new PetDonation
        {
            PetId = request.PetId,
            Amount = request.Amount
        };

        _db.PetDonations.Add(donation);
        await _db.SaveChangesAsync();
        return Ok(donation);
    }
}
