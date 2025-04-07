using IntexBackend.Data;
using IntexBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            var ratings = await _context.MovieRatings
                .Where(r => r.ShowId == showId)
                .ToListAsync();

            return Ok(ratings);
        }

        // GET: api/MovieRating/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<MovieRating>>> GetRatingsByUser(int userId)
        {
            var ratings = await _context.MovieRatings
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return Ok(ratings);
        }

        // POST: api/MovieRating
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<MovieRating>> PostRating(MovieRatingDto ratingDto)
        {
            // Check if the movie exists
            var movie = await _context.MovieTitles.FindAsync(ratingDto.ShowId);
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

            // Check if the user has already rated this movie
            var existingRating = await _context.MovieRatings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ShowId == ratingDto.ShowId);

            if (existingRating != null)
            {
                // Update existing rating
                existingRating.Rating = ratingDto.Rating;
                _context.Entry(existingRating).State = EntityState.Modified;
            }
            else
            {
                // Create new rating
                var newRating = new MovieRating
                {
                    UserId = userId,
                    ShowId = ratingDto.ShowId,
                    Rating = ratingDto.Rating
                };
                _context.MovieRatings.Add(newRating);
            }

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

            var rating = await _context.MovieRatings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ShowId == showId);

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

            if (currentUserId != userId && !User.IsInRole("Admin"))
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
        public string ShowId { get; set; }
        public int Rating { get; set; }
    }
}
