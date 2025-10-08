using FlashcardApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlashcardApi.Controllers;

/// <summary>
/// API controller for LLM operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LLMController : ControllerBase
{
    private readonly ILLMService _llmService;
    private readonly ILogger<LLMController> _logger;

    public LLMController(ILLMService llmService, ILogger<LLMController> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    /// <summary>
    /// Reformulates a flashcard question using an LLM
    /// </summary>
    /// <param name="request">The reformulation request</param>
    /// <returns>The reformulated question</returns>
    [HttpPost("reformulate")]
    public async Task<ActionResult<ReformulateResponse>> ReformulateQuestion([FromBody] ReformulateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OriginalQuestion))
                return BadRequest("Original question is required");

            if (string.IsNullOrWhiteSpace(request.Endpoint))
                return BadRequest("Endpoint is required");

            if (string.IsNullOrWhiteSpace(request.ModelName))
                return BadRequest("Model name is required");

            if (string.IsNullOrWhiteSpace(request.ApiKey))
                return BadRequest("API key is required");

            var reformulatedQuestion = await _llmService.ReformulateQuestionAsync(
                request.OriginalQuestion,
                request.OriginalAnswer ?? "",
                request.Endpoint,
                request.ModelName,
                request.ApiKey);

            return Ok(new ReformulateResponse
            {
                ReformulatedQuestion = reformulatedQuestion
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reformulating question");
            return StatusCode(500, "An error occurred while reformulating the question");
        }
    }

    /// <summary>
    /// Compares user's answer with the correct answer and provides feedback
    /// </summary>
    /// <param name="request">The comparison request</param>
    /// <returns>The feedback on the answer</returns>
    [HttpPost("compare")]
    public async Task<ActionResult<CompareAnswerResponse>> CompareAnswer([FromBody] CompareAnswerRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest("Question is required");

            if (string.IsNullOrWhiteSpace(request.UserAnswer))
                return BadRequest("User answer is required");

            if (string.IsNullOrWhiteSpace(request.CorrectAnswer))
                return BadRequest("Correct answer is required");

            if (string.IsNullOrWhiteSpace(request.Endpoint))
                return BadRequest("Endpoint is required");

            if (string.IsNullOrWhiteSpace(request.ModelName))
                return BadRequest("Model name is required");

            if (string.IsNullOrWhiteSpace(request.ApiKey))
                return BadRequest("API key is required");

            var feedback = await _llmService.CompareAnswerAsync(
                request.Question,
                request.UserAnswer,
                request.CorrectAnswer,
                request.Endpoint,
                request.ModelName,
                request.ApiKey);

            return Ok(new CompareAnswerResponse
            {
                Feedback = feedback
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing answer");
            return StatusCode(500, "An error occurred while comparing the answer");
        }
    }
}

/// <summary>
/// Request model for question reformulation
/// </summary>
public class ReformulateRequest
{
    public string OriginalQuestion { get; set; } = string.Empty;
    public string? OriginalAnswer { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Response model for question reformulation
/// </summary>
public class ReformulateResponse
{
    public string ReformulatedQuestion { get; set; } = string.Empty;
}

/// <summary>
/// Request model for answer comparison
/// </summary>
public class CompareAnswerRequest
{
    public string Question { get; set; } = string.Empty;
    public string UserAnswer { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Response model for answer comparison
/// </summary>
public class CompareAnswerResponse
{
    public string Feedback { get; set; } = string.Empty;
}
