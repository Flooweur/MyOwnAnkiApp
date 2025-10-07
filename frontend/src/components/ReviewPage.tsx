import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import apiService from '../api';
import { Card, ReviewGrade } from '../types';
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
  const [isFlipping, setIsFlipping] = useState(false);

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
   * Handles showing the answer with flip animation
   */
  const handleShowAnswer = () => {
    setIsFlipping(true);
    setTimeout(() => {
      setShowAnswer(true);
      setIsFlipping(false);
    }, 300);
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
          <span>Difficulty: {(currentCard.difficulty / 10).toFixed(2)}</span>
        </div>
      </div>

      {error && (
        <div className="error-message">
          <span>⚠️ {error}</span>
          <button onClick={() => setError(null)}>×</button>
        </div>
      )}

      <div className="card-container">
        <div className={`flashcard ${isFlipping ? 'flipping' : ''} ${showAnswer ? 'flipped' : ''}`}>
          {/* Front of card (Question) */}
          {!showAnswer && (
            <div className="card-face card-front" onClick={handleShowAnswer}>
              <div className="card-label">Question</div>
              <div className="card-content">
                {currentCard.front}
              </div>
              <div className="tap-hint">Tap to reveal answer</div>
            </div>
          )}

          {/* Back of card (Answer) */}
          {showAnswer && (
            <div className="card-face card-back">
              <div className="card-label">Answer</div>
              <div className="card-content">
                {currentCard.back}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Grade buttons (only show after revealing answer) */}
      {showAnswer && !reviewing && (
        <div className="grade-buttons">
          <button
            className={getGradeButtonClass(ReviewGrade.Again)}
            onClick={() => handleReview(ReviewGrade.Again)}
          >
            <span className="grade-label">Again</span>
            <span className="grade-hint">Forgot</span>
          </button>
          <button
            className={getGradeButtonClass(ReviewGrade.Hard)}
            onClick={() => handleReview(ReviewGrade.Hard)}
          >
            <span className="grade-label">Hard</span>
            <span className="grade-hint">Difficult</span>
          </button>
          <button
            className={getGradeButtonClass(ReviewGrade.Good)}
            onClick={() => handleReview(ReviewGrade.Good)}
          >
            <span className="grade-label">Good</span>
            <span className="grade-hint">Correct</span>
          </button>
          <button
            className={getGradeButtonClass(ReviewGrade.Easy)}
            onClick={() => handleReview(ReviewGrade.Easy)}
          >
            <span className="grade-label">Easy</span>
            <span className="grade-hint">Very easy</span>
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