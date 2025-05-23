using IntexBackend.Data;
using IntexBackend.Models;
using IntexBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace IntexBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MovieRatingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MovieRatingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/MovieRating/movie/{showId}
        [HttpGet("movie/{showId}")]
        public async Task<ActionResult<IEnumerable<MovieRating>>> GetRatingsByMovie(string showId)
        {
            // Get all ratings for this movie
            var ratings = await _context.MovieRatings
                .Where(r => r.ShowId == showId)
                .OrderByDescending(r => r.CreatedAt) // Sort by newest first
                .ToListAsync();

            // Log the ratings for debugging
            Console.WriteLine($"Found {ratings.Count} ratings for movie {showId}");
            foreach (var r in ratings)
            {
                Console.WriteLine($"Rating ID: {r.RatingId}, User ID: {r.UserId}, Rating: {r.Rating}, Review: {r.Review}");
            }

            // Create DTOs with user information
            var ratingDtos = new List<object>();
            foreach (var r in ratings)
            {
                // Get user information separately
                var user = await _context.MovieUsers.FindAsync(r.UserId);
                
                // HTML encode the review text to prevent XSS attacks when displayed in the browser
                string encodedReview = null;
                if (!string.IsNullOrEmpty(r.Review))
                {
                    encodedReview = SecurityUtils.HtmlEncode(r.Review);
                }

                // Create a DTO with all the necessary information
                ratingDtos.Add(new
                {
                    r.RatingId,
                    r.UserId,
                    r.ShowId,
                    r.Rating,
                    Review = encodedReview,
                    r.CreatedAt,
                    UserName = SecurityUtils.HtmlEncode(user?.Name ?? "Anonymous")
                });
            }

            // Log the DTOs for debugging
            Console.WriteLine($"Returning {ratingDtos.Count} rating DTOs");

            return Ok(ratingDtos);
        }

        // GET: api/MovieRating/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MovieRating>>> GetRatingsByUser(int userId)
        {
            var ratings = await _context.MovieRatings
                .Where(r => r.UserId == userId)
                .ToListAsync();

            // Create DTOs with encoded review text
            var ratingDtos = ratings.Select(r => new
            {
                r.RatingId,
                r.UserId,
                r.ShowId,
                r.Rating,
                Review = !string.IsNullOrEmpty(r.Review) ? SecurityUtils.HtmlEncode(r.Review) : null,
                r.CreatedAt
            }).ToList();

            return Ok(ratingDtos);
        }

        // POST: api/MovieRating
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<MovieRating>> PostRating([FromBody] MovieRatingDto ratingDto)
        {
        // Log the received data
        Console.WriteLine($"Received rating data: showId={ratingDto?.ShowId}, rating={ratingDto?.Rating}, review={ratingDto?.Review}");
        
        // Check if the movie exists
        var movie = await _context.MovieTitles.FindAsync(ratingDto?.ShowId);
            if (movie == null)
            {
                return NotFound("Movie not found");
            }

            // Get the current user ID from the JWT token
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString))
            {
                return BadRequest("User ID not found");
            }

            // Parse the user ID to an integer
            if (!int.TryParse(userIdString, out int userId))
            {
                // If the user ID is not an integer, use a hash code of the string as the user ID
                userId = userIdString.GetHashCode();
            }

            // Sanitize the review text to prevent XSS attacks
            string sanitizedReview = null;
            if (!string.IsNullOrEmpty(ratingDto.Review))
            {
                sanitizedReview = SecurityUtils.SanitizeInput(ratingDto.Review);
                Console.WriteLine($"Original review: {ratingDto.Review}");
                Console.WriteLine($"Sanitized review: {sanitizedReview}");
            }

            // Always create a new rating (allowing multiple reviews per user per movie)
            var newRating = new MovieRating
            {
                UserId = userId,
                ShowId = ratingDto.ShowId,
                Rating = ratingDto.Rating,
                Review = sanitizedReview,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.MovieRatings.Add(newRating);
            await _context.SaveChangesAsync();

            return Ok("Rating saved successfully");
        }

        // DELETE: api/MovieRating/{userId}/{showId}
        [HttpDelete("{userId}/{showId}")]
        [Authorize]
        public async Task<IActionResult> DeleteRating(string userIdStr, string showId)
        {
            // Parse the user ID to an integer
            if (!int.TryParse(userIdStr, out int userId))
            {
                // If the user ID is not an integer, use a hash code of the string as the user ID
                userId = userIdStr.GetHashCode();
            }

            // Find all ratings by this user for this movie
            var ratings = await _context.MovieRatings
                .Where(r => r.UserId == userId && r.ShowId == showId)
                .ToListAsync();

            if (ratings.Count == 0)
            {
                return NotFound();
            }

            // Check if the current user is authorized to delete these ratings
            var currentUserIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString))
            {
                return BadRequest("User ID not found");
            }

            // Parse the user ID to an integer
            if (!int.TryParse(currentUserIdString, out int currentUserId))
            {
                // If the user ID is not an integer, use a hash code of the string as the user ID
                currentUserId = currentUserIdString.GetHashCode();
            }

            if (currentUserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Remove all ratings by this user for this movie
            _context.MovieRatings.RemoveRange(ratings);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/MovieRating/single/{ratingId}
        [HttpDelete("single/{ratingId}")]
        [Authorize]
        public async Task<IActionResult> DeleteSingleRating(int ratingId)
        {
            var rating = await _context.MovieRatings.FindAsync(ratingId);

            if (rating == null)
            {
                return NotFound();
            }

            // Check if the current user is authorized to delete this rating
            var currentUserIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString))
            {
                return BadRequest("User ID not found");
            }

            // Parse the user ID to an integer
            if (!int.TryParse(currentUserIdString, out int currentUserId))
            {
                // If the user ID is not an integer, use a hash code of the string as the user ID
                currentUserId = currentUserIdString.GetHashCode();
            }

            if (currentUserId != rating.UserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            _context.MovieRatings.Remove(rating);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class MovieRatingDto
    {
        [JsonPropertyName("showId")]
        public string? ShowId { get; set; }
        
        [JsonPropertyName("rating")]
        public int Rating { get; set; }
        
        [JsonPropertyName("review")]
        public string? Review { get; set; }
    }
}
