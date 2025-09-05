using Application.Common.Models;
using Application.Settings;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace Application.Pets.Commands;

public class UpdatePetProfileCommand : IRequest<ApiResponse<long>>
{
    public string? IdentityId { get; set; }
    public long PetId { get; set; }
    //public long PetTypeId { get; set; }
    //public string? CustomPetTypeName { get; set; }
    //public string? CustomPetBreed { get; set; }
    //public string PetFoundAt { get; set; }
    public IFormFile? Image { get; set; }
    public string PetName { get; set; }

    public long? PetBreedId { get; set; }
    public bool IsMixedBreed { get; set; } = false;
    public string? FirstBreed { get; set; }
    public string? SecondBreed { get; set; }
    public string Gender { get; set; }
    public DateTime? DOB { get; set; }
    public long? PetColorId { get; set; }
    public string? MixColors { get; set; }
    public string? Food { get; set; }
    public decimal? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public string? Character { get; set; }

    [ModelBinder(BinderType = typeof(NullableLongBinder))]
    public long? PetFoodId { get; set; }
    public string? CustomFood { get; set; }
    public string? PetUserName { get; set; }
}
