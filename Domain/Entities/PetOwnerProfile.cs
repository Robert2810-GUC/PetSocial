using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("PetOwnerProfile")]

public class PetOwnerProfile
{
    public long Id { get; set; }
    public long UserId { get; set; }

    public string Gender { get; set; }
    public DateTime? DOB { get; set; }
    public string ImagePath { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public User User { get; set; }
}
