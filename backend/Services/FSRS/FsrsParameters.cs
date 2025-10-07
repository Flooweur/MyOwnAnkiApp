namespace FlashcardApi.Services.FSRS;

/// <summary>
/// FSRS-6 algorithm parameters (w0-w20)
/// These parameters are optimizable and can be customized per deck
/// </summary>
public class FsrsParameters
{
    /// <summary>
    /// Default FSRS parameters based on research data
    /// w0-w3: Initial stability for Again, Hard, Good, Easy
    /// w4: Default difficulty value
    /// w5-w7: Difficulty adjustment weights
    /// w8-w14: Stability calculation weights
    /// w15-w16: Grade weights for Hard/Easy
    /// w17-w18: Short-term review weights
    /// w19: Short-term stability dampening
    /// w20: Forgetting curve shape parameter
    /// </summary>
    public double[] Weights { get; set; } = new double[]
    {
        // w0-w3: Initial stability values for each grade (Again, Hard, Good, Easy)
        0.4,    // w0: Initial stability for "Again"
        0.6,    // w1: Initial stability for "Hard"
        2.4,    // w2: Initial stability for "Good"
        5.8,    // w3: Initial stability for "Easy"
        
        // w4: Default difficulty (mid-point of 1-10 range)
        4.93,   // w4: Default difficulty value
        
        // w5-w7: Difficulty calculation weights
        0.94,   // w5: Difficulty weight for Again grade
        0.86,   // w6: Difficulty weight for Hard grade
        0.01,   // w7: Mean reversion weight for difficulty
        
        // w8-w14: Stability increase calculation weights
        1.49,   // w8: Overall stability increase scale
        0.14,   // w9: Difficulty impact on stability increase
        0.94,   // w10: Stability saturation weight
        2.18,   // w11: Post-lapse stability scale
        0.05,   // w12: Post-lapse minimum stability
        0.34,   // w13: Post-lapse difficulty exponent
        1.26,   // w14: Post-lapse stability exponent
        
        // w15-w16: Grade-specific multipliers
        0.29,   // w15: Hard grade multiplier (< 1)
        2.61,   // w16: Easy grade multiplier (> 1)
        
        // w17-w19: Short-term review parameters
        0.5,    // w17: Short-term Hard/Again impact
        1.5,    // w18: Short-term Good/Easy impact
        0.5,    // w19: Short-term stability dampening
        
        // w20: Forgetting curve shape (personalized, typically 0.1-0.8)
        0.2     // w20: Forgetting curve exponent
    };

    /// <summary>
    /// Request retention rate (typically 0.9 for 90%)
    /// This determines how aggressively cards are scheduled
    /// </summary>
    public double RequestRetention { get; set; } = 0.9;

    /// <summary>
    /// Maximum interval in days
    /// </summary>
    public double MaximumInterval { get; set; } = 36500; // ~100 years

    /// <summary>
    /// Enable fuzzing of intervals to distribute reviews
    /// </summary>
    public bool EnableFuzz { get; set; } = true;
}