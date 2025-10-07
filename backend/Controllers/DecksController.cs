using FlashcardApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlashcardApi.Controllers;

/// <summary>
/// API controller for deck management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DecksController : ControllerBase
{
    private readonly IDeckService _deckService;
    private readonly IApkgParserService _apkgParser;
    private readonly ILogger<DecksController> _logger;

    public DecksController(
        IDeckService deckService,
        IApkgParserService apkgParser,
        ILogger<DecksController> logger)
    {
        _deckService = deckService;
        _apkgParser = apkgParser;
        _logger = logger;
    }

    /// <summary>
    /// Gets all decks with statistics
    /// </summary>
    /// <returns>List of decks with their stats</returns>
    [HttpGet]
    public async Task<ActionResult<List<DeckWithStats>>> GetDecks()
    {
        try
        {
            var decks = await _deckService.GetAllDecksWithStatsAsync();
            return Ok(decks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching decks");
            return StatusCode(500, "An error occurred while fetching decks");
        }
    }

    /// <summary>
    /// Gets a specific deck by ID
    /// </summary>
    /// <param name="id">Deck ID</param>
    /// <returns>Deck details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetDeck(int id)
    {
        try
        {
            var deck = await _deckService.GetDeckByIdAsync(id);
            
            if (deck == null)
                return NotFound($"Deck with ID {id} not found");

            return Ok(deck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching deck {DeckId}", id);
            return StatusCode(500, "An error occurred while fetching the deck");
        }
    }

    /// <summary>
    /// Creates a new empty deck
    /// </summary>
    /// <param name="request">Deck creation request</param>
    /// <returns>Created deck</returns>
    [HttpPost]
    public async Task<ActionResult> CreateDeck([FromBody] CreateDeckRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Deck name is required");

            var deck = await _deckService.CreateDeckAsync(request.Name, request.Description);
            return CreatedAtAction(nameof(GetDeck), new { id = deck.Id }, deck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating deck");
            return StatusCode(500, "An error occurred while creating the deck");
        }
    }

    /// <summary>
    /// Uploads and imports an Anki .apkg file
    /// </summary>
    /// <param name="file">The .apkg file to import</param>
    /// <returns>Created deck with imported cards</returns>
    [HttpPost("upload")]
    public async Task<ActionResult> UploadApkg(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (!file.FileName.EndsWith(".apkg", StringComparison.OrdinalIgnoreCase))
                return BadRequest("File must be an .apkg file");

            _logger.LogInformation("Importing .apkg file: {FileName} ({Size} bytes)", 
                file.FileName, file.Length);

            using var stream = file.OpenReadStream();
            var deck = await _apkgParser.ImportApkgAsync(stream, file.FileName);

            _logger.LogInformation("Successfully imported deck {DeckName} with {CardCount} cards", 
                deck.Name, deck.Cards.Count);

            return CreatedAtAction(nameof(GetDeck), new { id = deck.Id }, deck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading .apkg file");
            return StatusCode(500, $"An error occurred while importing the file: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a deck and all its cards
    /// </summary>
    /// <param name="id">Deck ID to delete</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDeck(int id)
    {
        try
        {
            var success = await _deckService.DeleteDeckAsync(id);
            
            if (!success)
                return NotFound($"Deck with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting deck {DeckId}", id);
            return StatusCode(500, "An error occurred while deleting the deck");
        }
    }
}

/// <summary>
/// Request model for creating a new deck
/// </summary>
public class CreateDeckRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}