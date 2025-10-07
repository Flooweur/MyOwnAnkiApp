import React from 'react';
import { DeckWithStats } from '../types';
import './DeckCard.css';

interface DeckCardProps {
  deck: DeckWithStats;
  onClick: () => void;
  onDelete: () => void;
  onViewStats: () => void;
}

/**
 * Card component displaying deck information and statistics
 */
const DeckCard: React.FC<DeckCardProps> = ({ deck, onClick, onDelete, onViewStats }) => {
  /**
   * Handles delete button click without triggering card click
   */
  const handleDeleteClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    onDelete();
  };

  /**
   * Handles stats button click without triggering card click
   */
  const handleStatsClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    onViewStats();
  };

  /**
   * Formats date to a readable string
   */
  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  return (
    <div className="deck-card" onClick={onClick}>
      <div className="deck-card-header">
        <h3 className="deck-name">{deck.name}</h3>
        <button
          className="delete-button"
          onClick={handleDeleteClick}
          title="Delete deck"
        >
          🗑️
        </button>
      </div>

      {deck.description && (
        <p className="deck-description">{deck.description}</p>
      )}

      <div className="deck-stats">
        <div className="stat-row">
          <div className="stat">
            <div className="stat-value">{deck.totalCards}</div>
            <div className="stat-label">Total</div>
          </div>
          <div className="stat">
            <div className="stat-value stat-new">{deck.newCards}</div>
            <div className="stat-label">New</div>
          </div>
          <div className="stat">
            <div className="stat-value stat-learning">{deck.learningCards}</div>
            <div className="stat-label">Learning</div>
          </div>
        </div>

        <div className="stat-row">
          <div className="stat">
            <div className="stat-value stat-review">{deck.reviewCards}</div>
            <div className="stat-label">Review</div>
          </div>
          <div className="stat">
            <div className="stat-value stat-mastered">{deck.masteredCards}</div>
            <div className="stat-label">Mastered</div>
          </div>
          <div className="stat">
            <div className="stat-value stat-due">{deck.dueToday}</div>
            <div className="stat-label">Due</div>
          </div>
        </div>
      </div>

      <div className="deck-footer">
        <span className="deck-date">Updated {formatDate(deck.updatedAt)}</span>
        <div className="deck-actions">
          <button className="stats-button" onClick={handleStatsClick} title="View statistics">
            📊
          </button>
          <span className="study-button">Study →</span>
        </div>
      </div>
    </div>
  );
};

export default DeckCard;