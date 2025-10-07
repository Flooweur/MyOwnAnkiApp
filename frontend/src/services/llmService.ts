/**
 * LLM Service for reformulating flashcard questions
 */

interface LLMConfig {
  endpoint: string;
  modelName: string;
  apiKey: string;
}

interface GenerateContentConfig {
  system_instruction: string;
}

interface LLMRequest {
  model: string;
  config: GenerateContentConfig;
  contents: string;
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
 * Reformulates a flashcard question using LLM
 */
export const reformulateQuestion = async (
  originalQuestion: string,
  originalAnswer: string
): Promise<string> => {
  const config = getLLMConfig();
  
  if (!config) {
    // If no LLM config, return original question
    return originalQuestion;
  }

  try {
    const systemInstruction = `You are a flashcard question reformulator. Your task is to:
1. Rephrase the question to make it less predictable and reduce memorization
2. Add context or a realistic scenario when appropriate
3. Keep the question testing the same knowledge
4. Make it engaging and practical
5. Keep the question concise and clear

Original answer for context: ${originalAnswer}`;

    const userPrompt = `Reformulate this flashcard question: "${originalQuestion}"

Return ONLY the reformulated question, nothing else.`;

    const requestBody: LLMRequest = {
      model: config.modelName,
      config: {
        system_instruction: systemInstruction
      },
      contents: userPrompt
    };

    const response = await fetch(config.endpoint, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${config.apiKey}`,
      },
      body: JSON.stringify(requestBody),
    });

    if (!response.ok) {
      console.error('LLM API error:', response.statusText);
      return originalQuestion;
    }

    const data = await response.json();
    
    // Try to extract the reformulated question from various response formats
    let reformulatedQuestion = originalQuestion;
    
    if (data.text) {
      reformulatedQuestion = data.text;
    } else if (data.content) {
      reformulatedQuestion = data.content;
    } else if (data.response) {
      reformulatedQuestion = data.response;
    } else if (data.choices && data.choices[0]?.message?.content) {
      reformulatedQuestion = data.choices[0].message.content;
    } else if (data.candidates && data.candidates[0]?.content?.parts?.[0]?.text) {
      // Google AI format
      reformulatedQuestion = data.candidates[0].content.parts[0].text;
    }

    return reformulatedQuestion.trim();
  } catch (error) {
    console.error('Error reformulating question:', error);
    return originalQuestion;
  }
};
