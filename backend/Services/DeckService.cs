using FlashcardApi.Data;
using FlashcardApi.Models;
using FlashcardApi.Services.FSRS;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
    /// Gets all decks with calculated statistics
    /// </summary>
    public async Task<List<DeckWithStats>> GetAllDecksWithStatsAsync()
    {
        var decks = await _context.Decks
            .Include(d => d.Cards)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;

        return decks.Select(deck => new DeckWithStats
        {
            Id = deck.Id,
            Name = deck.Name,
            Description = deck.Description,
            CreatedAt = deck.CreatedAt,
            UpdatedAt = deck.UpdatedAt,
            TotalCards = deck.Cards.Count,
            NewCards = deck.Cards.Count(c => c.State == CardState.New),
            LearningCards = deck.Cards.Count(c => c.State == CardState.Learning),
            ReviewCards = deck.Cards.Count(c => c.State == CardState.Review),
            MasteredCards = deck.Cards.Count(c => c.Stability > FSRS.FsrsConstants.MasteredStabilityThreshold),
            DueToday = deck.Cards.Count(c => c.DueDate.HasValue && c.DueDate.Value.Date <= today)
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
    /// Creates a new deck with default FSRS parameters
    /// </summary>
    public async Task<Deck> CreateDeckAsync(string name, string? description = null)
    {
        var defaultParams = new FsrsParameters();
        
        var deck = new Deck
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FsrsParameters = JsonSerializer.Serialize(defaultParams)
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