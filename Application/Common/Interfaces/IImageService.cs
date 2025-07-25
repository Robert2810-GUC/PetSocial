using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IImageService
    {
        Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file);
        Task DeleteImageAsync(string publicId);
    }
}
