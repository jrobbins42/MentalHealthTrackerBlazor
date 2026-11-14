namespace MentalHealthTrackerBlazor.Security
{
    /// <summary>
    /// Guards redirect targets. Only paths that stay inside this application are accepted, so a
    /// <c>returnUrl</c> supplied by the browser can never bounce the user to another site.
    /// </summary>
    public static class LocalUrl
    {
        public static bool IsLocal(string? url)
        {
            if (string.IsNullOrEmpty(url) || url.Length > 2048)
            {
                return false;
            }

            // Browsers strip or reinterpret control characters, so refuse anything containing them.
            foreach (char c in url)
            {
                if (char.IsControl(c))
                {
                    return false;
                }
            }

            // Must be an absolute path on this host: "/mht" starts with a single slash.
            // "//evil.example" (protocol-relative) and "/\evil.example" (backslash normalised by
            // browsers) are rejected, as are full URLs such as "https://evil.example".
            return url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
        }

        /// <summary>Returns <paramref name="url"/> when it is local, otherwise <paramref name="fallback"/>.</summary>
        public static string Sanitize(string? url, string fallback = "/mht") => IsLocal(url) ? url! : fallback;
    }
}
