using FlashcardApi.Controllers;

namespace FlashcardApi.Services;

/// <summary>
/// Interface for statistics service
/// </summary>
public interface IStatsService
{
    /// <summary>
    /// Gets daily statistics for a deck
    /// </summary>
    Task<List<DailyStats>> GetDailyStatsAsync(int deckId, int days);

    /// <summary>
    /// Gets retention statistics for a deck
    /// </summary>
    Task<RetentionStats> GetRetentionStatsAsync(int deckId);

    /// <summary>
    /// Gets comprehensive deck overview
    /// </summary>
    Task<DeckOverviewStats> GetDeckOverviewAsync(int deckId);
}