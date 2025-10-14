using FlashcardApi.Models;

namespace FlashcardApi.Services;

/// <summary>
/// Service interface for card operations
/// </summary>
public interface ICardService
{
    /// <summary>
    /// Gets all cards from a deck
    /// </summary>
    Task<List<Card>> GetCardsAsync(int deckId);

    /// <summary>
    /// Gets a specific card by ID
    /// </summary>
    Task<Card?> GetCardByIdAsync(int cardId);

    /// <summary>
    /// Creates a new card in a deck
    /// </summary>
    Task<Card> CreateCardAsync(int deckId, string front, string back);

    /// <summary>
    /// Records a card review
    /// </summary>
    Task<Card> ReviewCardAsync(int cardId, int grade);

    /// <summary>
    /// Gets a random card from a deck
    /// </summary>
    Task<Card?> GetRandomCardAsync(int deckId);
}