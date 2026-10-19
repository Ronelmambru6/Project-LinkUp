namespace LinkUpPro.Shared.Settings
{
    // Se llena desde la sección "MailSettings" de la configuración.
    // Host, puerto y remitente van en appsettings.json; usuario y contraseña en user-secrets.
    public class MailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string From { get; set; } = string.Empty;
        public string DisplayName { get; set; } = "LinkUp Pro";
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
