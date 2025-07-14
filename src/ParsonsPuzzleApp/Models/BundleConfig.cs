namespace ParsonsPuzzleApp.Models
{
    public class BundleConfig
    {
        public int BundleId { get; set; }
        public string Identifier { get; set; }
        public List<PuzzleConfig> Puzzles { get; set; }
    }

    public class PuzzleConfig
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Task { get; set; }
        public Languages Language { get; set; }
        public List<CodeBlock> CodeBlocks { get; set; }
        public List<MiniBlockConfig> MiniBlocks { get; set; }
    }

    public class CodeBlock
    {
        public string Content { get; set; }
        public int Indentation { get; set; }
        public string SlotName { get; set; } // null ако няма слот
    }

    public class MiniBlockConfig
    {
        public string Content { get; set; }
        public string SlotName { get; set; }
        public bool IsCorrect { get; set; }
    }
}
