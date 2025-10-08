namespace FlashcardApi.Services;

/// <summary>
/// Interface for LLM (Large Language Model) services
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// Reformulates a flashcard question using an LLM
    /// </summary>
    /// <param name="originalQuestion">The original question to reformulate</param>
    /// <param name="originalAnswer">The original answer for context</param>
    /// <param name="endpoint">The LLM API endpoint</param>
    /// <param name="modelName">The model name to use</param>
    /// <param name="apiKey">The API key for authentication</param>
    /// <returns>The reformulated question</returns>
    Task<string> ReformulateQuestionAsync(
        string originalQuestion, 
        string originalAnswer, 
        string endpoint, 
        string modelName, 
        string apiKey);

    /// <summary>
    /// Compares user's answer with the correct answer and provides feedback
    /// </summary>
    /// <param name="question">The question that was asked</param>
    /// <param name="userAnswer">The user's answer</param>
    /// <param name="correctAnswer">The correct answer</param>
    /// <param name="endpoint">The LLM API endpoint</param>
    /// <param name="modelName">The model name to use</param>
    /// <param name="apiKey">The API key for authentication</param>
    /// <returns>Feedback on the user's answer</returns>
    Task<string> CompareAnswerAsync(
        string question,
        string userAnswer,
        string correctAnswer,
        string endpoint,
        string modelName,
        string apiKey);
}
