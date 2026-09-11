using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MuranoApp.DTOs;

namespace MuranoApp.Services
{
    // Upload de imagem de produto passa por aqui (não direto do navegador),
    // pra manter a API key/secret só no backend e a rota protegida pelo
    // mesmo login que já cobre o resto do painel.
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        private static readonly string[] AllowedContentTypes =
        {
            "image/jpeg", "image/png", "image/webp", "image/gif"
        };

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrWhiteSpace(cloudName) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new InvalidOperationException("Configuração da Cloudinary ausente.");
            }

            _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        }

        public async Task<UploadImageResponseDTO> UploadProductImageAsync(IFormFile file)
        {
            if (file.Length == 0)
                throw new ArgumentException("File is empty.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException("Image must be 5 MB or smaller.");

            if (!AllowedContentTypes.Contains(file.ContentType))
                throw new ArgumentException("Image must be JPEG, PNG, WEBP or GIF.");

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "murano-produtos",
                // Redimensiona no upload pra não guardar fotos gigantes de
                // celular sem necessidade — o card do produto nunca precisa
                // de mais que isso.
                Transformation = new Transformation().Width(1200).Height(1200).Crop("limit")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

            return new UploadImageResponseDTO
            {
                ImagemUrl = result.SecureUrl.ToString(),
                ImagemPublicId = result.PublicId
            };
        }

        public async Task DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return;

            // Invalidate=true pede pra CDN também descartar a cópia em cache
            // nas edges, não só apagar do armazenamento de origem — sem
            // isso a URL antiga pode continuar servindo a imagem já
            // "excluída" por um tempo.
            var deleteParams = new DeletionParams(publicId) { Invalidate = true };
            await _cloudinary.DestroyAsync(deleteParams);
        }
    }
}
