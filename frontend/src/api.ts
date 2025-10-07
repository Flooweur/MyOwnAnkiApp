import axios from 'axios';
import { DeckWithStats, Card, ReviewResponse, NextCardResponse } from './types';

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
   * Gets the next card to review from a deck
   */
  async getNextCard(deckId: number): Promise<NextCardResponse> {
    const response = await api.get<NextCardResponse>(`/cards/next/${deckId}`);
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
};

export default apiService;