namespace LinkUpPro.Application.Common
{
    // Reglas de contraseña del enunciado: 8+ caracteres, mayúscula, minúscula, número y carácter especial.
    // Se reutilizan en registro, restablecimiento y Mi perfil.
    public static class PasswordRules
    {
        public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$";
        public const string Message = "La contraseña debe tener al menos 8 caracteres, una letra mayúscula, una letra minúscula, un número y un carácter especial.";
    }
}
