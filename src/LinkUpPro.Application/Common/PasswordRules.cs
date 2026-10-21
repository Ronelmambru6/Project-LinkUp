namespace LinkUpPro.Application.Common
{
    // Reglas de contraseña del enunciado: 8+ caracteres, mayúscula, minúscula, número y carácter especial.
    // Se reutilizan en registro, restablecimiento y Mi perfil.
    public static class PasswordRules
    {
        public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$";
        public const string Message = "La contraseña debe tener al menos 8 caracteres, una letra mayúscula, una letra minúscula, un número y un carácter especial.";

        // Enunciado (Mi perfil): si la nueva contraseña no cumple las reglas, indicar cuáles criterios faltan
        public static List<string> GetMissingCriteria(string password)
        {
            var faltan = new List<string>();
            if (password.Length < 8) faltan.Add("al menos 8 caracteres");
            if (!password.Any(char.IsUpper)) faltan.Add("una letra mayúscula");
            if (!password.Any(char.IsLower)) faltan.Add("una letra minúscula");
            if (!password.Any(char.IsDigit)) faltan.Add("un número");
            if (password.All(char.IsLetterOrDigit)) faltan.Add("un carácter especial");
            return faltan;
        }
    }
}
