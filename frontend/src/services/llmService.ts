/**
 * LLM Service for reformulating flashcard questions
 */

interface LLMConfig {
  endpoint: string;
  modelName: string;
  apiKey: string;
}


/**
 * Gets LLM configuration from localStorage
 */
export const getLLMConfig = (): LLMConfig | null => {
  const endpoint = localStorage.getItem('llm_endpoint');
  const modelName = localStorage.getItem('llm_model_name');
  const apiKey = localStorage.getItem('llm_api_key');

  if (!endpoint || !modelName || !apiKey) {
    return null;
  }

  return { endpoint, modelName, apiKey };
};

/**
 * Reformulates a flashcard question using LLM via backend proxy
 */
export const reformulateQuestion = async (
  originalQuestion: string,
  originalAnswer: string
): Promise<string> => {
  console.log('[LLM] reformulateQuestion called');
  console.log('[LLM] Original Question:', originalQuestion);
  console.log('[LLM] Original Answer:', originalAnswer);

  const config = getLLMConfig();
  
  if (!config) {
    console.log('[LLM] No LLM config found, returning original question');
    return originalQuestion;
  }

  try {
    const requestBody = {
      originalQuestion,
      originalAnswer,
      endpoint: config.endpoint,
      modelName: config.modelName,
      apiKey: config.apiKey
    };

    console.log('[LLM] Sending request to backend proxy');
    console.log('[LLM] Request body:', { ...requestBody, apiKey: '[REDACTED]' });

    const response = await fetch('/api/llm/reformulate', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(requestBody),
    });

    console.log('[LLM] Response status:', response.status);

    if (!response.ok) {
      console.error('[LLM] Backend proxy error:', response.statusText);
      return originalQuestion;
    }

    const data = await response.json();
    console.log('[LLM] Response data:', data);

    const reformulatedQuestion = data.reformulatedQuestion || originalQuestion;
    console.log('[LLM] Final reformulated question:', reformulatedQuestion.trim());
    return reformulatedQuestion.trim();
  } catch (error) {
    console.error('[LLM] Error reformulating question:', error);
    return originalQuestion;
  }
};
