namespace ParsonsPuzzleApp.Models
{
    public class Bundle
    {
        public int Id { get; set; }
        public string Identifier { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public string InstructorId { get; set; } // Foreign key to AspNetUsers
        public bool IsPublished { get; set; } = false;
        public Guid ShareableLink { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }

        // Navigation properties
        public List<BundlePuzzle> BundlePuzzles { get; set; } = new List<BundlePuzzle>();
    }
}