using FlashcardApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlashcardApi.Controllers;

/// <summary>
/// API controller for card operations and reviews
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;
    private readonly ILogger<CardsController> _logger;

    public CardsController(ICardService cardService, ILogger<CardsController> logger)
    {
        _cardService = cardService;
        _logger = logger;
    }

    /// <summary>
    /// Gets cards that are due for review in a specific deck
    /// </summary>
    /// <param name="deckId">Deck ID</param>
    /// <param name="limit">Maximum number of cards to return</param>
    /// <returns>List of due cards</returns>
    [HttpGet("due/{deckId}")]
    public async Task<ActionResult> GetDueCards(int deckId, [FromQuery] int limit = 20)
    {
        try
        {
            var cards = await _cardService.GetDueCardsAsync(deckId, limit);
            return Ok(cards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching due cards for deck {DeckId}", deckId);
            return StatusCode(500, "An error occurred while fetching due cards");
        }
    }

    /// <summary>
    /// Gets the next card to review from a deck
    /// </summary>
    /// <param name="deckId">Deck ID</param>
    /// <returns>Next card to review, or null if no cards are due</returns>
    [HttpGet("next/{deckId}")]
    public async Task<ActionResult> GetNextCard(int deckId)
    {
        try
        {
            var card = await _cardService.GetNextCardToReviewAsync(deckId);
            
            if (card == null)
                return Ok(new { message = "No cards due for review", card = (object?)null, schedulingIntervals = (object?)null });

            // Get scheduling intervals for all grades
            var intervals = _cardService.GetSchedulingIntervals(card);
            
            // Format intervals for display
            var schedulingIntervals = new Dictionary<string, string>();
            foreach (var kvp in intervals)
            {
                schedulingIntervals[kvp.Key.ToString()] = FormatInterval(kvp.Value);
            }

            return Ok(new { message = "Card found", card, schedulingIntervals });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching next card for deck {DeckId}", deckId);
            return StatusCode(500, "An error occurred while fetching the next card");
        }
    }

    /// <summary>
    /// Formats a TimeSpan interval into a human-readable string
    /// </summary>
    private string FormatInterval(TimeSpan interval)
    {
        if (interval.TotalMinutes < 60)
            return $"{(int)interval.TotalMinutes}m";
        if (interval.TotalHours < 24)
            return $"{(int)interval.TotalHours}h";
        if (interval.TotalDays < 30)
            return $"{(int)interval.TotalDays}d";
        if (interval.TotalDays < 365)
            return $"{(int)(interval.TotalDays / 30)}mo";
        return $"{(int)(interval.TotalDays / 365)}y";
    }

    /// <summary>
    /// Gets a specific card by ID
    /// </summary>
    /// <param name="id">Card ID</param>
    /// <returns>Card details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetCard(int id)
    {
        try
        {
            var card = await _cardService.GetCardByIdAsync(id);
            
            if (card == null)
                return NotFound($"Card with ID {id} not found");

            return Ok(card);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching card {CardId}", id);
            return StatusCode(500, "An error occurred while fetching the card");
        }
    }

    /// <summary>
    /// Reviews a card with a grade and updates its schedule using FSRS
    /// </summary>
    /// <param name="id">Card ID</param>
    /// <param name="request">Review request containing the grade</param>
    /// <returns>Updated card with new schedule</returns>
    [HttpPost("{id}/review")]
    public async Task<ActionResult> ReviewCard(int id, [FromBody] ReviewCardRequest request)
    {
        try
        {
            // Validate grade
            if (request.Grade < Services.FSRS.FsrsConstants.MinGrade || 
                request.Grade > Services.FSRS.FsrsConstants.MaxGrade)
                return BadRequest($"Grade must be between {Services.FSRS.FsrsConstants.MinGrade} (Again) and {Services.FSRS.FsrsConstants.MaxGrade} (Easy)");

            _logger.LogInformation("Reviewing card {CardId} with grade {Grade}", id, request.Grade);

            var updatedCard = await _cardService.ReviewCardAsync(id, request.Grade);

            return Ok(new
            {
                card = updatedCard,
                message = GetGradeMessage(request.Grade)
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Card not found: {CardId}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing card {CardId}", id);
            return StatusCode(500, "An error occurred while reviewing the card");
        }
    }

    /// <summary>
    /// Gets a user-friendly message for each grade
    /// </summary>
    private string GetGradeMessage(int grade) => grade switch
    {
        1 => "Don't worry, you'll get it next time!",
        2 => "That was challenging, but you got it!",
        3 => "Good job!",
        4 => "Excellent! You know this well!",
        _ => "Card reviewed"
    };
}

/// <summary>
/// Request model for reviewing a card
/// </summary>
public class ReviewCardRequest
{
    /// <summary>
    /// Grade for the review (1=Again, 2=Hard, 3=Good, 4=Easy)
    /// </summary>
    public int Grade { get; set; }
}