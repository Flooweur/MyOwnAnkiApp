namespace FlashcardApi.Services.FSRS;

/// <summary>
/// Implementation of the FSRS-6 (Free Spaced Repetition Scheduler) algorithm
/// Based on research by Jarrett Ye and LMSherlock
/// </summary>
public static class FsrsAlgorithm
{
    /// <summary>
    /// Calculates retrievability (R) using the FSRS-6 forgetting curve
    /// R represents the probability of successfully recalling a card
    /// Formula: R = (1 + (t/(9*S))^w20)^-1
    /// where t is elapsed time and S is stability
    /// </summary>
    /// <param name="elapsedDays">Days since last review</param>
    /// <param name="stability">Current memory stability</param>
    /// <param name="w20">Forgetting curve shape parameter</param>
    /// <returns>Retrievability between 0 and 1</returns>
    public static double CalculateRetrievability(double elapsedDays, double stability, double w20)
    {
        if (stability <= 0) return 0;
        
        // FSRS-6 forgetting curve: R = (1 + (t/(9*S))^w20)^-1
        double exponent = Math.Pow(elapsedDays / (9.0 * stability), w20);
        return Math.Pow(1.0 + exponent, -1.0);
    }

    /// <summary>
    /// Calculates the interval in days until next review
    /// Formula: I = S * (ln(DR) / ln(0.9))
    /// where DR is desired retention (request retention)
    /// </summary>
    /// <param name="stability">Current memory stability</param>
    /// <param name="desiredRetention">Target retention rate (e.g., 0.9)</param>
    /// <returns>Interval in days</returns>
    public static double CalculateInterval(double stability, double desiredRetention)
    {
        // When desired retention is 0.9 (90%), interval equals stability
        // Formula: I = S * (ln(DR) / ln(0.9))
        if (desiredRetention >= 1.0) return stability;
        
        double interval = stability * (Math.Log(desiredRetention) / Math.Log(0.9));
        return Math.Max(1, interval); // Minimum interval of 1 day
    }

    /// <summary>
    /// Calculates initial stability after the first review
    /// Different values for each grade (Again, Hard, Good, Easy)
    /// </summary>
    /// <param name="grade">Review grade (1=Again, 2=Hard, 3=Good, 4=Easy)</param>
    /// <param name="parameters">FSRS parameters</param>
    /// <returns>Initial stability value</returns>
    public static double CalculateInitialStability(int grade, FsrsParameters parameters)
    {
        // Use w0-w3 based on grade
        return grade switch
        {
            1 => parameters.Weights[0], // Again
            2 => parameters.Weights[1], // Hard
            3 => parameters.Weights[2], // Good
            4 => parameters.Weights[3], // Easy
            _ => parameters.Weights[2]  // Default to Good
        };
    }

    /// <summary>
    /// Calculates initial difficulty after the first review
    /// Formula: D0 = w4 - w5 * (G - 3)
    /// where G is grade (1-4), clamped to 1-10 range
    /// </summary>
    /// <param name="grade">Review grade (1=Again, 2=Hard, 3=Good, 4=Easy)</param>
    /// <param name="parameters">FSRS parameters</param>
    /// <returns>Initial difficulty value (1-10)</returns>
    public static double CalculateInitialDifficulty(int grade, FsrsParameters parameters)
    {
        // Formula: D0 = w4 - w5 * (G - 3)
        double difficulty = parameters.Weights[4] - parameters.Weights[5] * (grade - 3);
        
        // Clamp to valid range [1, 10]
        return Math.Clamp(difficulty, 1.0, 10.0);
    }

    /// <summary>
    /// Updates difficulty after a review
    /// Applies grade-based change with linear damping and mean reversion
    /// </summary>
    /// <param name="currentDifficulty">Current difficulty value</param>
    /// <param name="grade">Review grade (1=Again, 2=Hard, 3=Good, 4=Easy)</param>
    /// <param name="parameters">FSRS parameters</param>
    /// <returns>Updated difficulty value</returns>
    public static double UpdateDifficulty(double currentDifficulty, int grade, FsrsParameters parameters)
    {
        // Calculate difficulty change based on grade
        double difficultyChange = grade switch
        {
            1 => parameters.Weights[5],  // Again: add a lot
            2 => parameters.Weights[6],  // Hard: add a little
            3 => 0,                       // Good: no change
            4 => -parameters.Weights[6], // Easy: subtract a little
            _ => 0
        };

        // Apply linear damping: as D approaches 10, changes get smaller
        // This prevents D from ever reaching exactly 10
        double nextDifficulty = currentDifficulty + difficultyChange * (10 - currentDifficulty) / 10.0;

        // Apply mean reversion towards default difficulty (w4)
        // Formula: D'' = w7 * D0 + (1 - w7) * D'
        nextDifficulty = parameters.Weights[7] * parameters.Weights[4] + 
                        (1 - parameters.Weights[7]) * nextDifficulty;

        // Clamp to valid range [1, 10]
        return Math.Clamp(nextDifficulty, 1.0, 10.0);
    }

