/**
 * Chart color constants for consistent theming across statistics visualizations
 */
export const CHART_COLORS = {
  again: '#ef4444',
  hard: '#f59e0b',
  good: '#10b981',
  easy: '#7c3aed',
  primary: '#7c3aed',
  secondary: '#a78bfa',
  // Card state colors
  new: '#3b82f6',
  learning: '#f59e0b',
  review: '#8b5cf6',
  relearning: '#ef4444',
} as const;

/**
 * Tooltip style for charts
 */
export const CHART_TOOLTIP_STYLE = {
  backgroundColor: '#1a1a2e',
  border: '1px solid rgba(255,255,255,0.1)',
  borderRadius: '8px',
  color: '#f8fafc',
} as const;

/**
 * Maps grade names to their corresponding colors
 */
export const GRADE_COLOR_MAP: { [key: string]: string } = {
  'Again': CHART_COLORS.again,
  'Hard': CHART_COLORS.hard,
  'Good': CHART_COLORS.good,
  'Easy': CHART_COLORS.easy,
};

/**
 * Maps card state names to their corresponding colors
 */
export const STATE_COLOR_MAP: { [key: string]: string } = {
  'New': CHART_COLORS.new,
  'Learning': CHART_COLORS.learning,
  'Review': CHART_COLORS.review,
  'Relearning': CHART_COLORS.relearning,
};
