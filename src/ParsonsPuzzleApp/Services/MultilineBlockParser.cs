using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Services
{
    public interface IMultilineBlockParser
    {
        List<PuzzleBlock> ParseSourceCode(string sourceCode, int puzzleId, Languages language);
        string GetCommentSyntaxForLanguage(Languages language);
    }

    public class MultilineBlockParser : IMultilineBlockParser
    {
        private readonly Dictionary<Languages, string> _commentSyntax = new()
        {
            { Languages.C, "//" },
            { Languages.Cpp, "//" },
            { Languages.CSharp, "//" },
            { Languages.Java, "//" },
            { Languages.JavaScript, "//" },
            { Languages.Python, "#" },
            { Languages.TSQL, "--" },
            { Languages.MySQL, "--" },
            { Languages.PostgreSQL, "--" },
            { Languages.plSQL, "--" }
        };

        public List<PuzzleBlock> ParseSourceCode(string sourceCode, int puzzleId, Languages language)
        {
            var blocks = new List<PuzzleBlock>();

            // Preprocess the source code for bracket-based languages
            var preprocessedCode = PreprocessBrackets(sourceCode, language);

            var lines = preprocessedCode.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
            var commentSyntax = _commentSyntax[language];

            // Simple patterns: just start and end markers
            var startPattern = $@"^{Regex.Escape(commentSyntax)}-->\s*$";
            var endPattern = $@"^{Regex.Escape(commentSyntax)}<--\s*$";

            int orderIndex = 0;
            int i = 0;

            while (i < lines.Length)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();

                if (Regex.IsMatch(trimmedLine, startPattern))
                {
                    // Found start of multiline block
                    var blockLines = new List<string>();
                    i++; // Skip the start marker

                    // Collect lines until end marker
                    while (i < lines.Length && !Regex.IsMatch(lines[i].Trim(), endPattern))
                    {
                        blockLines.Add(lines[i]);
                        i++;
                    }

                    if (blockLines.Any())
                    {
                        // For multiline blocks: preserve ALL lines (including empty ones) but remove indentation hints
                        var cleanedBlockLines = CleanMultilineBlockContent(blockLines);
                        var blockContent = string.Join("\n", cleanedBlockLines);
                        var slotName = ExtractSlotName(blockContent);

                        // Create multiline block (even if it contains empty lines - that's intentional formatting)
                        var block = new PuzzleBlock
                        {
                            PuzzleId = puzzleId,
                            GroupId = $"multiline_{orderIndex}",
                            BlockType = "multiline",
                            IsMultiline = true,
                            IsOrderIndependent = false,
                            OrderIndex = orderIndex++,
                            IsDistractor = false,
                            Content = blockContent,
                            SlotName = slotName
                        };

                        blocks.Add(block);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(trimmedLine) &&
                         !Regex.IsMatch(trimmedLine, endPattern))
                {
                    // Regular single line block - remove indentation hints
                    var normalizedLine = line.Trim();

                    // Skip empty lines outside of multiline blocks
                    if (!string.IsNullOrWhiteSpace(normalizedLine))
                    {
                        blocks.Add(new PuzzleBlock
                        {
                            PuzzleId = puzzleId,
                            Content = normalizedLine,
                            BlockType = "single",
                            IsMultiline = false,
                            IsOrderIndependent = false,
                            OrderIndex = orderIndex++,
                            IsDistractor = false,
                            SlotName = ExtractSlotName(normalizedLine)
                        });
                    }
                }

                i++;
            }

            return blocks;
        }

        private string PreprocessBrackets(string sourceCode, Languages language)
        {
            // Only process bracket-based languages
            if (!IsBracketBasedLanguage(language))
                return sourceCode;

            var lines = sourceCode.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
            var processedLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Skip comment markers and empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine) || IsCommentMarker(trimmedLine, language))
                {
                    processedLines.Add(line);
                    continue;
                }

                // Process lines that contain brackets mixed with other code
                var processedLine = ProcessBracketsInLine(line);
                processedLines.AddRange(processedLine);
            }

            return string.Join("\n", processedLines);
        }

        private List<string> ProcessBracketsInLine(string line)
        {
            var result = new List<string>();
            var trimmedLine = line.Trim();

            // Get original indentation
            var indentation = line.Substring(0, line.Length - line.TrimStart().Length);

            // Check if line has opening brace with other content
            if (trimmedLine.Contains('{') && trimmedLine != "{")
            {
                // Split: code before brace, brace alone
                var openBraceIndex = trimmedLine.IndexOf('{');
                var beforeBrace = trimmedLine.Substring(0, openBraceIndex).Trim();
                var afterBrace = trimmedLine.Substring(openBraceIndex + 1).Trim();

                if (!string.IsNullOrWhiteSpace(beforeBrace))
                {
                    result.Add(indentation + beforeBrace);
                }
                result.Add(indentation + "{");
                if (!string.IsNullOrWhiteSpace(afterBrace))
                {
                    // Recursively process the rest in case there are more brackets
                    var remainingProcessed = ProcessBracketsInLine(indentation + afterBrace);
                    result.AddRange(remainingProcessed);
                }
            }
            // Check if line has closing brace with other content  
            else if (trimmedLine.Contains('}') && trimmedLine != "}")
            {
                // Split: code before brace, brace alone
                var closeBraceIndex = trimmedLine.IndexOf('}');
                var beforeBrace = trimmedLine.Substring(0, closeBraceIndex).Trim();
                var afterBrace = trimmedLine.Substring(closeBraceIndex + 1).Trim();

                if (!string.IsNullOrWhiteSpace(beforeBrace))
                {
                    result.Add(indentation + beforeBrace);
                }
                result.Add(indentation + "}");
                if (!string.IsNullOrWhiteSpace(afterBrace))
                {
                    // Recursively process the rest in case there are more brackets
                    var remainingProcessed = ProcessBracketsInLine(indentation + afterBrace);
                    result.AddRange(remainingProcessed);
                }
            }
            else
            {
                // No problematic brackets, keep line as is
                result.Add(line);
            }

            return result;
        }

        private bool IsBracketBasedLanguage(Languages language)
        {
            return language == Languages.C ||
                   language == Languages.Cpp ||
                   language == Languages.CSharp ||
                   language == Languages.Java ||
                   language == Languages.JavaScript;
        }

        private bool IsCommentMarker(string line, Languages language)
        {
            var commentSyntax = _commentSyntax[language];
            var startPattern = $@"^{Regex.Escape(commentSyntax)}-->\s*$";
            var endPattern = $@"^{Regex.Escape(commentSyntax)}<--\s*$";

            return Regex.IsMatch(line, startPattern) || Regex.IsMatch(line, endPattern);
        }

        private List<string> CleanMultilineBlockContent(List<string> blockLines)
        {
            // For multiline blocks: PRESERVE ALL LINES (including empty ones) but remove indentation hints
            var cleanedLines = new List<string>();

            foreach (var line in blockLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    // PRESERVE empty lines exactly as they are - important for code formatting!
                    cleanedLines.Add(line);
                }
                else
                {
                    // Only remove indentation hints from non-empty lines
                    cleanedLines.Add(line.Trim());
                }
            }

            return cleanedLines;
        }

        private string? ExtractSlotName(string content)
        {
            var match = Regex.Match(content, @"§(\w+)§");
            return match.Success ? match.Groups[1].Value : null;
        }

        public string GetCommentSyntaxForLanguage(Languages language)
        {
            var syntax = _commentSyntax[language];
            return $@"За {language} използвайте:
{syntax}--> за начало на многоредов блок
{syntax}<-- за край на многоредов блок

Пример:
{syntax}-->
int x = 10;
int y = 20;
{syntax}<--

ВАЖНО: Индентацията от изходния код се премахва автоматично, за да не се дават подсказки на студентите.";
        }
    }

    // Simplified validator
    public static class MultilineBlockValidator
    {
        private static readonly Dictionary<Languages, string> _commentSyntaxMap = new()
        {
            { Languages.C, "//" },
            { Languages.Cpp, "//" },
            { Languages.CSharp, "//" },
            { Languages.Java, "//" },
            { Languages.JavaScript, "//" },
            { Languages.Python, "#" },
            { Languages.TSQL, "--" },
            { Languages.MySQL, "--" },
            { Languages.PostgreSQL, "--" },
            { Languages.plSQL, "--" }
        };

        public static bool ValidateBlockSyntax(string sourceCode, Languages language)
        {
            if (!_commentSyntaxMap.TryGetValue(language, out var commentSyntax))
                return true;

            var startPattern = $@"^{Regex.Escape(commentSyntax)}-->\s*$";
            var endPattern = $@"^{Regex.Escape(commentSyntax)}<--\s*$";

            var lines = sourceCode.Split('\n');
            int openBlocks = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (Regex.IsMatch(trimmed, startPattern))
                    openBlocks++;
                else if (Regex.IsMatch(trimmed, endPattern))
                    openBlocks--;

                if (openBlocks < 0)
                    return false;
            }

            return openBlocks == 0;
        }
    }
}