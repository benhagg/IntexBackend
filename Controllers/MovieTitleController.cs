using IntexBackend.Data;
using IntexBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntexBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MovieTitleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MovieTitleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/MovieTitle
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MovieTitle>>> GetMovieTitles(
            [FromQuery] string? genre = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            IQueryable<MovieTitle> query = _context.MovieTitles;

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m => m.Title.ToLower().Contains(search.ToLower()));
            }

            // Apply genre filter if provided
            if (!string.IsNullOrEmpty(genre))
            {
                // Handle genre filtering based on the genre columns
                // This is a simplified approach - you may need to adjust based on your specific needs
                switch (genre.ToLower())
                {
                    case "action":
                        query = query.Where(m => m.Action == 1);
                        break;
                    case "adventure":
                        query = query.Where(m => m.Adventure == 1);
                        break;
                    case "comedy":
                        query = query.Where(m => m.Comedies == 1);
                        break;
                    case "drama":
                        query = query.Where(m => m.Dramas == 1);
                        break;
                    case "horror":
                        query = query.Where(m => m.HorrorMovies == 1);
                        break;
                    case "thriller":
                        query = query.Where(m => m.Thrillers == 1);
                        break;
                    // Add more cases as needed
                }
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var movies = await query
                .OrderBy(m => m.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Return with pagination metadata
            return Ok(new
            {
                movies,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                currentPage = page,
                pageSize
            });
        }

        // GET: api/MovieTitle/genres
        [HttpGet("genres")]
        public async Task<ActionResult<IEnumerable<string>>> GetGenres()
        {
            // Return a list of available genres
            // This is a simplified approach - you may need to adjust based on your specific needs
            var genres = new List<string>
            {
                "Action",
                "Adventure",
                "Comedy",
                "Drama",
                "Horror",
                "Thriller"
                // Add more genres as needed
            };

            return Ok(genres);
        }

        // GET: api/MovieTitle/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MovieTitle>> GetMovieTitle(string id)
        {
            var movieTitle = await _context.MovieTitles.FindAsync(id);

            if (movieTitle == null)
            {
                return NotFound();
            }

            return movieTitle;
        }

        // POST: api/MovieTitle
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MovieTitle>> PostMovieTitle(MovieTitle movieTitle)
        {
            _context.MovieTitles.Add(movieTitle);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMovieTitle), new { id = movieTitle.ShowId }, movieTitle);
        }

        // PUT: api/MovieTitle/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutMovieTitle(string id, MovieTitle movieTitle)
        {
            if (id != movieTitle.ShowId)
            {
                return BadRequest();
            }

            _context.Entry(movieTitle).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MovieTitleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/MovieTitle/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMovieTitle(string id)
        {
            var movieTitle = await _context.MovieTitles.FindAsync(id);
            if (movieTitle == null)
            {
                return NotFound();
            }

            _context.MovieTitles.Remove(movieTitle);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MovieTitleExists(string id)
        {
            return _context.MovieTitles.Any(e => e.ShowId == id);
        }
    }
}
