using FlashcardApi.Data;
using FlashcardApi.Models;
using FlashcardApi.Services.FSRS;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FlashcardApi.Services;

/// <summary>
/// Service for managing cards and reviews
/// </summary>
public class CardService : ICardService
{
    private readonly FlashcardDbContext _context;
    private readonly IFsrsService _fsrsService;

    public CardService(FlashcardDbContext context, IFsrsService fsrsService)
    {
        _context = context;
        _fsrsService = fsrsService;
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
            State = CardState.New,
            Stability = 0,
            Difficulty = 5, // Default difficulty
            Retrievability = 1,
            CreatedAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow // New cards are immediately available
        };

        _context.Cards.Add(card);
        await _context.SaveChangesAsync();

        return card;
    }

    /// <summary>
    /// Reviews a card and updates its scheduling using FSRS
    /// </summary>
    public async Task<Card> ReviewCardAsync(int cardId, int grade)
    {
        // Validate grade
        if (grade < 1 || grade > 4)
            throw new ArgumentException("Grade must be between 1 (Again) and 4 (Easy)", nameof(grade));

        var card = await _context.Cards
            .Include(c => c.Deck)
            .FirstOrDefaultAsync(c => c.Id == cardId);

        if (card == null)
            throw new InvalidOperationException($"Card with ID {cardId} not found");

        // Get FSRS parameters for this deck
        var parameters = string.IsNullOrEmpty(card.Deck.FsrsParameters)
            ? _fsrsService.GetDefaultParameters()
            : JsonSerializer.Deserialize<FsrsParameters>(card.Deck.FsrsParameters) ?? _fsrsService.GetDefaultParameters();

        // Process the review using FSRS
        var (updatedCard, reviewLog) = _fsrsService.ReviewCard(card, grade, parameters);

        // Update deck's last modified time
        card.Deck.UpdatedAt = DateTime.UtcNow;

        // Save review log
        _context.ReviewLogs.Add(reviewLog);
        
        // Save all changes
        await _context.SaveChangesAsync();

        return updatedCard;
    }

    /// <summary>
    /// Gets the next card to review from a deck
    /// Prioritizes: due cards > new cards, ordered by due date
    /// </summary>
    public async Task<Card?> GetNextCardToReviewAsync(int deckId)
    {
        var now = DateTime.UtcNow;

        // First, try to get due cards (including new cards)
        var nextCard = await _context.Cards
            .Where(c => c.DeckId == deckId && 
                       (c.DueDate == null || c.DueDate <= now))
            .OrderBy(c => c.State == CardState.New ? 0 : 1) // New cards first
            .ThenBy(c => c.DueDate ?? DateTime.MinValue) // Then by due date
            .FirstOrDefaultAsync();

        return nextCard;
    }
}