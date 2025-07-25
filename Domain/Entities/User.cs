
namespace Domain.Entities;

public class User
{
    public long Id { get; set; }
    public string IdentityId { get; set; } // Link to Identity's AspNetUsers.Id (string GUID)
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Gender { get; set; }
    public DateTime DOB { get; set; }
    public string ImagePath { get; set; }
}
