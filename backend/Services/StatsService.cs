using FlashcardApi.Controllers;
using FlashcardApi.Data;
using FlashcardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlashcardApi.Services;

/// <summary>
/// Service for generating basic statistics and analytics
/// </summary>
public class StatsService : IStatsService
{
    private readonly FlashcardDbContext _context;

    public StatsService(FlashcardDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets basic daily statistics for a deck over the specified number of days
    /// </summary>
    public async Task<List<DailyStats>> GetDailyStatsAsync(int deckId, int days)
    {
        var dailyStats = new List<DailyStats>();
        
        for (int i = 0; i < days; i++)
        {
            var date = DateTime.UtcNow.Date.AddDays(-days + i + 1);
            
            var stats = new DailyStats
            {
                Date = date.ToString("yyyy-MM-dd"),
                CardsReviewed = 0, // No review tracking in simplified version
                CardsAgain = 0,
                CardsHard = 0,
                CardsGood = 0,
                CardsEasy = 0,
                AverageRetention = 0,
                TimeSpentMinutes = 0
            };
            
            dailyStats.Add(stats);
        }
        
        return dailyStats;
    }

    /// <summary>
    /// Gets basic retention statistics for a deck
    /// </summary>
    public async Task<RetentionStats> GetRetentionStatsAsync(int deckId)
    {
        var stats = new RetentionStats
        {
            OverallRetention = 0,
            Last7DaysRetention = 0,
            Last30DaysRetention = 0,
            GradeDistribution = new List<GradeDistribution>()
        };

        return stats;
    }

    /// <summary>
    /// Gets comprehensive deck overview statistics
    /// </summary>
    public async Task<DeckOverviewStats> GetDeckOverviewAsync(int deckId)
    {
        var cards = await _context.Cards
            .Where(c => c.DeckId == deckId)
            .ToListAsync();

        var stats = new DeckOverviewStats
        {
            TotalCards = cards.Count,
            TotalReviews = 0, // No review tracking in simplified version
            AverageRetention = 0,
            AverageDifficulty = 0,
            StreakDays = 0,
            StateDistribution = new List<CardStateDistribution>()
        };

        return stats;
    }
}