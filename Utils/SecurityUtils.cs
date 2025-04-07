using System.Text.RegularExpressions;
using System.Web;

namespace IntexBackend.Utils
{
    public static class SecurityUtils
    {
        /// <summary>
        /// Sanitizes input text to prevent XSS and other injection attacks
        /// </summary>
        /// <param name="input">The input string to sanitize</param>
        /// <returns>A sanitized string</returns>
        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // Remove potentially dangerous scripts and HTML
            var sanitized = Regex.Replace(input, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"<[^>]*>", "", RegexOptions.IgnoreCase);
            
            // Remove JavaScript event handlers
            sanitized = Regex.Replace(sanitized, @"on\w+\s*=\s*""[^""]*""", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"on\w+\s*=\s*'[^']*'", "", RegexOptions.IgnoreCase);
            
            // Remove JavaScript URLs
            sanitized = Regex.Replace(sanitized, @"javascript:", "", RegexOptions.IgnoreCase);
            
            // Remove other potentially dangerous content
            sanitized = Regex.Replace(sanitized, @"vbscript:", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"data:", "", RegexOptions.IgnoreCase);
            
            return sanitized;
        }

        /// <summary>
        /// HTML encodes a string to safely display it in HTML
        /// </summary>
        /// <param name="input">The input string to encode</param>
        /// <returns>An HTML encoded string</returns>
        public static string HtmlEncode(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return HttpUtility.HtmlEncode(input);
        }
    }
}
