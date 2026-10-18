using Microsoft.AspNetCore.Http;

namespace LinkUpPro.Application.Interfaces.Services
{
    public interface IImageStorageService
    {
        // Valida extensión, tipo real, contenido y tamaño (máx. 5 MB).
        bool IsValidImage(IFormFile file);

        // Guarda la imagen con un nombre generado internamente y devuelve su ruta relativa.
        Task<string> SaveAsync(IFormFile file, string folder);

        Task DeleteAsync(string relativePath);
    }
}