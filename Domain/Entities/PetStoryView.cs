using System;

namespace Domain.Entities;

public class PetStoryView
{
    public long Id { get; set; }
    public long StoryId { get; set; }
    public long ViewerPetId { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    public PetStory? Story { get; set; }
}
