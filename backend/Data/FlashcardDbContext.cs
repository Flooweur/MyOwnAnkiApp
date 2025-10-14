using FlashcardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlashcardApi.Data;

/// <summary>
/// Database context for the flashcard application
/// </summary>
public class FlashcardDbContext : DbContext
{
    public FlashcardDbContext(DbContextOptions<FlashcardDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Decks table
    /// </summary>
    public DbSet<Deck> Decks { get; set; } = null!;

    /// <summary>
    /// Cards table
    /// </summary>
    public DbSet<Card> Cards { get; set; } = null!;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Deck entity
        modelBuilder.Entity<Deck>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure Card entity
        modelBuilder.Entity<Card>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Front).IsRequired();
            entity.Property(e => e.Back).IsRequired();
            entity.Property(e => e.ReviewCount).HasDefaultValue(0);
            
            // Create relationship with Deck
            entity.HasOne(e => e.Deck)
                  .WithMany(d => d.Cards)
                  .HasForeignKey(e => e.DeckId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Create indexes for common queries
            entity.HasIndex(e => e.DeckId);
        });

    }
}