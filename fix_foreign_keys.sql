-- Disable foreign key constraints temporarily
PRAGMA foreign_keys = OFF;

-- Create a new table with the desired schema but without foreign key constraints
CREATE TABLE movies_ratings_new (
    rating_id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    show_id TEXT,
    rating INTEGER NOT NULL,
    review TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Copy data from the old table to the new table
INSERT INTO movies_ratings_new (user_id, show_id, rating, review)
SELECT user_id, show_id, rating, review FROM movies_ratings;

-- Drop the old table
DROP TABLE movies_ratings;

-- Rename the new table to the original name
ALTER TABLE movies_ratings_new RENAME TO movies_ratings;

-- Create indexes
CREATE INDEX idx_movies_ratings_user_id ON movies_ratings (user_id);
CREATE INDEX idx_movies_ratings_show_id ON movies_ratings (show_id);

-- Re-enable foreign key constraints
PRAGMA foreign_keys = ON;
