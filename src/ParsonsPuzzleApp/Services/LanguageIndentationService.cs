namespace ParsonsPuzzleApp.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Web;
    using ParsonsPuzzleApp.Models;

    public class LanguageIndentationService : ILanguageIndentationService
    {
        public string ProcessIndentation(string code, Languages language)
        {
            // For bracket-based languages, add braces when indentation changes
            if (IsBracketBasedLanguage(language))
            {
                return AddBracesForIndentation(code);
            }

            // For other languages, return as-is
            return code;
        }

        public bool ValidateIndentation(string studentCode, string correctCode, Languages language)
        {
            // Clean and normalize both codes
            var normalizedStudent = CleanAndNormalizeCode(studentCode, language);
            var normalizedCorrect = CleanAndNormalizeCode(correctCode, language);

            if (language == Languages.Python)
            {
                // For Python, indentation matters - compare with normalized indentation
                return CompareWithIndentation(normalizedStudent, normalizedCorrect);
            }
            else if (IsBracketBasedLanguage(language))
            {
                // For bracket-based languages, ignore indentation but keep structure
                return CompareIgnoringIndentation(normalizedStudent, normalizedCorrect);
            }
            else
            {
                // For SQL and other languages, compare ignoring indentation
                return CompareIgnoringIndentation(normalizedStudent, normalizedCorrect);
            }
        }

        private bool IsBracketBasedLanguage(Languages language)
        {
            return language == Languages.C ||
                   language == Languages.Cpp ||
                   language == Languages.CSharp ||
                   language == Languages.Java ||
                   language == Languages.JavaScript;
        }

        private string CleanAndNormalizeCode(string code, Languages language)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            // Decode HTML entities that might have been encoded
            code = HttpUtility.HtmlDecode(code);

            // Split into lines
            var lines = code.Split('\n')
                .Select(l => l.TrimEnd('\r', '\n'));

            // Remove comment markers for multiline blocks
            var commentSyntax = GetCommentSyntax(language);
            var cleanedLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                // Skip multiline block markers
                if (IsCommentMarker(trimmedLine, commentSyntax))
                    continue;

                // Keep the line with its original indentation (for Python)
                cleanedLines.Add(line.TrimEnd());
            }

            return string.Join("\n", cleanedLines);
        }

        private bool IsCommentMarker(string line, string commentSyntax)
        {
            // Check for multiline block markers
            // Match patterns like //-->, //<--, #-->, #<--, etc.
            var patterns = new[]
            {
                $@"^{Regex.Escape(commentSyntax)}--+>\s*$",  // Start marker with one or more dashes
                $@"^{Regex.Escape(commentSyntax)}<--+\s*$",  // End marker with one or more dashes
            };

            return patterns.Any(pattern => Regex.IsMatch(line, pattern));
        }

        private string GetCommentSyntax(Languages language)
        {
            return language switch
            {
                Languages.C or Languages.Cpp or Languages.CSharp or Languages.Java or Languages.JavaScript => "//",
                Languages.Python => "#",
                Languages.TSQL or Languages.MySQL or Languages.PostgreSQL or Languages.plSQL => "--",
                _ => "//"
            };
        }

        private bool CompareWithIndentation(string studentCode, string correctCode)
        {
            // For Python - normalize tabs to spaces and then compare
            var studentLines = NormalizeIndentation(studentCode);
            var correctLines = NormalizeIndentation(correctCode);

            if (studentLines.Length != correctLines.Length)
                return false;

            for (int i = 0; i < studentLines.Length; i++)
            {
                // Compare both content and relative indentation
                if (!string.Equals(studentLines[i], correctLines[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private string[] NormalizeIndentation(string code)
        {
            var lines = code.Split('\n')
                .Select(l => l.TrimEnd())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            // Normalize indentation: convert tabs to spaces and standardize
            var normalizedLines = new string[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                // Convert tabs to 4 spaces
                line = line.Replace("\t", "    ");

                // Calculate indentation level (assuming 2 or 4 spaces per level)
                int leadingSpaces = 0;
                for (int j = 0; j < line.Length; j++)
                {
                    if (line[j] == ' ')
                        leadingSpaces++;
                    else
                        break;
                }

                // Normalize to 2 spaces per indentation level
                int indentLevel = 0;
                if (leadingSpaces > 0)
                {
                    // Handle both 2-space and 4-space indentation
                    indentLevel = leadingSpaces / 2;
                    if (leadingSpaces % 4 == 0)
                        indentLevel = leadingSpaces / 4;
                }

                var content = line.TrimStart();
                normalizedLines[i] = new string(' ', indentLevel * 2) + content;
            }

            return normalizedLines;
        }

        private bool CompareIgnoringIndentation(string studentCode, string correctCode)
        {
            // For bracket-based and SQL languages - ignore indentation, compare content only
            var studentLines = studentCode.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            var correctLines = correctCode.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (studentLines.Length != correctLines.Length)
                return false;

            // Compare line by line - DO NOT SORT!
            for (int i = 0; i < studentLines.Length; i++)
            {
                if (!string.Equals(studentLines[i], correctLines[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private string AddBracesForIndentation(string code)
        {
            var lines = code.Split('\n').ToList();
            var result = new List<string>();
            var braceStack = new Stack<int>();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var currentIndent = GetIndentationLevel(line);

                if (i > 0)
                {
                    var prevIndent = GetIndentationLevel(lines[i - 1]);

                    // Close braces if indentation decreased
                    while (braceStack.Count > 0 && braceStack.Peek() >= currentIndent)
                    {
                        var braceIndent = braceStack.Pop();
                        result.Add(new string(' ', braceIndent * 2) + "}");
                    }

                    // Open brace if indentation increased
                    if (currentIndent > prevIndent)
                    {
                        result.Add(new string(' ', prevIndent * 2) + "{");
                        braceStack.Push(currentIndent);
                    }
                }
                else if (currentIndent > 0)
                {
                    // First line with indentation
                    result.Add("{");
                    braceStack.Push(currentIndent);
                }

                result.Add(line);
            }

            // Close any remaining open braces
            while (braceStack.Count > 0)
            {
                var braceIndent = braceStack.Pop();
                result.Add(new string(' ', braceIndent * 2) + "}");
            }

            return string.Join("\n", result);
        }

        private int GetIndentationLevel(string line)
        {
            int spaces = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ' ')
                    spaces++;
                else if (line[i] == '\t')
                    spaces += 4; // Assume 4 spaces per tab
                else
                    break;
            }
            return spaces / 2; // Assuming 2 spaces per indentation level
        }
    }
}