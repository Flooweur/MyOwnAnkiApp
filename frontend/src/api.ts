import axios from 'axios';
import { DeckWithStats, ReviewResponse, NextCardResponse, DailyStats, RetentionStats, DeckOverviewStats } from './types';

// API base URL - will use proxy in development
const API_BASE_URL = process.env.REACT_APP_API_URL || '/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * API service for communicating with the backend
 */
export const apiService = {
  /**
   * Gets all decks with statistics
   */
  async getDecks(): Promise<DeckWithStats[]> {
    const response = await api.get<DeckWithStats[]>('/decks');
    return response.data;
  },

  /**
   * Uploads and imports an .apkg file
   */
  async uploadApkg(file: File): Promise<void> {
    const formData = new FormData();
    formData.append('file', file);

    await api.post('/decks/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  },

  /**
   * Deletes a deck
   */
  async deleteDeck(deckId: number): Promise<void> {
    await api.delete(`/decks/${deckId}`);
  },

  /**
   * Gets a random card from a deck
   */
  async getRandomCard(deckId: number): Promise<NextCardResponse> {
    const response = await api.get<NextCardResponse>(`/cards/random/${deckId}`);
    return response.data;
  },

  /**
   * Reviews a card with a grade
   */
  async reviewCard(cardId: number, grade: number): Promise<ReviewResponse> {
    const response = await api.post<ReviewResponse>(`/cards/${cardId}/review`, {
      grade,
    });
    return response.data;
  },

  /**
   * Deletes a card
   */
  async deleteCard(cardId: number): Promise<void> {
    await api.delete(`/cards/${cardId}`);
  },

  /**
   * Gets daily statistics for a deck
   */
  async getDailyStats(deckId: number, days: number = 30): Promise<DailyStats[]> {
    const response = await api.get<DailyStats[]>(`/stats/deck/${deckId}/daily?days=${days}`);
    return response.data;
  },

  /**
   * Gets retention statistics for a deck
   */
  async getRetentionStats(deckId: number): Promise<RetentionStats> {
    const response = await api.get<RetentionStats>(`/stats/deck/${deckId}/retention`);
    return response.data;
  },

  /**
   * Gets deck overview statistics
   */
  async getDeckOverview(deckId: number): Promise<DeckOverviewStats> {
    const response = await api.get<DeckOverviewStats>(`/stats/deck/${deckId}/overview`);
    return response.data;
  },
};

export default apiService;