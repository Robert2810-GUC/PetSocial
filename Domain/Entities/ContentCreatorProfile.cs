using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

[Table("ContentCreatorProfile")]

public class ContentCreatorProfile
{
    public long Id { get; set; }
    public long UserId { get; set; }

    public string ChannelName { get; set; }
    public int FollowersCount { get; set; }


    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    // Add more fields as needed

    // Navigation property
    public User User { get; set; }
}