    /// <summary>
    /// Calculates the new stability after a successful review (Hard, Good, or Easy)
    /// Main FSRS formula: S' = S * (e^w8 * (11 - D) * S^-w9 * (e^(w10 * (1 - R)) - 1) * w15 * w16 + 1)
    /// </summary>
    /// <param name="currentStability">Current memory stability</param>
    /// <param name="difficulty">Current difficulty</param>
    /// <param name="retrievability">Retrievability at time of review</param>
    /// <param name="grade">Review grade (2=Hard, 3=Good, 4=Easy)</param>
    /// <param name="parameters">FSRS parameters</param>
    /// <returns>Updated stability value</returns>
    public static double CalculateNextStability(
        double currentStability,
        double difficulty,
        double retrievability,
        int grade,
        FsrsParameters parameters)
    {
        // Grade-specific multipliers
        double w15 = grade == 2 ? parameters.Weights[15] : 1.0; // Hard multiplier
        double w16 = grade == 4 ? parameters.Weights[16] : 1.0; // Easy multiplier

        // Function of difficulty: f(D) = (11 - D)
        double difficultyFactor = 11.0 - difficulty;

        // Function of stability: f(S) = S^(-w9)
        // As S increases, this factor decreases (stability saturation)
        double stabilityFactor = Math.Pow(currentStability, -parameters.Weights[9]);

        // Function of retrievability: f(R) = e^(w10 * (1 - R)) - 1
        // Lower R means higher increase (spacing effect)
        double retrievabilityFactor = Math.Exp(parameters.Weights[10] * (1.0 - retrievability)) - 1.0;

        // Stability increase factor
        // SInc = e^w8 * f(D) * f(S) * f(R) * w15 * w16 + 1
        double stabilityIncrease = Math.Exp(parameters.Weights[8]) *
                                  difficultyFactor *
                                  stabilityFactor *
                                  retrievabilityFactor *
                                  w15 *
                                  w16 + 1.0;

        // New stability: S' = S * SInc
        return currentStability * stabilityIncrease;
    }

    /// <summary>
    /// Calculates stability after a lapse (Again grade)
    /// Formula: S' = min(S, w11 * D^(-w12) * ((S+1)^w13 - 1) * e^(w14 * (1-R)))
    /// </summary>
    /// <param name="currentStability">Current memory stability</param>
    /// <param name="difficulty">Current difficulty</param>
    /// <param name="retrievability">Retrievability at time of review</param>
    /// <param name="parameters">FSRS parameters</param>
    /// <returns>Post-lapse stability value</returns>
    public static double CalculatePostLapseStability(
        double currentStability,
        double difficulty,
        double retrievability,
        FsrsParameters parameters)
    {
        // Formula: S' = w11 * D^(-w12) * ((S+1)^w13 - 1) * e^(w14 * (1-R))
        double postLapseStability = parameters.Weights[11] *
                                   Math.Pow(difficulty, -parameters.Weights[12]) *
                                   (Math.Pow(currentStability + 1, parameters.Weights[13]) - 1) *
                                   Math.Exp(parameters.Weights[14] * (1.0 - retrievability));

        // Post-lapse stability cannot exceed pre-lapse stability
        return Math.Min(currentStability, postLapseStability);
    }

    /// <summary>
    /// Calculates stability for short-term (same-day) reviews
    /// Formula: S' = S * e^(w17 * (G - 3 + w18))
    /// With additional check that Good/Easy cannot decrease stability
    /// </summary>
    /// <param name="currentStability">Current memory stability</param>
    /// <param name="grade">Review grade (1=Again, 2=Hard, 3=Good, 4=Easy)</param>
    /// <param name="parameters">FSRS parameters</param>
    /// <returns>Updated stability for short-term review</returns>
    public static double CalculateShortTermStability(
        double currentStability,
        int grade,
        FsrsParameters parameters)
    {
        // Formula: S' = S * e^(w17 * (G - 3 + w18))
        // w19 dampens the effect for higher stability values
        double exponent = parameters.Weights[17] * (grade - 3 + parameters.Weights[18]);
        double newStability = currentStability * Math.Exp(exponent / (1 + parameters.Weights[19] * currentStability));

        // Good and Easy (G >= 3) cannot decrease stability
        if (grade >= 3)
        {
            return Math.Max(currentStability, newStability);
        }

        return newStability;
    }

    /// <summary>
    /// Applies fuzzing to the interval to distribute reviews more evenly
    /// Adds randomness within ±5% of the interval
    /// </summary>
    /// <param name="interval">Base interval in days</param>
    /// <param name="enableFuzz">Whether to apply fuzzing</param>
    /// <returns>Fuzzed interval</returns>
    public static double ApplyFuzz(double interval, bool enableFuzz)
    {
        if (!enableFuzz || interval < 2.5) return interval;

        // Apply ±5% fuzz using a static Random instance to avoid predictable values
        double fuzzFactor = 0.95 + _random.NextDouble() * 0.1; // 0.95 to 1.05
        return interval * fuzzFactor;
    }

    // Static Random instance to ensure proper randomization across rapid calls
    private static readonly Random _random = new Random();
}