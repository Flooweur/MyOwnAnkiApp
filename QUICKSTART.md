# Quick Start Guide

## 🚀 Getting Started in 3 Steps

### 1. Start the Application

```bash
docker-compose up --build
```

This will:
- Build the Flask web application
- Start a PostgreSQL database
- Initialize the database schema
- Start the web server on http://localhost:5000

### 2. Open Your Browser

Navigate to: **http://localhost:5000**

You should see the FSRS Flashcards home screen with an upload zone.

### 3. Import Your First Deck

You have two options:

#### Option A: Drag and Drop
- Simply drag an `.apkg` file onto the upload zone
- The deck will be imported automatically

#### Option B: Click to Browse
- Click on the upload zone
- Select an `.apkg` file from your computer
- Click "Open"

## 📚 Using the Application

### Home Screen
- View all your imported decks
- See statistics for each deck:
  - **Due**: Cards ready to review now
  - **Total**: Total number of cards
  - **New**: Cards you haven't studied yet
  - **Review**: Cards in the review phase
- Delete decks by clicking the trash icon (🗑️)

### Studying Cards

1. **Click on a deck** to start studying
2. **Read the question** on the card
3. **Click "Show Answer"** to reveal the answer
4. **Rate yourself** based on how well you remembered:
   - **Again**: Forgot the card (will show again soon)
   - **Hard**: Remembered with difficulty
   - **Good**: Remembered correctly (recommended for most reviews)
   - **Easy**: Remembered very easily
5. **Repeat** until all due cards are reviewed

## 🧠 How FSRS Works

The app uses the **FSRS-6 algorithm** to determine when you should review each card:

- When you rate a card as "Good", it will show up again after an optimal interval
- The algorithm learns from your ratings to personalize intervals
- Cards you find difficult appear more frequently
- Cards you find easy appear less frequently
- The goal is to review cards just before you forget them (maximum learning efficiency)

## 🛠️ Common Commands

### View Logs
```bash
docker-compose logs -f
```

### Stop the Application
```bash
docker-compose down
```

### Reset Everything (Delete all data)
```bash
docker-compose down -v
docker-compose up --build
```

### Rebuild After Code Changes
```bash
docker-compose up --build
```

## 📝 Getting .apkg Files

.apkg files are Anki deck export files. You can:

1. **Export from Anki**: If you use Anki, export any deck as .apkg
2. **Download shared decks**: Visit [AnkiWeb Shared Decks](https://ankiweb.net/shared/decks/)
3. **Create your own**: Use Anki desktop to create custom decks and export them

## ❓ Troubleshooting

### Port 5000 Already in Use
Edit `docker-compose.yml` and change the port mapping:
```yaml
ports:
  - "8080:5000"  # Use port 8080 instead
```

### Database Connection Errors
Wait a few seconds for the database to initialize on first startup. The app will retry automatically.

### Upload Not Working
- Ensure the file is a `.apkg` file
- Check that the file size is under 100MB
- Check the logs: `docker-compose logs web`

## 🎯 Next Steps

- Import multiple decks to organize different subjects
- Study regularly to build a consistent learning habit
- The algorithm works best with daily reviews
- Try to complete all "due" cards each day for optimal results

## 📊 Understanding Stats

- **New**: Cards you've never reviewed
- **Learning**: Cards you're currently learning (recently introduced)
- **Review**: Cards you've learned and are now reviewing periodically
- **Due**: Cards that are ready to be reviewed right now

Enjoy your learning journey! 🎓