using System.ComponentModel.DataAnnotations;

namespace ParsonsPuzzleApp.Models
{
    public class Puzzle
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Заглавието е задължително")]
        [StringLength(200, ErrorMessage = "Заглавието не може да бъде по-дълго от 200 символа")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Задачата е задължителна")]
        public string Task { get; set; }

        [Required(ErrorMessage = "Изходният код е задължителен")]
        public string SourceCode { get; set; }

        // Distractors are now optional - no [Required] attribute
        public string? Distractors { get; set; }
        [Required(ErrorMessage = "Езикът е задължителен")]
        public Languages Language { get; set; }
        public string InstructorId { get; set; } // Foreign key to AspNetUsers
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }
        public List<BundlePuzzle> BundlePuzzles { get; set; } = new List<BundlePuzzle>();
        public List<MiniBlock> MiniBlocks { get; set; } = new List<MiniBlock>();
        public List<PuzzleBlock> PuzzleBlocks { get; set; } = new List<PuzzleBlock>();
        public string? BlockConfiguration { get; set; }
    }
}