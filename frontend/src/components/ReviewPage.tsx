import React, { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { ReviewGrade } from '../types';
import { useCardReview } from '../hooks/useCardReview';
import { useAIAnswer } from '../hooks/useAIAnswer';
import { UI_TEXT } from '../constants/messages';
import './ReviewPage.css';

/**
 * Review page for studying flashcards with FSRS scheduling
 */
const ReviewPage: React.FC = () => {
  const { deckId } = useParams<{ deckId: string }>();
  const navigate = useNavigate();

  // Custom hooks for card review and AI answer management
  const {
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
  } = useCardReview(deckId);

  const {
    userAnswer,
    answerFeedback,
    comparingAnswer,
    showOriginalQuestion,
    setUserAnswer,
    submitAnswer,
    resetAnswer,
    toggleOriginalQuestion,
  } = useAIAnswer();

  // Load first card on mount
  useEffect(() => {
    loadNextCard();
  }, [loadNextCard]);

  // Reset answer state when loading new card
  useEffect(() => {
    if (loading) {
      resetAnswer();
    }
  }, [loading, resetAnswer]);

  /**
   * Handles keyboard events for answer input
   */
  const handleAnswerKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmitAnswer();
    }
  };

  /**
   * Handles submitting user's answer for comparison
   */
  const handleSubmitAnswer = async () => {
    if (!currentCard) return;
    
    try {
      await submitAnswer(displayQuestion, currentCard.back);
    } catch (err) {
      setError((err as Error).message);
    }
  };

  /**
   * Handles showing the answer
   */
  const handleShowAnswer = () => {
    revealAnswer();
  };

  /**
   * Handles card review with a grade
   */
  const handleReview = (grade: ReviewGrade) => {
    reviewCard(grade);
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
    const gradeClassMap = {
      [ReviewGrade.Again]: 'grade-button grade-again',
      [ReviewGrade.Hard]: 'grade-button grade-hard',
      [ReviewGrade.Good]: 'grade-button grade-good',
      [ReviewGrade.Easy]: 'grade-button grade-easy',
    };
    return gradeClassMap[grade] || 'grade-button';
  };

  // Loading state
  if (loading) {
    return (
      <div className="review-page">
        <div className="loading-state">
          <div className="spinner"></div>
          <p>{UI_TEXT.LOADING_CARD}</p>
        </div>
      </div>
    );
  }

  // No cards available state
  if (!currentCard) {
    return (
      <div className="review-page">
        <div className="completion-state">
          <div className="completion-icon">🎉</div>
          <h2>{UI_TEXT.ALL_DONE}</h2>
          <p>{UI_TEXT.NO_CARDS_DUE}</p>
          <button className="back-button" onClick={handleBackToHome}>
            Back to Decks
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="review-page">
      {/* Header */}
      <div className="review-header">
        <button className="back-button-small" onClick={handleBackToHome}>
          ← Back
        </button>
        <div className="card-stats">
          <span>Reviews: {currentCard.reviewCount}</span>
          <span>Difficulty: {currentCard.difficulty > 0 ? (currentCard.difficulty / 10).toFixed(2) : 'N/A'}</span>
        </div>
      </div>

      {/* Error message */}
      {error && (
        <div className="error-message">
          <span>⚠️ {error}</span>
          <button onClick={() => setError(null)}>×</button>
        </div>
      )}

      {/* Card container */}
      <div className="card-container">
        <div className={`cards-wrapper ${showAnswer ? 'showing-answer' : ''}`}>
          {/* Question card */}
          <div
            className="card-face card-question"
            onClick={!showAnswer && !isAiEnabled ? handleShowAnswer : undefined}
            style={{ cursor: !showAnswer && !isAiEnabled ? 'pointer' : 'default' }}
          >
            <div className="card-label">Question</div>
            <div 
              className="card-content"
              dangerouslySetInnerHTML={{ 
                __html: showOriginalQuestion ? currentCard.front : (displayQuestion || currentCard.front) 
              }}
            />
            {isAiEnabled && displayQuestion !== currentCard.front && (
              <button className="toggle-original-button" onClick={toggleOriginalQuestion}>
                {showOriginalQuestion ? 'Show AI Version' : 'Show Original'}
              </button>
            )}
            {!showAnswer && !isAiEnabled && <div className="tap-hint">{UI_TEXT.TAP_TO_REVEAL}</div>}
          </div>

          {/* Answer card */}
          <div className="card-face card-answer">
            <div className="card-label">Answer</div>
            <div 
              className="card-content"
              dangerouslySetInnerHTML={{ __html: currentCard.back }}
            />
          </div>
        </div>
      </div>

      {/* Answer input box for AI augmented mode */}
      {isAiEnabled && !showAnswer && (
        <div className="answer-input-container">
          <textarea
            className="answer-input"
            placeholder="Type your answer here... (Press Enter to submit, Shift+Enter for new line)"
            value={userAnswer}
            onChange={(e) => setUserAnswer(e.target.value)}
            onKeyDown={handleAnswerKeyDown}
            disabled={comparingAnswer}
          />
          <button
            className="submit-answer-button"
            onClick={handleSubmitAnswer}
            disabled={!userAnswer.trim() || comparingAnswer}
          >
            {comparingAnswer ? UI_TEXT.CHECKING_ANSWER : 'Submit Answer'}
          </button>
        </div>
      )}

      {/* Answer feedback */}
      {answerFeedback && (
        <div className="answer-feedback">
          <div className="feedback-header">Feedback</div>
          <div className="feedback-content">
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
              {answerFeedback}
            </ReactMarkdown>
          </div>
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

      {/* Reviewing state */}
      {reviewing && (
        <div className="reviewing-state">
          <div className="spinner"></div>
          <p>{UI_TEXT.SCHEDULING}</p>
        </div>
      )}
    </div>
  );
};

export default ReviewPage;
