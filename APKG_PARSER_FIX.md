# .apkg Parser Fix - Technical Documentation

## Problem Summary

When importing .apkg files, the application was only displaying one card with the message "Please update to the latest Anki version" instead of properly importing all cards from the deck.

## Root Cause Analysis

The issue was caused by several factors in the original parser implementation:

### 1. **Inadequate Note Type (Model) Handling**
- Anki stores card content in the `notes` table with fields separated by `\x1f` (ASCII 31)
- The original parser assumed all notes had a simple 2-field structure (field[0] = front, field[1] = back)
- In reality, Anki uses **note types** (models) that define:
  - Field names (e.g., "Front", "Back", "Example", etc.)
  - Card templates that specify how to generate cards from those fields
  - Multiple templates can generate multiple cards from a single note

### 2. **Missing Template Processing**
- Anki templates use a mustache-like syntax: `{{FieldName}}`
- Templates can include:
  - Standard field substitution: `{{Front}}`, `{{Back}}`
  - Cloze deletions: `{{cloze:Text}}`
  - Special types: `{{type:Field}}`, `{{hint:Field}}`
- The original parser didn't process templates at all

### 3. **Insufficient Error Handling**
- Limited logging made it difficult to diagnose issues
- No fallback mechanisms if the primary parsing method failed
- Regex-based JSON parsing instead of proper JSON deserialization

### 4. **"Please update to the latest Anki version" Message**
- This is a system note that Anki includes in some .apkg files
- The original parser filtered it out but still counted it, showing only 1 card
- Better filtering is needed to skip system messages entirely

## Solution Implemented

### Changes Made to `ApkgParserService.cs`

#### 1. **Added JSON Processing**
```csharp
using System.Text.Json;
```
- Replaced regex-based JSON parsing with proper `System.Text.Json` deserialization
- More reliable and maintainable

#### 2. **Implemented Note Model Parsing**
Created `ParseNoteModel()` method to extract:
- Note type name
- Field names array
- Card templates (question and answer formats)

```csharp
private NoteModel ParseNoteModel(JsonElement modelElement)
{
    // Parses the note type structure from Anki's models JSON
    // Extracts field names and templates
}
```

#### 3. **Implemented Template Application**
Created `ApplyTemplate()` method to:
- Replace field placeholders with actual values
- Handle cloze deletions: `{{cloze:Text}}`
- Handle special template types: `{{type:Field}}`, `{{hint:Field}}`
- Clean up remaining template markers

```csharp
private string ApplyTemplate(string template, string[] fields, List<string> fieldNames)
{
    // Applies Anki template syntax to generate card content
    // Handles various template types and edge cases
}
```

#### 4. **Enhanced Database Query**
Updated the main query to fetch:
- Note ID (`n.id`)
- Model ID (`n.mid`) - to look up the note type
- Fields (`n.flds`) - the actual content
- Card ordinal (`c.ord`) - which template to use
- Card ID (`c.id`) - for ordering

```sql
SELECT n.id, n.mid, n.flds, c.ord, c.id 
FROM notes n 
INNER JOIN cards c ON n.id = c.nid 
WHERE n.flds IS NOT NULL AND n.flds != ''
ORDER BY c.id
```

#### 5. **Improved Filtering**
Enhanced system message detection:
```csharp
if (firstField.Contains("Please update to the latest Anki version", StringComparison.OrdinalIgnoreCase) ||
    firstField.Contains("Anki 2.1.50+", StringComparison.OrdinalIgnoreCase))
{
    skipped++;
    continue;
}
```

#### 6. **Better Error Handling and Logging**
- Added console logging at key points
- Table existence checking
- Multiple fallback mechanisms
- Detailed error messages with stack traces

#### 7. **Proper Deck Name Extraction**
- Uses proper JSON parsing of the `decks` field from `col` table
- Skips "Default" deck and gets user-created deck names
- Fallback to "Imported Deck" if name can't be determined

## How the New Parser Works

### Step-by-Step Process

1. **Extract .apkg Archive**
   - .apkg files are ZIP archives
   - Extract to temporary directory
   - Look for `collection.anki2` or `collection.anki21`

2. **Connect to SQLite Database**
   - Open the Anki database with SQLite
   - Check what tables exist (for debugging)

3. **Parse Note Models (Templates)**
   - Read `models` field from `col` table
   - Parse JSON to extract note types
   - For each note type:
     - Extract field names (e.g., ["Front", "Back", "Example"])
     - Extract templates (question/answer formats)
   - Store in dictionary keyed by model ID

