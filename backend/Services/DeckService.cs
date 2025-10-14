using FlashcardApi.Data;
using FlashcardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlashcardApi.Services;

/// <summary>
/// Service for managing decks
/// </summary>
public class DeckService : IDeckService
{
    private readonly FlashcardDbContext _context;

    public DeckService(FlashcardDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all decks with simplified statistics
    /// </summary>
    public async Task<List<DeckWithStats>> GetAllDecksWithStatsAsync()
    {
        var decks = await _context.Decks
            .Include(d => d.Cards)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync();

        return decks.Select(deck => new DeckWithStats
        {
            Id = deck.Id,
            Name = deck.Name,
            Description = deck.Description,
            CreatedAt = deck.CreatedAt,
            UpdatedAt = deck.UpdatedAt,
            TotalCards = deck.Cards.Count,
            NewCards = 0, // No longer relevant
            LearningCards = 0, // No longer relevant
            ReviewCards = 0, // No longer relevant
            MasteredCards = 0, // No longer relevant
            DueToday = 0 // No longer relevant
        }).ToList();
    }

    /// <summary>
    /// Gets a specific deck by ID
    /// </summary>
    public async Task<Deck?> GetDeckByIdAsync(int deckId)
    {
        return await _context.Decks
            .Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.Id == deckId);
    }

    /// <summary>
    /// Creates a new deck
    /// </summary>
    public async Task<Deck> CreateDeckAsync(string name, string? description = null)
    {
        var deck = new Deck
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FsrsParameters = null // No longer needed
        };

        _context.Decks.Add(deck);
        await _context.SaveChangesAsync();

        return deck;
    }

    /// <summary>
    /// Deletes a deck and all associated cards and review logs
    /// </summary>
    public async Task<bool> DeleteDeckAsync(int deckId)
    {
        var deck = await _context.Decks.FindAsync(deckId);
        if (deck == null)
            return false;

        _context.Decks.Remove(deck);
        await _context.SaveChangesAsync();

        return true;
    }
}