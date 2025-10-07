import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import HomePage from './components/HomePage';
import ReviewPage from './components/ReviewPage';
import StatsPage from './components/StatsPage';
import './App.css';

/**
 * Main application component with routing
 */
function App() {
  return (
    <Router>
      <div className="App">
        <header className="App-header">
          <h1>🎴 Flashcard App</h1>
          <p className="subtitle">Powered by FSRS-6 Algorithm</p>
        </header>
        
        <main className="App-main">
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/review/:deckId" element={<ReviewPage />} />
            <Route path="/stats/:deckId" element={<StatsPage />} />
          </Routes>
        </main>
      </div>
    </Router>
  );
}

export default App;