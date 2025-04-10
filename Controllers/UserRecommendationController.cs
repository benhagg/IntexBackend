using IntexBackend.Data;
using IntexBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntexBackend.Controllers
{
    [ApiController]
    [Route("api/movies")]
    public class UserRecommendationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly string _connectionString;

        public UserRecommendationController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            // Get the connection string from the DbContext
            _connectionString = _context.Database.GetConnectionString() ?? 
                                "Data Source=Movies.db";
        }

        // GET: api/movies/user-recommendations/{userId}
        [HttpGet("user-recommendations/{userId}")]
        public async Task<ActionResult<object>> GetUserRecommendations(string userId, [FromQuery] bool kidsMode = false)
        {
            // For ASP.NET Identity users, we need to map the GUID userId to an integer
            // that can be used with our recommendation tables
            // For simplicity, we'll use a deterministic mapping based on the first few characters of the GUID
            int mappedUserId;
            
            // Try to parse the first 8 characters of the GUID as a hex number
            if (userId.Length >= 8 && int.TryParse(userId.Substring(0, 8), System.Globalization.NumberStyles.HexNumber, null, out int parsedId))
            {
                // Use modulo to ensure the ID is within the range of our recommendation data (1-201)
                mappedUserId = (parsedId % 200) + 1;
            }
            else
            {
                // Fallback to a default user ID if parsing fails
                mappedUserId = 1;
            }
            
            Console.WriteLine($"Mapped user ID {userId} to recommendation user ID {mappedUserId}");

            // Create response object with three recommendation categories
            var response = new
            {
                locationRecommendations = await GetLocationRecommendations(mappedUserId, kidsMode),
                basicRecommendations = await GetBasicRecommendations(mappedUserId, kidsMode),
                streamingRecommendations = await GetStreamingRecommendations(mappedUserId, kidsMode)
            };

            return Ok(response);
        }

        // Helper method to get location-based recommendations
        private async Task<List<MovieTitleDto>> GetLocationRecommendations(int userId, bool kidsMode = false)
        {
            var recommendedMovieIds = new List<string>();

            // Query the location_recommended_shows_ids table to get recommendations
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Recommended_Item_1, Recommended_Item_2, Recommended_Item_3, Recommended_Item_4, Recommended_Item_5
                        FROM location_recommended_shows_ids 
                        WHERE User = @userId
                    ";
                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Add recommendation IDs to our list
                            for (int i = 0; i < 5; i++)
                            {
                                if (!reader.IsDBNull(i))
                                {
                                    string recId = reader.GetString(i);
                                    if (!string.IsNullOrEmpty(recId))
                                    {
                                        recommendedMovieIds.Add(recId);
                                    }
                                }
                            }
                        }
                    }
                }

                // If we don't have enough recommendations, get some random movies
                if (recommendedMovieIds.Count < 5)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT show_id 
                            FROM movies_titles 
                            ORDER BY RANDOM() 
                            LIMIT @limit
                        ";
                        command.Parameters.AddWithValue("@limit", 5 - recommendedMovieIds.Count);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string recId = reader.GetString(0);
                                if (!recommendedMovieIds.Contains(recId))
                                {
                                    recommendedMovieIds.Add(recId);
                                }
                            }
                        }
                    }
                }
            }

            // Limit to 5 recommendations
            recommendedMovieIds = recommendedMovieIds.Take(5).ToList();

            // Get the full movie details for each recommendation
            return await GetMovieDetailsForIds(recommendedMovieIds, kidsMode);
        }

        // Helper method to get basic recommendations
        private async Task<List<MovieTitleDto>> GetBasicRecommendations(int userId, bool kidsMode = false)
        {
            var recommendedMovieIds = new List<string>();

            // Query the basic_recommended_shows_ids table to get recommendations
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Recommended_Item_1, Recommended_Item_2, Recommended_Item_3, Recommended_Item_4, Recommended_Item_5
                        FROM basic_recommended_shows_ids 
                        WHERE User = @userId
                    ";
                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Add recommendation IDs to our list
                            for (int i = 0; i < 5; i++)
                            {
                                if (!reader.IsDBNull(i))
                                {
                                    string recId = reader.GetString(i);
                                    if (!string.IsNullOrEmpty(recId))
                                    {
                                        recommendedMovieIds.Add(recId);
                                    }
                                }
                            }
                        }
                    }
                }

                // If we don't have enough recommendations, get some random movies
                if (recommendedMovieIds.Count < 5)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT show_id 
                            FROM movies_titles 
                            ORDER BY RANDOM() 
                            LIMIT @limit
                        ";
                        command.Parameters.AddWithValue("@limit", 5 - recommendedMovieIds.Count);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string recId = reader.GetString(0);
                                if (!recommendedMovieIds.Contains(recId))
                                {
                                    recommendedMovieIds.Add(recId);
                                }
                            }
                        }
                    }
                }
            }

            // Limit to 5 recommendations
            recommendedMovieIds = recommendedMovieIds.Take(5).ToList();

            // Get the full movie details for each recommendation
            return await GetMovieDetailsForIds(recommendedMovieIds, kidsMode);
        }

        // Helper method to get streaming recommendations
        private async Task<List<MovieTitleDto>> GetStreamingRecommendations(int userId, bool kidsMode = false)
        {
            var recommendedMovieIds = new List<string>();

            // Query the streaming_recommended_shows_ids table to get recommendations
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Recommended_Item_1, Recommended_Item_2, Recommended_Item_3, Recommended_Item_4, Recommended_Item_5
                        FROM streaming_recommended_shows_ids 
                        WHERE User = @userId
                    ";
                    command.Parameters.AddWithValue("@userId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Add recommendation IDs to our list
                            for (int i = 0; i < 5; i++)
                            {
                                if (!reader.IsDBNull(i))
                                {
                                    string recId = reader.GetString(i);
                                    if (!string.IsNullOrEmpty(recId))
                                    {
                                        recommendedMovieIds.Add(recId);
                                    }
                                }
                            }
                        }
                    }
                }

                // If we don't have enough recommendations, get some random movies
                if (recommendedMovieIds.Count < 5)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT show_id 
                            FROM movies_titles 
                            ORDER BY RANDOM() 
                            LIMIT @limit
                        ";
                        command.Parameters.AddWithValue("@limit", 5 - recommendedMovieIds.Count);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string recId = reader.GetString(0);
                                if (!recommendedMovieIds.Contains(recId))
                                {
                                    recommendedMovieIds.Add(recId);
                                }
                            }
                        }
                    }
                }
            }

            // Limit to 5 recommendations
            recommendedMovieIds = recommendedMovieIds.Take(5).ToList();

            // Get the full movie details for each recommendation
            return await GetMovieDetailsForIds(recommendedMovieIds, kidsMode);
        }

        // Helper method to get movie details for a list of movie IDs
        private async Task<List<MovieTitleDto>> GetMovieDetailsForIds(List<string> movieIds, bool kidsMode = false)
        {
            var movies = new List<MovieTitleDto>();

            foreach (var movieId in movieIds)
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        // If Kids Mode is enabled, filter out non-kid-friendly movies
                        if (kidsMode)
                        {
                            command.CommandText = @"
                                SELECT show_id, title, description, release_year, director, country, imageUrl, rating
                                FROM movies_titles 
                                WHERE show_id = @movieId
                                AND (rating IS NULL OR rating NOT IN ('PG-13', 'TV-14', 'TV-MA', 'R', 'NR', 'TV-Y7-FV', 'UR'))
                            ";
                        }
                        else
                        {
                            command.CommandText = @"
                                SELECT show_id, title, description, release_year, director, country, imageUrl, rating
                                FROM movies_titles 
                                WHERE show_id = @movieId
                            ";
                        }
                        command.Parameters.AddWithValue("@movieId", movieId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                try
                                {
                                    var movieDto = new MovieTitleDto
                                    {
                                        ShowId = reader.GetString(0),
                                        Title = !reader.IsDBNull(1) ? reader.GetString(1) : "Unknown Title",
                                        Description = !reader.IsDBNull(2) ? reader.GetString(2) : "No description available",
                                        ReleaseYear = !reader.IsDBNull(3) ? reader.GetInt32(3) : 0,
                                        Director = !reader.IsDBNull(4) ? reader.GetString(4) : "Unknown Director",
                                        Country = !reader.IsDBNull(5) ? reader.GetString(5) : null,
                                        ImageUrl = !reader.IsDBNull(6) ? reader.GetString(6) : null,
                                        Rating = !reader.IsDBNull(7) ? reader.GetString(7) : null,
                                        // Determine genre from the movie record
                                        Genre = await GetGenreFromMovieId(connection, movieId)
                                    };
                                    
                                    movies.Add(movieDto);
                                }
                                catch (Exception ex)
                                {
                                    // Log the error but continue with other recommendations
                                    Console.WriteLine($"Error creating MovieTitleDto for movie {movieId}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }

            return movies;
        }

        // Helper method to get the genre from a movie ID
        private async Task<string> GetGenreFromMovieId(SqliteConnection connection, string movieId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT 
                        ""Action"", ""Adventure"", ""Anime Series International TV Shows"", 
                        ""British TV Shows Docuseries International TV Shows"", ""Children"", 
                        ""Comedies"", ""Comedies Dramas International Movies"", ""Comedies Romantic Movies"", 
                        ""Crime TV Shows Docuseries"", ""Documentaries"", ""Documentaries International Movies"", 
                        ""Docuseries"", ""Dramas"", ""Dramas International Movies"", ""Dramas Romantic Movies"", 
                        ""Family Movies"", ""Fantasy"", ""Horror Movies"", ""International Movies Thrillers"", 
                        ""International TV Shows Romantic TV Shows TV Dramas"", ""Kids' TV"", 
                        ""Language TV Shows"", ""Musicals"", ""Nature TV"", ""Reality TV"", ""Spirituality"", 
                        ""TV Action"", ""TV Comedies"", ""TV Dramas"", ""Talk Shows TV Comedies"", ""Thrillers""
                    FROM movies_titles 
                    WHERE show_id = @movieId
                ";
                command.Parameters.AddWithValue("@movieId", movieId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        // Check each genre column and return the first one that is 1
                        if (reader.GetInt32(0) == 1) return "Action";
                        if (reader.GetInt32(1) == 1) return "Adventure";
                        if (reader.GetInt32(2) == 1) return "Anime Series International TV Shows";
                        if (reader.GetInt32(3) == 1) return "British TV Shows Docuseries International TV Shows";
                        if (reader.GetInt32(4) == 1) return "Children";
                        if (reader.GetInt32(5) == 1) return "Comedy";
                        if (reader.GetInt32(6) == 1) return "Comedy Dramas International Movies";
                        if (reader.GetInt32(7) == 1) return "Comedy Romantic Movies";
                        if (reader.GetInt32(8) == 1) return "Crime TV Shows Docuseries";
                        if (reader.GetInt32(9) == 1) return "Documentaries";
                        if (reader.GetInt32(10) == 1) return "Documentaries International Movies";
                        if (reader.GetInt32(11) == 1) return "Docuseries";
                        if (reader.GetInt32(12) == 1) return "Drama";
                        if (reader.GetInt32(13) == 1) return "Drama International Movies";
                        if (reader.GetInt32(14) == 1) return "Drama Romantic Movies";
                        if (reader.GetInt32(15) == 1) return "Family Movies";
                        if (reader.GetInt32(16) == 1) return "Fantasy";
                        if (reader.GetInt32(17) == 1) return "Horror";
                        if (reader.GetInt32(18) == 1) return "International Movies Thrillers";
                        if (reader.GetInt32(19) == 1) return "International TV Shows Romantic TV Shows TV Dramas";
                        if (reader.GetInt32(20) == 1) return "Kids' TV";
                        if (reader.GetInt32(21) == 1) return "Language TV Shows";
                        if (reader.GetInt32(22) == 1) return "Musicals";
                        if (reader.GetInt32(23) == 1) return "Nature TV";
                        if (reader.GetInt32(24) == 1) return "Reality TV";
                        if (reader.GetInt32(25) == 1) return "Spirituality";
                        if (reader.GetInt32(26) == 1) return "TV Action";
                        if (reader.GetInt32(27) == 1) return "TV Comedies";
                        if (reader.GetInt32(28) == 1) return "TV Dramas";
                        if (reader.GetInt32(29) == 1) return "Talk Shows TV Comedies";
                        if (reader.GetInt32(30) == 1) return "Thriller";
                    }
                }
            }

            return "Other";
        }
    }
}
