using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace LinkUpPro.Application.Common
{
    // Valida enlaces de YouTube y extrae el identificador del video (11 caracteres).
    // Solo se aceptan dominios de YouTube: nunca se incrusta una dirección arbitraria.
    public static class YouTubeLinkParser
    {
        private static readonly Regex VideoIdPattern = new("^[A-Za-z0-9_-]{11}$", RegexOptions.Compiled);

        private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            "youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com",
            "youtube-nocookie.com", "www.youtube-nocookie.com",
            "youtu.be", "www.youtu.be"
        };

        // Formatos: youtube.com/watch?v=ID, youtu.be/ID, youtube.com/embed/ID,
        // youtube.com/shorts/ID, youtube.com/live/ID y youtube.com/v/ID
        public static bool TryGetVideoId(string? url, out string videoId)
        {
            videoId = string.Empty;

            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                return false;

            if (!AllowedHosts.Contains(uri.Host))
                return false;

            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string? candidate = null;

            if (uri.Host.EndsWith("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                if (segments.Length == 1)
                    candidate = segments[0];
            }
            else if (segments.Length == 1 && segments[0].Equals("watch", StringComparison.OrdinalIgnoreCase))
            {
                var query = QueryHelpers.ParseQuery(uri.Query);
                if (query.TryGetValue("v", out var values))
                    candidate = values.ToString();
            }
            else if (segments.Length == 2 &&
                     segments[0].ToLowerInvariant() is "embed" or "shorts" or "live" or "v")
            {
                candidate = segments[1];
            }

            if (candidate is null || !VideoIdPattern.IsMatch(candidate))
                return false;

            videoId = candidate;
            return true;
        }
    }
}