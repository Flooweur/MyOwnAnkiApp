namespace FlashcardApi.Models;

/// <summary>
/// Represents a single review session for a card
/// </summary>
public class ReviewLog
{
    /// <summary>
    /// Unique identifier for the review log
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the card that was reviewed
    /// </summary>
    public int CardId { get; set; }

    /// <summary>
    /// Navigation property to the reviewed card
    /// </summary>
    public virtual Card Card { get; set; } = null!;

    /// <summary>
    /// Grade given during this review (1=Again, 2=Hard, 3=Good, 4=Easy)
    /// </summary>
    public int Grade { get; set; }

    /// <summary>
    /// Card state before this review
    /// </summary>
    public CardState StateBefore { get; set; }

    /// <summary>
    /// Card state after this review
    /// </summary>
    public CardState StateAfter { get; set; }

    /// <summary>
    /// Stability value before this review
    /// </summary>
    public double StabilityBefore { get; set; }

    /// <summary>
    /// Stability value after this review
    /// </summary>
    public double StabilityAfter { get; set; }

    /// <summary>
    /// Difficulty value before this review
    /// </summary>
    public double DifficultyBefore { get; set; }

    /// <summary>
    /// Difficulty value after this review
    /// </summary>
    public double DifficultyAfter { get; set; }

    /// <summary>
    /// Retrievability at the time of review
    /// </summary>
    public double Retrievability { get; set; }

    /// <summary>
    /// Interval in days until next review
    /// </summary>
    public double ScheduledInterval { get; set; }

    /// <summary>
    /// Date and time when this review occurred
    /// </summary>
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Time in milliseconds taken to answer the card
    /// </summary>
    public int? TimeToAnswer { get; set; }
}

/// <summary>
/// Grade options for card review
/// </summary>
public enum ReviewGrade
{
    /// <summary>
    /// Completely forgot the card
    /// </summary>
    Again = 1,

    /// <summary>
    /// Recalled with difficulty
    /// </summary>
    Hard = 2,

    /// <summary>
    /// Recalled correctly with some effort
    /// </summary>
    Good = 3,

    /// <summary>
    /// Recalled easily
    /// </summary>
    Easy = 4
}