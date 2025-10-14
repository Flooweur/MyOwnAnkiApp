import React, { useState } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import HomePage from './components/HomePage';
import ReviewPage from './components/ReviewPage';
import StatsPage from './components/StatsPage';
import Settings from './components/Settings';
import './App.css';

/**
 * Main application component with routing
 */
function App() {
  const [settingsOpen, setSettingsOpen] = useState(false);

  return (
    <Router>
      <div className="App">
        <header className="App-header">
          <h1>🎴 Flashcard App</h1>
          <p className="subtitle">Random Card Selection</p>
          <button 
            className="settings-icon-button"
            onClick={() => setSettingsOpen(true)}
            aria-label="Settings"
          >
            ⚙️
          </button>
        </header>
        
        <main className="App-main">
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/review/:deckId" element={<ReviewPage />} />
            <Route path="/stats/:deckId" element={<StatsPage />} />
          </Routes>
        </main>

        <Settings 
          isOpen={settingsOpen} 
          onClose={() => setSettingsOpen(false)} 
        />
      </div>
    </Router>
  );
}

export default App;