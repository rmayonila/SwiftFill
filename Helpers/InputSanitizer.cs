using System.Text.RegularExpressions;
using System.Net;

namespace SwiftFill.Helpers
{
    public static class InputSanitizer
    {
        public static string? Sanitize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // Remove all HTML tags
            string cleaned = Regex.Replace(input, "<.*?>", string.Empty);
            
            // Decode any existing entities then re-encode to be safe
            cleaned = WebUtility.HtmlEncode(WebUtility.HtmlDecode(cleaned));
            
            return cleaned.Trim();
        }

        public static string? StripScripts(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // Remove script tags and everything in between
            string pattern = @"<script\b[^>]*>([\s\S]*?)<\/script>";
            string cleaned = Regex.Replace(input, pattern, string.Empty, RegexOptions.IgnoreCase);
            
            return cleaned;
        }
    }
}
