/**
 * User-facing messages and text constants
 */
export const ERROR_MESSAGES = {
  LOAD_DECKS_FAILED: 'Failed to load decks. Please try again.',
  LOAD_CARD_FAILED: 'Failed to load card. Please try again.',
  REVIEW_CARD_FAILED: 'Failed to review card. Please try again.',
  UPLOAD_FILE_FAILED: 'Failed to upload file. Please try again.',
  DELETE_DECK_FAILED: 'Failed to delete deck. Please try again.',
  COMPARE_ANSWER_FAILED: 'Failed to compare answer. Please try again.',
  LOAD_STATS_FAILED: 'Failed to load statistics',
  INVALID_FILE_TYPE: 'Please upload a valid .apkg file',
  AI_EVALUATION_UNAVAILABLE: 'Unable to evaluate your answer at this time.',
} as const;

export const SUCCESS_MESSAGES = {
  FILE_UPLOADED: 'File uploaded successfully',
  DECK_DELETED: 'Deck deleted successfully',
} as const;

export const PLACEHOLDERS = {
  ANSWER_INPUT: 'Type your answer here... (Press Enter to submit, Shift+Enter for new line)',
} as const;

export const UI_TEXT = {
  LOADING: 'Loading...',
  LOADING_CARD: 'Loading card...',
  LOADING_DECKS: 'Loading decks...',
  LOADING_STATS: 'Loading statistics...',
  UPLOADING: 'Uploading and importing deck...',
  CHECKING_ANSWER: 'Checking...',
  SCHEDULING: 'Scheduling next review...',
  TAP_TO_REVEAL: 'Tap to reveal answer',
  NO_DECKS: 'No decks yet',
  NO_DECKS_DESCRIPTION: 'Upload an .apkg file to get started!',
  ALL_DONE: 'All done!',
  NO_CARDS_DUE: 'No more cards due for review in this deck.',
} as const;
