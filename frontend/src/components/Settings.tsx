import React, { useState } from 'react';
import { useLocalStorage } from '../hooks/useLocalStorage';
import './Settings.css';

interface SettingsProps {
  isOpen: boolean;
  onClose: () => void;
}

/**
 * Settings modal for LLM configuration and Anki scheduler parameters
 */
const Settings: React.FC<SettingsProps> = ({ isOpen, onClose }) => {
  const [activeTab, setActiveTab] = useState<'llm' | 'scheduler'>('llm');
  
  // LLM Settings
  const [endpoint, setEndpoint] = useLocalStorage('llm_endpoint', '');
  const [modelName, setModelName] = useLocalStorage('llm_model_name', '');
  const [apiKey, setApiKey] = useLocalStorage('llm_api_key', '');
  const [aiAugmentedEnabled, setAiAugmentedEnabled] = useLocalStorage('ai_augmented_enabled', false);

  // Anki Scheduler Settings (Global Defaults)
  const [learningSteps, setLearningSteps] = useLocalStorage('learning_steps', '1 10');
  const [graduatingIntervalGood, setGraduatingIntervalGood] = useLocalStorage('graduating_interval_good', '1');
  const [graduatingIntervalEasy, setGraduatingIntervalEasy] = useLocalStorage('graduating_interval_easy', '4');
  const [relearningSteps, setRelearningSteps] = useLocalStorage('relearning_steps', '10');
  const [lapseMultiplier, setLapseMultiplier] = useLocalStorage('lapse_multiplier', '0');
  const [minimumLapseInterval, setMinimumLapseInterval] = useLocalStorage('minimum_lapse_interval', '1');
  const [leechThreshold, setLeechThreshold] = useLocalStorage('leech_threshold', '8');
  const [initialEaseFactor, setInitialEaseFactor] = useLocalStorage('initial_ease_factor', '2.5');
  const [hardMultiplier, setHardMultiplier] = useLocalStorage('hard_multiplier', '1.2');
  const [easyMultiplier, setEasyMultiplier] = useLocalStorage('easy_multiplier', '1.3');
  const [intervalMultiplier, setIntervalMultiplier] = useLocalStorage('interval_multiplier', '1.0');
  const [maximumInterval, setMaximumInterval] = useLocalStorage('maximum_interval', '36500');
  const [requestRetention, setRequestRetention] = useLocalStorage('request_retention', '0.9');

  const handleSave = () => {
    onClose();
  };

  const handleClearLLM = () => {
    setEndpoint('');
    setModelName('');
    setApiKey('');
    setAiAugmentedEnabled(false);
  };

  const handleResetScheduler = () => {
    setLearningSteps('1 10');
    setGraduatingIntervalGood('1');
    setGraduatingIntervalEasy('4');
    setRelearningSteps('10');
    setLapseMultiplier('0');
    setMinimumLapseInterval('1');
    setLeechThreshold('8');
    setInitialEaseFactor('2.5');
    setHardMultiplier('1.2');
    setEasyMultiplier('1.3');
    setIntervalMultiplier('1.0');
    setMaximumInterval('36500');
    setRequestRetention('0.9');
  };

  if (!isOpen) return null;

  return (
    <div className="settings-overlay" onClick={onClose}>
      <div className="settings-modal" onClick={(e) => e.stopPropagation()}>
        <div className="settings-header">
          <h2>Settings</h2>
          <button className="close-button" onClick={onClose}>×</button>
        </div>

        <div className="settings-tabs">
          <button 
            className={`tab-button ${activeTab === 'llm' ? 'active' : ''}`}
            onClick={() => setActiveTab('llm')}
          >
            LLM
          </button>
          <button 
            className={`tab-button ${activeTab === 'scheduler' ? 'active' : ''}`}
            onClick={() => setActiveTab('scheduler')}
          >
            Scheduler
          </button>
        </div>
        
        <div className="settings-body">
          {activeTab === 'llm' ? (
            <>
              <div className="settings-field settings-toggle">
                <div className="toggle-label-group">
                  <label htmlFor="aiAugmented">AI Augmented Cards</label>
                  <p className="toggle-description">Reword questions using AI to reduce memorization</p>
                </div>
                <label className="toggle-switch">
                  <input
                    id="aiAugmented"
                    type="checkbox"
                    checked={aiAugmentedEnabled}
                    onChange={(e) => setAiAugmentedEnabled(e.target.checked)}
                  />
                  <span className="toggle-slider"></span>
                </label>
              </div>

              <div className="settings-field">
                <label htmlFor="endpoint">API Endpoint</label>
                <input
                  id="endpoint"
                  type="text"
                  value={endpoint}
                  onChange={(e) => setEndpoint(e.target.value)}
                  placeholder="https://api.example.com/v1"
                />
              </div>

              <div className="settings-field">
                <label htmlFor="modelName">Model Name</label>
                <input
                  id="modelName"
                  type="text"
                  value={modelName}
                  onChange={(e) => setModelName(e.target.value)}
                  placeholder="gpt-4, gemini-pro, etc."
                />
              </div>

              <div className="settings-field">
                <label htmlFor="apiKey">API Key</label>
                <input
                  id="apiKey"
                  type="password"
                  value={apiKey}
                  onChange={(e) => setApiKey(e.target.value)}
                  placeholder="Your API key"
                />
              </div>

              <div className="settings-info">
                <p>Configure your LLM settings to enable AI-powered question reformulation. The AI will rephrase questions to reduce memorization and add contextual scenarios.</p>
              </div>
            </>
          ) : (
            <>
              <div className="settings-section">
                <h3>Learning</h3>
                <div className="settings-field">
                  <label htmlFor="learningSteps">Learning Steps (minutes)</label>
                  <input
                    id="learningSteps"
                    type="text"
                    value={learningSteps}
                    onChange={(e) => setLearningSteps(e.target.value)}
                    placeholder="1 10"
                  />
                  <small>Space-separated values (e.g., "1 10" for 1min and 10min)</small>
                </div>

                <div className="settings-field">
                  <label htmlFor="graduatingIntervalGood">Graduating Interval - Good (days)</label>
                  <input
                    id="graduatingIntervalGood"
                    type="number"
                    value={graduatingIntervalGood}
                    onChange={(e) => setGraduatingIntervalGood(e.target.value)}
                    min="1"
                  />
                </div>

                <div className="settings-field">
                  <label htmlFor="graduatingIntervalEasy">Graduating Interval - Easy (days)</label>
                  <input
                    id="graduatingIntervalEasy"
                    type="number"
                    value={graduatingIntervalEasy}
                    onChange={(e) => setGraduatingIntervalEasy(e.target.value)}
                    min="1"
                  />
                </div>
              </div>

              <div className="settings-section">
                <h3>Lapses</h3>
                <div className="settings-field">
                  <label htmlFor="relearningSteps">Relearning Steps (minutes)</label>
                  <input
                    id="relearningSteps"
                    type="text"
                    value={relearningSteps}
                    onChange={(e) => setRelearningSteps(e.target.value)}
                    placeholder="10"
                  />
                  <small>Space-separated values</small>
                </div>

                <div className="settings-field">
                  <label htmlFor="lapseMultiplier">Lapse Multiplier</label>
                  <input
                    id="lapseMultiplier"
                    type="number"
                    value={lapseMultiplier}
                    onChange={(e) => setLapseMultiplier(e.target.value)}
                    step="0.1"
                    min="0"
                    max="1"
                  />
                  <small>0 = Use FSRS calculation</small>
                </div>

                <div className="settings-field">
                  <label htmlFor="minimumLapseInterval">Minimum Lapse Interval (days)</label>
                  <input
                    id="minimumLapseInterval"
                    type="number"
                    value={minimumLapseInterval}
                    onChange={(e) => setMinimumLapseInterval(e.target.value)}
                    min="1"
                  />
                </div>

                <div className="settings-field">
                  <label htmlFor="leechThreshold">Leech Threshold</label>
                  <input
                    id="leechThreshold"
                    type="number"
                    value={leechThreshold}
                    onChange={(e) => setLeechThreshold(e.target.value)}
                    min="1"
                  />
                  <small>Number of lapses before marking as leech</small>
                </div>
              </div>

              <div className="settings-section">
                <h3>Review</h3>
                <div className="settings-field">
                  <label htmlFor="initialEaseFactor">Initial Ease Factor</label>
                  <input
                    id="initialEaseFactor"
                    type="number"
                    value={initialEaseFactor}
                    onChange={(e) => setInitialEaseFactor(e.target.value)}
                    step="0.1"
                    min="1.3"
                  />
                </div>

                <div className="settings-field">
                  <label htmlFor="hardMultiplier">Hard Interval Multiplier</label>
                  <input
                    id="hardMultiplier"
                    type="number"
                    value={hardMultiplier}
                    onChange={(e) => setHardMultiplier(e.target.value)}
                    step="0.1"
                    min="0.5"
                    max="2"
                  />
                </div>

                <div className="settings-field">
                  <label htmlFor="easyMultiplier">Easy Interval Multiplier</label>
                  <input
                    id="easyMultiplier"
                    type="number"
                    value={easyMultiplier}
                    onChange={(e) => setEasyMultiplier(e.target.value)}
                    step="0.1"
                    min="1"
                    max="3"
                  />
                </div>

                <div className="settings-field">
                  <label htmlFor="intervalMultiplier">Interval Multiplier</label>
                  <input
                    id="intervalMultiplier"
                    type="number"
                    value={intervalMultiplier}
                    onChange={(e) => setIntervalMultiplier(e.target.value)}
                    step="0.1"
                    min="0.5"
                    max="2"
                  />
                  <small>Global multiplier for all review intervals</small>
                </div>

                <div className="settings-field">
                  <label htmlFor="maximumInterval">Maximum Interval (days)</label>
                  <input
                    id="maximumInterval"
                    type="number"
                    value={maximumInterval}
                    onChange={(e) => setMaximumInterval(e.target.value)}
                    min="1"
                  />
                </div>

                <div className="settings-field">
                  <label htmlFor="requestRetention">Desired Retention</label>
                  <input
                    id="requestRetention"
                    type="number"
                    value={requestRetention}
                    onChange={(e) => setRequestRetention(e.target.value)}
                    step="0.01"
                    min="0.7"
                    max="0.99"
                  />
                  <small>Target retention rate (0.9 = 90%)</small>
                </div>
              </div>

              <div className="settings-info">
                <p>These are global default scheduler parameters based on the Anki algorithm. They control learning steps, interval calculations, and lapse handling.</p>
              </div>
            </>
          )}
        </div>

        <div className="settings-footer">
          <button className="clear-button" onClick={activeTab === 'llm' ? handleClearLLM : handleResetScheduler}>
            {activeTab === 'llm' ? 'Clear All' : 'Reset to Defaults'}
          </button>
          <button className="save-button" onClick={handleSave}>
            Save Settings
          </button>
        </div>
      </div>
    </div>
  );
};

export default Settings;
