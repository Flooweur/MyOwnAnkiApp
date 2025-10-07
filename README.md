# FSRS Flashcards - Dockerized Spaced Repetition App

A modern web-based flashcard application using the FSRS-6 (Free Spaced Repetition Scheduler) algorithm for optimal learning. Built with Flask, PostgreSQL, and Docker.

## Features

- 🧠 **FSRS-6 Algorithm**: State-of-the-art spaced repetition scheduling
- 📥 **Import Anki Decks**: Drop .apkg files to import your existing Anki decks
- 📊 **Deck Statistics**: Track your progress with detailed stats (total, new, learning, review cards)
- 🎨 **Modern UI**: Clean, responsive design with smooth animations
- 🐳 **Dockerized**: Easy deployment with Docker Compose
- 💾 **Persistent Storage**: PostgreSQL database for reliable data storage

## Quick Start

### Prerequisites

- Docker and Docker Compose installed on your system

### Running the Application

1. Clone this repository:
```bash
git clone <your-repo-url>
cd <repo-directory>
```

2. Start the application:
```bash
docker-compose up --build
```

3. Open your browser and navigate to:
```
http://localhost:5000
```

4. Import your first deck by dragging and dropping a .apkg file onto the upload zone!

## Project Structure

```
.
├── app/
│   ├── __init__.py          # Flask app initialization
│   ├── models.py            # Database models (Deck, Card, Review)
│   ├── routes.py            # API endpoints
│   ├── fsrs.py              # FSRS-6 algorithm implementation
│   ├── apkg_parser.py       # Anki .apkg file parser
│   └── static/              # Frontend files
│       ├── index.html       # Main HTML
│       ├── styles.css       # Styling
│       └── app.js           # Frontend JavaScript
├── docker-compose.yml       # Docker Compose configuration
├── Dockerfile               # Docker image definition
├── requirements.txt         # Python dependencies
└── run.py                   # Application entry point
```

## How It Works

### FSRS-6 Algorithm

This app implements the FSRS-6 algorithm, which calculates optimal review intervals based on:

- **Stability (S)**: How long a memory will last
- **Difficulty (D)**: How hard the material is for you (1-10 scale)
- **Retrievability (R)**: Current probability of recalling the card

When you review a card, you rate it:
- **Again** (1): Forgot the card
- **Hard** (2): Remembered with difficulty
- **Good** (3): Remembered correctly
- **Easy** (4): Remembered easily

The algorithm adjusts the next review interval based on your rating and the card's current state.

### Study Flow

1. **Home Screen**: View all your decks with statistics
2. **Click a Deck**: Start studying cards that are due for review
3. **See Question**: Read the question on the card
4. **Show Answer**: Reveal the answer
5. **Rate Yourself**: Choose Again, Hard, Good, or Easy
6. **Repeat**: Continue with the next due card

## API Endpoints

- `GET /api/decks` - Get all decks
- `GET /api/decks/:id` - Get specific deck
- `DELETE /api/decks/:id` - Delete a deck
- `GET /api/decks/:id/cards` - Get all cards in a deck
- `GET /api/decks/:id/due-cards` - Get cards due for review
- `POST /api/cards/:id/review` - Review a card with a grade
- `POST /api/upload` - Upload and import .apkg file

## Database Schema

### Decks Table
- `id`, `name`, `description`, `created_at`

### Cards Table
- `id`, `deck_id`, `front`, `back`
- FSRS parameters: `state`, `difficulty`, `stability`, `retrievability`
- Review tracking: `due_date`, `last_review`, `interval`, `reps`, `lapses`

### Reviews Table
- `id`, `card_id`, `grade`, `reviewed_at`
- State snapshots: before/after difficulty, stability, retrievability

## Development

### Running in Development Mode

The app runs in development mode by default with hot reloading enabled.

### Environment Variables

Copy `.env.example` to `.env` and customize:

```bash
DATABASE_URL=postgresql://fsrs:fsrs_password@db:5432/fsrs_flashcards
FLASK_ENV=development
FLASK_DEBUG=True
```

### Stopping the Application

```bash
docker-compose down
```

### Resetting the Database

```bash
docker-compose down -v  # Removes volumes including database
docker-compose up --build
```

## Technologies Used

- **Backend**: Python, Flask, SQLAlchemy
- **Database**: PostgreSQL
- **Frontend**: Vanilla JavaScript, HTML5, CSS3
- **Containerization**: Docker, Docker Compose

## Credits

- FSRS Algorithm: Developed by Jarrett Ye and the FSRS community
- Inspired by Anki, the popular spaced repetition software

## License

MIT License - feel free to use this project for learning and personal use!