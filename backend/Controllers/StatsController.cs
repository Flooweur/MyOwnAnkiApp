using FlashcardApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlashcardApi.Controllers;

/// <summary>
/// API controller for statistics and analytics
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;
    private readonly ILogger<StatsController> _logger;

    public StatsController(IStatsService statsService, ILogger<StatsController> logger)
    {
        _statsService = statsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets daily review statistics for a deck
    /// </summary>
    /// <param name="deckId">Deck ID</param>
    /// <param name="days">Number of days to retrieve (default 30)</param>
    [HttpGet("deck/{deckId}/daily")]
    public async Task<ActionResult<List<DailyStats>>> GetDailyStats(int deckId, [FromQuery] int days = 30)
    {
        try
        {
            var stats = await _statsService.GetDailyStatsAsync(deckId, days);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching daily stats for deck {DeckId}", deckId);
            return StatusCode(500, "An error occurred while fetching statistics");
        }
    }

    /// <summary>
    /// Gets retention rate statistics for a deck
    /// </summary>
    /// <param name="deckId">Deck ID</param>
    [HttpGet("deck/{deckId}/retention")]
    public async Task<ActionResult<RetentionStats>> GetRetentionStats(int deckId)
    {
        try
        {
            var stats = await _statsService.GetRetentionStatsAsync(deckId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching retention stats for deck {DeckId}", deckId);
            return StatusCode(500, "An error occurred while fetching retention statistics");
        }
    }

    /// <summary>
    /// Gets comprehensive deck statistics
    /// </summary>
    /// <param name="deckId">Deck ID</param>
    [HttpGet("deck/{deckId}/overview")]
    public async Task<ActionResult<DeckOverviewStats>> GetDeckOverview(int deckId)
    {
        try
        {
            var stats = await _statsService.GetDeckOverviewAsync(deckId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching deck overview for deck {DeckId}", deckId);
            return StatusCode(500, "An error occurred while fetching deck overview");
        }
    }
}

/// <summary>
/// Daily statistics for a deck
/// </summary>
public class DailyStats
{
    public string Date { get; set; } = string.Empty;
    public int CardsReviewed { get; set; }
    public int CardsAgain { get; set; }
    public int CardsHard { get; set; }
    public int CardsGood { get; set; }
    public int CardsEasy { get; set; }
    public double AverageRetention { get; set; }
    public int TimeSpentMinutes { get; set; }
}

/// <summary>
/// Retention statistics
/// </summary>
public class RetentionStats
{
    public double OverallRetention { get; set; }
    public double Last7DaysRetention { get; set; }
    public double Last30DaysRetention { get; set; }
    public List<GradeDistribution> GradeDistribution { get; set; } = new();
}

/// <summary>
/// Grade distribution
/// </summary>
public class GradeDistribution
{
    public string Grade { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Comprehensive deck overview
/// </summary>
public class DeckOverviewStats
{
    public int TotalCards { get; set; }
    public int TotalReviews { get; set; }
    public double AverageRetention { get; set; }
    public double AverageDifficulty { get; set; }
    public int StreakDays { get; set; }
    public List<CardStateDistribution> StateDistribution { get; set; } = new();
}

/// <summary>
/// Card state distribution
/// </summary>
public class CardStateDistribution
{
    public string State { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}