namespace LinkUpPro.Application.Common
{
    // Una amistad es una sola relación por pareja, sin importar quién envió la solicitud.
    // Para que el índice único de la base de datos funcione, la pareja se guarda SIEMPRE
    // en el mismo orden: UsuarioAId = el Id menor según comparación ordinal.
    public static class UserPair
    {
        public static (string UsuarioAId, string UsuarioBId) Order(string userId1, string userId2) =>
            string.CompareOrdinal(userId1, userId2) <= 0
                ? (userId1, userId2)
                : (userId2, userId1);
    }
}
