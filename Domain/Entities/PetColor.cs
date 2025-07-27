namespace Domain.Entities;
public class PetColor
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int SortOrder { get; set; } = 9999;

    public ICollection<UserPetColor>? UserPetColors { get; set; }
}
