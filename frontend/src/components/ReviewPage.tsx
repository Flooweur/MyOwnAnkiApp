import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import apiService from '../api';
import { Card, ReviewGrade } from '../types';
import { reformulateQuestion, compareAnswer } from '../services/llmService';
import './ReviewPage.css';

/**
 * Review page for studying flashcards with FSRS scheduling
 */
const ReviewPage: React.FC = () => {
  const { deckId } = useParams<{ deckId: string }>();
  const navigate = useNavigate();
  
  const [currentCard, setCurrentCard] = useState<Card | null>(null);
  const [showAnswer, setShowAnswer] = useState(false);
  const [loading, setLoading] = useState(true);
  const [reviewing, setReviewing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [schedulingIntervals, setSchedulingIntervals] = useState<{ [grade: string]: string }>({});
  const [displayQuestion, setDisplayQuestion] = useState<string>('');
  const [showOriginalQuestion, setShowOriginalQuestion] = useState(false);
  const [userAnswer, setUserAnswer] = useState<string>('');
  const [answerFeedback, setAnswerFeedback] = useState<string | null>(null);
  const [comparingAnswer, setComparingAnswer] = useState(false);

  /**
   * Loads the next card to review
   */
  const loadNextCard = useCallback(async () => {
    if (!deckId) return;

    try {
      setLoading(true);
      setError(null);
      setShowAnswer(false);
      setShowOriginalQuestion(false);
      setUserAnswer('');
      setAnswerFeedback(null);
      const response = await apiService.getNextCard(parseInt(deckId));
      setCurrentCard(response.card);
      setSchedulingIntervals(response.schedulingIntervals || {});
      
      // Reformulate question using LLM if AI augmentation is enabled
      if (response.card) {
        const aiAugmentedEnabled = localStorage.getItem('ai_augmented_enabled') === 'true';
        
        if (aiAugmentedEnabled) {
          const reformulated = await reformulateQuestion(
            response.card.front,
            response.card.back
          );
          setDisplayQuestion(reformulated);
        } else {
          // Use original question if AI augmentation is disabled
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

  // Load first card on mount
  useEffect(() => {
    loadNextCard();
  }, [loadNextCard]);

  /**
   * Handles showing the answer with slide animation
   */
  const handleShowAnswer = () => {
    setShowAnswer(true);
  };

  /**
   * Handles toggling between original and reformulated question
   */
  const handleToggleOriginalQuestion = () => {
    setShowOriginalQuestion(!showOriginalQuestion);
  };

  /**
   * Handles submitting user's answer for comparison
   */
  const handleSubmitAnswer = async () => {
    if (!currentCard || !userAnswer.trim()) return;

    try {
      setComparingAnswer(true);
      setError(null);
      const feedback = await compareAnswer(
        displayQuestion,
        userAnswer,
        currentCard.back
      );
      setAnswerFeedback(feedback);
    } catch (err) {
      console.error('Error comparing answer:', err);
      setError('Failed to compare answer. Please try again.');
    } finally {
      setComparingAnswer(false);
    }
  };

  /**
   * Handles card review with a grade
   */
  const handleReview = async (grade: ReviewGrade) => {
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
  };

  /**
   * Navigates back to home page
   */
  const handleBackToHome = () => {
    navigate('/');
  };

  /**
   * Gets button color based on grade
   */
  const getGradeButtonClass = (grade: ReviewGrade): string => {
    switch (grade) {
      case ReviewGrade.Again:
        return 'grade-button grade-again';
      case ReviewGrade.Hard:
        return 'grade-button grade-hard';
      case ReviewGrade.Good:
        return 'grade-button grade-good';
      case ReviewGrade.Easy:
        return 'grade-button grade-easy';
      default:
        return 'grade-button';
    }
  };

  if (loading) {
    return (
      <div className="review-page">
        <div className="loading-state">
          <div className="spinner"></div>
          <p>Loading card...</p>
        </div>
      </div>
    );
  }

  if (!currentCard) {
    return (
      <div className="review-page">
        <div className="completion-state">
          <div className="completion-icon">🎉</div>
          <h2>All done!</h2>
          <p>No more cards due for review in this deck.</p>
          <button className="back-button" onClick={handleBackToHome}>
            Back to Decks
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="review-page">
      <div className="review-header">
        <button className="back-button-small" onClick={handleBackToHome}>
          ← Back
        </button>
        <div className="card-stats">
          <span>Reviews: {currentCard.reviewCount}</span>
          <span>Difficulty: {currentCard.difficulty > 0 ? (currentCard.difficulty / 10).toFixed(2) : 'N/A'}</span>
        </div>
      </div>

      {error && (
        <div className="error-message">
          <span>⚠️ {error}</span>
          <button onClick={() => setError(null)}>×</button>
        </div>
      )}

      <div className="card-container">
        <div className={`cards-wrapper ${showAnswer ? 'showing-answer' : ''}`}>
          {/* Question card */}
          <div className="card-face card-question" onClick={!showAnswer && !localStorage.getItem('ai_augmented_enabled') ? handleShowAnswer : undefined}>
            <div className="card-label">Question</div>
            <div className="card-content">
              {showOriginalQuestion ? currentCard.front : (displayQuestion || currentCard.front)}
            </div>
            {localStorage.getItem('ai_augmented_enabled') === 'true' && displayQuestion !== currentCard.front && (
              <button className="toggle-original-button" onClick={handleToggleOriginalQuestion}>
                {showOriginalQuestion ? 'Show AI Version' : 'Show Original'}
              </button>
            )}
            {!showAnswer && !localStorage.getItem('ai_augmented_enabled') && <div className="tap-hint">Tap to reveal answer</div>}
          </div>

          {/* Answer card */}
          <div className="card-face card-answer">
            <div className="card-label">Answer</div>
            <div className="card-content">
              {currentCard.back}
            </div>
          </div>
        </div>
      </div>

      {/* Answer input box for AI augmented mode */}
      {localStorage.getItem('ai_augmented_enabled') === 'true' && !showAnswer && (
        <div className="answer-input-container">
          <textarea
            className="answer-input"
            placeholder="Type your answer here..."
            value={userAnswer}
            onChange={(e) => setUserAnswer(e.target.value)}
            disabled={comparingAnswer}
          />
          <button
            className="submit-answer-button"
            onClick={handleSubmitAnswer}
            disabled={!userAnswer.trim() || comparingAnswer}
          >
            {comparingAnswer ? 'Checking...' : 'Submit Answer'}
          </button>
        </div>
      )}

      {/* Answer feedback */}
      {answerFeedback && (
        <div className="answer-feedback">
          <div className="feedback-header">Feedback</div>
          <div className="feedback-content">{answerFeedback}</div>
          <button className="reveal-answer-button" onClick={handleShowAnswer}>
            Reveal Answer Card
          </button>
        </div>
      )}

      {/* Grade buttons (only show after revealing answer) */}
      {showAnswer && !reviewing && (
        <div className="grade-buttons">
          <button
            className={getGradeButtonClass(ReviewGrade.Again)}
            onClick={() => handleReview(ReviewGrade.Again)}
          >
            <span className="grade-label">Again</span>
            <span className="grade-interval">{schedulingIntervals['1'] || '...'}</span>
          </button>
          <button
            className={getGradeButtonClass(ReviewGrade.Hard)}
            onClick={() => handleReview(ReviewGrade.Hard)}
          >
            <span className="grade-label">Hard</span>
            <span className="grade-interval">{schedulingIntervals['2'] || '...'}</span>
          </button>
          <button
            className={getGradeButtonClass(ReviewGrade.Good)}
            onClick={() => handleReview(ReviewGrade.Good)}
          >
            <span className="grade-label">Good</span>
            <span className="grade-interval">{schedulingIntervals['3'] || '...'}</span>
          </button>
          <button
            className={getGradeButtonClass(ReviewGrade.Easy)}
            onClick={() => handleReview(ReviewGrade.Easy)}
          >
            <span className="grade-label">Easy</span>
            <span className="grade-interval">{schedulingIntervals['4'] || '...'}</span>
          </button>
        </div>
      )}

      {reviewing && (
        <div className="reviewing-state">
          <div className="spinner"></div>
          <p>Scheduling next review...</p>
        </div>
      )}
    </div>
  );
};

export default ReviewPage;