using FlashcardApi.Data;
using FlashcardApi.Models;
using System.Data.SQLite;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Text.Json;

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

            // First, let's check what tables exist in the database
            var tables = new List<string>();
            using (var cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table'", connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            
            Console.WriteLine($"Tables found in database: {string.Join(", ", tables)}");

            // Parse models (note types) from the collection to understand field mappings
            Dictionary<long, NoteModel> noteModels = new Dictionary<long, NoteModel>();
            try
            {
                using (var cmd = new SQLiteCommand("SELECT models FROM col LIMIT 1", connection))
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        var modelsJson = result.ToString();
                        if (!string.IsNullOrEmpty(modelsJson))
                        {
                            try
                            {
                                var modelsDoc = JsonDocument.Parse(modelsJson);
                                foreach (var modelProp in modelsDoc.RootElement.EnumerateObject())
                                {
                                    if (long.TryParse(modelProp.Name, out var modelId))
                                    {
                                        var model = ParseNoteModel(modelProp.Value);
                                        noteModels[modelId] = model;
                                        Console.WriteLine($"Found model: {model.Name} with {model.FieldNames.Count} fields and {model.Templates.Count} templates");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing models JSON: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading models: {ex.Message}");
            }

            // Get deck name from col table
            try
            {
                using (var cmd = new SQLiteCommand("SELECT decks FROM col LIMIT 1", connection))
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        var decksJson = result.ToString();
                        if (!string.IsNullOrEmpty(decksJson))
                        {
                            try
                            {
                                var decksDoc = JsonDocument.Parse(decksJson);
                                // Get the first non-default deck
                                foreach (var deckProp in decksDoc.RootElement.EnumerateObject())
                                {
                                    if (deckProp.Value.TryGetProperty("name", out var nameElement))
                                    {
                                        var name = nameElement.GetString();
                                        if (!string.IsNullOrEmpty(name) && name != "Default")
                                        {
                                            parsedDeck.Name = name;
                                            break;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing decks JSON: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading decks: {ex.Message}");
            }

            // Now get cards by joining notes and cards tables
            // We need to get: note fields, note model id, and card template ordinal
            var query = @"
                SELECT n.id, n.mid, n.flds, c.ord, c.id 
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
                        
                        var noteId = reader.GetInt64(0);
                        var modelId = reader.GetInt64(1);
                        var fieldsStr = reader.GetString(2);
                        var cardOrd = reader.GetInt32(3);
                        
                        var fields = fieldsStr.Split('\x1f');
                        
                        // Skip empty cards or system messages
                        if (fields.Length == 0)
                        {
                            skippedRows++;
                            continue;
                        }

                        // Check if this is a system message
                        var firstField = fields[0]?.Trim() ?? "";
                        if (firstField.Contains("Please update to the latest Anki version", StringComparison.OrdinalIgnoreCase) ||
                            firstField.Contains("Anki 2.1.50+", StringComparison.OrdinalIgnoreCase))
                        {
                            skippedRows++;
                            continue;
                        }

                        // Try to use the model template to generate the card
                        string front = "";
                        string back = "";
                        
                        if (noteModels.TryGetValue(modelId, out var model) && cardOrd < model.Templates.Count)
                        {
                            var template = model.Templates[cardOrd];
                            front = ApplyTemplate(template.QuestionFormat, fields, model.FieldNames);
                            back = ApplyTemplate(template.AnswerFormat, fields, model.FieldNames);
                        }
                        else
                        {
                            // Fallback: use simple field mapping
                            if (fields.Length >= 2)
                            {
                                front = fields[0];
                                back = fields[1];
                            }
                            else if (fields.Length == 1)
                            {
                                front = fields[0];
                                back = fields[0];
                            }
                            else
                            {
                                skippedRows++;
                                continue;
                            }
                        }

                        // Only add if both front and back have content
                        if (!string.IsNullOrWhiteSpace(front) && !string.IsNullOrWhiteSpace(back))
                        {
                            cards.Add(new ParsedCard
                            {
                                Front = front,
                                Back = back
                            });
                        }
                        else
                        {
                            skippedRows++;
                        }
                    }
                    
                    Console.WriteLine($"Total rows processed: {totalRows}, Skipped: {skippedRows}, Cards added: {cards.Count}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with main query: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Fallback: try simpler approach
                try
                {
                    var simpleQuery = "SELECT flds FROM notes WHERE flds IS NOT NULL AND flds != ''";
                    using (var cmd = new SQLiteCommand(simpleQuery, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var fieldsStr = reader.GetString(0);
                            var fields = fieldsStr.Split('\x1f');
                            
                            if (fields.Length < 1)
                                continue;

                            var firstField = fields[0]?.Trim() ?? "";
                            if (firstField.Contains("Please update to the latest Anki version", StringComparison.OrdinalIgnoreCase))
                                continue;
                            
                            if (fields.Length >= 2)
                            {
                                cards.Add(new ParsedCard
                                {
                                    Front = fields[0],
                                    Back = fields[1]
                                });
                            }
                            else if (fields.Length == 1 && !string.IsNullOrWhiteSpace(fields[0]))
                            {
                                cards.Add(new ParsedCard
                                {
                                    Front = fields[0],
                                    Back = fields[0]
                                });
                            }
                        }
                    }
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"Fallback query also failed: {fallbackEx.Message}");
                }
            }
        });

        parsedDeck.Cards = cards;
        
        // Use default name if none found
        if (string.IsNullOrEmpty(parsedDeck.Name))
        {
            parsedDeck.Name = "Imported Deck";
        }

        Console.WriteLine($"Successfully parsed deck '{parsedDeck.Name}' with {parsedDeck.Cards.Count} cards");

        return parsedDeck;
    }

    /// <summary>
    /// Parses a note model (note type) from JSON
    /// </summary>
    private NoteModel ParseNoteModel(JsonElement modelElement)
    {
        var model = new NoteModel();
        
        if (modelElement.TryGetProperty("name", out var nameElement))
        {
            model.Name = nameElement.GetString() ?? "Unknown";
        }
        
        if (modelElement.TryGetProperty("flds", out var fieldsElement) && fieldsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var field in fieldsElement.EnumerateArray())
            {
                if (field.TryGetProperty("name", out var fieldName))
                {
                    model.FieldNames.Add(fieldName.GetString() ?? "");
                }
            }
        }
        
        if (modelElement.TryGetProperty("tmpls", out var templatesElement) && templatesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var tmpl in templatesElement.EnumerateArray())
            {
                var template = new CardTemplate();
                
                if (tmpl.TryGetProperty("qfmt", out var qfmt))
                {
                    template.QuestionFormat = qfmt.GetString() ?? "";
                }
                
                if (tmpl.TryGetProperty("afmt", out var afmt))
                {
                    template.AnswerFormat = afmt.GetString() ?? "";
                }
                
                model.Templates.Add(template);
            }
        }
        
        return model;
    }

    /// <summary>
    /// Applies an Anki template to fields to generate card content
    /// </summary>
    private string ApplyTemplate(string template, string[] fields, List<string> fieldNames)
    {
        if (string.IsNullOrEmpty(template))
            return "";
        
        var result = template;
        
        // Replace field references like {{Field1}} with actual field values
        for (int i = 0; i < fieldNames.Count && i < fields.Length; i++)
        {
            var fieldName = fieldNames[i];
            var fieldValue = fields[i];
            
            // Handle standard field substitution
            result = result.Replace($"{{{{{fieldName}}}}}", fieldValue);
            
            // Handle cloze deletions {{cloze:Text}}
            result = Regex.Replace(result, @"\{\{cloze:([^}]+)\}\}", m => {
                var fieldRef = m.Groups[1].Value;
                var idx = fieldNames.IndexOf(fieldRef);
                if (idx >= 0 && idx < fields.Length)
                {
                    // Remove cloze markers like {{c1::text}}
                    return Regex.Replace(fields[idx], @"\{\{c\d+::([^}]+)\}\}", "$1");
                }
                return "";
            });
            
            // Handle other template types (type:, hint:, etc.)
            result = Regex.Replace(result, @"\{\{(?:type|hint|text):([^}]+)\}\}", m => {
                var fieldRef = m.Groups[1].Value;
                var idx = fieldNames.IndexOf(fieldRef);
                if (idx >= 0 && idx < fields.Length)
                {
                    return fields[idx];
                }
                return "";
            });
        }
        
        // Remove any remaining template markers
        result = Regex.Replace(result, @"\{\{[^}]+\}\}", "");
        
        // Clean up the result
        result = result.Trim();
        
        return result;
    }

    /// <summary>
    /// Represents an Anki note model (note type)
    /// </summary>
    private class NoteModel
    {
        public string Name { get; set; } = "";
        public List<string> FieldNames { get; set; } = new List<string>();
        public List<CardTemplate> Templates { get; set; } = new List<CardTemplate>();
    }

    /// <summary>
    /// Represents a card template
    /// </summary>
    private class CardTemplate
    {
        public string QuestionFormat { get; set; } = "";
        public string AnswerFormat { get; set; } = "";
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