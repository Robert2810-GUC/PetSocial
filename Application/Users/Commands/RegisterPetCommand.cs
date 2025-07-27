using Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace Application.Pets.Commands;

public class RegisterPetCommand : IRequest<ApiResponse<long>> // returns PetId
{
    public string? IdentityId { get; set; } 
    public long PetTypeId { get; set; } 
    public string CustomPetTypeName { get; set; }
    public string CustomPetBreed { get; set; }
    public string PetFoundAt { get; set; }    
    public IFormFile Image { get; set; }     
    public string PetName { get; set; }

    public long PetBreedId { get; set; }   
    public bool IsMixedBreed {  get; set; }=false;
    public string? FirstBreed { get; set; } 
    public string? SecondBreed { get; set; }  
    public string Gender { get; set; }
    public DateTime DOB { get; set; }
    public long PetColorId { get; set; }
    public string? MixColors { get;set; }
    public string Food { get; set; }
    public decimal Weight { get; set; }
    public string WeightUnit { get; set; }
    public string Character { get; set; }
}

public class PetColorDto
{
    public long PetColorId { get; set; } // "Mix Color" if ID=9999 or reserved
    public List<MixColorDto> MixColors { get; set; } = new();
}

public class MixColorDto
{
    [JsonPropertyName("color")]
    public string Color { get; set; }
    [JsonPropertyName("percentage")]
    public decimal Percentage { get; set; }
}
