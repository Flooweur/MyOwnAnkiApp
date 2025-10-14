using FlashcardApi.Data;
using FlashcardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlashcardApi.Services;

/// <summary>
/// Service for managing cards and reviews
/// </summary>
public class CardService : ICardService
{
    private readonly FlashcardDbContext _context;

    public CardService(FlashcardDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets cards that are due for review, prioritizing by due date
    /// </summary>
    public async Task<List<Card>> GetDueCardsAsync(int deckId, int limit = 20)
    {
        var now = DateTime.UtcNow;

        return await _context.Cards
            .Where(c => c.DeckId == deckId && 
                       (c.DueDate == null || c.DueDate <= now))
            .OrderBy(c => c.DueDate ?? DateTime.MinValue)
            .ThenBy(c => c.State) // Prioritize by state
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific card by ID
    /// </summary>
    public async Task<Card?> GetCardByIdAsync(int cardId)
    {
        return await _context.Cards
            .Include(c => c.Deck)
            .FirstOrDefaultAsync(c => c.Id == cardId);
    }

    /// <summary>
    /// Creates a new card in a deck
    /// </summary>
    public async Task<Card> CreateCardAsync(int deckId, string front, string back)
    {
        var card = new Card
        {
            DeckId = deckId,
            Front = front,
            Back = back,
            CreatedAt = DateTime.UtcNow
        };

        _context.Cards.Add(card);
        await _context.SaveChangesAsync();

        return card;
    }

    /// <summary>
    /// Reviews a card (simplified - no FSRS scheduling)
    /// </summary>
    public async Task<Card> ReviewCardAsync(int cardId, int grade)
    {
        var card = await _context.Cards
            .Include(c => c.Deck)
            .FirstOrDefaultAsync(c => c.Id == cardId);

        if (card == null)
            throw new InvalidOperationException($"Card with ID {cardId} not found");

        // Simply increment review count and update last reviewed time
        card.ReviewCount++;
        card.LastReviewedAt = DateTime.UtcNow;

        // Update deck's last modified time
        card.Deck.UpdatedAt = DateTime.UtcNow;
        
        // Save all changes
        await _context.SaveChangesAsync();

        return card;
    }

    /// <summary>
    /// Gets a random card from a deck for review
    /// </summary>
    public async Task<Card?> GetNextCardToReviewAsync(int deckId)
    {
        // Get all cards in the deck
        var cards = await _context.Cards
            .Where(c => c.DeckId == deckId)
            .ToListAsync();

        if (!cards.Any())
            return null;

        // Select a random card
        var random = new Random();
        var randomIndex = random.Next(cards.Count);
        return cards[randomIndex];
    }

}