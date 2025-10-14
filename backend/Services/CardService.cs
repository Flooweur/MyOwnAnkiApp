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
    /// Gets all cards from a deck
    /// </summary>
    public async Task<List<Card>> GetCardsAsync(int deckId)
    {
        return await _context.Cards
            .Where(c => c.DeckId == deckId)
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
    /// Records a card review (simplified - just updates review count and timestamp)
    /// </summary>
    public async Task<Card> ReviewCardAsync(int cardId, int grade)
    {
        var card = await _context.Cards
            .Include(c => c.Deck)
            .FirstOrDefaultAsync(c => c.Id == cardId);

        if (card == null)
            throw new InvalidOperationException($"Card with ID {cardId} not found");

        // Simply update review count and timestamp
        card.ReviewCount++;
        card.LastReviewedAt = DateTime.UtcNow;

        // Update deck's last modified time
        card.Deck.UpdatedAt = DateTime.UtcNow;
        
        // Save changes
        await _context.SaveChangesAsync();

        return card;
    }

    /// <summary>
    /// Gets a random card from a deck
    /// </summary>
    public async Task<Card?> GetRandomCardAsync(int deckId)
    {
        var cards = await _context.Cards
            .Where(c => c.DeckId == deckId)
            .ToListAsync();

        if (!cards.Any())
            return null;

        var random = new Random();
        var randomIndex = random.Next(cards.Count);
        return cards[randomIndex];
    }

    /// <summary>
    /// Deletes a card by ID
    /// </summary>
    public async Task<bool> DeleteCardAsync(int cardId)
    {
        var card = await _context.Cards
            .Include(c => c.Deck)
            .FirstOrDefaultAsync(c => c.Id == cardId);

        if (card == null)
            return false;

        _context.Cards.Remove(card);
        
        // Update deck's last modified time
        card.Deck.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }
}