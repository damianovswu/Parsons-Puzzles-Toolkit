namespace ParsonsPuzzleApp.Models
{
    public class BundlePuzzle
    {
        public int BundleId { get; set; }
        public Bundle Bundle { get; set; }
        public int PuzzleId { get; set; }
        public Puzzle Puzzle { get; set; }
    }
}
