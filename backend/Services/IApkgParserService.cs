using FlashcardApi.Models;

namespace FlashcardApi.Services;

/// <summary>
/// Service interface for parsing Anki .apkg files
/// </summary>
public interface IApkgParserService
{
    /// <summary>
    /// Parses an .apkg file and imports it as a new deck
    /// </summary>
    /// <param name="apkgStream">Stream containing the .apkg file data</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>The created deck with imported cards</returns>
    Task<Deck> ImportApkgAsync(Stream apkgStream, string fileName);
    
    /// <summary>
    /// Analyzes an APKG file without importing it (for debugging)
    /// </summary>
    /// <param name="apkgStream">Stream containing the .apkg file data</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>Analysis results</returns>
    Task<Dictionary<string, object>> AnalyzeApkgFileAsync(Stream apkgStream, string fileName);
}

/// <summary>
/// DTO representing a parsed Anki deck
/// </summary>
public class ParsedDeck
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ParsedCard> Cards { get; set; } = new();
}

/// <summary>
/// DTO representing a parsed Anki card
/// </summary>
public class ParsedCard
{
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
}