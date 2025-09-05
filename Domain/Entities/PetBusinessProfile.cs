namespace Domain.Entities;

public class PetBusinessProfile
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string BusinessName { get; set; }
    public string? OwnerName { get; set; }
    public DateTime? BusinessStartDate { get; set; }
    public string Address { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string SecurityNumber { get; set; }
    public string SecurityType { get; set; }
    public int? NumberOfEmployees { get; set; }
    public string? BusinessType { get; set; }
    public double? GoogleRating { get; set; }
    public string? GoogleRatingLink { get; set; }
    public string? BannerImagePath { get; set; }
    public string? ProfileImagePath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
