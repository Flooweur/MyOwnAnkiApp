namespace FlashcardApi.Services.FSRS;

/// <summary>
/// Constants used in the FSRS-6 algorithm
/// </summary>
public static class FsrsConstants
{
    /// <summary>
    /// Minimum stability value (in days)
    /// </summary>
    public const double MinStability = 0.1;

    /// <summary>
    /// Minimum difficulty value (1-10 scale)
    /// </summary>
    public const double MinDifficulty = 1.0;

    /// <summary>
    /// Maximum difficulty value (1-10 scale)
    /// </summary>
    public const double MaxDifficulty = 10.0;

    /// <summary>
    /// Minimum interval between reviews (in days)
    /// </summary>
    public const double MinInterval = 1.0;

    /// <summary>
    /// Stability threshold for "mastered" cards (in days)
    /// </summary>
    public const double MasteredStabilityThreshold = 100.0;

    /// <summary>
    /// Factor used in retrievability calculation
    /// </summary>
    public const double RetrievabilityFactor = 9.0;

    /// <summary>
    /// Default request retention rate
    /// </summary>
    public const double DefaultRequestRetention = 0.9;

    /// <summary>
    /// Minimum fuzz factor for interval randomization
    /// </summary>
    public const double MinFuzzFactor = 0.95;

    /// <summary>
    /// Maximum fuzz factor for interval randomization
    /// </summary>
    public const double MaxFuzzFactor = 1.05;

    /// <summary>
    /// Minimum interval for fuzzing to be applied (in days)
    /// </summary>
    public const double MinFuzzInterval = 2.5;

    /// <summary>
    /// Review grade: Again (completely forgot)
    /// </summary>
    public const int GradeAgain = 1;

    /// <summary>
    /// Review grade: Hard (recalled with difficulty)
    /// </summary>
    public const int GradeHard = 2;

    /// <summary>
    /// Review grade: Good (recalled correctly)
    /// </summary>
    public const int GradeGood = 3;

    /// <summary>
    /// Review grade: Easy (recalled easily)
    /// </summary>
    public const int GradeEasy = 4;

    /// <summary>
    /// Minimum valid grade
    /// </summary>
    public const int MinGrade = 1;

    /// <summary>
    /// Maximum valid grade
    /// </summary>
    public const int MaxGrade = 4;
}