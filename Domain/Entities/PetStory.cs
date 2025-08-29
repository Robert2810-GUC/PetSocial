using System;
using System.Collections.Generic;

namespace Domain.Entities;

public class PetStory
{
    public long Id { get; set; }
    public long PetId { get; set; }
    public string MediaUrl { get; set; }
    public string MediaType { get; set; } // "image" or "video"
    public string? Caption { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(1);

    public ICollection<PetStoryView>? Views { get; set; }
    public ICollection<PetStoryLike>? Likes { get; set; }
    public ICollection<PetStoryComment>? Comments { get; set; }
}
