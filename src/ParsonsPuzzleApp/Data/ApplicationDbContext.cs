using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ParsonsPuzzleApp.Models;

namespace ParsonsPuzzleApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Bundle> Bundles { get; set; }
        public DbSet<Puzzle> Puzzles { get; set; }
        public DbSet<BundlePuzzle> BundlePuzzles { get; set; }
        public DbSet<MiniBlock> MiniBlocks { get; set; }
        public DbSet<StudentAttempt> StudentAttempts { get; set; }
        public DbSet<PuzzleBlock> PuzzleBlocks { get; set; }
        public DbSet<PuzzleBlockLine> PuzzleBlockLines { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BundlePuzzle>()
                .HasKey(bp => new { bp.BundleId, bp.PuzzleId });

            modelBuilder.Entity<BundlePuzzle>()
                .HasOne(bp => bp.Bundle)
                .WithMany(b => b.BundlePuzzles)
                .HasForeignKey(bp => bp.BundleId);

            modelBuilder.Entity<BundlePuzzle>()
                .HasOne(bp => bp.Puzzle)
                .WithMany(p => p.BundlePuzzles)
                .HasForeignKey(bp => bp.PuzzleId);

            modelBuilder.Entity<MiniBlock>()
                .HasOne(mb => mb.Puzzle)
                .WithMany(p => p.MiniBlocks)
                .HasForeignKey(mb => mb.PuzzleId);

            modelBuilder.Entity<StudentAttempt>()
                .HasOne(sa => sa.Bundle)
                .WithMany()
                .HasForeignKey(sa => sa.BundleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAttempt>()
                .HasOne(sa => sa.Puzzle)
                .WithMany()
                .HasForeignKey(sa => sa.PuzzleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PuzzleBlock>()
                .HasOne(pb => pb.Puzzle)
                .WithMany(p => p.PuzzleBlocks)
                .HasForeignKey(pb => pb.PuzzleId);

            modelBuilder.Entity<PuzzleBlockLine>()
                .HasOne(pbl => pbl.PuzzleBlock)
                .WithMany(pb => pb.Lines)
                .HasForeignKey(pbl => pbl.PuzzleBlockId);

            modelBuilder.Entity<Bundle>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(b => b.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Puzzle>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(p => p.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bundle>()
                .HasIndex(b => b.ShareableLink)
                .IsUnique();
        }
    }
}