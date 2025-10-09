using System.Text.Json.Serialization;

namespace FlashcardApi.Models;

/// <summary>
/// Represents a single flashcard
/// </summary>
public class Card
{
    /// <summary>
    /// Unique identifier for the card
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the deck this card belongs to
    /// </summary>
    public int DeckId { get; set; }

    /// <summary>
    /// Navigation property to the parent deck
    /// </summary>
    [JsonIgnore]
    public virtual Deck Deck { get; set; } = null!;

    /// <summary>
    /// Front of the card (question)
    /// </summary>
    public string Front { get; set; } = string.Empty;

    /// <summary>
    /// Back of the card (answer)
    /// </summary>
    public string Back { get; set; } = string.Empty;

    /// <summary>
    /// Memory stability - time in days for retrievability to decrease from 100% to 90%
    /// </summary>
    public double Stability { get; set; } = 0;

    /// <summary>
    /// Difficulty rating (1-10, displayed as 0-1 in UI)
    /// </summary>
    public double Difficulty { get; set; } = 5;

    /// <summary>
    /// Current retrievability (0-1)
    /// </summary>
    public double Retrievability { get; set; } = 1;

    /// <summary>
    /// Number of times this card has been reviewed
    /// </summary>
    public int ReviewCount { get; set; } = 0;

    /// <summary>
    /// Number of times this card was forgotten (Again button)
    /// </summary>
    public int LapseCount { get; set; } = 0;

    /// <summary>
    /// Current state of the card
    /// </summary>
    public CardState State { get; set; } = CardState.New;

    /// <summary>
    /// Ease factor for SM-2 style interval calculation (typically 2.5 = 250%)
    /// Used in Anki's classic scheduler alongside FSRS
    /// </summary>
    public double EaseFactor { get; set; } = 2.5;

    /// <summary>
    /// Current learning/relearning step index (for cards in Learning/Relearning state)
    /// 0-based index into the learning steps array
    /// </summary>
    public int CurrentStep { get; set; } = 0;

    /// <summary>
    /// Scheduled interval in seconds (for cards in learning/relearning state)
    /// Used for sub-day intervals
    /// </summary>
    public int ScheduledSeconds { get; set; } = 0;

    /// <summary>
    /// Elapsed seconds since card entered current state (for learning/relearning)
    /// </summary>
    public int ElapsedSeconds { get; set; } = 0;

    /// <summary>
    /// Date when this card is due for next review
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Date and time of last review
    /// </summary>
    public DateTime? LastReviewedAt { get; set; }

    /// <summary>
    /// Date and time when the card was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Collection of review logs for this card
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<ReviewLog> ReviewLogs { get; set; } = new List<ReviewLog>();
}

/// <summary>
/// Represents the current state of a card in the learning process
/// </summary>
public enum CardState
{
    /// <summary>
    /// Card has never been reviewed
    /// </summary>
    New = 0,

    /// <summary>
    /// Card is being learned (first few reviews)
    /// </summary>
    Learning = 1,

    /// <summary>
    /// Card is in review phase (graduated from learning)
    /// </summary>
    Review = 2,

    /// <summary>
    /// Card has been forgotten and needs to be relearned
    /// </summary>
    Relearning = 3
}