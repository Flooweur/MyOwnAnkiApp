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
    /// Gets a random card from a deck for review
    /// </summary>
    /// <param name="deckId">Deck ID</param>
    /// <returns>Random card from the deck, or null if no cards exist</returns>
    [HttpGet("next/{deckId}")]
    public async Task<ActionResult> GetNextCard(int deckId)
    {
        try
        {
            var card = await _cardService.GetNextCardToReviewAsync(deckId);
            
            if (card == null)
                return Ok(new { message = "No cards available in this deck", card = (object?)null });

            return Ok(new { message = "Card found", card });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching next card for deck {DeckId}", deckId);
            return StatusCode(500, "An error occurred while fetching the next card");
        }
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
    /// Reviews a card (simplified - no grading system)
    /// </summary>
    /// <param name="id">Card ID</param>
    /// <param name="request">Review request (grade parameter ignored)</param>
    /// <returns>Updated card</returns>
    [HttpPost("{id}/review")]
    public async Task<ActionResult> ReviewCard(int id, [FromBody] ReviewCardRequest request)
    {
        try
        {
            _logger.LogInformation("Reviewing card {CardId}", id);

            var updatedCard = await _cardService.ReviewCardAsync(id, 1); // Grade doesn't matter anymore

            return Ok(new
            {
                card = updatedCard,
                message = "Card reviewed successfully"
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