#!/bin/bash

# Flashcard App Stop Script
# This script stops all Docker containers for the application

echo "🛑 Stopping Flashcard App..."
echo ""

# Stop containers
docker-compose down

echo ""
echo "✅ Application stopped successfully!"
echo ""
echo "💡 To restart the application, run: ./start.sh"
echo "🗑️  To remove all data (including database), run: docker-compose down -v"