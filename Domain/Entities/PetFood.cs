namespace Domain.Entities;
public class PetFood
{
    public long Id { get; set; }
    public string Name { get; set; }

    public int SortOrder { get; set; } = 9999;
}
