using Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Businesses.Commands;

public class UpdatePetBusinessProfileCommand : IRequest<ApiResponse<long>>
{
    public string IdentityId { get; set; }
    public string BusinessName { get; set; }
    public string? OwnerName { get; set; }
    public DateTime? BusinessStartDate { get; set; }
    public string Address { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string EINorSSN { get; set; }
    public int? NumberOfEmployees { get; set; }
    public string? BusinessType { get; set; }
    public string? GoogleRatingLink { get; set; }
    public IFormFile? BannerImage { get; set; }
    public IFormFile? ProfileImage { get; set; }
}
