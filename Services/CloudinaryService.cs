using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SwiftFill.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var account = new Account(
                configuration["CLOUDINARY_CLOUD_NAME"],
                configuration["CLOUDINARY_API_KEY"],
                configuration["CLOUDINARY_API_SECRET"]
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folder = "swiftfill/pod")
        {
            if (file == null || file.Length == 0) return null;

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult?.SecureUrl?.ToString();
        }
    }
}
