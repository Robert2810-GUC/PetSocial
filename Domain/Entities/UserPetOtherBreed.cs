namespace Domain.Entities;

public class UserPetOtherBreed
{
    public long Id { get; set; }
    public long UserPetId { get; set; }
    public string FirstBreed { get; set; }
    public string SecondBreed { get; set; }

    public UserPet? UserPet { get; set; }
}

