
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;
public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string CountryCode { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string? Gender { get; set; }
    public DateTime? DOB { get; set; }
    public string? ImagePath { get; set; }
    public string IdentityId { get; set; } 

    public ICollection<UserPet>? UserPets { get; set; }
}
