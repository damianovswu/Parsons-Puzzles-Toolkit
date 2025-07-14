using System.Text.RegularExpressions;

namespace ParsonsPuzzleApp.Services
{
    public interface IHtmlSanitizerService
    {
        string SanitizeHtml(string html);
        bool ContainsDangerousContent(string html);
    }

    public class HtmlSanitizerService : IHtmlSanitizerService
    {
        // Dangerous patterns that should be removed
        private static readonly string[] DangerousTags = new[]
        {
            "script", "iframe", "object", "embed", "form", "input", "button",
            "select", "textarea", "style", "link", "meta", "base"
        };

        // Event handlers that could execute JavaScript
        private static readonly string[] DangerousAttributes = new[]
        {
            "onabort", "onblur", "onchange", "onclick", "ondblclick", "onerror",
            "onfocus", "onkeydown", "onkeypress", "onkeyup", "onload", "onmousedown",
            "onmousemove", "onmouseout", "onmouseover", "onmouseup", "onreset",
            "onresize", "onselect", "onsubmit", "onunload", "onbeforeunload",
            "onhashchange", "onmessage", "onoffline", "ononline", "onpopstate",
            "onredo", "onstorage", "onundo", "oncontextmenu"
        };

        public string SanitizeHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            string sanitized = html;

            // Remove dangerous tags but preserve their content
            foreach (var tag in DangerousTags)
            {
                // Remove opening tags
                sanitized = Regex.Replace(sanitized,
                    $@"<\s*{tag}(\s+[^>]*)?>",
                    "",
                    RegexOptions.IgnoreCase);

                // Remove closing tags
                sanitized = Regex.Replace(sanitized,
                    $@"<\s*/\s*{tag}\s*>",
                    "",
                    RegexOptions.IgnoreCase);
            }

            // Remove dangerous attributes from remaining tags
            foreach (var attr in DangerousAttributes)
            {
                sanitized = Regex.Replace(sanitized,
                    $@"\s*{attr}\s*=\s*[""']?[^""']*[""']?",
                    "",
                    RegexOptions.IgnoreCase);
            }

            // Remove javascript: protocol
            sanitized = Regex.Replace(sanitized,
                @"javascript\s*:",
                "",
                RegexOptions.IgnoreCase);

            // Remove data: protocol that could contain base64 encoded scripts
            sanitized = Regex.Replace(sanitized,
                @"data:[^;]*;base64",
                "",
                RegexOptions.IgnoreCase);

            return sanitized;
        }

        public bool ContainsDangerousContent(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return false;

            // Check for dangerous tags
            foreach (var tag in DangerousTags)
            {
                if (Regex.IsMatch(html, $@"<\s*{tag}(\s+[^>]*)?>", RegexOptions.IgnoreCase))
                    return true;
            }

            // Check for event handlers
            foreach (var attr in DangerousAttributes)
            {
                if (Regex.IsMatch(html, $@"\s*{attr}\s*=", RegexOptions.IgnoreCase))
                    return true;
            }

            // Check for javascript: protocol
            if (Regex.IsMatch(html, @"javascript\s*:", RegexOptions.IgnoreCase))
                return true;

            return false;
        }
    }
}