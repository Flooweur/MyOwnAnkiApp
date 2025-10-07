// FSRS Flashcards - Frontend Application

const API_BASE = '/api';

// State
let currentDeck = null;
let dueCards = [];
let currentCardIndex = 0;
let showingAnswer = false;

// DOM Elements
const homeScreen = document.getElementById('home-screen');
const studyScreen = document.getElementById('study-screen');
const uploadZone = document.getElementById('upload-zone');
const fileInput = document.getElementById('file-input');
const decksContainer = document.getElementById('decks-container');
const noDecksMessage = document.getElementById('no-decks-message');
const backButton = document.getElementById('back-button');
const deckNameEl = document.getElementById('deck-name');
const studyProgress = document.getElementById('study-progress');
const questionCard = document.getElementById('question-card');
const answerCard = document.getElementById('answer-card');
const questionContent = document.getElementById('question-content');
const answerContent = document.getElementById('answer-content');
const showAnswerBtn = document.getElementById('show-answer-btn');
const noCardsMessage = document.getElementById('no-cards-message');
const cardDisplay = document.getElementById('card-display');
const finishButton = document.getElementById('finish-button');

// Initialize app
document.addEventListener('DOMContentLoaded', () => {
    loadDecks();
    setupEventListeners();
});

// Event Listeners
function setupEventListeners() {
    // File upload
    uploadZone.addEventListener('click', () => fileInput.click());
    uploadZone.addEventListener('dragover', handleDragOver);
    uploadZone.addEventListener('dragleave', handleDragLeave);
    uploadZone.addEventListener('drop', handleDrop);
    fileInput.addEventListener('change', handleFileSelect);
    
    // Navigation
    backButton.addEventListener('click', () => {
        showScreen('home');
        loadDecks();
    });
    
    finishButton.addEventListener('click', () => {
        showScreen('home');
        loadDecks();
    });
    
    // Study
    showAnswerBtn.addEventListener('click', showAnswer);
    
    // Rating buttons
    document.querySelectorAll('.btn-rating').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const grade = parseInt(e.currentTarget.dataset.grade);
            rateCard(grade);
        });
    });
}

// Screen Management
function showScreen(screen) {
    homeScreen.classList.remove('active');
    studyScreen.classList.remove('active');
    
    if (screen === 'home') {
        homeScreen.classList.add('active');
    } else if (screen === 'study') {
        studyScreen.classList.add('active');
    }
}

// API Calls
async function apiCall(endpoint, options = {}) {
    try {
        const response = await fetch(`${API_BASE}${endpoint}`, options);
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'API request failed');
        }
        return await response.json();
    } catch (error) {
        console.error('API Error:', error);
        alert(`Error: ${error.message}`);
        throw error;
    }
}

// Load and Display Decks
async function loadDecks() {
    const decks = await apiCall('/decks');
    displayDecks(decks);
}

function displayDecks(decks) {
    if (decks.length === 0) {
        decksContainer.innerHTML = '';
        noDecksMessage.style.display = 'block';
        return;
    }
    
    noDecksMessage.style.display = 'none';
    decksContainer.innerHTML = decks.map(deck => createDeckCard(deck)).join('');
    
    // Add event listeners to deck cards
    document.querySelectorAll('.deck-card').forEach(card => {
        const deckId = parseInt(card.dataset.deckId);
        const deleteBtn = card.querySelector('.delete-btn');
        
        card.addEventListener('click', (e) => {
            if (!e.target.classList.contains('delete-btn')) {
                startStudying(deckId);
            }
        });
        
        deleteBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            deleteDeck(deckId);
        });
    });
}

function createDeckCard(deck) {
    return `
        <div class="deck-card" data-deck-id="${deck.id}">
            <div class="deck-header">
                <h3 class="deck-name">${escapeHtml(deck.name)}</h3>
                <button class="delete-btn" title="Delete deck">🗑️</button>
            </div>
            ${deck.description ? `<p class="deck-description">${escapeHtml(deck.description)}</p>` : ''}
            <div class="deck-stats">
                <div class="stat due">
                    <span class="stat-value">${deck.stats.due}</span>
                    <span class="stat-label">Due</span>
                </div>
                <div class="stat">
                    <span class="stat-value">${deck.stats.total}</span>
                    <span class="stat-label">Total</span>
                </div>
                <div class="stat">
                    <span class="stat-value">${deck.stats.new}</span>
                    <span class="stat-label">New</span>
                </div>
                <div class="stat">
                    <span class="stat-value">${deck.stats.review}</span>
                    <span class="stat-label">Review</span>
                </div>
            </div>
        </div>
    `;
}

