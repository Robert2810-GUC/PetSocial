
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;
public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string CountryCode { get; set; }
    public string PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string IdentityId { get; set; }
    public long UserTypeId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public ICollection<UserPet>? UserPets { get; set; }

    public UserType UserType { get; set; }
    public PetOwnerProfile PetOwnerProfile { get; set; }
    public PetBusinessProfile PetBusinessProfile { get; set; }
    public ContentCreatorProfile ContentCreatorProfile { get; set; }
}

