namespace LinkUpPro.Application.ViewModels.Users
{
    public class UserSummaryViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string FotoPerfil { get; set; } = string.Empty;

        public string NombreCompleto => $"{Nombre} {Apellido}";
    }
}
