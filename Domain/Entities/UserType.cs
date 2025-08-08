namespace Domain.Entities;

public class UserType
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string ImagePath { get; set; }
    public string Description { get; set; }

    public ICollection<User> Users { get; set; }
}