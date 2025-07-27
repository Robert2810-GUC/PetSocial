namespace Domain.Entities;

public class PetType
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string ImagePath { get; set; }
    public int SortOrder { get; set; } = 9999;

    public ICollection<PetBreed>? PetBreeds { get; set; }
}

