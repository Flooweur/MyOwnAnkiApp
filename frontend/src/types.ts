/**
 * Type definitions for the Flashcard application
 */

/**
 * Deck with statistics
 */
export interface DeckWithStats {
  id: number;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
  totalCards: number;
  newCards: number;
  learningCards: number;
  reviewCards: number;
  masteredCards: number;
  dueToday: number;
}

/**
 * Card state enum
 */
export enum CardState {
  New = 0,
  Learning = 1,
  Review = 2,
  Relearning = 3,
}

/**
 * Flashcard
 */
export interface Card {
  id: number;
  deckId: number;
  front: string;
  back: string;
  stability: number;
  difficulty: number;
  retrievability: number;
  reviewCount: number;
  lapseCount: number;
  state: CardState;
  dueDate?: string;
  lastReviewedAt?: string;
  createdAt: string;
}

/**
 * Review grade options
 */
export enum ReviewGrade {
  Again = 1,
  Hard = 2,
  Good = 3,
  Easy = 4,
}

/**
 * API response for card review
 */
export interface ReviewResponse {
  card: Card;
  message: string;
}

/**
 * API response for next card
 */
export interface NextCardResponse {
  message: string;
  card: Card | null;
}