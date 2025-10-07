using FlashcardApi.Models;

namespace FlashcardApi.Services;

/// <summary>
/// Service interface for deck operations
/// </summary>
public interface IDeckService
{
    /// <summary>
    /// Gets all decks with their statistics
    /// </summary>
    Task<List<DeckWithStats>> GetAllDecksWithStatsAsync();

    /// <summary>
    /// Gets a specific deck by ID
    /// </summary>
    Task<Deck?> GetDeckByIdAsync(int deckId);

    /// <summary>
    /// Creates a new deck
    /// </summary>
    Task<Deck> CreateDeckAsync(string name, string? description = null);

    /// <summary>
    /// Deletes a deck and all its cards
    /// </summary>
    Task<bool> DeleteDeckAsync(int deckId);
}

/// <summary>
/// Deck with calculated statistics
/// </summary>
public class DeckWithStats
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Total number of cards in the deck
    /// </summary>
    public int TotalCards { get; set; }
    
    /// <summary>
    /// Number of new cards (never reviewed)
    /// </summary>
    public int NewCards { get; set; }
    
    /// <summary>
    /// Number of cards in learning state
    /// </summary>
    public int LearningCards { get; set; }
    
    /// <summary>
    /// Number of cards in review state
    /// </summary>
    public int ReviewCards { get; set; }
    
    /// <summary>
    /// Number of cards that have been mastered (stability > 100 days)
    /// </summary>
    public int MasteredCards { get; set; }
    
    /// <summary>
    /// Number of cards due for review today
    /// </summary>
    public int DueToday { get; set; }
}