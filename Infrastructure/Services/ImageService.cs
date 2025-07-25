using Application.Common.Interfaces; 
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;
        public ImageService(IConfiguration config)
        {
            var settings = config.GetSection("Cloudinary");
            Account account = new Account(
                settings["CloudName"],
                settings["ApiKey"],
                settings["ApiSecret"]);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream)
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
            }
            throw new Exception("Image upload failed");
        }

        public async Task DeleteImageAsync(string publicId)
        {
            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);
        }
    }
}
