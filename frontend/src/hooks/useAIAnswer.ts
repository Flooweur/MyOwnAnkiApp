import { useState, useCallback } from 'react';
import { compareAnswer } from '../services/llmService';

/**
 * Custom hook for managing AI answer comparison
 */
export function useAIAnswer() {
  const [userAnswer, setUserAnswer] = useState<string>('');
  const [answerFeedback, setAnswerFeedback] = useState<string | null>(null);
  const [comparingAnswer, setComparingAnswer] = useState(false);
  const [showOriginalQuestion, setShowOriginalQuestion] = useState(false);

  /**
   * Submits user's answer for comparison
   */
  const submitAnswer = useCallback(async (
    question: string,
    correctAnswer: string
  ) => {
    if (!userAnswer.trim()) return;

    try {
      setComparingAnswer(true);
      const feedback = await compareAnswer(
        question,
        userAnswer,
        correctAnswer
      );
      setAnswerFeedback(feedback);
    } catch (err) {
      console.error('Error comparing answer:', err);
      throw new Error('Failed to compare answer. Please try again.');
    } finally {
      setComparingAnswer(false);
    }
  }, [userAnswer]);

  /**
   * Resets the answer state
   */
  const resetAnswer = useCallback(() => {
    setUserAnswer('');
    setAnswerFeedback(null);
    setShowOriginalQuestion(false);
  }, []);

  /**
   * Toggles between original and reformulated question
   */
  const toggleOriginalQuestion = useCallback(() => {
    setShowOriginalQuestion(prev => !prev);
  }, []);

  return {
    userAnswer,
    answerFeedback,
    comparingAnswer,
    showOriginalQuestion,
    setUserAnswer,
    submitAnswer,
    resetAnswer,
    toggleOriginalQuestion,
  };
}
