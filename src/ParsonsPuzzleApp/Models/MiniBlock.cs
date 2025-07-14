namespace ParsonsPuzzleApp.Models
{
    public class MiniBlock
    {
        public int Id { get; set; }
        public int PuzzleId { get; set; }
        public string SlotName { get; set; }
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
        public Puzzle Puzzle { get; set; }
    }
}
