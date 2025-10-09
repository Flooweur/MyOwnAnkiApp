using FlashcardApi.Models;
using FlashcardApi.Services.FSRS;

namespace FlashcardApi.Services;

/// <summary>
/// Service for managing FSRS scheduling and card reviews with Anki-compatible learning steps
/// </summary>
public class FsrsService : IFsrsService
{
    /// <summary>
    /// Processes a card review and updates all card parameters using FSRS-6 with Anki scheduler extensions
    /// </summary>
    public (Card UpdatedCard, ReviewLog ReviewLog) ReviewCard(Card card, int grade, FsrsParameters parameters)
    {
        var now = DateTime.UtcNow;
        
        // Calculate elapsed time since last review
        double elapsedDays = card.LastReviewedAt.HasValue
            ? (now - card.LastReviewedAt.Value).TotalDays
            : 0;

        int elapsedSeconds = card.LastReviewedAt.HasValue
            ? (int)(now - card.LastReviewedAt.Value).TotalSeconds
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

        // Update card based on state and grade
        double interval = 0;

        if (card.State == CardState.New)
        {
            interval = HandleNewCard(card, grade, parameters);
        }
        else if (card.State == CardState.Learning)
        {
            interval = HandleLearningCard(card, grade, parameters, elapsedSeconds);
        }
        else if (card.State == CardState.Relearning)
        {
            interval = HandleRelearningCard(card, grade, parameters, retrievability, elapsedSeconds);
        }
        else if (card.State == CardState.Review)
        {
            interval = HandleReviewCard(card, grade, parameters, retrievability, isSameDayReview);
        }

        // Apply interval multiplier (except for learning/relearning steps)
        if (card.State == CardState.Review)
        {
            interval *= parameters.IntervalMultiplier;
        }

        // Apply fuzzing if enabled and interval is long enough
        if (parameters.EnableFuzz && interval >= 1.0)
        {
            interval = FsrsAlgorithm.ApplyFuzz(interval, parameters.EnableFuzz);
        }

        // Clamp interval to maximum
        interval = Math.Min(interval, parameters.MaximumInterval);

        // Set next review date based on interval type
        if (interval < 1.0 && (card.State == CardState.Learning || card.State == CardState.Relearning))
        {
            // Sub-day interval (in seconds)
            card.ScheduledSeconds = (int)(interval * 86400);
            card.DueDate = now.AddSeconds(card.ScheduledSeconds);
        }
        else
        {
            // Day-based interval
            card.ScheduledSeconds = 0;
            card.DueDate = now.AddDays(interval);
        }

        card.ElapsedSeconds = elapsedSeconds;
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
    /// Handles a review for a new card
    /// </summary>
    private double HandleNewCard(Card card, int grade, FsrsParameters parameters)
    {
        // Calculate initial FSRS values
        card.Stability = FsrsAlgorithm.CalculateInitialStability(grade, parameters);
        card.Difficulty = FsrsAlgorithm.CalculateInitialDifficulty(grade, parameters);
        card.EaseFactor = parameters.InitialEaseFactor;

        if (grade == 1) // Again
        {
            // Start relearning with first step
            card.State = CardState.Relearning;
            card.CurrentStep = 0;
            return GetStepInterval(parameters.RelearnSteps, 0);
        }
        else if (parameters.LearningSteps.Length > 0)
        {
            // Use learning steps
            card.State = CardState.Learning;
            
            if (grade == 2) // Hard - use first learning step
            {
                card.CurrentStep = 0;
                return GetStepInterval(parameters.LearningSteps, 0);
            }
            else if (grade == 3) // Good
            {
                if (parameters.LearningSteps.Length > 1)
                {
                    // Move to next step
                    card.CurrentStep = 1;
                    return GetStepInterval(parameters.LearningSteps, 1);
                }
                else
                {
                    // Graduate immediately
                    card.State = CardState.Review;
                    card.CurrentStep = 0;
                    return parameters.GraduatingIntervalGood;
                }
            }
            else // Easy (grade 4)
            {
                // Graduate with easy interval
                card.State = CardState.Review;
                card.CurrentStep = 0;
                return parameters.GraduatingIntervalEasy;
            }
        }
        else
        {
            // No learning steps - graduate immediately
            card.State = CardState.Review;
            card.CurrentStep = 0;
            
            if (grade == 4) // Easy
            {
                return parameters.GraduatingIntervalEasy;
            }
            else
            {
                return parameters.GraduatingIntervalGood;
            }
        }
    }

    /// <summary>
    /// Handles a review for a card in learning state
    /// </summary>
    private double HandleLearningCard(Card card, int grade, FsrsParameters parameters, int elapsedSeconds)
    {
        // Update FSRS parameters for short-term reviews
        if (parameters.FsrsShortTermEnabled)
        {
            card.Stability = FsrsAlgorithm.CalculateShortTermStability(card.Stability, grade, parameters);
            card.Difficulty = FsrsAlgorithm.UpdateDifficulty(card.Difficulty, grade, parameters);
        }

        if (grade == 1) // Again - restart learning
        {
            card.CurrentStep = 0;
            card.LapseCount++;
            return GetStepInterval(parameters.LearningSteps, 0);
        }
        else if (grade == 2) // Hard - repeat current step
        {
            return GetStepInterval(parameters.LearningSteps, card.CurrentStep);
        }
        else if (grade == 3) // Good - advance to next step
        {
            card.CurrentStep++;
            
            if (card.CurrentStep >= parameters.LearningSteps.Length)
            {
                // Graduate to review
                card.State = CardState.Review;
                card.CurrentStep = 0;
                return parameters.GraduatingIntervalGood;
            }
            
            return GetStepInterval(parameters.LearningSteps, card.CurrentStep);
        }
        else // Easy (grade 4) - graduate immediately
        {
            card.State = CardState.Review;
            card.CurrentStep = 0;
            return parameters.GraduatingIntervalEasy;
        }
    }

    /// <summary>
    /// Handles a review for a card in relearning state
    /// </summary>
    private double HandleRelearningCard(Card card, int grade, FsrsParameters parameters, double retrievability, int elapsedSeconds)
    {
        // Update FSRS parameters for short-term reviews
        if (parameters.FsrsShortTermEnabled)
        {
            card.Stability = FsrsAlgorithm.CalculateShortTermStability(card.Stability, grade, parameters);
            card.Difficulty = FsrsAlgorithm.UpdateDifficulty(card.Difficulty, grade, parameters);
        }

        if (grade == 1) // Again - restart relearning
        {
            card.CurrentStep = 0;
            card.LapseCount++;
            return GetStepInterval(parameters.RelearnSteps, 0);
        }
        else if (grade == 2) // Hard - repeat current step
        {
            return GetStepInterval(parameters.RelearnSteps, card.CurrentStep);
        }
        else if (grade == 3) // Good - advance to next step
        {
            card.CurrentStep++;
            
            if (card.CurrentStep >= parameters.RelearnSteps.Length)
            {
                // Graduate back to review
                card.State = CardState.Review;
                card.CurrentStep = 0;
                
                // Use lapse interval calculation
                double lapseInterval = card.Stability * parameters.LapseMultiplier;
                lapseInterval = Math.Max(lapseInterval, parameters.MinimumLapseInterval);
                
                // Update ease factor for lapse
                card.EaseFactor = Math.Max(card.EaseFactor - 0.2, 1.3);
                
                return lapseInterval;
            }
            
            return GetStepInterval(parameters.RelearnSteps, card.CurrentStep);
        }
        else // Easy (grade 4) - graduate immediately with good interval
        {
            card.State = CardState.Review;
            card.CurrentStep = 0;
            
            double lapseInterval = card.Stability * parameters.LapseMultiplier;
            lapseInterval = Math.Max(lapseInterval, parameters.MinimumLapseInterval);
            
            return lapseInterval;
        }
    }

    /// <summary>
    /// Handles a review for a card in review state
    /// </summary>
    private double HandleReviewCard(Card card, int grade, FsrsParameters parameters, double retrievability, bool isSameDayReview)
    {
        if (isSameDayReview && parameters.FsrsShortTermEnabled)
        {
            // Short-term review (same day)
            card.Stability = FsrsAlgorithm.CalculateShortTermStability(card.Stability, grade, parameters);
            card.Difficulty = FsrsAlgorithm.UpdateDifficulty(card.Difficulty, grade, parameters);
            
            return FsrsAlgorithm.CalculateInterval(card.Stability, parameters.RequestRetention, parameters.Weights[20]);
        }
        
        if (grade == 1) // Again - lapse
        {
            // Calculate post-lapse stability
            card.Stability = FsrsAlgorithm.CalculatePostLapseStability(
                card.Stability, card.Difficulty, retrievability, parameters);
            
            card.Difficulty = FsrsAlgorithm.UpdateDifficulty(card.Difficulty, grade, parameters);
            card.LapseCount++;
            
            // Update ease factor
            card.EaseFactor = Math.Max(card.EaseFactor - 0.2, 1.3);
            
            // Check for leech
            if (card.LapseCount >= parameters.LeechThreshold && 
                (card.LapseCount - parameters.LeechThreshold) % (parameters.LeechThreshold / 2 == 0 ? 1 : parameters.LeechThreshold / 2) == 0)
            {
                // Mark as leech (in Anki this would suspend the card, but we just track it)
            }
            
            // Enter relearning if steps are configured
            if (parameters.RelearnSteps.Length > 0)
            {
                card.State = CardState.Relearning;
                card.CurrentStep = 0;
                return GetStepInterval(parameters.RelearnSteps, 0);
            }
            
            // Otherwise stay in review with reduced interval
            double lapseInterval = card.Stability * parameters.LapseMultiplier;
            return Math.Max(lapseInterval, parameters.MinimumLapseInterval);
        }
        else
        {
            // Successful recall - calculate new stability
            card.Stability = FsrsAlgorithm.CalculateNextStability(
                card.Stability, card.Difficulty, retrievability, grade, parameters);
            
            card.Difficulty = FsrsAlgorithm.UpdateDifficulty(card.Difficulty, grade, parameters);
            
            // Calculate base interval
            double baseInterval = FsrsAlgorithm.CalculateInterval(
                card.Stability, parameters.RequestRetention, parameters.Weights[20]);
            
            // Apply grade-specific multipliers for non-FSRS calculation
            double interval = baseInterval;
            
            if (grade == 2) // Hard
            {
                interval *= parameters.HardMultiplier;
                card.EaseFactor = Math.Max(card.EaseFactor - 0.15, 1.3);
            }
            else if (grade == 3) // Good
            {
                // Use FSRS interval as-is
            }
            else if (grade == 4) // Easy
            {
                interval *= parameters.EasyMultiplier;
                card.EaseFactor += 0.15;
            }
            
            return interval;
        }
    }

    /// <summary>
    /// Gets the interval for a specific learning/relearning step
    /// </summary>
    private double GetStepInterval(double[] steps, int stepIndex)
    {
        if (steps.Length == 0 || stepIndex < 0 || stepIndex >= steps.Length)
            return 1.0; // Default to 1 day
        
        // Steps are in minutes, return as fraction of a day
        return steps[stepIndex] / (24.0 * 60.0);
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

        // Calculate intervals for each grade by simulating what would happen
        for (int grade = 1; grade <= 4; grade++)
        {
            var testCard = CloneCard(card);
            double interval = 0;

            // Calculate elapsed time
            double elapsedDays = card.LastReviewedAt.HasValue
                ? (DateTime.UtcNow - card.LastReviewedAt.Value).TotalDays
                : 0;

            int elapsedSeconds = card.LastReviewedAt.HasValue
                ? (int)(DateTime.UtcNow - card.LastReviewedAt.Value).TotalSeconds
                : 0;

            bool isSameDayReview = card.LastReviewedAt.HasValue && 
                                   card.LastReviewedAt.Value.Date == DateTime.UtcNow.Date;

            double retrievability = card.ReviewCount > 0
                ? FsrsAlgorithm.CalculateRetrievability(elapsedDays, card.Stability, parameters.Weights[20])
                : 1.0;

            // Simulate the review for this grade
            if (card.State == CardState.New)
            {
                interval = HandleNewCard(testCard, grade, parameters);
            }
            else if (card.State == CardState.Learning)
            {
                interval = HandleLearningCard(testCard, grade, parameters, elapsedSeconds);
            }
            else if (card.State == CardState.Relearning)
            {
                interval = HandleRelearningCard(testCard, grade, parameters, retrievability, elapsedSeconds);
            }
            else if (card.State == CardState.Review)
            {
                interval = HandleReviewCard(testCard, grade, parameters, retrievability, isSameDayReview);
            }

            // Apply interval multiplier for review state
            if (testCard.State == CardState.Review)
            {
                interval *= parameters.IntervalMultiplier;
            }

            // Apply fuzzing if enabled and interval is long enough
            if (parameters.EnableFuzz && interval >= 1.0)
            {
                interval = FsrsAlgorithm.ApplyFuzz(interval, parameters.EnableFuzz);
            }

            // Clamp interval to maximum
            interval = Math.Min(interval, parameters.MaximumInterval);

            // Convert to TimeSpan
            if (interval < 1.0)
            {
                // Sub-day interval (in seconds)
                intervals[grade] = TimeSpan.FromSeconds(interval * 86400);
            }
            else
            {
                intervals[grade] = TimeSpan.FromDays(interval);
            }
        }

        return intervals;
    }

    /// <summary>
    /// Creates a shallow clone of a card for interval calculation
    /// </summary>
    private Card CloneCard(Card card)
    {
        return new Card
        {
            Id = card.Id,
            DeckId = card.DeckId,
            Front = card.Front,
            Back = card.Back,
            Stability = card.Stability,
            Difficulty = card.Difficulty,
            Retrievability = card.Retrievability,
            ReviewCount = card.ReviewCount,
            LapseCount = card.LapseCount,
            State = card.State,
            EaseFactor = card.EaseFactor,
            CurrentStep = card.CurrentStep,
            ScheduledSeconds = card.ScheduledSeconds,
            ElapsedSeconds = card.ElapsedSeconds,
            DueDate = card.DueDate,
            LastReviewedAt = card.LastReviewedAt,
            CreatedAt = card.CreatedAt
        };
    }
}