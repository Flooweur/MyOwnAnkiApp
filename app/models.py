from datetime import datetime
from app import db

class Deck(db.Model):
    __tablename__ = 'decks'
    
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(255), nullable=False)
    description = db.Column(db.Text)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    
    cards = db.relationship('Card', backref='deck', lazy=True, cascade='all, delete-orphan')
    
    def to_dict(self):
        total_cards = len(self.cards)
        new_cards = sum(1 for c in self.cards if c.state == 'new')
        learning_cards = sum(1 for c in self.cards if c.state == 'learning')
        review_cards = sum(1 for c in self.cards if c.state == 'review')
        
        # Count cards due for review (including new cards)
        due_count = sum(1 for c in self.cards if c.is_due())
        
        return {
            'id': self.id,
            'name': self.name,
            'description': self.description,
            'created_at': self.created_at.isoformat(),
            'stats': {
                'total': total_cards,
                'new': new_cards,
                'learning': learning_cards,
                'review': review_cards,
                'due': due_count
            }
        }


class Card(db.Model):
    __tablename__ = 'cards'
    
    id = db.Column(db.Integer, primary_key=True)
    deck_id = db.Column(db.Integer, db.ForeignKey('decks.id'), nullable=False)
    
    # Card content
    front = db.Column(db.Text, nullable=False)
    back = db.Column(db.Text, nullable=False)
    
    # FSRS parameters
    state = db.Column(db.String(20), default='new')  # new, learning, review, relearning
    difficulty = db.Column(db.Float, default=5.0)  # D: 1-10
    stability = db.Column(db.Float, default=0.0)  # S: days
    retrievability = db.Column(db.Float, default=1.0)  # R: 0-1
    
    # Review tracking
    due_date = db.Column(db.DateTime, default=datetime.utcnow)
    last_review = db.Column(db.DateTime)
    interval = db.Column(db.Float, default=0.0)  # days
    
    # Statistics
    reps = db.Column(db.Integer, default=0)
    lapses = db.Column(db.Integer, default=0)
    
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    
    reviews = db.relationship('Review', backref='card', lazy=True, cascade='all, delete-orphan')
    
    def is_due(self):
        """Check if card is due for review"""
        return self.due_date <= datetime.utcnow()
    
    def to_dict(self):
        return {
            'id': self.id,
            'deck_id': self.deck_id,
            'front': self.front,
            'back': self.back,
            'state': self.state,
            'difficulty': self.difficulty,
            'stability': self.stability,
            'retrievability': self.retrievability,
            'due_date': self.due_date.isoformat(),
            'last_review': self.last_review.isoformat() if self.last_review else None,
            'interval': self.interval,
            'reps': self.reps,
            'lapses': self.lapses,
            'is_due': self.is_due()
        }


class Review(db.Model):
    __tablename__ = 'reviews'
    
    id = db.Column(db.Integer, primary_key=True)
    card_id = db.Column(db.Integer, db.ForeignKey('cards.id'), nullable=False)
    
    grade = db.Column(db.Integer, nullable=False)  # 1=Again, 2=Hard, 3=Good, 4=Easy
    
    # State before review
    state_before = db.Column(db.String(20))
    difficulty_before = db.Column(db.Float)
    stability_before = db.Column(db.Float)
    retrievability_before = db.Column(db.Float)
    
    # State after review
    difficulty_after = db.Column(db.Float)
    stability_after = db.Column(db.Float)
    interval_after = db.Column(db.Float)
    
    reviewed_at = db.Column(db.DateTime, default=datetime.utcnow)
    
    def to_dict(self):
        return {
            'id': self.id,
            'card_id': self.card_id,
            'grade': self.grade,
            'reviewed_at': self.reviewed_at.isoformat()
        }