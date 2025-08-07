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
    
    public User User { get; set; }
}
