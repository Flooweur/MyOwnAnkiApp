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
    private readonly ILogger<ApkgParserService> _logger;

    public ApkgParserService(FlashcardDbContext context, IDeckService deckService, ILogger<ApkgParserService> logger)
    {
        _context = context;
        _deckService = deckService;
        _logger = logger;
    }

    /// <summary>
    /// Imports an Anki .apkg file and creates a new deck with cards
    /// </summary>
    public async Task<Deck> ImportApkgAsync(Stream apkgStream, string fileName)
    {
        _logger.LogInformation("Starting APKG import for file: {FileName}", fileName);
        
        var parsedDeck = await ParseApkgFileAsync(apkgStream);
        
        _logger.LogInformation("Parsed deck '{DeckName}' with {CardCount} cards from file {FileName}", 
            parsedDeck.Name, parsedDeck.Cards.Count, fileName);
        
        // Create the deck
        var deckName = string.IsNullOrEmpty(parsedDeck.Name) 
            ? Path.GetFileNameWithoutExtension(fileName) 
            : parsedDeck.Name;
            
        _logger.LogInformation("Creating deck '{DeckName}' in database", deckName);
        var deck = await _deckService.CreateDeckAsync(deckName, parsedDeck.Description);

        // Import all cards
        _logger.LogInformation("Importing {CardCount} cards to deck {DeckId}", parsedDeck.Cards.Count, deck.Id);
        
        int importedCount = 0;
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
            importedCount++;
            
            // Log every 10th card for progress tracking
            if (importedCount % 10 == 0)
            {
                _logger.LogDebug("Imported {ImportedCount}/{TotalCount} cards", importedCount, parsedDeck.Cards.Count);
            }
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Successfully imported {ImportedCount} cards to deck '{DeckName}' (ID: {DeckId})", 
            importedCount, deck.Name, deck.Id);

        return deck;
    }

    /// <summary>
    /// Parses the .apkg file structure
    /// </summary>
    private async Task<ParsedDeck> ParseApkgFileAsync(Stream apkgStream)
    {
        _logger.LogInformation("Starting APKG file parsing");
        
        var parsedDeck = new ParsedDeck();

        // Create a temporary directory to extract the .apkg file
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        _logger.LogDebug("Created temporary directory: {TempDir}", tempDir);

        try
        {
            // Extract the .apkg file (it's a ZIP archive)
            _logger.LogInformation("Extracting APKG file contents");
            using (var archive = new ZipArchive(apkgStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                var entryCount = archive.Entries.Count;
                _logger.LogInformation("APKG archive contains {EntryCount} entries", entryCount);
                
                // Log all entries for debugging
                foreach (var entry in archive.Entries)
                {
                    _logger.LogDebug("Archive entry: {EntryName} ({EntryLength} bytes)", entry.Name, entry.Length);
                }
                
                archive.ExtractToDirectory(tempDir);
            }

            // Find and parse the collection database
            var dbPath = Path.Combine(tempDir, "collection.anki21");
            if (!File.Exists(dbPath))
            {
                // Try alternative name
                dbPath = Path.Combine(tempDir, "collection.anki2");
                _logger.LogDebug("collection.anki21 not found, trying collection.anki2");
            }

            if (File.Exists(dbPath))
            {
                _logger.LogInformation("Found Anki database at: {DbPath}", dbPath);
                var fileInfo = new FileInfo(dbPath);
                _logger.LogInformation("Database file size: {FileSize} bytes", fileInfo.Length);
                
                parsedDeck = await ParseAnki2DatabaseAsync(dbPath);
            }
            else
            {
                _logger.LogError("Could not find Anki database file in APKG archive");
                throw new InvalidOperationException("Could not find Anki database in .apkg file");
            }
        }
        finally
        {
            // Clean up temporary directory
            try
            {
                Directory.Delete(tempDir, true);
                _logger.LogDebug("Cleaned up temporary directory: {TempDir}", tempDir);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temporary directory: {TempDir}", tempDir);
            }
        }

        _logger.LogInformation("Completed APKG file parsing");
        return parsedDeck;
    }

    /// <summary>
    /// Parses the Anki SQLite database to extract deck and card information
    /// </summary>
    private async Task<ParsedDeck> ParseAnki2DatabaseAsync(string dbPath)
    {
        _logger.LogInformation("Starting Anki database parsing from: {DbPath}", dbPath);
        
        var parsedDeck = new ParsedDeck();
        var cards = new List<ParsedCard>();

        // Open SQLite database
        var connectionString = $"Data Source={dbPath};Version=3;Read Only=True;";
        
        await Task.Run(() =>
        {
            using var connection = new SQLiteConnection(connectionString);
            connection.Open();
            _logger.LogDebug("Connected to Anki SQLite database");

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
            
            _logger.LogInformation("Tables found in database: {Tables}", string.Join(", ", tables));

            // Parse models (note types) from the collection to understand field mappings
            Dictionary<long, NoteModel> noteModels = new Dictionary<long, NoteModel>();
            try
            {
                _logger.LogDebug("Parsing note models from collection");
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
                                        _logger.LogInformation("Found model: {ModelName} with {FieldCount} fields and {TemplateCount} templates", 
                                            model.Name, model.FieldNames.Count, model.Templates.Count);
                                    }
                                }
                                _logger.LogInformation("Parsed {ModelCount} note models", noteModels.Count);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error parsing models JSON");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading models from collection");
            }

            // Get deck name from col table
            try
            {
                _logger.LogDebug("Parsing deck information from collection");
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
                                            _logger.LogInformation("Found deck name: {DeckName}", name);
                                            break;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error parsing decks JSON");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading decks from collection");
            }

            // First, let's check how many notes and cards exist
            int noteCount = 0;
            int cardCount = 0;
            
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM notes", connection))
            {
                noteCount = Convert.ToInt32(cmd.ExecuteScalar());
            }
            
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM cards", connection))
            {
                cardCount = Convert.ToInt32(cmd.ExecuteScalar());
            }
            
            _logger.LogInformation("Database contains {NoteCount} notes and {CardCount} cards", noteCount, cardCount);
            
            // Let's also check for non-empty notes
            int nonEmptyNoteCount = 0;
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM notes WHERE flds IS NOT NULL AND flds != ''", connection))
            {
                nonEmptyNoteCount = Convert.ToInt32(cmd.ExecuteScalar());
            }
            
            _logger.LogInformation("Database contains {NonEmptyNoteCount} non-empty notes", nonEmptyNoteCount);
            
            // Detect the field separator by analyzing sample data
            char detectedSeparator = '\x1f'; // Default Anki separator
            try
            {
                using (var cmd = new SQLiteCommand("SELECT flds FROM notes WHERE flds IS NOT NULL AND flds != '' LIMIT 5", connection))
                using (var reader = cmd.ExecuteReader())
                {
                    var separators = new char[] { '\x1f', '\x1e', '\x1d', '\t' };
                    var separatorCounts = new Dictionary<char, int>();
                    
                    foreach (var sep in separators)
                        separatorCounts[sep] = 0;
                    
                    while (reader.Read())
                    {
                        var fieldsStr = reader.GetString(0);
                        foreach (var sep in separators)
                        {
                            var parts = fieldsStr.Split(sep);
                            if (parts.Length > 1) // More than one field suggests this is the separator
                            {
                                separatorCounts[sep]++;
                            }
                        }
                    }
                    
                    // Find the separator with the most occurrences
                    detectedSeparator = separatorCounts.OrderByDescending(kvp => kvp.Value).First().Key;
                    _logger.LogInformation("Detected field separator: {Separator} (0x{Separator:X2}) with {Count} occurrences", 
                        detectedSeparator, (int)detectedSeparator, separatorCounts[detectedSeparator]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not detect field separator, using default");
            }

            // Now get cards by joining notes and cards tables
            // We need to get: note fields, note model id, and card template ordinal
            var query = @"
                SELECT n.id, n.mid, n.flds, c.ord, c.id 
                FROM notes n 
                INNER JOIN cards c ON n.id = c.nid 
                WHERE n.flds IS NOT NULL AND n.flds != ''
                ORDER BY c.id";

            _logger.LogInformation("Starting card parsing with query: {Query}", query);

            try
            {
                using (var cmd = new SQLiteCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    int totalRows = 0;
                    int skippedRows = 0;
                    int processedCards = 0;
                    
                    _logger.LogDebug("Executing card query");
                    
                    while (reader.Read())
                    {
                        totalRows++;
                        
                        var noteId = reader.GetInt64(0);
                        var modelId = reader.GetInt64(1);
                        var fieldsStr = reader.GetString(2);
                        var cardOrd = reader.GetInt32(3);
                        
                        var fields = fieldsStr.Split(detectedSeparator);
                        
                        // Log detailed info for first few rows
                        if (totalRows <= 5)
                        {
                            _logger.LogInformation("Row {RowNumber}: NoteId={NoteId}, ModelId={ModelId}, CardOrd={CardOrd}, FieldsCount={FieldsCount}", 
                                totalRows, noteId, modelId, cardOrd, fields.Length);
                            _logger.LogInformation("Fields string: {FieldsStr}", fieldsStr);
                            for (int i = 0; i < fields.Length; i++)
                            {
                                _logger.LogInformation("Field {FieldIndex}: '{FieldValue}'", i, fields[i]);
                            }
                        }
                        
                        // Log every 100th card for progress tracking
                        if (totalRows % 100 == 0)
                        {
                            _logger.LogDebug("Processed {ProcessedRows} rows, found {CardCount} cards so far", totalRows, cards.Count);
                        }
                        
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
                            _logger.LogDebug("Skipped system message card: {FirstField}", firstField.Substring(0, Math.Min(50, firstField.Length)));
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
                            
                            // Log template details for first few cards
                            if (totalRows <= 5)
                            {
                                _logger.LogInformation("Used template for card {NoteId}: Model={ModelName}, TemplateOrd={CardOrd}", noteId, model.Name, cardOrd);
                                _logger.LogInformation("Question format: {QuestionFormat}", template.QuestionFormat);
                                _logger.LogInformation("Answer format: {AnswerFormat}", template.AnswerFormat);
                                _logger.LogInformation("Generated front: '{Front}'", front);
                                _logger.LogInformation("Generated back: '{Back}'", back);
                            }
                            else
                            {
                                _logger.LogTrace("Used template for card {NoteId} with model {ModelName}", noteId, model.Name);
                            }
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
                            
                            // Log fallback details for first few cards
                            if (totalRows <= 5)
                            {
                                _logger.LogInformation("Used fallback mapping for card {NoteId}: ModelId={ModelId}, CardOrd={CardOrd}", noteId, modelId, cardOrd);
                                _logger.LogInformation("Model found: {ModelFound}, Template count: {TemplateCount}", 
                                    noteModels.ContainsKey(modelId), 
                                    noteModels.ContainsKey(modelId) ? noteModels[modelId].Templates.Count : 0);
                                _logger.LogInformation("Generated front: '{Front}'", front);
                                _logger.LogInformation("Generated back: '{Back}'", back);
                            }
                            else
                            {
                                _logger.LogTrace("Used fallback mapping for card {NoteId}", noteId);
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
                            processedCards++;
                            
                            // Log first few cards for debugging
                            if (processedCards <= 3)
                            {
                                _logger.LogInformation("Added card {CardNumber}: Front='{Front}' Back='{Back}'", 
                                    processedCards, 
                                    front.Substring(0, Math.Min(50, front.Length)), 
                                    back.Substring(0, Math.Min(50, back.Length)));
                            }
                        }
                        else
                        {
                            skippedRows++;
                            if (totalRows <= 10) // Log details for first 10 skipped cards
                            {
                                _logger.LogInformation("Skipped card {NoteId}: Front='{Front}' Back='{Back}' (FrontEmpty={FrontEmpty}, BackEmpty={BackEmpty})", 
                                    noteId, front, back, string.IsNullOrWhiteSpace(front), string.IsNullOrWhiteSpace(back));
                            }
                            else
                            {
                                _logger.LogTrace("Skipped empty card {NoteId}: Front='{Front}' Back='{Back}'", 
                                    noteId, front, back);
                            }
                        }
                    }
                    
                    _logger.LogInformation("Card parsing completed: Total rows processed: {TotalRows}, Skipped: {SkippedRows}, Cards added: {CardCount}", 
                        totalRows, skippedRows, cards.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error with main card query");
                
                // Fallback: try simpler approach
                try
                {
                    _logger.LogWarning("Attempting fallback query for card parsing");
                    
                    // Try different field separators that might be used
                    var separators = new char[] { '\x1f', '\x1e', '\x1d', '\t' };
                    
                    foreach (var separator in separators)
                    {
                        _logger.LogInformation("Trying fallback with separator: {Separator} (0x{Separator:X2})", separator, (int)separator);
                        
                        var simpleQuery = "SELECT flds FROM notes WHERE flds IS NOT NULL AND flds != ''";
                        using (var cmd = new SQLiteCommand(simpleQuery, connection))
                        using (var reader = cmd.ExecuteReader())
                        {
                            int fallbackCount = 0;
                            while (reader.Read())
                            {
                                var fieldsStr = reader.GetString(0);
                                var fields = fieldsStr.Split(separator);
                                
                                if (fields.Length < 1)
                                    continue;

                                var firstField = fields[0]?.Trim() ?? "";
                                if (firstField.Contains("Please update to the latest Anki version", StringComparison.OrdinalIgnoreCase) ||
                                    firstField.Contains("Anki 2.1.50+", StringComparison.OrdinalIgnoreCase))
                                    continue;
                                
                                if (fields.Length >= 2)
                                {
                                    cards.Add(new ParsedCard
                                    {
                                        Front = fields[0],
                                        Back = fields[1]
                                    });
                                    fallbackCount++;
                                }
                                else if (fields.Length == 1 && !string.IsNullOrWhiteSpace(fields[0]))
                                {
                                    cards.Add(new ParsedCard
                                    {
                                        Front = fields[0],
                                        Back = fields[0]
                                    });
                                    fallbackCount++;
                                }
                            }
                            
                            if (fallbackCount > 0)
                            {
                                _logger.LogInformation("Fallback query with separator {Separator} completed, added {FallbackCount} cards", separator, fallbackCount);
                                break; // Stop trying other separators if we found cards
                            }
                            else
                            {
                                _logger.LogInformation("Fallback query with separator {Separator} found no cards", separator);
                            }
                        }
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback query also failed");
                }
            }
        });

        parsedDeck.Cards = cards;
        
        // Use default name if none found
        if (string.IsNullOrEmpty(parsedDeck.Name))
        {
            parsedDeck.Name = "Imported Deck";
            _logger.LogWarning("No deck name found in APKG file, using default name: {DefaultName}", parsedDeck.Name);
        }

        _logger.LogInformation("Successfully parsed deck '{DeckName}' with {CardCount} cards", 
            parsedDeck.Name, parsedDeck.Cards.Count);

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
    /// Debug method to analyze an APKG file without importing it
    /// </summary>
    public async Task<Dictionary<string, object>> AnalyzeApkgFileAsync(Stream apkgStream, string fileName)
    {
        _logger.LogInformation("Starting APKG file analysis for: {FileName}", fileName);
        
        var analysis = new Dictionary<string, object>();
        
        // Create a temporary directory to extract the .apkg file
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Extract the .apkg file
            using (var archive = new ZipArchive(apkgStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                var entryCount = archive.Entries.Count;
                analysis["ArchiveEntries"] = entryCount;
                
                var entries = new List<string>();
                foreach (var entry in archive.Entries)
                {
                    entries.Add($"{entry.Name} ({entry.Length} bytes)");
                }
                analysis["ArchiveEntryList"] = entries;
                
                archive.ExtractToDirectory(tempDir);
            }

            // Find and analyze the collection database
            var dbPath = Path.Combine(tempDir, "collection.anki21");
            if (!File.Exists(dbPath))
            {
                dbPath = Path.Combine(tempDir, "collection.anki2");
            }

            if (File.Exists(dbPath))
            {
                analysis["DatabasePath"] = dbPath;
                analysis["DatabaseSize"] = new FileInfo(dbPath).Length;
                
                // Analyze the database
                var connectionString = $"Data Source={dbPath};Version=3;Read Only=True;";
                
                await Task.Run(() =>
                {
                    using var connection = new SQLiteConnection(connectionString);
                    connection.Open();
                    
                    // Get table information
                    var tables = new List<string>();
                    using (var cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table'", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }
                    analysis["Tables"] = tables;
                    
                    // Get counts
                    if (tables.Contains("notes"))
                    {
                        using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM notes", connection))
                        {
                            analysis["NoteCount"] = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        
                        using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM notes WHERE flds IS NOT NULL AND flds != ''", connection))
                        {
                            analysis["NonEmptyNoteCount"] = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }
                    
                    if (tables.Contains("cards"))
                    {
                        using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM cards", connection))
                        {
                            analysis["CardCount"] = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }
                    
                    // Sample some field data
                    if (tables.Contains("notes"))
                    {
                        using (var cmd = new SQLiteCommand("SELECT flds FROM notes WHERE flds IS NOT NULL AND flds != '' LIMIT 3", connection))
                        using (var reader = cmd.ExecuteReader())
                        {
                            var sampleFields = new List<string>();
                            while (reader.Read())
                            {
                                var fieldsStr = reader.GetString(0);
                                sampleFields.Add(fieldsStr);
                            }
                            analysis["SampleFields"] = sampleFields;
                        }
                    }
                });
            }
            else
            {
                analysis["Error"] = "Could not find Anki database file";
            }
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temporary directory");
            }
        }
        
        return analysis;
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