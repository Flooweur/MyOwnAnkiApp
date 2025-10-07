import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { LineChart, Line, BarChart, Bar, PieChart, Pie, Cell, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import apiService from '../api';
import { DailyStats, RetentionStats, DeckOverviewStats } from '../types';
import './StatsPage.css';

/**
 * Statistics page showing deck analytics and visualizations
 */
const StatsPage: React.FC = () => {
  const { deckId } = useParams<{ deckId: string }>();
  const navigate = useNavigate();
  
  const [dailyStats, setDailyStats] = useState<DailyStats[]>([]);
  const [retentionStats, setRetentionStats] = useState<RetentionStats | null>(null);
  const [overviewStats, setOverviewStats] = useState<DeckOverviewStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [timeRange, setTimeRange] = useState<number>(30);

  useEffect(() => {
    loadStats();
  }, [deckId, timeRange]);

  const loadStats = async () => {
    if (!deckId) return;

    try {
      setLoading(true);
      setError(null);
      
      const [daily, retention, overview] = await Promise.all([
        apiService.getDailyStats(parseInt(deckId), timeRange),
        apiService.getRetentionStats(parseInt(deckId)),
        apiService.getDeckOverview(parseInt(deckId))
      ]);
      
      setDailyStats(daily);
      setRetentionStats(retention);
      setOverviewStats(overview);
    } catch (err) {
      console.error('Error loading stats:', err);
      setError('Failed to load statistics');
    } finally {
      setLoading(false);
    }
  };

  const handleBack = () => {
    navigate('/');
  };

  if (loading) {
    return (
      <div className="stats-page">
        <div className="loading-state">
          <div className="spinner"></div>
          <p>Loading statistics...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="stats-page">
        <div className="error-message">
          <span>⚠️ {error}</span>
          <button onClick={() => setError(null)}>×</button>
        </div>
      </div>
    );
  }

  // Colors for charts
  const COLORS = {
    again: '#ef4444',
    hard: '#f59e0b',
    good: '#10b981',
    easy: '#7c3aed',
    primary: '#7c3aed',
    secondary: '#a78bfa'
  };

  return (
    <div className="stats-page">
      <div className="stats-header">
        <button className="back-button-small" onClick={handleBack}>
          ← Back to Decks
        </button>
        <h2>Deck Statistics</h2>
      </div>

      {/* Overview Cards */}
      {overviewStats && (
        <div className="overview-cards">
          <div className="stat-card">
            <div className="stat-card-icon">🎴</div>
            <div className="stat-card-value">{overviewStats.totalCards}</div>
            <div className="stat-card-label">Total Cards</div>
          </div>
          <div className="stat-card">
            <div className="stat-card-icon">📝</div>
            <div className="stat-card-value">{overviewStats.totalReviews}</div>
            <div className="stat-card-label">Total Reviews</div>
          </div>
          <div className="stat-card">
            <div className="stat-card-icon">🎯</div>
            <div className="stat-card-value">{(overviewStats.averageRetention * 100).toFixed(1)}%</div>
            <div className="stat-card-label">Avg Retention</div>
          </div>
          <div className="stat-card">
            <div className="stat-card-icon">🔥</div>
            <div className="stat-card-value">{overviewStats.streakDays}</div>
            <div className="stat-card-label">Day Streak</div>
          </div>
        </div>
      )}

      {/* Time Range Selector */}
      <div className="time-range-selector">
        <button 
          className={timeRange === 7 ? 'active' : ''} 
          onClick={() => setTimeRange(7)}
        >
          7 Days
        </button>
        <button 
          className={timeRange === 30 ? 'active' : ''} 
          onClick={() => setTimeRange(30)}
        >
          30 Days
        </button>
        <button 
          className={timeRange === 90 ? 'active' : ''} 
          onClick={() => setTimeRange(90)}
        >
          90 Days
        </button>
      </div>

      {/* Charts Grid */}
      <div className="charts-grid">
        {/* Daily Reviews Chart */}
        <div className="chart-container">
          <h3>Cards Reviewed Per Day</h3>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={dailyStats}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.1)" />
              <XAxis dataKey="date" stroke="#cbd5e1" tick={{ fontSize: 12 }} />
              <YAxis stroke="#cbd5e1" />
              <Tooltip 
                contentStyle={{ 
                  backgroundColor: '#1a1a2e', 
                  border: '1px solid rgba(255,255,255,0.1)',
                  borderRadius: '8px',
                  color: '#f8fafc'
                }} 
              />
              <Legend />
              <Bar dataKey="cardsReviewed" fill={COLORS.primary} name="Cards Reviewed" />
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Grade Distribution Chart */}
        <div className="chart-container">
          <h3>Performance by Grade</h3>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={dailyStats}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.1)" />
              <XAxis dataKey="date" stroke="#cbd5e1" tick={{ fontSize: 12 }} />
              <YAxis stroke="#cbd5e1" />
              <Tooltip 
                contentStyle={{ 
                  backgroundColor: '#1a1a2e', 
                  border: '1px solid rgba(255,255,255,0.1)',
                  borderRadius: '8px',
                  color: '#f8fafc'
                }} 
              />
              <Legend />
              <Bar dataKey="cardsAgain" stackId="a" fill={COLORS.again} name="Again" />
              <Bar dataKey="cardsHard" stackId="a" fill={COLORS.hard} name="Hard" />
              <Bar dataKey="cardsGood" stackId="a" fill={COLORS.good} name="Good" />
              <Bar dataKey="cardsEasy" stackId="a" fill={COLORS.easy} name="Easy" />
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Retention Rate Chart */}
        <div className="chart-container">
          <h3>Average Retention Rate</h3>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={dailyStats}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.1)" />
              <XAxis dataKey="date" stroke="#cbd5e1" tick={{ fontSize: 12 }} />
              <YAxis stroke="#cbd5e1" domain={[0, 1]} />
              <Tooltip 
                contentStyle={{ 
                  backgroundColor: '#1a1a2e', 
                  border: '1px solid rgba(255,255,255,0.1)',
                  borderRadius: '8px',
                  color: '#f8fafc'
                }}
                formatter={(value: number) => `${(value * 100).toFixed(1)}%`}
              />
              <Legend />
              <Line 
                type="monotone" 
                dataKey="averageRetention" 
                stroke={COLORS.good} 
                strokeWidth={2}
                name="Retention Rate"
                dot={{ fill: COLORS.good, r: 4 }}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>

        {/* Overall Grade Distribution Pie Chart */}
        {retentionStats && retentionStats.gradeDistribution.length > 0 && (
          <div className="chart-container">
            <h3>Overall Grade Distribution</h3>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={retentionStats.gradeDistribution}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ grade, percentage }) => `${grade}: ${percentage.toFixed(1)}%`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="count"
                >
                  {retentionStats.gradeDistribution.map((entry, index) => {
                    const colorMap: { [key: string]: string } = {
                      'Again': COLORS.again,
                      'Hard': COLORS.hard,
                      'Good': COLORS.good,
                      'Easy': COLORS.easy
                    };
                    return <Cell key={`cell-${index}`} fill={colorMap[entry.grade] || COLORS.primary} />;
                  })}
                </Pie>
                <Tooltip 
                  contentStyle={{ 
                    backgroundColor: '#1a1a2e', 
                    border: '1px solid rgba(255,255,255,0.1)',
                    borderRadius: '8px',
                    color: '#f8fafc'
                  }} 
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
        )}

        {/* Card State Distribution */}
        {overviewStats && overviewStats.stateDistribution.length > 0 && (
          <div className="chart-container">
            <h3>Card State Distribution</h3>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={overviewStats.stateDistribution}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ state, percentage }) => `${state}: ${percentage.toFixed(1)}%`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="count"
                >
                  {overviewStats.stateDistribution.map((entry, index) => {
                    const stateColors: { [key: string]: string } = {
                      'New': '#3b82f6',
                      'Learning': '#f59e0b',
                      'Review': '#8b5cf6',
                      'Relearning': '#ef4444'
                    };
                    return <Cell key={`cell-${index}`} fill={stateColors[entry.state] || COLORS.primary} />;
                  })}
                </Pie>
                <Tooltip 
                  contentStyle={{ 
                    backgroundColor: '#1a1a2e', 
                    border: '1px solid rgba(255,255,255,0.1)',
                    borderRadius: '8px',
                    color: '#f8fafc'
                  }} 
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
        )}

        {/* Retention Stats Summary */}
        {retentionStats && (
          <div className="chart-container retention-summary">
            <h3>Retention Summary</h3>
            <div className="retention-stats">
              <div className="retention-stat">
                <div className="retention-label">Overall</div>
                <div className="retention-value">{retentionStats.overallRetention.toFixed(1)}%</div>
              </div>
              <div className="retention-stat">
                <div className="retention-label">Last 7 Days</div>
                <div className="retention-value">{retentionStats.last7DaysRetention.toFixed(1)}%</div>
              </div>
              <div className="retention-stat">
                <div className="retention-label">Last 30 Days</div>
                <div className="retention-value">{retentionStats.last30DaysRetention.toFixed(1)}%</div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default StatsPage;