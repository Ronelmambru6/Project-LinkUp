namespace LinkUpPro.Application.Dtos.Users
{
    // Datos básicos de un usuario para listados (solicitudes, amigos, amigos en común…).
    public class UserSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string FotoPerfil { get; set; } = string.Empty;

        public string NombreCompleto => $"{Nombre} {Apellido}";
    }
}
