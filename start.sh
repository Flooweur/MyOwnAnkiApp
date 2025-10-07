#!/bin/bash

# Flashcard App Startup Script
# This script starts all Docker containers for the application

echo "🎴 Starting Flashcard App with FSRS..."
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Error: Docker is not running. Please start Docker and try again."
    exit 1
fi

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Error: docker-compose is not installed. Please install docker-compose and try again."
    exit 1
fi

echo "✅ Docker is running"
echo ""

# Build and start containers
echo "🔨 Building and starting containers..."
docker-compose up --build -d

# Wait for services to be ready
echo ""
echo "⏳ Waiting for services to start..."
sleep 10

# Check if containers are running
if docker-compose ps | grep -q "Up"; then
    echo ""
    echo "✅ Application started successfully!"
    echo ""
    echo "🌐 Access the application at:"
    echo "   Frontend: http://localhost:3000"
    echo "   Backend API: http://localhost:5000"
    echo "   Swagger UI: http://localhost:5000/swagger"
    echo ""
    echo "📊 To view logs, run: docker-compose logs -f"
    echo "🛑 To stop the application, run: docker-compose down"
else
    echo ""
    echo "⚠️  Warning: Some containers may not have started correctly."
    echo "   Run 'docker-compose ps' to check status"
    echo "   Run 'docker-compose logs' to view logs"
fi