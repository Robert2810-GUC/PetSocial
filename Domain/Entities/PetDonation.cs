using System;

namespace Domain.Entities;

public class PetDonation
{
    public long Id { get; set; }
    public long PetId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DonatedAt { get; set; } = DateTime.UtcNow;

    public UserPet? Pet { get; set; }
}
