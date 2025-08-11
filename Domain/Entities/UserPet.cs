namespace Domain.Entities;
public class UserPet
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long PetTypeId { get; set; } // PetType
    public string? CustomPetTypeName { get; set; }
    public string? CustomPetBreed { get; set; } 
    public string PetName { get; set; }
    public string PetFoundAt { get; set; }
    public string ImagePath { get; set; }
    public string? Gender { get; set; }
    public DateTime? DOB { get; set; }
    public long? PetBreedId { get; set; }
    public string? Food { get; set; }
    public decimal? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public string? Character { get; set; }
    public long? PetFoodId { get; set; }
    public string? CustomFood { get; set; }


    public User? User { get; set; }
    public PetType? PetType { get; set; }
    public PetBreed? PetBreed { get; set; }
    public PetFood PetFood { get; set; }



    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ICollection<UserPetOtherBreed>? UserPetOtherBreeds { get; set; }
    public ICollection<UserPetColor>? UserPetColors { get; set; }
}

