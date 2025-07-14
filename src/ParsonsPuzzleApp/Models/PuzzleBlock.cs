using System.Collections.Generic;

namespace ParsonsPuzzleApp.Models
{
    public class PuzzleBlock
    {
        public int Id { get; set; }
        public int PuzzleId { get; set; }
        public string Content { get; set; }
        public int OrderIndex { get; set; }
        public bool IsDistractor { get; set; }
        public string? SlotName { get; set; } // Nullable
        public bool IsMultiline { get; set; }
        public string? GroupId { get; set; } // Nullable
        public bool IsOrderIndependent { get; set; }
        public string BlockType { get; set; } = "single"; // Default стойност

        public Puzzle Puzzle { get; set; }
        public List<PuzzleBlockLine> Lines { get; set; } = new List<PuzzleBlockLine>();
    }

    public class PuzzleBlockLine
    {
        public int Id { get; set; }
        public int PuzzleBlockId { get; set; }
        public string Content { get; set; }
        public int LineOrder { get; set; }
        public bool IsOptional { get; set; }
        public PuzzleBlock PuzzleBlock { get; set; }
    }
}