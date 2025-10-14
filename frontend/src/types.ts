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
}

/**
 * Flashcard
 */
export interface Card {
  id: number;
  deckId: number;
  front: string;
  back: string;
  reviewCount: number;
  lastReviewedAt?: string;
  createdAt: string;
}

/**
 * API response for card review
 */
export interface ReviewResponse {
  card: Card;
  message: string;
}

/**
 * API response for random card
 */
export interface NextCardResponse {
  message: string;
  card: Card | null;
}

/**
 * Daily statistics
 */
export interface DailyStats {
  date: string;
  cardsReviewed: number;
  cardsAgain: number;
  cardsHard: number;
  cardsGood: number;
  cardsEasy: number;
  averageRetention: number;
  timeSpentMinutes: number;
}

/**
 * Grade distribution
 */
export interface GradeDistribution {
  grade: string;
  count: number;
  percentage: number;
}

/**
 * Retention statistics
 */
export interface RetentionStats {
  overallRetention: number;
  last7DaysRetention: number;
  last30DaysRetention: number;
  gradeDistribution: GradeDistribution[];
}

/**
 * Card state distribution
 */
export interface CardStateDistribution {
  state: string;
  count: number;
  percentage: number;
}

/**
 * Deck overview statistics
 */
export interface DeckOverviewStats {
  totalCards: number;
  totalReviews: number;
  averageRetention: number;
  averageDifficulty: number;
  streakDays: number;
  stateDistribution: CardStateDistribution[];
}