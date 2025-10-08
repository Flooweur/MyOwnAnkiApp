import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import apiService from '../api';
import { DeckWithStats } from '../types';
import DeckCard from './DeckCard';
import { ERROR_MESSAGES, UI_TEXT } from '../constants/messages';
import './HomePage.css';

/**
 * Home page component displaying deck list and file upload
 */
const HomePage: React.FC = () => {
  const [decks, setDecks] = useState<DeckWithStats[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [dragActive, setDragActive] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  /**
   * Loads all decks from the API
   */
  const loadDecks = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await apiService.getDecks();
      setDecks(data);
    } catch (err) {
      console.error('Error loading decks:', err);
      setError(ERROR_MESSAGES.LOAD_DECKS_FAILED);
    } finally {
      setLoading(false);
    }
  }, []);

  // Load decks on component mount
  useEffect(() => {
    loadDecks();
  }, [loadDecks]);

  /**
   * Handles file upload
   */
  const handleFileUpload = async (file: File) => {
    if (!file.name.endsWith('.apkg')) {
      setError(ERROR_MESSAGES.INVALID_FILE_TYPE);
      return;
    }

    try {
      setUploading(true);
      setError(null);
      await apiService.uploadApkg(file);
      await loadDecks(); // Reload decks after upload
    } catch (err) {
      console.error('Error uploading file:', err);
      setError(ERROR_MESSAGES.UPLOAD_FILE_FAILED);
    } finally {
      setUploading(false);
    }
  };

  /**
   * Handles drag events
   */
  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  };

  /**
   * Handles file drop
   */
  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);

    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      handleFileUpload(e.dataTransfer.files[0]);
    }
  };

  /**
   * Handles file input change
   */
  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      handleFileUpload(e.target.files[0]);
    }
  };

  /**
   * Navigates to the review page for a deck
   */
  const handleDeckClick = (deckId: number) => {
    navigate(`/review/${deckId}`);
  };

  /**
   * Navigates to the stats page for a deck
   */
  const handleViewStats = (deckId: number) => {
    navigate(`/stats/${deckId}`);
  };

  /**
   * Deletes a deck
   */
  const handleDeleteDeck = async (deckId: number, deckName: string) => {
    if (!window.confirm(`Are you sure you want to delete "${deckName}"?`)) {
      return;
    }

    try {
      await apiService.deleteDeck(deckId);
      await loadDecks();
    } catch (err) {
      console.error('Error deleting deck:', err);
      setError(ERROR_MESSAGES.DELETE_DECK_FAILED);
    }
  };

  return (
    <div className="home-page">
      {/* Error message */}
      {error && (
        <div className="error-message">
          <span>⚠️ {error}</span>
          <button onClick={() => setError(null)}>×</button>
        </div>
      )}

      {/* Deck list */}
      <div className="deck-list-section">
        <h2>Your Decks</h2>
        
        {loading ? (
          <div className="loading-state">
            <div className="spinner"></div>
            <p>{UI_TEXT.LOADING_DECKS}</p>
          </div>
        ) : decks.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">📚</div>
            <h3>{UI_TEXT.NO_DECKS}</h3>
            <p>{UI_TEXT.NO_DECKS_DESCRIPTION}</p>
          </div>
        ) : (
          <div className="deck-grid">
            {decks.map((deck) => (
              <DeckCard
                key={deck.id}
                deck={deck}
                onClick={() => handleDeckClick(deck.id)}
                onDelete={() => handleDeleteDeck(deck.id, deck.name)}
                onViewStats={() => handleViewStats(deck.id)}
              />
            ))}
          </div>
        )}
      </div>

      {/* Drop zone for file upload */}
      <div
        className={`drop-zone ${dragActive ? 'drag-active' : ''} ${uploading ? 'uploading' : ''}`}
        onDragEnter={handleDrag}
        onDragLeave={handleDrag}
        onDragOver={handleDrag}
        onDrop={handleDrop}
      >
        {uploading ? (
          <div className="upload-status">
            <div className="spinner"></div>
            <p>{UI_TEXT.UPLOADING}</p>
          </div>
        ) : (
          <>
            <div className="drop-zone-icon">📁</div>
            <h2>Drop .apkg file here</h2>
            <p>or</p>
            <label className="file-input-label">
              <input
                type="file"
                accept=".apkg"
                onChange={handleFileInputChange}
                className="file-input"
              />
              <span className="file-input-button">Browse Files</span>
            </label>
          </>
        )}
      </div>
    </div>
  );
};

export default HomePage;