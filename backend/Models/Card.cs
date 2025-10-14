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
    /// Number of times this card has been reviewed
    /// </summary>
    public int ReviewCount { get; set; } = 0;

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
