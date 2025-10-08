# Media Implementation Summary

## Changes Made

### 1. Fixed Card Display (Horizontal Overflow)

**File: `frontend/src/components/ReviewPage.css`**

Updated the card container and wrapper styles to properly display both question and answer cards side-by-side with horizontal scrolling:

- Changed `.card-container` to use `overflow-x: auto` and `justify-content: flex-start`
- Updated `.cards-wrapper` to use `width: fit-content` when showing answer
- Changed `.card-face` from `flex: 1` to `flex: 0 0 auto` with fixed width of 600px
- Added proper responsive handling for mobile devices

**Result:** Both cards now display side-by-side with horizontal scrolling when the answer is revealed, while remaining centered.

### 2. Media Handling in .apkg Files

#### Research Findings:
- .apkg files contain a `media` JSON file mapping numerical IDs to filenames
- Media files are stored with numerical names (0, 1, 2, etc.) in the zip
- Card content references media using:
  - `<img src="filename.jpg">` for images
  - `[sound:filename.mp3]` for audio files

#### Backend Implementation:

**File: `backend/Services/ApkgParserService.cs`**

Added media extraction and processing functionality:

1. **Media Storage Setup:**
   - Created `_mediaBasePath` in `wwwroot/media` directory
   - Each deck gets a unique media subdirectory (using GUID)

2. **New Methods:**
   - `ParseMediaMappingAsync()`: Parses the media JSON file from .apkg
   - `ExtractMediaFilesAsync()`: Copies media files to deck directory
   - `ProcessMediaReferences()`: Updates card content to reference correct media paths
     - Converts image src to `/media/{deckId}/{filename}`
     - Converts `[sound:file]` to HTML5 audio tags

3. **Updated CleanHtml():**
   - Preserves HTML tags when media is present
   - Only removes HTML for pure text cards

**File: `backend/Models/Deck.cs`**

Added new property:
```csharp
public string? MediaDirectory { get; set; }
```

**File: `backend/Migrations/20251008000000_AddMediaDirectory.cs`**

Created migration to add `MediaDirectory` column to Decks table.

**File: `backend/Services/IApkgParserService.cs`**

Updated `ParsedDeck` class:
```csharp
public Dictionary<string, string> MediaMapping { get; set; } = new();
public string? MediaDirectory { get; set; }
```

**File: `backend/Program.cs`**

Added static file serving middleware:
```csharp
app.UseStaticFiles();
```

#### Frontend Implementation:

**File: `frontend/src/components/ReviewPage.tsx`**

Updated card rendering to support HTML content:
- Changed from plain text rendering to `dangerouslySetInnerHTML`
- Both question and answer cards now render HTML with embedded media

**File: `frontend/src/components/ReviewPage.css`**

Added media-specific styles:
```css
.card-content img {
  max-width: 100%;
  max-height: 300px;
  object-fit: contain;
  border-radius: 8px;
  margin: 0.5rem 0;
}

.card-content audio {
  width: 100%;
  max-width: 400px;
  margin: 0.5rem 0;
}
```

## How It Works

### Import Process:
1. User uploads .apkg file
2. Backend extracts the zip archive
3. Parses media mapping JSON (e.g., `{"0": "image.jpg", "1": "audio.mp3"}`)
4. Creates unique media directory for the deck
5. Copies all media files to deck directory
6. Processes card content:
   - Updates image src paths to `/media/{deckGuid}/filename`
   - Converts `[sound:file]` to HTML5 audio tags
7. Stores cards with processed HTML content
8. Saves media directory reference in deck

### Review Process:
1. Frontend fetches card with HTML content
2. Renders using `dangerouslySetInnerHTML`
3. Browser loads images and audio from `/media/{deckGuid}/filename`
4. Static file middleware serves files from `wwwroot/media/`

## Media Types Supported

- **Images:** jpg, png, gif, svg, etc. (any format supported by HTML `<img>` tag)
- **Audio:** mp3, ogg, wav, etc. (any format supported by HTML5 `<audio>` tag)

## File Structure

```
wwwroot/
  media/
    {deck-guid-1}/
      image1.jpg
      audio1.mp3
    {deck-guid-2}/
      picture.png
      sound.mp3
```

## Security Considerations

- Media files are isolated per deck using unique GUIDs
- Static file middleware only serves from `wwwroot` directory
- HTML rendering uses React's `dangerouslySetInnerHTML` (sanitization may be added later)

## Future Enhancements

- Add HTML sanitization for XSS protection
- Support for video files
- Media compression/optimization
- Media cleanup when deck is deleted
- Media preview in deck management
