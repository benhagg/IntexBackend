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
    [Route("api/movies/{movieId}/recommendations")]
    public class MovieRecommendationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly string _connectionString;

        public MovieRecommendationController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            // Get the connection string from the DbContext
            _connectionString = _context.Database.GetConnectionString() ?? 
                                "Data Source=Movies.db";
        }

        // GET: api/movies/{movieId}/recommendations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MovieTitleDto>>> GetRecommendations(string movieId)
        {
            // Validate the movie exists
            var movie = await _context.MovieTitles.FindAsync(movieId);
            if (movie == null)
            {
                return NotFound($"Movie with ID {movieId} not found");
            }

            // Create a list to store the recommended movie IDs
            List<string> recommendedMovieIds = new List<string>();

            // Use a direct connection to query the recommender1 table
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Query the recommender1 table to get recommendations
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT * 
                        FROM recommender1 
                        WHERE [show_id] = @movieId
                    ";
                    command.Parameters.AddWithValue("@movieId", movieId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Get the column names from the schema
                            var columnNames = new List<string> { "collab1_id", "collab2_id", "collab3_id", "content1_id", "content2_id", "content3_id" };
                            
                            // Add recommendation IDs to our list
                            // The first column is show_id (0), the second is original_title (1)
                            // The recommendation IDs start at index 2
                            for (int i = 2; i < 8 && i < reader.FieldCount; i++) // Columns 2-7 are the recommendation IDs
                            {
                                try
                                {
                                    if (!reader.IsDBNull(i))
                                    {
                                        string recId = reader.GetString(i);
                                        if (!string.IsNullOrEmpty(recId) && !recommendedMovieIds.Contains(recId))
                                        {
                                            recommendedMovieIds.Add(recId);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Log the error but continue with other columns
                                    Console.WriteLine($"Error reading column {i} ({columnNames[i-2]}): {ex.Message}");
                                }
                            }
                        }
                    }
                }

                // If we don't have enough recommendations, get some based on genre
                if (recommendedMovieIds.Count < 5)
                {
                    using (var command = connection.CreateCommand())
                    {
                        // Get the primary genre of the current movie
                        string primaryGenre = GetPrimaryGenre(movie);
                        
                        // Query to get additional movies of the same genre
                        command.CommandText = @"
                            SELECT show_id 
                            FROM movies_titles 
                            WHERE show_id != @movieId 
                            AND " + GetGenreColumnName(primaryGenre) + @" = 1
                            LIMIT 10
                        ";
                        command.Parameters.AddWithValue("@movieId", movieId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync() && recommendedMovieIds.Count < 5)
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
            var recommendedMovies = new List<MovieTitleDto>();
            foreach (var recId in recommendedMovieIds)
            {
                // Query the movies_titles table to get movie details
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT show_id, title, description, release_year, director, country, imageUrl
                            FROM movies_titles 
                            WHERE show_id = @recId
                        ";
                        command.Parameters.AddWithValue("@recId", recId);

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
                                        // Determine genre from the movie record
                                        Genre = await GetGenreFromMovieId(connection, recId)
                                    };
                                    
                                    recommendedMovies.Add(movieDto);
                                }
                                catch (Exception ex)
                                {
                                    // Log the error but continue with other recommendations
                                    Console.WriteLine($"Error creating MovieTitleDto for movie {recId}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }

            return Ok(recommendedMovies);
        }

        // Helper method to get the primary genre of a movie
        private string GetPrimaryGenre(MovieTitle movie)
        {
            if (movie.Action == 1) return "Action";
            if (movie.Adventure == 1) return "Adventure";
            if (movie.AnimeSeriesInternationalTVShows == 1) return "Anime Series International TV Shows";
            if (movie.BritishTVShowsDocuseriesInternationalTVShows == 1) return "British TV Shows Docuseries International TV Shows";
            if (movie.Children == 1) return "Children";
            if (movie.Comedies == 1) return "Comedy";
            if (movie.ComediesDramasInternationalMovies == 1) return "Comedy Dramas International Movies";
            if (movie.ComediesRomanticMovies == 1) return "Comedy Romantic Movies";
            if (movie.CrimeTVShowsDocuseries == 1) return "Crime TV Shows Docuseries";
            if (movie.Documentaries == 1) return "Documentaries";
            if (movie.DocumentariesInternationalMovies == 1) return "Documentaries International Movies";
            if (movie.Docuseries == 1) return "Docuseries";
            if (movie.Dramas == 1) return "Drama";
            if (movie.DramasInternationalMovies == 1) return "Drama International Movies";
            if (movie.DramasRomanticMovies == 1) return "Drama Romantic Movies";
            if (movie.FamilyMovies == 1) return "Family Movies";
            if (movie.Fantasy == 1) return "Fantasy";
            if (movie.HorrorMovies == 1) return "Horror";
            if (movie.InternationalMoviesThrillers == 1) return "International Movies Thrillers";
            if (movie.InternationalTVShowsRomanticTVShowsTVDramas == 1) return "International TV Shows Romantic TV Shows TV Dramas";
            if (movie.KidsTV == 1) return "Kids' TV";
            if (movie.LanguageTVShows == 1) return "Language TV Shows";
            if (movie.Musicals == 1) return "Musicals";
            if (movie.NatureTV == 1) return "Nature TV";
            if (movie.RealityTV == 1) return "Reality TV";
            if (movie.Spirituality == 1) return "Spirituality";
            if (movie.TVAction == 1) return "TV Action";
            if (movie.TVComedies == 1) return "TV Comedies";
            if (movie.TVDramas == 1) return "TV Dramas";
            if (movie.TalkShowsTVComedies == 1) return "Talk Shows TV Comedies";
            if (movie.Thrillers == 1) return "Thriller";
            
            return "Other";
        }

        // Helper method to get the column name for a genre
        private string GetGenreColumnName(string genre)
        {
            switch (genre)
            {
                case "Action": return "\"Action\"";
                case "Adventure": return "\"Adventure\"";
                case "Anime Series International TV Shows": return "\"Anime Series International TV Shows\"";
                case "British TV Shows Docuseries International TV Shows": return "\"British TV Shows Docuseries International TV Shows\"";
                case "Children": return "\"Children\"";
                case "Comedy": return "\"Comedies\"";
                case "Comedy Dramas International Movies": return "\"Comedies Dramas International Movies\"";
                case "Comedy Romantic Movies": return "\"Comedies Romantic Movies\"";
                case "Crime TV Shows Docuseries": return "\"Crime TV Shows Docuseries\"";
                case "Documentaries": return "\"Documentaries\"";
                case "Documentaries International Movies": return "\"Documentaries International Movies\"";
                case "Docuseries": return "\"Docuseries\"";
                case "Drama": return "\"Dramas\"";
                case "Drama International Movies": return "\"Dramas International Movies\"";
                case "Drama Romantic Movies": return "\"Dramas Romantic Movies\"";
                case "Family Movies": return "\"Family Movies\"";
                case "Fantasy": return "\"Fantasy\"";
                case "Horror": return "\"Horror Movies\"";
                case "International Movies Thrillers": return "\"International Movies Thrillers\"";
                case "International TV Shows Romantic TV Shows TV Dramas": return "\"International TV Shows Romantic TV Shows TV Dramas\"";
                case "Kids' TV": return "\"Kids' TV\"";
                case "Language TV Shows": return "\"Language TV Shows\"";
                case "Musicals": return "\"Musicals\"";
                case "Nature TV": return "\"Nature TV\"";
                case "Reality TV": return "\"Reality TV\"";
                case "Spirituality": return "\"Spirituality\"";
                case "TV Action": return "\"TV Action\"";
                case "TV Comedies": return "\"TV Comedies\"";
                case "TV Dramas": return "\"TV Dramas\"";
                case "Talk Shows TV Comedies": return "\"Talk Shows TV Comedies\"";
                case "Thriller": return "\"Thrillers\"";
                default: return "\"Dramas\""; // Default to Drama if genre not found
            }
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