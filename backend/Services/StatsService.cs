using FlashcardApi.Controllers;
using FlashcardApi.Data;
using FlashcardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlashcardApi.Services;

/// <summary>
/// Service for generating statistics and analytics
/// </summary>
public class StatsService : IStatsService
{
    private readonly FlashcardDbContext _context;

    public StatsService(FlashcardDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets daily statistics for a deck over the specified number of days
    /// </summary>
    public async Task<List<DailyStats>> GetDailyStatsAsync(int deckId, int days)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        
        var reviewLogs = await _context.ReviewLogs
            .Include(r => r.Card)
            .Where(r => r.Card.DeckId == deckId && r.ReviewedAt >= startDate)
            .ToListAsync();

        var dailyStats = new List<DailyStats>();
        
        for (int i = 0; i < days; i++)
        {
            var date = DateTime.UtcNow.Date.AddDays(-days + i + 1);
            var dayReviews = reviewLogs.Where(r => r.ReviewedAt.Date == date).ToList();
            
            var stats = new DailyStats
            {
                Date = date.ToString("yyyy-MM-dd"),
                CardsReviewed = dayReviews.Count,
                CardsAgain = dayReviews.Count(r => r.Grade == 1),
                CardsHard = dayReviews.Count(r => r.Grade == 2),
                CardsGood = dayReviews.Count(r => r.Grade == 3),
                CardsEasy = dayReviews.Count(r => r.Grade == 4),
                AverageRetention = dayReviews.Any() 
                    ? dayReviews.Average(r => r.Retrievability) 
                    : 0,
                TimeSpentMinutes = 0 // Could be calculated if we tracked time
            };
            
            dailyStats.Add(stats);
        }
        
        return dailyStats;
    }

    /// <summary>
    /// Gets retention statistics for a deck
    /// </summary>
    public async Task<RetentionStats> GetRetentionStatsAsync(int deckId)
    {
        var allReviews = await _context.ReviewLogs
            .Include(r => r.Card)
            .Where(r => r.Card.DeckId == deckId)
            .ToListAsync();

        var last7Days = DateTime.UtcNow.AddDays(-7);
        var last30Days = DateTime.UtcNow.AddDays(-30);

        var last7DaysReviews = allReviews.Where(r => r.ReviewedAt >= last7Days).ToList();
        var last30DaysReviews = allReviews.Where(r => r.ReviewedAt >= last30Days).ToList();

        // Calculate retention as percentage of reviews that weren't "Again" (grade 1)
        double CalculateRetention(List<ReviewLog> reviews) =>
            reviews.Any() ? (double)reviews.Count(r => r.Grade > 1) / reviews.Count * 100 : 0;

        var stats = new RetentionStats
        {
            OverallRetention = CalculateRetention(allReviews),
            Last7DaysRetention = CalculateRetention(last7DaysReviews),
            Last30DaysRetention = CalculateRetention(last30DaysReviews),
            GradeDistribution = new List<GradeDistribution>()
        };

        // Calculate grade distribution
        var totalReviews = allReviews.Count;
        if (totalReviews > 0)
        {
            stats.GradeDistribution = new List<GradeDistribution>
            {
                new() { Grade = "Again", Count = allReviews.Count(r => r.Grade == 1), 
                       Percentage = (double)allReviews.Count(r => r.Grade == 1) / totalReviews * 100 },
                new() { Grade = "Hard", Count = allReviews.Count(r => r.Grade == 2), 
                       Percentage = (double)allReviews.Count(r => r.Grade == 2) / totalReviews * 100 },
                new() { Grade = "Good", Count = allReviews.Count(r => r.Grade == 3), 
                       Percentage = (double)allReviews.Count(r => r.Grade == 3) / totalReviews * 100 },
                new() { Grade = "Easy", Count = allReviews.Count(r => r.Grade == 4), 
                       Percentage = (double)allReviews.Count(r => r.Grade == 4) / totalReviews * 100 }
            };
        }

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

        var reviewLogs = await _context.ReviewLogs
            .Include(r => r.Card)
            .Where(r => r.Card.DeckId == deckId)
            .ToListAsync();

        var stats = new DeckOverviewStats
        {
            TotalCards = cards.Count,
            TotalReviews = reviewLogs.Count,
            AverageRetention = cards.Any(c => c.ReviewCount > 0) 
                ? cards.Where(c => c.ReviewCount > 0).Average(c => c.Retrievability) 
                : 0,
            AverageDifficulty = cards.Any() 
                ? cards.Average(c => c.Difficulty) / 10.0  // Convert to 0-1 scale
                : 0,
            StreakDays = CalculateStreakDays(reviewLogs),
            StateDistribution = new List<CardStateDistribution>()
        };

        // Calculate state distribution
        var totalCards = cards.Count;
        if (totalCards > 0)
        {
            stats.StateDistribution = new List<CardStateDistribution>
            {
                new() { State = "New", Count = cards.Count(c => c.State == CardState.New), 
                       Percentage = (double)cards.Count(c => c.State == CardState.New) / totalCards * 100 },
                new() { State = "Learning", Count = cards.Count(c => c.State == CardState.Learning), 
                       Percentage = (double)cards.Count(c => c.State == CardState.Learning) / totalCards * 100 },
                new() { State = "Review", Count = cards.Count(c => c.State == CardState.Review), 
                       Percentage = (double)cards.Count(c => c.State == CardState.Review) / totalCards * 100 },
                new() { State = "Relearning", Count = cards.Count(c => c.State == CardState.Relearning), 
                       Percentage = (double)cards.Count(c => c.State == CardState.Relearning) / totalCards * 100 }
            };
        }

        return stats;
    }

    /// <summary>
    /// Calculates the current review streak in days
    /// </summary>
    private int CalculateStreakDays(List<ReviewLog> reviewLogs)
    {
        if (!reviewLogs.Any()) return 0;

        var reviewDates = reviewLogs
            .Select(r => r.ReviewedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        var streak = 0;
        var currentDate = DateTime.UtcNow.Date;

        foreach (var date in reviewDates)
        {
            if (date == currentDate || date == currentDate.AddDays(-streak))
            {
                streak++;
                currentDate = date;
            }
            else
            {
                break;
            }
        }

        return streak;
    }
}