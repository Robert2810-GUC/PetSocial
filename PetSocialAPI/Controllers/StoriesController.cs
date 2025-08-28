using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Domain.Entities;
using Application.Common.Interfaces;

namespace PetSocialAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IImageService _imageService;

    public StoriesController(ApplicationDbContext db, IImageService imageService)
    {
        _db = db;
        _imageService = imageService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] long petId, [FromForm] IFormFile media, [FromForm] string mediaType)
    {
        if (media == null)
            return BadRequest("Media file is required.");
        if (string.IsNullOrEmpty(mediaType))
            return BadRequest("Media type is required.");

        var upload = await _imageService.UploadImageAsync(media);
        var story = new PetStory
        {
            PetId = petId,
            MediaUrl = upload.Url,
            MediaType = mediaType
        };
        _db.PetStories.Add(story);
        await _db.SaveChangesAsync();
        return Ok(new { story.Id, story.MediaUrl });
    }

    [HttpGet("{petId}")]
    public async Task<IActionResult> GetStories(long petId)
    {
        var now = DateTime.UtcNow;
        var stories = await _db.PetStories
            .Where(s => s.PetId == petId && s.ExpiresAt > now)
            .Include(s => s.Views)
            .Include(s => s.Likes)
            .Include(s => s.Comments)
            .ToListAsync();
        return Ok(stories);
    }

    [HttpPost("{id}/view")]
    public async Task<IActionResult> ViewStory(long id, [FromForm] long viewerPetId)
    {
        var exists = await _db.PetStoryViews.AnyAsync(v => v.StoryId == id && v.ViewerPetId == viewerPetId);
        if (!exists)
        {
            _db.PetStoryViews.Add(new PetStoryView { StoryId = id, ViewerPetId = viewerPetId });
            await _db.SaveChangesAsync();
        }
        return Ok();
    }

    [HttpPost("{id}/like")]
    public async Task<IActionResult> LikeStory(long id, [FromForm] long likerPetId)
    {
        var like = await _db.PetStoryLikes.FirstOrDefaultAsync(l => l.StoryId == id && l.LikerPetId == likerPetId);
        if (like == null)
        {
            _db.PetStoryLikes.Add(new PetStoryLike { StoryId = id, LikerPetId = likerPetId });
        }
        else
        {
            _db.PetStoryLikes.Remove(like);
        }
        await _db.SaveChangesAsync();
        return Ok();
    }

    public class CommentRequest
    {
        public long CommenterPetId { get; set; }
        public string Text { get; set; }
    }

    [HttpPost("{id}/comment")]
    public async Task<IActionResult> CommentStory(long id, [FromBody] CommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Comment text is required.");
        var comment = new PetStoryComment
        {
            StoryId = id,
            CommenterPetId = request.CommenterPetId,
            Text = request.Text
        };
        _db.PetStoryComments.Add(comment);
        await _db.SaveChangesAsync();
        return Ok(comment);
    }

    [HttpGet("{id}/views")]
    public async Task<IActionResult> GetViews(long id)
    {
        var views = await _db.PetStoryViews.Where(v => v.StoryId == id).ToListAsync();
        return Ok(views);
    }

    [HttpGet("{id}/likes")]
    public async Task<IActionResult> GetLikes(long id)
    {
        var likes = await _db.PetStoryLikes.Where(l => l.StoryId == id).ToListAsync();
        return Ok(likes);
    }

    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetComments(long id)
    {
        var comments = await _db.PetStoryComments.Where(c => c.StoryId == id).ToListAsync();
        return Ok(comments);
    }
}
