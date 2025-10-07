# Deployment Checklist

## ✅ Pre-Deployment Verification

Use this checklist before deploying or starting the application.

### 1. System Requirements
- [ ] Docker installed (version 20.10+)
- [ ] Docker Compose installed (version 2.0+)
- [ ] Ports 3000, 5000, and 1433 are available
- [ ] At least 4GB of free RAM
- [ ] At least 5GB of free disk space

### 2. File Integrity
- [ ] `docker-compose.yml` exists
- [ ] `backend/Dockerfile` exists
- [ ] `frontend/Dockerfile` exists
- [ ] `backend/FlashcardApi.csproj` exists
- [ ] `frontend/package.json` exists

### 3. Configuration
- [ ] Database password configured in `docker-compose.yml`
- [ ] CORS policy configured in `backend/Program.cs`
- [ ] API URL configured in `frontend/package.json`

## 🚀 Deployment Steps

### Step 1: Start Services
```bash
# Using startup script (Linux/Mac)
./start.sh

# Or using Docker Compose directly
docker-compose up --build -d
```

### Step 2: Verify Services
```bash
# Check all containers are running
docker-compose ps

# Expected output:
# - flashcard-frontend (Up)
# - flashcard-backend (Up)
# - flashcard-db (Up, healthy)
```

### Step 3: Check Logs
```bash
# View all logs
docker-compose logs

# View specific service logs
docker-compose logs backend
docker-compose logs frontend
docker-compose logs database
```

### Step 4: Test Endpoints
- [ ] Frontend accessible at http://localhost:3000
- [ ] Backend accessible at http://localhost:5000
- [ ] Swagger UI accessible at http://localhost:5000/swagger
- [ ] API health check: `curl http://localhost:5000/api/decks`

## 🧪 Testing

### Manual Testing Checklist
- [ ] Home page loads without errors
- [ ] Upload area is visible
- [ ] Can drag and drop .apkg file
- [ ] Can browse and select .apkg file
- [ ] Deck appears in list after upload
- [ ] Deck statistics are displayed correctly
- [ ] Can click on deck to review
- [ ] Card question is displayed
- [ ] Can click to flip card
- [ ] Card answer is displayed
- [ ] All four grade buttons are visible
- [ ] Can click grade button to review
- [ ] Next card loads automatically
- [ ] Can delete deck
- [ ] Can navigate back to home

### API Testing
```bash
# Get all decks
curl http://localhost:5000/api/decks

# Upload .apkg file
curl -X POST http://localhost:5000/api/decks/upload \
  -F "file=@your-deck.apkg"

# Get next card for deck 1
curl http://localhost:5000/api/cards/next/1

# Review card with grade
curl -X POST http://localhost:5000/api/cards/1/review \
  -H "Content-Type: application/json" \
  -d '{"grade": 3}'
```

## 🔍 Troubleshooting

### Container Won't Start
```bash
# View detailed logs
docker-compose logs -f [service-name]

# Restart specific service
docker-compose restart [service-name]

# Rebuild and restart
docker-compose up --build --force-recreate [service-name]
```

### Database Issues
```bash
# Check database health
docker-compose exec database /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong!Passw0rd" -Q "SELECT 1"

# View database logs
docker-compose logs database

# Reset database (WARNING: deletes all data)
docker-compose down -v
docker-compose up -d database
```

### Port Conflicts
```bash
# Check what's using ports
lsof -i :3000
lsof -i :5000
lsof -i :1433

# Or on Windows:
netstat -ano | findstr :3000
netstat -ano | findstr :5000
netstat -ano | findstr :1433
```

## 🛑 Shutdown

### Graceful Shutdown
```bash
# Using stop script (Linux/Mac)
./stop.sh

# Or using Docker Compose
docker-compose down
```

### Complete Cleanup
```bash
# Stop and remove all data (including database)
docker-compose down -v

# Remove images (optional)
docker-compose down --rmi all
```

## 📊 Monitoring

### Real-time Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f backend
```

### Resource Usage
```bash
# Check resource usage
docker stats
```

### Database Queries
```bash
# Connect to database
docker-compose exec database /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong!Passw0rd"

# Example queries:
# SELECT COUNT(*) FROM Decks;
# SELECT COUNT(*) FROM Cards;
# SELECT COUNT(*) FROM ReviewLogs;
```

## ✅ Post-Deployment Verification

### Smoke Tests
1. [ ] Frontend loads
2. [ ] Can upload a deck
3. [ ] Can review a card
4. [ ] Can rate a card
5. [ ] Data persists after container restart

### Performance Checks
- [ ] Page load time < 2 seconds
- [ ] File upload completes successfully
- [ ] Card transitions are smooth
- [ ] No console errors in browser
- [ ] No errors in backend logs

## 🎉 Success Criteria

All of the following should be true:
- ✅ All three containers are running
- ✅ Frontend is accessible
- ✅ Backend API is responding
- ✅ Database is healthy
- ✅ Can upload and import decks
- ✅ Can review cards
- ✅ Data persists between restarts
- ✅ No critical errors in logs

If all criteria are met, your deployment is successful! 🎊