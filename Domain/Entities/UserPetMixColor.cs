namespace Domain.Entities;
public class UserPetMixColor
{
    public long Id { get; set; }
    public long UserPetColorId { get; set; }
    public string Color { get; set; }
    public decimal Percentage { get; set; }

    public UserPetColor? UserPetColor { get; set; }
}

