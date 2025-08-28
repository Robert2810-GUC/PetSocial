using System;

namespace Domain.Entities;

public class PetStoryComment
{
    public long Id { get; set; }
    public long StoryId { get; set; }
    public long CommenterPetId { get; set; }
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PetStory? Story { get; set; }
}
