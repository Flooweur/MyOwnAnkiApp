import os
from app import create_app, db
import time

app = create_app()

if __name__ == '__main__':
    # Wait for database to be ready
    max_retries = 30
    retry_count = 0
    while retry_count < max_retries:
        try:
            with app.app_context():
                db.create_all()
            print("Database connected and tables created!")
            break
        except Exception as e:
            retry_count += 1
            print(f"Waiting for database... ({retry_count}/{max_retries})")
            time.sleep(1)
    
    app.run(host='0.0.0.0', port=5000, debug=True)