
namespace Domain.Entities;

public class UserLogin
{
    public long Id { get; set; }
    public long UserId { get; set; }//its a foreign key to User table
    public string Password { get; set; }
    public User? User { get; set; }

}
