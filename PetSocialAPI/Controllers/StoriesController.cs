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
using Application.Common.Models;

namespace PetSocialAPI.Controllers;
public sealed class CreateStoryForm
{
    public long PetId { get; set; }
    public IFormFile Media { get; set; } = default!;
    public string MediaType { get; set; } = default!;
    public string? Caption { get; set; }
}

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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateStoryForm form)
    {
        if (form.Media is null)
            return StatusCode(400, ApiResponse<string>.Fail("Media file is required.", 400));
        if (string.IsNullOrWhiteSpace(form.MediaType))
            return StatusCode(400, ApiResponse<string>.Fail("Media type is required.", 400));

        var upload = await _imageService.UploadImageAsync(form.Media);

        var story = new PetStory
        {
            PetId = form.PetId,
            MediaUrl = upload.Url,
            MediaType = form.MediaType,
            Caption = form.Caption
        };

        _db.PetStories.Add(story);
        await _db.SaveChangesAsync();

        var result = new { story.Id, story.MediaUrl, story.Caption };
        return StatusCode(200, ApiResponse<object>.Success(result));
    }
    [HttpGet("{petId}")]
    public async Task<IActionResult> GetStories(long petId)
    {
        try
        {
            var now = DateTime.UtcNow;

            var stories = await _db.PetStories
                .Where(s => s.PetId != petId && s.ExpiresAt > now)
                .Include(s => s.Views)
                .ToListAsync();

            var petIds = stories.Select(s => s.PetId).Distinct().ToList();
            var pets = await _db.UserPets
                .Where(p => petIds.Contains(p.Id))
                .Select(p => new { p.Id, Username = p.PetName })
                .ToListAsync();

            var result = pets.Select(p =>
            {
                var petStories = stories.Where(s => s.PetId == p.Id).Select(s => new
                {
                    storyId = s.Id,
                    mediaUrl = s.MediaUrl,
                    mediaType = s.MediaType,
                    caption = s.Caption,
                    isSeen = s.Views.Any(v => v.ViewerPetId == petId)
                }).ToList();

                return new
                {
                    petId = p.Id,
                    username = p.Username,
                    isStorySeen = petStories.All(ps => ps.isSeen),
                    stories = petStories
                };
            }).ToList();

            return StatusCode(200, ApiResponse<object>.Success(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail(ex.Message, 500));
        }
    }
    [HttpGet("MyStories/{petId}")]
    public async Task<IActionResult> MyStories(long petId)
    {
        try
        {
            var now = DateTime.UtcNow;
            var stories = await _db.PetStories
                .Where(s => s.PetId == petId && s.ExpiresAt > now)
                .Include(s => s.Views)
                .Include(s => s.Likes)
                .Include(s => s.Comments)
                .ToListAsync();

            return StatusCode(200, ApiResponse<object>.Success(stories));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail(ex.Message, 500));
        }
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
        return StatusCode(200, ApiResponse<object>.Success(null));
    }

    [HttpPost("{id}/like")]
    public async Task<IActionResult> LikeStory(long id, [FromForm] long likerPetId)
    {
        var isVerified = await _db.PetDonations.AnyAsync(d => d.PetId == likerPetId);
        if (!isVerified)
            return StatusCode(400, ApiResponse<string>.Fail("Pet is not verified.", 400));

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
        return StatusCode(200, ApiResponse<object>.Success(null));
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
            return StatusCode(400, ApiResponse<string>.Fail("Comment text is required.", 400));
        var isVerified = await _db.PetDonations.AnyAsync(d => d.PetId == request.CommenterPetId);
        if (!isVerified)
            return StatusCode(400, ApiResponse<string>.Fail("Pet is not verified.", 400));
        var comment = new PetStoryComment
        {
            StoryId = id,
            CommenterPetId = request.CommenterPetId,
            Text = request.Text
        };
        _db.PetStoryComments.Add(comment);
        await _db.SaveChangesAsync();
        return StatusCode(200, ApiResponse<object>.Success(comment));
    }

    [HttpGet("{id}/views")]
    public async Task<IActionResult> GetViews(long id)
    {
        var views = await _db.PetStoryViews
            .Where(v => v.StoryId == id)
            .Join(
                _db.UserPets,
                v => v.ViewerPetId,
                p => p.Id,
                (v, p) => new
                {
                    v.Id,
                    v.StoryId,
                    v.ViewerPetId,
                    v.ViewedAt,
                    PetName = p.PetName,
                    ProfilePic = p.ImagePath,
                    ProfileLink = $"/api/pets/{p.Id}"
                })
            .ToListAsync();

        return StatusCode(200, ApiResponse<object>.Success(views));
    }

    [HttpGet("{id}/likes")]
    public async Task<IActionResult> GetLikes(long id)
    {
        var likes = await _db.PetStoryLikes
            .Where(l => l.StoryId == id)
            .Join(
                _db.UserPets,
                l => l.LikerPetId,
                p => p.Id,
                (l, p) => new
                {
                    l.Id,
                    l.StoryId,
                    l.LikerPetId,
                    l.LikedAt,
                    PetName = p.PetName,
                    ProfilePic = p.ImagePath,
                    ProfileLink = $"/api/pets/{p.Id}"
                })
            .ToListAsync();

        return StatusCode(200, ApiResponse<object>.Success(likes));
    }

    [HttpGet("{id}/comments")]
    public async Task<IActionResult> GetComments(long id)
    {
        var comments = await _db.PetStoryComments
            .Where(c => c.StoryId == id)
            .Join(
                _db.UserPets,
                c => c.CommenterPetId,
                p => p.Id,
                (c, p) => new
                {
                    c.Id,
                    c.StoryId,
                    c.CommenterPetId,
                    c.Text,
                    c.CreatedAt,
                    PetName = p.PetName,
                    ProfilePic = p.ImagePath,
                    ProfileLink = $"/api/pets/{p.Id}"
                })
            .ToListAsync();

        return StatusCode(200, ApiResponse<object>.Success(comments));
    }
}
