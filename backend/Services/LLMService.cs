using System.Text;
using System.Text.Json;

namespace FlashcardApi.Services;

/// <summary>
/// Service for interacting with LLM APIs
/// </summary>
public class LLMService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LLMService> _logger;

    public LLMService(HttpClient httpClient, ILogger<LLMService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Reformulates a flashcard question using an LLM
    /// </summary>
    public async Task<string> ReformulateQuestionAsync(
        string originalQuestion,
        string originalAnswer,
        string endpoint,
        string modelName,
        string apiKey)
    {
        try
        {
            _logger.LogInformation("Reformulating question: {Question}", originalQuestion);

            // Compose the system instruction and user prompt
            var systemInstruction = $@"You are a flashcard question reformulator. Your task is to:
1. Rephrase the question to make it less predictable and reduce memorization
2. Add context or a realistic scenario when appropriate
3. Keep the question testing the same knowledge
4. Make it engaging and practical
5. Keep the question concise and clear

Original answer for context: {originalAnswer}";

            var userPrompt = $@"Reformulate this flashcard question: ""{originalQuestion}""

Return ONLY the reformulated question, nothing else.";

            // Detect if this is a Google Gemini endpoint (v1beta/models/gemini)
            bool isGoogleGemini = endpoint.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase);

            HttpRequestMessage request;
            if (isGoogleGemini)
            {
                // Google Gemini API expects:
                // {
                //   "contents": [
                //     {
                //       "parts": [
                //         { "text": "<prompt>" }
                //       ]
                //     }
                //   ]
                // }
                var geminiRequestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = $"{systemInstruction}\n\n{userPrompt}"
                                }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(geminiRequestBody);
                request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Clear();
                request.Headers.Add("x-goog-api-key", apiKey);
            }
            else
            {
                // Default: OpenAI or other LLMs
                var requestBody = new
                {
                    model = modelName,
                    config = new
                    {
                        system_instruction = systemInstruction
                    },
                    contents = userPrompt
                };

                var json = JsonSerializer.Serialize(requestBody);
                request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Clear();
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
            }

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("LLM API request failed with status: {StatusCode}, Response: {ErrorContent}",
                    response.StatusCode, errorContent);
                return originalQuestion;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("LLM API response: {Response}", responseContent);

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogWarning("LLM API returned empty response");
                return originalQuestion;
            }

            var trimmedResponse = responseContent.TrimStart();
            if (!trimmedResponse.StartsWith("{") && !trimmedResponse.StartsWith("["))
            {
                _logger.LogWarning("LLM API returned non-JSON response (likely HTML error page): {Response}", responseContent);
                return originalQuestion;
            }

            JsonElement responseData;
            try
            {
                responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to parse LLM API response as JSON: {Response}", responseContent);
                return originalQuestion;
            }

            string reformulatedQuestion = originalQuestion;

            if (isGoogleGemini)
            {
                // Google Gemini format:
                // {
                //   "candidates": [
                //     {
                //       "content": {
                //         "parts": [
                //           { "text": "..." }
                //         ]
                //       }
                //     }
                //   ]
                // }
                if (responseData.TryGetProperty("candidates", out var candidatesElement) &&
                    candidatesElement.ValueKind == JsonValueKind.Array &&
                    candidatesElement.GetArrayLength() > 0)
                {
                    var firstCandidate = candidatesElement[0];
                    if (firstCandidate.TryGetProperty("content", out var candidateContentElement) &&
                        candidateContentElement.TryGetProperty("parts", out var partsElement) &&
                        partsElement.ValueKind == JsonValueKind.Array &&
                        partsElement.GetArrayLength() > 0)
                    {
                        var firstPart = partsElement[0];
                        if (firstPart.TryGetProperty("text", out var textPartElement))
                        {
                            reformulatedQuestion = textPartElement.GetString() ?? originalQuestion;
                        }
                    }
                }
            }
            else
            {
                // Try to extract the reformulated question from various response formats
                if (responseData.TryGetProperty("text", out var textElement))
                {
                    reformulatedQuestion = textElement.GetString() ?? originalQuestion;
                }
                else if (responseData.TryGetProperty("content", out var contentElement))
                {
                    reformulatedQuestion = contentElement.GetString() ?? originalQuestion;
                }
                else if (responseData.TryGetProperty("response", out var responseElement))
                {
                    reformulatedQuestion = responseElement.GetString() ?? originalQuestion;
                }
                else if (responseData.TryGetProperty("choices", out var choicesElement) &&
                         choicesElement.ValueKind == JsonValueKind.Array &&
                         choicesElement.GetArrayLength() > 0)
                {
                    var firstChoice = choicesElement[0];
                    if (firstChoice.TryGetProperty("message", out var messageElement) &&
                        messageElement.TryGetProperty("content", out var messageContentElement))
                    {
                        reformulatedQuestion = messageContentElement.GetString() ?? originalQuestion;
                    }
                }
            }

            _logger.LogInformation("Successfully reformulated question");
            return reformulatedQuestion.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reformulating question with LLM");
            return originalQuestion;
        }
    }
}
