namespace FlashcardApi.Services.FSRS;

/// <summary>
/// FSRS-6 algorithm parameters (w0-w20)
/// These parameters are optimizable and can be customized per deck
/// </summary>
public class FsrsParameters
{
    /// <summary>
    /// Default FSRS-6 parameters based on research data
    /// w0-w3: Initial stability for Again, Hard, Good, Easy
    /// w4: Default difficulty value (D0(1) in FSRS-5/6)
    /// w5-w7: Difficulty adjustment weights
    /// w8-w14: Stability calculation weights
    /// w15-w16: Grade weights for Hard/Easy
    /// w17-w18: Short-term review weights
    /// w19: Short-term stability dampening
    /// w20: Forgetting curve shape parameter
    /// </summary>
    public double[] Weights { get; set; } = new double[]
    {
        // FSRS-6 default parameters from official research
        // w0-w3: Initial stability values for each grade (Again, Hard, Good, Easy)
        0.212,    // w0: Initial stability for "Again"
        1.2931,   // w1: Initial stability for "Hard"
        2.3065,   // w2: Initial stability for "Good"
        8.2956,   // w3: Initial stability for "Easy"
        
        // w4-w7: Difficulty calculation weights
        6.4133,   // w4: Initial difficulty D0(1) - difficulty when first rating is Again
        0.8334,   // w5: Exponential factor in initial difficulty formula
        3.0194,   // w6: Difficulty change rate
        0.001,    // w7: Mean reversion weight for difficulty
        
        // w8-w14: Stability calculation weights
        1.8722,   // w8: Overall stability increase scale
        0.1666,   // w9: Stability saturation exponent
        0.796,    // w10: Retrievability (spacing effect) weight
        1.4835,   // w11: Post-lapse stability scale
        0.0614,   // w12: Post-lapse difficulty exponent
        0.2629,   // w13: Post-lapse stability exponent
        1.6483,   // w14: Post-lapse retrievability weight
        
        // w15-w16: Grade-specific multipliers
        0.6014,   // w15: Hard grade multiplier (< 1)
        1.8729,   // w16: Easy grade multiplier (> 1)
        
        // w17-w19: Short-term (same-day) review parameters
        0.5425,   // w17: Short-term stability change rate
        0.0912,   // w18: Short-term grade offset
        0.0658,   // w19: Short-term stability dampening exponent
        
        // w20: Forgetting curve shape parameter
        0.1542    // w20: Forgetting curve decay exponent
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