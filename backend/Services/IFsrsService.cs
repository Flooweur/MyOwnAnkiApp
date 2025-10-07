using FlashcardApi.Models;
using FlashcardApi.Services.FSRS;

namespace FlashcardApi.Services;

/// <summary>
/// Service interface for FSRS scheduling operations
/// </summary>
public interface IFsrsService
{
    /// <summary>
    /// Processes a card review and updates card parameters
    /// </summary>
    /// <param name="card">Card being reviewed</param>
    /// <param name="grade">Review grade (1=Again, 2=Hard, 3=Good, 4=Easy)</param>
    /// <param name="parameters">FSRS parameters for scheduling</param>
    /// <returns>Updated card and review log</returns>
    (Card UpdatedCard, ReviewLog ReviewLog) ReviewCard(Card card, int grade, FsrsParameters parameters);

    /// <summary>
    /// Gets the default FSRS parameters
    /// </summary>
    FsrsParameters GetDefaultParameters();

    /// <summary>
    /// Calculates current retrievability for a card
    /// </summary>
    double GetCurrentRetrievability(Card card, FsrsParameters parameters);

    /// <summary>
    /// Calculates scheduling intervals for all grades without updating the card
    /// </summary>
    Dictionary<int, TimeSpan> CalculateSchedulingIntervals(Card card, FsrsParameters parameters);
}