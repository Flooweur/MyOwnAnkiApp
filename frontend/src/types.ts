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
  schedulingIntervals?: { [grade: string]: string };
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