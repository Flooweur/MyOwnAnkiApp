using FlashcardApi.Data;
using FlashcardApi.Models;
using System.Data.SQLite;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace FlashcardApi.Services;

/// <summary>
/// Service for parsing Anki .apkg files
/// .apkg files are ZIP archives containing:
/// - collection.anki2: SQLite database with deck and card data
/// - media: JSON file mapping media files
/// - Media files (images, audio, etc.)
/// </summary>
public class ApkgParserService : IApkgParserService
{
    private readonly FlashcardDbContext _context;
    private readonly IDeckService _deckService;

    public ApkgParserService(FlashcardDbContext context, IDeckService deckService)
    {
        _context = context;
        _deckService = deckService;
    }

    /// <summary>
    /// Imports an Anki .apkg file and creates a new deck with cards
    /// </summary>
    public async Task<Deck> ImportApkgAsync(Stream apkgStream, string fileName)
    {
        var parsedDeck = await ParseApkgFileAsync(apkgStream);
        
        // Create the deck
        var deckName = string.IsNullOrEmpty(parsedDeck.Name) 
            ? Path.GetFileNameWithoutExtension(fileName) 
            : parsedDeck.Name;
            
        var deck = await _deckService.CreateDeckAsync(deckName, parsedDeck.Description);

        // Import all cards
        foreach (var parsedCard in parsedDeck.Cards)
        {
            var card = new Card
            {
                DeckId = deck.Id,
                Front = CleanHtml(parsedCard.Front),
                Back = CleanHtml(parsedCard.Back),
                State = CardState.New,
                Stability = 0,
                Difficulty = 5,
                Retrievability = 1,
                CreatedAt = DateTime.UtcNow,
                DueDate = DateTime.UtcNow
            };

            _context.Cards.Add(card);
        }

        await _context.SaveChangesAsync();

        return deck;
    }

