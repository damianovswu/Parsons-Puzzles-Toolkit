namespace ParsonsPuzzleApp.Services
{
    using System.Collections.Generic;
    using ParsonsPuzzleApp.Models;

    public interface ILanguageIndentationService
    {
        string ProcessIndentation(string code, Languages language);
        bool ValidateIndentation(string studentCode, string correctCode, Languages language);
    }
}