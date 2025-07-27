namespace Domain.Entities;
public class UserPetColor
{
    public long Id { get; set; }
    public long UserPetId { get; set; }
    public long PetColorId { get; set; }

    public UserPet? UserPet { get; set; }
    public PetColor? PetColor { get; set; }
    public ICollection<UserPetMixColor> UserPetMixColors { get; set; }
}

