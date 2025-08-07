using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("PetBusinessProfile")]
public class PetBusinessProfile
{
    public long Id { get; set; }
    public long UserId { get; set; }

    public string BusinessName { get; set; }
    public string Address { get; set; }
    public string WebsiteUrl { get; set; }
    public User User { get; set; }
}