    /// <summary>
    /// Parses the .apkg file structure
    /// </summary>
    private async Task<ParsedDeck> ParseApkgFileAsync(Stream apkgStream)
    {
        var parsedDeck = new ParsedDeck();

        // Create a temporary directory to extract the .apkg file
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Extract the .apkg file (it's a ZIP archive)
            using (var archive = new ZipArchive(apkgStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                archive.ExtractToDirectory(tempDir);
            }

            // Find and parse the collection database
            var dbPath = Path.Combine(tempDir, "collection.anki2");
            if (!File.Exists(dbPath))
            {
                // Try alternative name
                dbPath = Path.Combine(tempDir, "collection.anki21");
            }

            if (File.Exists(dbPath))
            {
                parsedDeck = await ParseAnki2DatabaseAsync(dbPath);
            }
            else
            {
                throw new InvalidOperationException("Could not find Anki database in .apkg file");
            }
        }
        finally
        {
            // Clean up temporary directory
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        return parsedDeck;
    }

    /// <summary>
    /// Parses the Anki SQLite database to extract deck and card information
    /// </summary>
    private async Task<ParsedDeck> ParseAnki2DatabaseAsync(string dbPath)
    {
        var parsedDeck = new ParsedDeck();
        var cards = new List<ParsedCard>();

        // Open SQLite database
        var connectionString = $"Data Source={dbPath};Version=3;Read Only=True;";
        
        await Task.Run(() =>
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();

            // Get deck name from col table
            using (var cmd = new SQLiteCommand("SELECT decks FROM col LIMIT 1", connection))
            {
                var result = cmd.ExecuteScalar();
                if (result != null)
                {
                    // The decks field is a JSON object, parse it to get the first deck name
                    var decksJson = result.ToString();
                    // Simple extraction - in production, use a JSON parser
                    var nameMatch = Regex.Match(decksJson ?? "", @"""name""\s*:\s*""([^""]+)""");
                    if (nameMatch.Success)
                    {
                        parsedDeck.Name = nameMatch.Groups[1].Value;
                    }
                }
            }

            // If no deck name found, try to get it from the cards table
            if (string.IsNullOrEmpty(parsedDeck.Name))
            {
                using (var cmd = new SQLiteCommand("SELECT DISTINCT did FROM cards LIMIT 1", connection))
                {
                    var deckId = cmd.ExecuteScalar();
                    if (deckId != null)
                    {
                        // Try to get deck name from the decks JSON using the deck ID
                        using (var deckCmd = new SQLiteCommand("SELECT decks FROM col LIMIT 1", connection))
                        {
                            var decksJson = deckCmd.ExecuteScalar()?.ToString();
                            if (!string.IsNullOrEmpty(decksJson))
                            {
                                var deckIdStr = deckId.ToString();
                                var nameMatch = Regex.Match(decksJson, $@"""{deckIdStr}""\s*:\s*{{[^}}]*""name""\s*:\s*""([^""]+)""");
                                if (nameMatch.Success)
                                {
                                    parsedDeck.Name = nameMatch.Groups[1].Value;
                                }
                            }
                        }
                    }
                }
            }

            // Try to get cards by joining notes and cards tables first
            // Anki stores actual card content in the 'notes' table with fields separated by \x1f
            // The 'cards' table contains card instances that reference notes via nid
            var query = @"
                SELECT n.flds, c.did 
                FROM notes n 
                INNER JOIN cards c ON n.id = c.nid 
                WHERE n.flds IS NOT NULL AND n.flds != ''
                ORDER BY c.id";

            try
            {
                using (var cmd = new SQLiteCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    int totalRows = 0;
                    int skippedRows = 0;
                    
                    while (reader.Read())
                    {
                        totalRows++;
                        var fields = reader.GetString(0).Split('\x1f');
                        
                        // Skip system messages and empty cards
                        if (fields.Length == 0 || 
                            (fields.Length == 1 && fields[0].Contains("Please update to the latest Anki version")))
                        {
                            skippedRows++;
                            continue;
                        }
                        
                        if (fields.Length >= 2)
                        {
                            cards.Add(new ParsedCard
                            {
                                Front = fields[0],
                                Back = fields[1]
                            });
                        }
                        else if (fields.Length == 1)
                        {
                            // Single field cards - use same content for front and back
                            cards.Add(new ParsedCard
                            {
                                Front = fields[0],
                                Back = fields[0]
                            });
                        }
                    }
                    
                    // Log debugging information
                    Console.WriteLine($"Total rows processed: {totalRows}, Skipped: {skippedRows}, Cards added: {cards.Count}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with JOIN query: {ex.Message}");
                
                // Fallback: try to get cards directly from notes table
                // This is less accurate but might work for some Anki versions
                using (var cmd = new SQLiteCommand("SELECT flds FROM notes WHERE flds IS NOT NULL AND flds != ''", connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var fields = reader.GetString(0).Split('\x1f');
                        
                        // Skip system messages and empty cards
                        if (fields.Length == 0 || 
                            (fields.Length == 1 && fields[0].Contains("Please update to the latest Anki version")))
                        {
                            continue;
                        }
                        
                        if (fields.Length >= 2)
                        {
                            cards.Add(new ParsedCard
                            {
                                Front = fields[0],
                                Back = fields[1]
                            });
                        }
                        else if (fields.Length == 1)
                        {
                            // Single field cards - use same content for front and back
                            cards.Add(new ParsedCard
                            {
                                Front = fields[0],
                                Back = fields[0]
                            });
                        }
                    }
                }
            }
        });

        parsedDeck.Cards = cards;
        
        // Use default name if none found
        if (string.IsNullOrEmpty(parsedDeck.Name))
        {
            parsedDeck.Name = "Imported Deck";
        }

        return parsedDeck;
    }

    /// <summary>
    /// Cleans HTML tags and formatting from Anki card content
    /// </summary>
    private string CleanHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        // Remove HTML tags but preserve line breaks
        var cleaned = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"<[^>]+>", "");
        
        // Decode HTML entities
        cleaned = System.Net.WebUtility.HtmlDecode(cleaned);
        
        // Trim whitespace
        cleaned = cleaned.Trim();

        return cleaned;
    }
}