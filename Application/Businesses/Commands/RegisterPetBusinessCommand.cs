using Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.Businesses.Commands;

public class RegisterPetBusinessCommand : IRequest<ApiResponse<long>>
{
    public string? IdentityId { get; set; }
    [Required]
    public string BusinessName { get; set; }
    public string? OwnerName { get; set; }
    public DateTime? BusinessStartDate { get; set; }
    public string? Address { get; set; }
    [Required]
    public string PhoneNumber { get; set; }
    [Required]
    public string Email { get; set; }
    public string? SecurityNumber { get; set; }
    public string? SecurityType { get; set; }
    public int? NumberOfEmployees { get; set; }
    public string? BusinessType { get; set; }
    public string? ServicesOffered { get; set; }
    public bool? HasParking { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? PaymentMethods { get; set; }
    public string? GoogleRatingLink { get; set; }
    public IFormFile? BannerImage { get; set; }
    public IFormFile? ProfileImage { get; set; }
}
