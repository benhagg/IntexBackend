-- Update imageUrl column with values based on movie title
-- (Column already exists, so we're just updating values)
UPDATE movies_titles
SET imageUrl = '/MoviePosters/' || title || '.jpg'
WHERE imageUrl IS NULL;
