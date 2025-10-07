"""
.apkg file parser for Anki deck files
.apkg files are SQLite databases in a ZIP container
"""
import os
import zipfile
import sqlite3
import tempfile
from app.models import Deck, Card

def parse_apkg(file_path):
    """
    Parse an .apkg file and extract deck and card data
    
    Returns:
        dict with 'deck' info and 'cards' list
    """
    # .apkg is a ZIP file containing collection.anki2 (SQLite database)
    with tempfile.TemporaryDirectory() as tmpdir:
        # Extract the .apkg file
        with zipfile.ZipFile(file_path, 'r') as zip_ref:
            zip_ref.extractall(tmpdir)
        
        # Connect to the collection database
        db_path = os.path.join(tmpdir, 'collection.anki2')
        if not os.path.exists(db_path):
            # Try alternative name
            db_path = os.path.join(tmpdir, 'collection.anki21')
        
        if not os.path.exists(db_path):
            raise ValueError("No collection database found in .apkg file")
        
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Get deck information
        cursor.execute("SELECT decks FROM col")
        decks_json = cursor.fetchone()[0]
        
        # Get deck name (simplified - in real Anki this is JSON)
        # For simplicity, we'll use the filename as deck name
        deck_name = os.path.splitext(os.path.basename(file_path))[0]
        
        # Get notes (cards content)
        cursor.execute("SELECT id, flds FROM notes")
        notes = cursor.fetchall()
        
        # Get cards
        cursor.execute("SELECT id, nid, ord FROM cards")
        cards_data = cursor.fetchall()
        
        # Build card objects
        cards = []
        note_dict = {note[0]: note[1] for note in notes}
        
        for card_id, note_id, ord_num in cards_data:
            if note_id in note_dict:
                # Fields are separated by \x1f
                fields = note_dict[note_id].split('\x1f')
                
                # Typically, first field is front, second is back
                front = fields[0] if len(fields) > 0 else ""
                back = fields[1] if len(fields) > 1 else ""
                
                # Clean HTML tags (basic cleaning)
                front = clean_html(front)
                back = clean_html(back)
                
                cards.append({
                    'front': front,
                    'back': back
                })
        
        conn.close()
        
        return {
            'deck_name': deck_name,
            'deck_description': f'Imported from {os.path.basename(file_path)}',
            'cards': cards
        }

def clean_html(text):
    """Basic HTML tag removal"""
    import re
    # Remove HTML tags
    text = re.sub(r'<[^>]+>', '', text)
    # Decode common HTML entities
    text = text.replace('&nbsp;', ' ')
    text = text.replace('&lt;', '<')
    text = text.replace('&gt;', '>')
    text = text.replace('&amp;', '&')
    text = text.strip()
    return text