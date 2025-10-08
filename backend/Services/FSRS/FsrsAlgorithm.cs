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
    /// Formula: R = (1 + factor * t/S)^(-w20)
    /// where factor = 0.9^(-1/w20) - 1 to ensure R(S,S) = 90%
    /// </summary>
    /// <param name="elapsedDays">Days since last review</param>
    /// <param name="stability">Current memory stability</param>
    /// <param name="w20">Forgetting curve shape parameter</param>
    /// <returns>Retrievability between 0 and 1</returns>
    public static double CalculateRetrievability(double elapsedDays, double stability, double w20)
    {
        if (stability <= 0) return 0;
        
        // FSRS-6 forgetting curve: R = (1 + factor * t/S)^(-w20)
        // where factor = 0.9^(-1/w20) - 1 to ensure R(S,S) = 90%
        double factor = Math.Pow(0.9, -1.0 / w20) - 1.0;
        double base_value = 1.0 + factor * elapsedDays / stability;
        return Math.Pow(base_value, -w20);
    }

    /// <summary>
    /// Calculates the interval in days until next review
    /// FSRS-6 Formula: Solving R = (1 + factor * t/S)^(-w20) for t
    /// Result: I = S/factor * (r^(-1/w20) - 1)
    /// where r is desired retention and factor = 0.9^(-1/w20) - 1
    /// </summary>
    /// <param name="stability">Current memory stability</param>
    /// <param name="desiredRetention">Target retention rate (e.g., 0.9)</param>
    /// <param name="w20">Forgetting curve shape parameter</param>
    /// <returns>Interval in days</returns>
    public static double CalculateInterval(double stability, double desiredRetention, double w20)
    {
        // When desired retention is >= 1.0, return stability
        if (desiredRetention >= 1.0) return stability;
        if (desiredRetention <= 0.0) return FsrsConstants.MinInterval;
        
        // FSRS-6 interval formula: I = S/factor * (r^(-1/w20) - 1)
        double factor = Math.Pow(0.9, -1.0 / w20) - 1.0;
        double interval = stability / factor * (Math.Pow(desiredRetention, -1.0 / w20) - 1.0);
        
        return Math.Max(FsrsConstants.MinInterval, interval);
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
    /// FSRS-5/6 Formula: D0(G) = w4 - e^(w5 * (G - 1)) + 1
    /// where w4 = D0(1), i.e., the initial difficulty when the first rating is Again
    /// </summary>
    /// <param name="grade">Review grade (1=Again, 2=Hard, 3=Good, 4=Easy)</param>
    /// <param name="parameters">FSRS parameters</param>
    /// <returns>Initial difficulty value (1-10)</returns>
    public static double CalculateInitialDifficulty(int grade, FsrsParameters parameters)
    {
        // FSRS-5/6 Formula: D0(G) = w4 - e^(w5 * (G - 1)) + 1
        double difficulty = parameters.Weights[4] - Math.Exp(parameters.Weights[5] * (grade - 1)) + 1;
        
        // Clamp to valid range [1, 10]
        return Math.Clamp(difficulty, FsrsConstants.MinDifficulty, FsrsConstants.MaxDifficulty);
    }

    /// <summary>
    /// Updates difficulty after a review
    /// FSRS-5/6 Formula with linear damping:
    /// ΔD(G) = -w6 * (G - 3)
    /// D' = D + ΔD * (10 - D) / 9
    /// D'' = w7 * D0(4) + (1 - w7) * D'
    /// Mean reversion to D0(4) instead of D0(3)
    /// </summary>
    /// <param name="currentDifficulty">Current difficulty value</param>
    /// <param name="grade">Review grade (1=Again, 2=Hard, 3=Good, 4=Easy)</param>
    /// <param name="parameters">FSRS parameters</param>
    /// <returns>Updated difficulty value</returns>
    public static double UpdateDifficulty(double currentDifficulty, int grade, FsrsParameters parameters)
    {
        // FSRS-5/6 difficulty update with linear damping
        // ΔD(G) = -w6 * (G - 3)
        // This means:
        // - Again (G=1): ΔD = -w6 * (-2) = 2*w6 (increase)
        // - Hard (G=2): ΔD = -w6 * (-1) = w6 (increase)
        // - Good (G=3): ΔD = -w6 * 0 = 0 (no change)
        // - Easy (G=4): ΔD = -w6 * 1 = -w6 (decrease)
        double deltaD = -parameters.Weights[6] * (grade - FsrsConstants.GradeGood);
        
        // Apply linear damping: D' = D + ΔD * (10 - D) / 9
        double nextDifficulty = currentDifficulty + deltaD * (FsrsConstants.MaxDifficulty - currentDifficulty) / 9.0;

        // Calculate D0(4) for mean reversion target (FSRS-5/6 uses D0(4) instead of D0(3))
        double d0_easy = CalculateInitialDifficulty(FsrsConstants.GradeEasy, parameters);
        
        // Apply mean reversion towards D0(4)
        // Formula: D'' = w7 * D0(4) + (1 - w7) * D'
        nextDifficulty = parameters.Weights[7] * d0_easy + 
                        (1 - parameters.Weights[7]) * nextDifficulty;

        // Clamp to valid range [1, 10]
        return Math.Clamp(nextDifficulty, FsrsConstants.MinDifficulty, FsrsConstants.MaxDifficulty);
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
    /// FSRS-6 Formula: S' = S * e^(w17 * (G - 3 + w18)) * S^(-w19)
    /// Stability increases faster when it's small and slower when it's large.
    /// S will converge when SInc = e^(w17 * (G - 3 + w18)) * S^(-w19) = 1
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
        // FSRS-6 Formula: S' = S * e^(w17 * (G - 3 + w18)) * S^(-w19)
        double stabilityIncrease = Math.Exp(parameters.Weights[17] * (grade - 3 + parameters.Weights[18])) * 
                                   Math.Pow(currentStability, -parameters.Weights[19]);
        double newStability = currentStability * stabilityIncrease;

        // Good and Easy (G >= 3) should ensure SInc >= 1 (stability doesn't decrease)
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
        if (!enableFuzz || interval < FsrsConstants.MinFuzzInterval) return interval;

        // Apply ±5% fuzz using a static Random instance to avoid predictable values
        double fuzzRange = FsrsConstants.MaxFuzzFactor - FsrsConstants.MinFuzzFactor;
        double fuzzFactor = FsrsConstants.MinFuzzFactor + _random.NextDouble() * fuzzRange;
        return interval * fuzzFactor;
    }

    // Static Random instance to ensure proper randomization across rapid calls
    private static readonly Random _random = new Random();
}