async function deleteDeck(deckId) {
    if (!confirm('Are you sure you want to delete this deck? This cannot be undone.')) {
        return;
    }
    
    await apiCall(`/decks/${deckId}`, { method: 'DELETE' });
    loadDecks();
}

// File Upload
function handleDragOver(e) {
    e.preventDefault();
    uploadZone.classList.add('drag-over');
}

function handleDragLeave(e) {
    e.preventDefault();
    uploadZone.classList.remove('drag-over');
}

function handleDrop(e) {
    e.preventDefault();
    uploadZone.classList.remove('drag-over');
    
    const files = e.dataTransfer.files;
    if (files.length > 0) {
        uploadFile(files[0]);
    }
}

function handleFileSelect(e) {
    const files = e.target.files;
    if (files.length > 0) {
        uploadFile(files[0]);
    }
}

async function uploadFile(file) {
    if (!file.name.endsWith('.apkg')) {
        alert('Please upload a .apkg file');
        return;
    }
    
    const formData = new FormData();
    formData.append('file', file);
    
    try {
        const result = await apiCall('/upload', {
            method: 'POST',
            body: formData
        });
        
        alert(`Deck "${result.deck.name}" imported successfully!`);
        loadDecks();
        fileInput.value = ''; // Reset file input
    } catch (error) {
        // Error already handled in apiCall
    }
}

// Study Session
async function startStudying(deckId) {
    currentDeck = await apiCall(`/decks/${deckId}`);
    dueCards = await apiCall(`/decks/${deckId}/due-cards`);
    
    if (dueCards.length === 0) {
        // Show no cards message
        showScreen('study');
        deckNameEl.textContent = currentDeck.name;
        noCardsMessage.style.display = 'block';
        cardDisplay.style.display = 'none';
        return;
    }
    
    currentCardIndex = 0;
    showScreen('study');
    deckNameEl.textContent = currentDeck.name;
    noCardsMessage.style.display = 'none';
    cardDisplay.style.display = 'block';
    
    displayCurrentCard();
}

function displayCurrentCard() {
    if (currentCardIndex >= dueCards.length) {
        // All cards reviewed
        noCardsMessage.style.display = 'block';
        cardDisplay.style.display = 'none';
        return;
    }
    
    const card = dueCards[currentCardIndex];
    showingAnswer = false;
    
    // Update progress
    studyProgress.textContent = `${currentCardIndex + 1} / ${dueCards.length}`;
    
    // Display question
    questionContent.textContent = card.front;
    answerContent.textContent = card.back;
    
    // Reset card states
    questionCard.style.display = 'block';
    answerCard.classList.remove('show');
    showAnswerBtn.style.display = 'block';
}

function showAnswer() {
    showingAnswer = true;
    answerCard.classList.add('show');
    showAnswerBtn.style.display = 'none';
    
    // Update button labels with estimated intervals
    updateRatingButtons();
}

function updateRatingButtons() {
    // For now, we'll just show placeholder times
    // In a more advanced version, we could call the API to get preview intervals
    document.getElementById('again-time').textContent = '< 10m';
    document.getElementById('hard-time').textContent = '< 1d';
    document.getElementById('good-time').textContent = '2-4d';
    document.getElementById('easy-time').textContent = '> 4d';
}

async function rateCard(grade) {
    if (!showingAnswer) {
        return;
    }
    
    const card = dueCards[currentCardIndex];
    
    try {
        await apiCall(`/cards/${card.id}/review`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ grade })
        });
        
        // Move to next card
        currentCardIndex++;
        displayCurrentCard();
        
    } catch (error) {
        // Error already handled in apiCall
    }
}

// Utility Functions
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function formatInterval(days) {
    if (days < 1) {
        return `${Math.round(days * 24)}h`;
    } else if (days < 30) {
        return `${Math.round(days)}d`;
    } else if (days < 365) {
        return `${Math.round(days / 30)}mo`;
    } else {
        return `${Math.round(days / 365)}y`;
    }
}