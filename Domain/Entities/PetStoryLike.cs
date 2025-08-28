using System;

namespace Domain.Entities;

public class PetStoryLike
{
    public long Id { get; set; }
    public long StoryId { get; set; }
    public long LikerPetId { get; set; }
    public DateTime LikedAt { get; set; } = DateTime.UtcNow;

    public PetStory? Story { get; set; }
}
