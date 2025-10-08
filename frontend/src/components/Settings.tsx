import React, { useState, useEffect } from 'react';
import './Settings.css';

interface SettingsProps {
  isOpen: boolean;
  onClose: () => void;
}

/**
 * Settings modal for LLM configuration
 */
const Settings: React.FC<SettingsProps> = ({ isOpen, onClose }) => {
  const [endpoint, setEndpoint] = useState('');
  const [modelName, setModelName] = useState('');
  const [apiKey, setApiKey] = useState('');
  const [aiAugmentedEnabled, setAiAugmentedEnabled] = useState(false);

  // Load settings from localStorage on mount
  useEffect(() => {
    const savedEndpoint = localStorage.getItem('llm_endpoint') || '';
    const savedModelName = localStorage.getItem('llm_model_name') || '';
    const savedApiKey = localStorage.getItem('llm_api_key') || '';
    const savedAiAugmented = localStorage.getItem('ai_augmented_enabled') === 'true';
    
    setEndpoint(savedEndpoint);
    setModelName(savedModelName);
    setApiKey(savedApiKey);
    setAiAugmentedEnabled(savedAiAugmented);
  }, []);

  const handleSave = () => {
    localStorage.setItem('llm_endpoint', endpoint);
    localStorage.setItem('llm_model_name', modelName);
    localStorage.setItem('llm_api_key', apiKey);
    localStorage.setItem('ai_augmented_enabled', aiAugmentedEnabled.toString());
    onClose();
  };

  const handleClear = () => {
    setEndpoint('');
    setModelName('');
    setApiKey('');
    setAiAugmentedEnabled(false);
    localStorage.removeItem('llm_endpoint');
    localStorage.removeItem('llm_model_name');
    localStorage.removeItem('llm_api_key');
    localStorage.removeItem('ai_augmented_enabled');
  };

  if (!isOpen) return null;

  return (
    <div className="settings-overlay" onClick={onClose}>
      <div className="settings-modal" onClick={(e) => e.stopPropagation()}>
        <div className="settings-header">
          <h2>LLM Settings</h2>
          <button className="close-button" onClick={onClose}>×</button>
        </div>
        
        <div className="settings-body">
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
        </div>

        <div className="settings-footer">
          <button className="clear-button" onClick={handleClear}>
            Clear All
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