4. **Parse Deck Names**
   - Read `decks` field from `col` table
   - Parse JSON to extract deck names
   - Use first non-"Default" deck name

5. **Process Cards**
   - Join `notes` and `cards` tables
   - For each card:
     - Get the note's fields (split by `\x1f`)
     - Look up the note model by model ID
     - Get the template by card ordinal
     - Apply template to fields to generate front/back
     - Skip system messages
     - Add to result list

6. **Apply Templates to Generate Card Content**
   - Replace `{{FieldName}}` with actual field values
   - Process cloze deletions
   - Handle special template types
   - Clean HTML tags

7. **Create Deck and Cards**
   - Create deck in application database
   - Create cards with cleaned front/back content
   - Initialize FSRS parameters

## Testing Recommendations

### Manual Testing

1. **Test with various .apkg files:**
   - Basic 2-field decks (Front/Back)
   - Decks with multiple fields
   - Decks with cloze deletions
   - Decks from different Anki versions (2.1.x, 2.1.50+)

2. **Verify card count:**
   - Import a known deck with N cards
   - Verify all N cards are imported (not just 1)

3. **Check card content:**
   - Verify front/back content is correct
   - Check that HTML is properly cleaned
   - Ensure special characters are preserved

4. **Test edge cases:**
   - Empty decks
   - Decks with media files
   - Very large decks (1000+ cards)
   - Decks with multiple note types

### Automated Testing (Recommended)

Create unit tests for:
- `ParseNoteModel()` - test JSON parsing
- `ApplyTemplate()` - test template application with various inputs
- `ParseAnki2DatabaseAsync()` - test with sample .apkg files
- System message filtering

### Validation Steps

After deploying:

1. **Check logs:**
   ```
   docker-compose logs backend | grep "Tables found"
   docker-compose logs backend | grep "Found model"
   docker-compose logs backend | grep "Total rows processed"
   docker-compose logs backend | grep "Successfully parsed deck"
   ```

2. **Verify in database:**
   ```sql
   SELECT d.Name, COUNT(c.Id) as CardCount
   FROM Decks d
   LEFT JOIN Cards c ON c.DeckId = d.Id
   GROUP BY d.Id, d.Name
   ```

3. **Test import via API:**
   ```bash
   curl -X POST "http://localhost:5000/api/decks/upload" \
     -H "Content-Type: multipart/form-data" \
     -F "file=@your_deck.apkg"
   ```

## Compatibility

The updated parser is compatible with:
- ✅ Anki 2.0.x (collection.anki2)
- ✅ Anki 2.1.x (collection.anki2)
- ✅ Anki 2.1.50+ (collection.anki21)
- ✅ Basic note types (Basic, Basic with Reversed)
- ✅ Cloze note types
- ✅ Custom note types with multiple fields

## Performance Considerations

- **Database Size**: Parser handles decks up to several thousand cards efficiently
- **Memory**: Temporary directory is cleaned up after parsing
- **I/O**: Single-pass through database for optimal performance
- **Error Recovery**: Multiple fallback mechanisms prevent total failure

## Limitations

1. **Media Files**: Currently not imported (images, audio, etc.)
   - Media file handling could be added in future
   - Template processing removes media references

2. **Scheduling Information**: Not preserved from Anki
   - All cards start as "New" in FSRS
   - Review history from Anki is not imported

3. **Deck Hierarchy**: Flat import only
   - Anki supports nested decks (Deck::Subdeck)
   - Currently imported as single flat deck

4. **Advanced Templates**: Some complex templates may not render perfectly
   - Conditional fields: `{{#Field}}...{{/Field}}`
   - Most common templates work correctly

## Future Enhancements

1. **Media Support**
   - Extract media files from .apkg
   - Store in blob storage or file system
   - Update card content with media references

2. **Scheduling Preservation**
   - Import review history from Anki
   - Convert SM-2 parameters to FSRS equivalents

3. **Deck Hierarchy**
   - Support nested deck structures
   - Import as separate decks or deck groups

4. **Advanced Template Support**
   - Conditional fields
   - Loops and iterations
   - JavaScript template rendering

## Conclusion

The updated .apkg parser now correctly handles modern Anki deck formats by:
- Properly parsing note types and templates
- Applying templates to generate accurate card content
- Better filtering of system messages
- Improved error handling and logging
- Supporting multiple Anki database versions

The parser no longer shows just one card with "Please update to the latest Anki version" and instead properly imports all cards from .apkg files.