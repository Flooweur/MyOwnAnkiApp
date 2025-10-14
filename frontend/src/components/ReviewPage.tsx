import React, { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { useCardReview } from '../hooks/useCardReview';
import { useAIAnswer } from '../hooks/useAIAnswer';
import { UI_TEXT } from '../constants/messages';
import './ReviewPage.css';

/**
 * Review page for studying flashcards with random selection
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
   * Handles card review (simplified - no grading)
   */
  const handleReview = () => {
    reviewCard();
  };

  /**
   * Navigates back to home page
   */
  const handleBackToHome = () => {
    navigate('/');
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
          <p>{UI_TEXT.NO_CARDS_AVAILABLE}</p>
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

      {/* Next card button (only show after revealing answer) */}
      {showAnswer && !reviewing && (
        <div className="next-card-section">
          <button
            className="next-card-button"
            onClick={handleReview}
          >
            Next Card
          </button>
        </div>
      )}

      {/* Reviewing state */}
      {reviewing && (
        <div className="reviewing-state">
          <div className="spinner"></div>
          <p>{UI_TEXT.LOADING_NEXT}</p>
        </div>
      )}
    </div>
  );
};

export default ReviewPage;
