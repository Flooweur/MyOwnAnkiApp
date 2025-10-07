import os
from flask import Blueprint, request, jsonify, send_from_directory
from werkzeug.utils import secure_filename
from datetime import datetime

from app import db
from app.models import Deck, Card, Review
from app.fsrs import FSRS
from app.apkg_parser import parse_apkg

main_bp = Blueprint('main', __name__)
fsrs_scheduler = FSRS()

# Serve static files
@main_bp.route('/')
def index():
    return send_from_directory('static', 'index.html')

@main_bp.route('/<path:path>')
def serve_static(path):
    return send_from_directory('static', path)

# API Routes

@main_bp.route('/api/decks', methods=['GET'])
def get_decks():
    """Get all decks with stats"""
    decks = Deck.query.all()
    return jsonify([deck.to_dict() for deck in decks])

@main_bp.route('/api/decks/<int:deck_id>', methods=['GET'])
def get_deck(deck_id):
    """Get a specific deck"""
    deck = Deck.query.get_or_404(deck_id)
    return jsonify(deck.to_dict())

@main_bp.route('/api/decks/<int:deck_id>', methods=['DELETE'])
def delete_deck(deck_id):
    """Delete a deck"""
    deck = Deck.query.get_or_404(deck_id)
    db.session.delete(deck)
    db.session.commit()
    return jsonify({'message': 'Deck deleted successfully'})

@main_bp.route('/api/decks/<int:deck_id>/cards', methods=['GET'])
def get_deck_cards(deck_id):
    """Get all cards from a deck"""
    deck = Deck.query.get_or_404(deck_id)
    cards = Card.query.filter_by(deck_id=deck_id).all()
    return jsonify([card.to_dict() for card in cards])

@main_bp.route('/api/decks/<int:deck_id>/due-cards', methods=['GET'])
def get_due_cards(deck_id):
    """Get cards due for review"""
    deck = Deck.query.get_or_404(deck_id)
    
    # Get all cards that are due
    due_cards = [card for card in deck.cards if card.is_due()]
    
    # Sort: new cards first, then by due date
    due_cards.sort(key=lambda c: (c.state != 'new', c.due_date))
    
    return jsonify([card.to_dict() for card in due_cards])

@main_bp.route('/api/cards/<int:card_id>/review', methods=['POST'])
def review_card(card_id):
    """
    Review a card with a grade
    
    Expected JSON: { "grade": 1-4 }
    1 = Again, 2 = Hard, 3 = Good, 4 = Easy
    """
    card = Card.query.get_or_404(card_id)
    data = request.get_json()
    grade = data.get('grade')
    
    if grade not in [1, 2, 3, 4]:
        return jsonify({'error': 'Invalid grade. Must be 1-4'}), 400
    
    # Save state before review
    state_before = card.state
    difficulty_before = card.difficulty
    stability_before = card.stability
    retrievability_before = card.retrievability
    
    # Prepare card state for FSRS
    card_state = {
        'state': card.state,
        'stability': card.stability,
        'difficulty': card.difficulty,
        'retrievability': card.retrievability,
        'last_review': card.last_review,
        'reps': card.reps,
        'lapses': card.lapses
    }
    
    # Schedule card using FSRS
    new_state = fsrs_scheduler.schedule_card(grade, card_state)
    
    # Update card
    card.state = new_state['state']
    card.stability = new_state['stability']
    card.difficulty = new_state['difficulty']
    card.retrievability = new_state['retrievability']
    card.interval = new_state['interval']
    card.due_date = new_state['due_date']
    card.reps = new_state['reps']
    card.lapses = new_state['lapses']
    card.last_review = new_state['last_review']
    
    # Create review record
    review = Review(
        card_id=card.id,
        grade=grade,
        state_before=state_before,
        difficulty_before=difficulty_before,
        stability_before=stability_before,
        retrievability_before=retrievability_before,
        difficulty_after=card.difficulty,
        stability_after=card.stability,
        interval_after=card.interval
    )
    
    db.session.add(review)
    db.session.commit()
    
    return jsonify({
        'card': card.to_dict(),
        'review': review.to_dict()
    })

@main_bp.route('/api/upload', methods=['POST'])
def upload_deck():
    """
    Upload and import an .apkg file
    """
    if 'file' not in request.files:
        return jsonify({'error': 'No file provided'}), 400
    
    file = request.files['file']
    
    if file.filename == '':
        return jsonify({'error': 'No file selected'}), 400
    
    if not file.filename.endswith('.apkg'):
        return jsonify({'error': 'File must be .apkg format'}), 400
    
    try:
        # Save uploaded file temporarily
        filename = secure_filename(file.filename)
        upload_folder = '/app/uploads'
        os.makedirs(upload_folder, exist_ok=True)
        file_path = os.path.join(upload_folder, filename)
        file.save(file_path)
        
        # Parse .apkg file
        deck_data = parse_apkg(file_path)
        
        # Create deck
        deck = Deck(
            name=deck_data['deck_name'],
            description=deck_data['deck_description']
        )
        db.session.add(deck)
        db.session.flush()  # Get deck.id
        
        # Create cards
        for card_data in deck_data['cards']:
            card = Card(
                deck_id=deck.id,
                front=card_data['front'],
                back=card_data['back'],
                state='new',
                difficulty=5.0,  # Default difficulty
                stability=0.0,
                retrievability=1.0,
                due_date=datetime.utcnow()  # New cards are immediately available
            )
            db.session.add(card)
        
        db.session.commit()
        
        # Clean up uploaded file
        os.remove(file_path)
        
        return jsonify({
            'message': 'Deck imported successfully',
            'deck': deck.to_dict()
        })
    
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': f'Failed to import deck: {str(e)}'}), 500

@main_bp.route('/api/health', methods=['GET'])
def health():
    """Health check endpoint"""
    return jsonify({'status': 'healthy', 'timestamp': datetime.utcnow().isoformat()})