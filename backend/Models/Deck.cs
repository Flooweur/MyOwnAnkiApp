using System.Text.Json.Serialization;

namespace FlashcardApi.Models;

/// <summary>
/// Represents a flashcard deck
/// </summary>
public class Deck
{
    /// <summary>
    /// Unique identifier for the deck
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the deck
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the deck
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Date and time when the deck was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time of last update
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Collection of cards belonging to this deck
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();

    /// <summary>
    /// FSRS parameters for this deck (stored as JSON)
    /// </summary>
    public string FsrsParameters { get; set; } = string.Empty;
}