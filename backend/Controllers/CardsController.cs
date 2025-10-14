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
    /// Gets all cards in a specific deck
    /// </summary>
    /// <param name="deckId">Deck ID</param>
    /// <returns>List of all cards in the deck</returns>
    [HttpGet("deck/{deckId}")]
    public async Task<ActionResult> GetDeckCards(int deckId)
    {
        try
        {
            var cards = await _cardService.GetCardsAsync(deckId);
            return Ok(cards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cards for deck {DeckId}", deckId);
            return StatusCode(500, "An error occurred while fetching cards");
        }
    }

    /// <summary>
    /// Gets a random card from a deck
    /// </summary>
    /// <param name="deckId">Deck ID</param>
    /// <returns>Random card from the deck, or null if no cards exist</returns>
    [HttpGet("random/{deckId}")]
    public async Task<ActionResult> GetRandomCard(int deckId)
    {
        try
        {
            var card = await _cardService.GetRandomCardAsync(deckId);
            
            if (card == null)
                return Ok(new { message = "No cards in deck", card = (object?)null });

            return Ok(new { message = "Random card found", card });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching random card for deck {DeckId}", deckId);
            return StatusCode(500, "An error occurred while fetching a random card");
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
    /// Records a card review
    /// </summary>
    /// <param name="id">Card ID</param>
    /// <param name="request">Review request containing the grade</param>
    /// <returns>Updated card</returns>
    [HttpPost("{id}/review")]
    public async Task<ActionResult> ReviewCard(int id, [FromBody] ReviewCardRequest request)
    {
        try
        {
            // Validate grade (1-4 for simplicity)
            if (request.Grade < 1 || request.Grade > 4)
                return BadRequest("Grade must be between 1 and 4");

            _logger.LogInformation("Reviewing card {CardId} with grade {Grade}", id, request.Grade);

            var updatedCard = await _cardService.ReviewCardAsync(id, request.Grade);

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

    /// <summary>
    /// Deletes a card
    /// </summary>
    /// <param name="id">Card ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCard(int id)
    {
        try
        {
            _logger.LogInformation("Deleting card {CardId}", id);

            var deleted = await _cardService.DeleteCardAsync(id);

            if (!deleted)
                return NotFound($"Card with ID {id} not found");

            return Ok(new { message = "Card deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting card {CardId}", id);
            return StatusCode(500, "An error occurred while deleting the card");
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