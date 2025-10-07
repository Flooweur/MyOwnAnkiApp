# 🎴 Flashcard App with FSRS-6 Algorithm

A fully dockerized flashcard application with a React frontend, .NET backend, and SQL Server database. Features intelligent spaced repetition using the FSRS-6 (Free Spaced Repetition Scheduler) algorithm for optimal learning efficiency.

## ✨ Features

- 📚 **Import Anki Decks**: Drag and drop .apkg files to import your existing Anki decks
- 🧠 **FSRS-6 Algorithm**: State-of-the-art spaced repetition scheduling
- 📊 **Comprehensive Statistics**: Track total cards, new cards, learning progress, and mastered cards
- 🎴 **Beautiful Card Interface**: Interactive flip animation for reviewing cards
- 🎯 **Four-Grade System**: Rate your recall with Again, Hard, Good, or Easy
- 🐳 **Fully Dockerized**: Easy deployment with Docker Compose
- 💾 **Persistent Storage**: SQL Server database with automatic migrations
- 🎨 **Modern UI/UX**: Beautiful gradient design with smooth animations

## 🚀 Quick Start

### Prerequisites
- [Docker](https://www.docker.com/get-started) (version 20.10+)
- [Docker Compose](https://docs.docker.com/compose/install/) (version 2.0+)

### Running the Application

#### Option 1: Using the startup script (Linux/Mac)
```bash
./start.sh
```

#### Option 2: Using Docker Compose directly
```bash
docker-compose up --build
```

### Accessing the Application

Once started, you can access:
- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger (API documentation)
- **Database**: localhost:1433 (SQL Server)

### Stopping the Application

#### Option 1: Using the stop script (Linux/Mac)
```bash
./stop.sh
```

#### Option 2: Using Docker Compose directly
```bash
docker-compose down
```

To remove all data including the database:
```bash
docker-compose down -v
```

## 🏗️ Architecture

### Technology Stack

- **Frontend**: 
  - React 18 with TypeScript
  - React Router for navigation
  - Axios for API communication
  - CSS3 with modern animations and gradients

- **Backend**: 
  - .NET 8 Web API
  - Entity Framework Core for database access
  - SQLite library for .apkg file parsing
  - Swagger for API documentation

- **Database**: 
  - SQL Server 2022 Express
  - Automatic migrations on startup
  - Persistent volume storage

### Project Structure

```
.
├── backend/                    # .NET backend
│   ├── Controllers/            # API endpoints
│   ├── Data/                   # Database context
│   ├── Migrations/             # EF Core migrations
│   ├── Models/                 # Data models
│   ├── Services/               # Business logic
│   │   └── FSRS/               # FSRS algorithm implementation
│   └── Dockerfile
├── frontend/                   # React frontend
│   ├── public/                 # Static files
│   ├── src/
│   │   ├── components/         # React components
│   │   ├── api.ts              # API service
│   │   └── types.ts            # TypeScript definitions
│   └── Dockerfile
├── docker-compose.yml          # Docker orchestration
└── README.md
```

## 📖 Usage Guide

### 1. Importing a Deck

There are two ways to import an Anki deck:

**Method A: Drag and Drop**
- Visit the home page
- Drag your .apkg file and drop it onto the drop zone
- The deck will be automatically imported

**Method B: File Browser**
- Click the "Browse Files" button
- Select your .apkg file
- The deck will be automatically imported

### 2. Reviewing Cards

1. Click on any deck card on the home page
2. You'll see the question side of the first card
3. Click on the card to flip it and reveal the answer
4. Rate your recall using one of four buttons:
   - **Again** (Red): You forgot the card completely
   - **Hard** (Orange): You recalled it, but with difficulty
   - **Good** (Green): You recalled it correctly with some effort
   - **Easy** (Blue): You recalled it very easily

5. The next card will be automatically loaded

### 3. Understanding Deck Statistics

Each deck card displays:
- **Total**: Total number of cards in the deck
- **New**: Cards that have never been reviewed
- **Learning**: Cards in the initial learning phase
- **Review**: Cards in regular review rotation
- **Mastered**: Cards with stability > 100 days
- **Due**: Cards due for review today

## 🧠 FSRS-6 Algorithm

This application implements the FSRS-6 (Free Spaced Repetition Scheduler) algorithm, which provides more accurate scheduling than traditional algorithms like SM-2.

### Key Concepts

**Memory Stability (S)**
- Represents how long it takes for memory retrievability to decline from 100% to 90%
- Increases with each successful review
- Takes into account the spacing effect (optimal review timing)

**Retrievability (R)**
- The probability of successfully recalling a card at any given time
- Calculated using a power function forgetting curve
- Decreases over time since the last review

**Difficulty (D)**
- A measure of how difficult a card is for you personally (1-10 scale)
- Adjusts based on your review performance
- Uses mean reversion to stabilize over time

### How It Works

1. **Initial Review**: When you first review a card, it gets an initial stability based on your grade
2. **Subsequent Reviews**: 
   - If you recall successfully (Hard/Good/Easy), stability increases
   - The increase is larger when R is lower (spacing effect)
   - The increase is smaller for more difficult cards
   - Stability saturates (harder to increase as it gets higher)
3. **Lapses**: If you forget (Again), stability decreases but never goes below a minimum
4. **Scheduling**: Next review date is calculated to maintain ~90% retrievability

### Benefits

- ✅ More accurate than traditional algorithms
- ✅ Adapts to individual card difficulty
- ✅ Considers optimal spacing for maximum retention
- ✅ Reduces unnecessary reviews while maintaining high retention
- ✅ Based on extensive research and real-world data

## 🛠️ Development

### Backend Development

```bash
cd backend
dotnet restore
dotnet run
```

### Frontend Development

```bash
cd frontend
npm install
npm start
```

### Database Migrations

To create a new migration:
```bash
cd backend
dotnet ef migrations add MigrationName
```

To apply migrations:
```bash
dotnet ef database update
```

## 📝 API Documentation

When the backend is running, visit http://localhost:5000/swagger for interactive API documentation.

### Main Endpoints

**Decks**
- `GET /api/decks` - Get all decks with statistics
- `GET /api/decks/{id}` - Get a specific deck
- `POST /api/decks` - Create a new deck
- `POST /api/decks/upload` - Upload and import .apkg file
- `DELETE /api/decks/{id}` - Delete a deck

**Cards**
- `GET /api/cards/next/{deckId}` - Get next card to review
- `GET /api/cards/due/{deckId}` - Get all due cards
- `POST /api/cards/{id}/review` - Review a card with a grade

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is open source and available under the MIT License.

## 🙏 Acknowledgments

- FSRS algorithm developed by [Jarrett Ye](https://github.com/open-spaced-repetition/fsrs4anki) and LMSherlock
- Inspired by [Anki](https://apps.ankiweb.net/), the popular flashcard application