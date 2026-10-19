using LinkUpPro.Application.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace LinkUpPro.Shared.Services
{
    // Guarda las imágenes en wwwroot/uploads/{carpeta}/ con un nombre generado internamente.
    // Enunciado: solo .jpg, .jpeg, .png y .webp, máximo 5 MB, validando el tipo real del archivo.
    public class ImageStorageService : IImageStorageService
    {
        private const long MaxBytes = 5 * 1024 * 1024;
        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly IWebHostEnvironment _environment;

        public ImageStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public bool IsValidImage(IFormFile file)
        {
            if (file is null || file.Length == 0 || file.Length > MaxBytes)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
                return false;

            // Tipo real: se comparan los primeros bytes del archivo ("firma"),
            // así un .exe renombrado a .jpg no pasa la validación.
            var header = new byte[12];
            using (var stream = file.OpenReadStream())
            {
                if (stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false) < header.Length)
                    return false;
            }

            return extension switch
            {
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                       && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
                ".webp" => header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46   // RIFF
                        && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50, // WEBP
                _ => false
            };
        }

        public async Task<string> SaveAsync(IFormFile file, string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !folder.All(char.IsLetterOrDigit))
                throw new ArgumentException("La carpeta solo puede contener letras y números.", nameof(folder));

            if (!IsValidImage(file))
                throw new InvalidOperationException("El archivo no es una imagen válida.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var nombreArchivo = $"{Guid.NewGuid():N}{extension}"; // nunca se usa el nombre del usuario

            var directorio = Path.Combine(_environment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(directorio);

            var rutaFisica = Path.Combine(directorio, nombreArchivo);
            await using (var destino = new FileStream(rutaFisica, FileMode.CreateNew))
            {
                await file.CopyToAsync(destino);
            }

            return $"/uploads/{folder}/{nombreArchivo}";
        }

        public Task DeleteAsync(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return Task.CompletedTask;

            var raizUploads = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "uploads"));
            var rutaFisica = Path.GetFullPath(Path.Combine(
                _environment.WebRootPath,
                relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

            // Protección contra rutas como "../../appsettings.json": solo se borra dentro de uploads
            if (!rutaFisica.StartsWith(raizUploads + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;

            if (File.Exists(rutaFisica))
                File.Delete(rutaFisica);

            return Task.CompletedTask;
        }
    }
}
