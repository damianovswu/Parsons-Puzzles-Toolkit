namespace ParsonsPuzzleApp.Models
{
    public class StudentAttempt
    {
        public int Id { get; set; }
        public int BundleId { get; set; }
        public Bundle Bundle { get; set; }
        public int PuzzleId { get; set; }
        public Puzzle Puzzle { get; set; }
        public string StudentIdentifier { get; set; }
        public bool IsCorrect { get; set; }
        public string StudentArrangement { get; set; }
        public int TimeTakenSeconds { get; set; }
        public DateTime AttemptDate { get; set; } = DateTime.UtcNow;
        public Guid BundleAttemptId { get; set; }
    }
}
