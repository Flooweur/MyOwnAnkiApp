import { useState, useCallback } from 'react';
import apiService from '../api';
import { Card, ReviewGrade } from '../types';
import { reformulateQuestion } from '../services/llmService';

/**
 * Custom hook for managing card review logic
 */
export function useCardReview(deckId: string | undefined) {
  const [currentCard, setCurrentCard] = useState<Card | null>(null);
  const [showAnswer, setShowAnswer] = useState(false);
  const [loading, setLoading] = useState(true);
  const [reviewing, setReviewing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [schedulingIntervals, setSchedulingIntervals] = useState<{ [grade: string]: string }>({});
  const [displayQuestion, setDisplayQuestion] = useState<string>('');
  const [isAiEnabled, setIsAiEnabled] = useState(false);

  /**
   * Loads the next card to review
   */
  const loadNextCard = useCallback(async () => {
    if (!deckId) return;

    try {
      setLoading(true);
      setError(null);
      setShowAnswer(false);

      const response = await apiService.getNextCard(parseInt(deckId));
      setCurrentCard(response.card);
      setSchedulingIntervals(response.schedulingIntervals || {});

      // Reformulate question using LLM if AI augmentation is enabled
      if (response.card) {
        const aiAugmentedEnabled = localStorage.getItem('ai_augmented_enabled') === 'true';
        setIsAiEnabled(aiAugmentedEnabled);

        if (aiAugmentedEnabled) {
          const reformulated = await reformulateQuestion(
            response.card.front,
            response.card.back
          );
          setDisplayQuestion(reformulated);
        } else {
          setDisplayQuestion(response.card.front);
        }
      }
    } catch (err) {
      console.error('Error loading next card:', err);
      setError('Failed to load card. Please try again.');
    } finally {
      setLoading(false);
    }
  }, [deckId]);

  /**
   * Handles card review with a grade
   */
  const reviewCard = useCallback(async (grade: ReviewGrade) => {
    if (!currentCard || reviewing) return;

    try {
      setReviewing(true);
      setError(null);
      await apiService.reviewCard(currentCard.id, grade);

      // Load next card after a brief delay for visual feedback
      setTimeout(() => {
        setReviewing(false);
        loadNextCard();
      }, 500);
    } catch (err) {
      console.error('Error reviewing card:', err);
      setError('Failed to review card. Please try again.');
      setReviewing(false);
    }
  }, [currentCard, reviewing, loadNextCard]);

  /**
   * Reveals the answer
   */
  const revealAnswer = useCallback(() => {
    setShowAnswer(true);
  }, []);

  return {
    currentCard,
    showAnswer,
    loading,
    reviewing,
    error,
    schedulingIntervals,
    displayQuestion,
    isAiEnabled,
    setError,
    loadNextCard,
    reviewCard,
    revealAnswer,
  };
}
