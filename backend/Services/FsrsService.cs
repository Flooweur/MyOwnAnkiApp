using FlashcardApi.Models;
using FlashcardApi.Services.FSRS;

namespace FlashcardApi.Services;

/// <summary>
/// Service for managing FSRS scheduling and card reviews
/// </summary>
public class FsrsService : IFsrsService
{
    /// <summary>
    /// Processes a card review and updates all card parameters using FSRS-6
    /// </summary>
    public (Card UpdatedCard, ReviewLog ReviewLog) ReviewCard(Card card, int grade, FsrsParameters parameters)
    {
        var now = DateTime.UtcNow;
        
        // Calculate elapsed time since last review
        double elapsedDays = card.LastReviewedAt.HasValue
            ? (now - card.LastReviewedAt.Value).TotalDays
            : 0;

        // Determine if this is a same-day review
        bool isSameDayReview = card.LastReviewedAt.HasValue && 
                               card.LastReviewedAt.Value.Date == now.Date;

        // Store previous values for the review log
        var stateBefore = card.State;
        var stabilityBefore = card.Stability;
        var difficultyBefore = card.Difficulty;
        
        // Calculate current retrievability if card has been reviewed before
        double retrievability = card.ReviewCount > 0
            ? FsrsAlgorithm.CalculateRetrievability(elapsedDays, card.Stability, parameters.Weights[20])
            : 1.0;

        card.Retrievability = retrievability;

        // Update stability based on review grade
        if (card.State == CardState.New)
        {
            // First review: use initial stability
            card.Stability = FsrsAlgorithm.CalculateInitialStability(grade, parameters);
            card.Difficulty = FsrsAlgorithm.CalculateInitialDifficulty(grade, parameters);
            card.State = grade == 1 ? CardState.Relearning : CardState.Learning;
        }
        else if (isSameDayReview)
        {
            // Short-term review (same day)
            card.Stability = FsrsAlgorithm.CalculateShortTermStability(card.Stability, grade, parameters);
            card.Difficulty = FsrsAlgorithm.UpdateDifficulty(card.Difficulty, grade, parameters);
        }
        else
        {
            // Regular review
            if (grade == 1)
            {
                // Lapse (forgot the card)
                card.Stability = FsrsAlgorithm.CalculatePostLapseStability(
                    card.Stability, card.Difficulty, retrievability, parameters);
                card.State = CardState.Relearning;
                card.LapseCount++;
            }
            else
            {
                // Successful recall
                card.Stability = FsrsAlgorithm.CalculateNextStability(
                    card.Stability, card.Difficulty, retrievability, grade, parameters);
                card.State = CardState.Review;
            }
            
            // Update difficulty
            card.Difficulty = FsrsAlgorithm.UpdateDifficulty(card.Difficulty, grade, parameters);
        }

        // Calculate next review interval
        double interval = FsrsAlgorithm.CalculateInterval(card.Stability, parameters.RequestRetention, parameters.Weights[20]);
        
        // Apply fuzzing if enabled
        if (parameters.EnableFuzz)
        {
            interval = FsrsAlgorithm.ApplyFuzz(interval, parameters.EnableFuzz);
        }

        // Clamp interval to maximum
        interval = Math.Min(interval, parameters.MaximumInterval);

        // Set next review date
        card.DueDate = now.AddDays(interval);
        card.LastReviewedAt = now;
        card.ReviewCount++;

        // Create review log
        var reviewLog = new ReviewLog
        {
            CardId = card.Id,
            Grade = grade,
            StateBefore = stateBefore,
            StateAfter = card.State,
            StabilityBefore = stabilityBefore,
            StabilityAfter = card.Stability,
            DifficultyBefore = difficultyBefore,
            DifficultyAfter = card.Difficulty,
            Retrievability = retrievability,
            ScheduledInterval = interval,
            ReviewedAt = now
        };

        return (card, reviewLog);
    }

    /// <summary>
    /// Gets the default FSRS parameters
    /// </summary>
    public FsrsParameters GetDefaultParameters()
    {
        return new FsrsParameters();
    }

    /// <summary>
    /// Calculates the current retrievability for a card
    /// </summary>
    public double GetCurrentRetrievability(Card card, FsrsParameters parameters)
    {
        if (card.ReviewCount == 0 || !card.LastReviewedAt.HasValue)
            return 1.0;

        double elapsedDays = (DateTime.UtcNow - card.LastReviewedAt.Value).TotalDays;
        return FsrsAlgorithm.CalculateRetrievability(elapsedDays, card.Stability, parameters.Weights[20]);
    }

    /// <summary>
    /// Calculates scheduling intervals for all grades without updating the card
    /// </summary>
    public Dictionary<int, TimeSpan> CalculateSchedulingIntervals(Card card, FsrsParameters parameters)
    {
        var intervals = new Dictionary<int, TimeSpan>();
        var now = DateTime.UtcNow;

        // Calculate elapsed time since last review
        double elapsedDays = card.LastReviewedAt.HasValue
            ? (now - card.LastReviewedAt.Value).TotalDays
            : 0;

        // Determine if this is a same-day review
        bool isSameDayReview = card.LastReviewedAt.HasValue && 
                               card.LastReviewedAt.Value.Date == now.Date;

        // Calculate current retrievability if card has been reviewed before
        double retrievability = card.ReviewCount > 0
            ? FsrsAlgorithm.CalculateRetrievability(elapsedDays, card.Stability, parameters.Weights[20])
            : 1.0;

        // Calculate intervals for each grade (1=Again, 2=Hard, 3=Good, 4=Easy)
        for (int grade = 1; grade <= 4; grade++)
        {
            double stability;

            if (card.State == CardState.New)
            {
                // First review: use initial stability
                stability = FsrsAlgorithm.CalculateInitialStability(grade, parameters);
            }
            else if (isSameDayReview)
            {
                // Short-term review (same day)
                stability = FsrsAlgorithm.CalculateShortTermStability(card.Stability, grade, parameters);
            }
            else
            {
                // Regular review
                if (grade == 1)
                {
                    // Lapse (forgot the card)
                    stability = FsrsAlgorithm.CalculatePostLapseStability(
                        card.Stability, card.Difficulty, retrievability, parameters);
                }
                else
                {
                    // Successful recall
                    stability = FsrsAlgorithm.CalculateNextStability(
                        card.Stability, card.Difficulty, retrievability, grade, parameters);
                }
            }

            // Calculate next review interval
            double interval = FsrsAlgorithm.CalculateInterval(stability, parameters.RequestRetention, parameters.Weights[20]);
            
            // Apply fuzzing if enabled
            if (parameters.EnableFuzz)
            {
                interval = FsrsAlgorithm.ApplyFuzz(interval, parameters.EnableFuzz);
            }

            // Clamp interval to maximum
            interval = Math.Min(interval, parameters.MaximumInterval);

            intervals[grade] = TimeSpan.FromDays(interval);
        }

        return intervals;
    }
}