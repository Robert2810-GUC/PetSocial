namespace Domain.Entities;

public class PetBreed
{
    public long Id { get; set; }
    public long PetTypeID { get; set; }
    public string Name { get; set; }
    public int SortOrder { get; set; } = 9999;

    public PetType? PetType { get; set; }
    public ICollection<UserPet>? UserPets { get; set; }
}
