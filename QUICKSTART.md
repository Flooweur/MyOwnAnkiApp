# Quick Start Guide

## 🚀 Get Started in 3 Steps

### Step 1: Start the Application

**On Linux/Mac:**
```bash
./start.sh
```

**On Windows:**
```bash
docker-compose up --build
```

Wait for the services to start (about 30-60 seconds on first run).

### Step 2: Open Your Browser

Navigate to: **http://localhost:3000**

You should see the Flashcard App home page with a file upload area.

### Step 3: Import Your First Deck

1. **Get an .apkg file**: 
   - If you have Anki installed, export a deck as .apkg
   - Or download a sample deck from [AnkiWeb](https://ankiweb.net/shared/decks/)

2. **Upload it**:
   - Drag and drop the .apkg file onto the upload area
   - OR click "Browse Files" and select your file

3. **Start Studying**:
   - Click on your newly imported deck
   - Review cards by clicking to flip and rating your recall

## 📊 What You'll See

### Home Page
- **Drop Zone**: Upload new decks here
- **Deck List**: All your decks with statistics:
  - Total cards
  - New (never reviewed)
  - Learning (first few reviews)
  - Review (regular rotation)
  - Mastered (high stability)
  - Due today

### Review Page
- **Question Card**: Click to reveal answer
- **Answer Card**: Shows after clicking
- **Four Buttons**:
  - 🔴 **Again**: Forgot completely
  - 🟠 **Hard**: Difficult to recall
  - 🟢 **Good**: Recalled correctly
  - 🔵 **Easy**: Very easy

## 🎯 Tips for Effective Learning

1. **Be Honest**: Rate cards based on actual difficulty
2. **Review Daily**: The algorithm works best with consistent reviews
3. **Use All Grades**: Don't just use "Good" - vary your ratings
4. **Good ≠ Perfect**: "Good" means you recalled it, even if it took effort
5. **Trust the Algorithm**: Let FSRS-6 optimize your review schedule

## 🛑 Stop the Application

**On Linux/Mac:**
```bash
./stop.sh
```

**On Windows:**
```bash
docker-compose down
```

## 🆘 Troubleshooting

### "Port already in use"
- Another service is using ports 3000, 5000, or 1433
- Stop the conflicting service or change ports in docker-compose.yml

### "Cannot connect to database"
- Wait 10-20 more seconds - SQL Server takes time to initialize
- Check logs: `docker-compose logs database`

### "Upload failed"
- Ensure the file is a valid .apkg file
- Check file size (very large files may timeout)
- Check backend logs: `docker-compose logs backend`

### View All Logs
```bash
docker-compose logs -f
```

## 📚 Next Steps

- Read the full [README.md](README.md) for detailed documentation
- Check [API documentation](http://localhost:5000/swagger) when backend is running
- Explore the FSRS algorithm explanation in the README

## 🎉 Happy Learning!