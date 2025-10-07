using FlashcardApi.Models;

namespace FlashcardApi.Services;

/// <summary>
/// Service interface for card operations
/// </summary>
public interface ICardService
{
    /// <summary>
    /// Gets cards due for review in a deck
    /// </summary>
    Task<List<Card>> GetDueCardsAsync(int deckId, int limit = 20);

    /// <summary>
    /// Gets a specific card by ID
    /// </summary>
    Task<Card?> GetCardByIdAsync(int cardId);

    /// <summary>
    /// Creates a new card in a deck
    /// </summary>
    Task<Card> CreateCardAsync(int deckId, string front, string back);

    /// <summary>
    /// Reviews a card and updates its schedule
    /// </summary>
    Task<Card> ReviewCardAsync(int cardId, int grade);

    /// <summary>
    /// Gets the next card to review from a deck
    /// </summary>
    Task<Card?> GetNextCardToReviewAsync(int deckId);
}

/// <summary>
/// DTO for card review response
/// </summary>
public class CardReviewResponse
{
    public int CardId { get; set; }
    public double NewStability { get; set; }
    public double NewDifficulty { get; set; }
    public DateTime NextReviewDate { get; set; }
    public double Interval { get; set; }
}