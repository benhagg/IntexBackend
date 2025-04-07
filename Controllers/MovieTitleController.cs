using System.Linq.Expressions;
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

[HttpGet]
public async Task<ActionResult<IEnumerable<MovieTitleDto>>> GetMovieTitles(
    [FromQuery] string? genre = null,
    [FromQuery] string? search = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
{
    IQueryable<MovieTitle> query = _context.MovieTitles;

    if (!string.IsNullOrEmpty(search))
    {
        query = query.Where(m => m.Title.ToLower().Contains(search.ToLower()));
    }

    // Genre filter mapping
    var genreMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Action", "Action" },
        { "Adventure", "Adventure" },
        { "Anime Series International TV Shows", "AnimeSeriesInternationalTVShows" },
        { "British TV Shows Docuseries International TV Shows", "BritishTVShowsDocuseriesInternationalTVShows" },
        { "Children", "Children" },
        { "Comedy", "Comedies" },
        { "Comedy Dramas International Movies", "ComediesDramasInternationalMovies" },
        { "Comedy Romantic Movies", "ComediesRomanticMovies" },
        { "Crime TV Shows Docuseries", "CrimeTVShowsDocuseries" },
        { "Documentaries", "Documentaries" },
        { "Documentaries International Movies", "DocumentariesInternationalMovies" },
        { "Docuseries", "Docuseries" },
        { "Drama", "Dramas" },
        { "Drama International Movies", "DramasInternationalMovies" },
        { "Drama Romantic Movies", "DramasRomanticMovies" },
        { "Family Movies", "FamilyMovies" },
        { "Fantasy", "Fantasy" },
        { "Horror", "HorrorMovies" },
        { "International Movies Thrillers", "InternationalMoviesThrillers" },
        { "International TV Shows Romantic TV Shows TV Dramas", "InternationalTVShowsRomanticTVShowsTVDramas" },
        { "Kids' TV", "KidsTV" },
        { "Language TV Shows", "LanguageTVShows" },
        { "Musicals", "Musicals" },
        { "Nature TV", "NatureTV" },
        { "Reality TV", "RealityTV" },
        { "Spirituality", "Spirituality" },
        { "TV Action", "TVAction" },
        { "TV Comedies", "TVComedies" },
        { "TV Dramas", "TVDramas" },
        { "Talk Shows TV Comedies", "TalkShowsTVComedies" },
        { "Thriller", "Thrillers" }
    };

    if (!string.IsNullOrEmpty(genre) && genreMap.TryGetValue(genre, out var dbColumn))
    {
        query = query.Where(BuildGenreExpression(dbColumn));

        Expression<Func<MovieTitle, bool>> BuildGenreExpression(string value)
        {
            throw new NotImplementedException();
        }
    }

    var totalCount = await query.CountAsync();

    var movies = await query
        .OrderBy(m => m.Title)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    // Now map into the DTO and determine genre
    var movieDtos = movies.Select(m => new MovieTitleDto
    {
        ShowId = m.ShowId,
        Title = m.Title,
        Description = m.Description,
        ImageUrl = m.ImageUrl,
        ReleaseYear = m.ReleaseYear,
        Director = m.Director,
        Genre =
            m.Action == 1 ? "Action" :
            m.Adventure == 1 ? "Adventure" :
            m.AnimeSeriesInternationalTVShows == 1 ? "Anime Series International TV Shows" :
            m.BritishTVShowsDocuseriesInternationalTVShows == 1 ? "British TV Shows Docuseries International TV Shows" :
            m.Children == 1 ? "Children" :
            m.Comedies == 1 ? "Comedy" :
            m.ComediesDramasInternationalMovies == 1 ? "Comedy Dramas International Movies" :
            m.ComediesRomanticMovies == 1 ? "Comedy Romantic Movies" :
            m.CrimeTVShowsDocuseries == 1 ? "Crime TV Shows Docuseries" :
            m.Documentaries == 1 ? "Documentaries" :
            m.DocumentariesInternationalMovies == 1 ? "Documentaries International Movies" :
            m.Docuseries == 1 ? "Docuseries" :
            m.Dramas == 1 ? "Drama" :
            m.DramasInternationalMovies == 1 ? "Drama International Movies" :
            m.DramasRomanticMovies == 1 ? "Drama Romantic Movies" :
            m.FamilyMovies == 1 ? "Family Movies" :
            m.Fantasy == 1 ? "Fantasy" :
            m.HorrorMovies == 1 ? "Horror" :
            m.InternationalMoviesThrillers == 1 ? "International Movies Thrillers" :
            m.InternationalTVShowsRomanticTVShowsTVDramas == 1 ? "International TV Shows Romantic TV Shows TV Dramas" :
            m.KidsTV == 1 ? "Kids' TV" :
            m.LanguageTVShows == 1 ? "Language TV Shows" :
            m.Musicals == 1 ? "Musicals" :
            m.NatureTV == 1 ? "Nature TV" :
            m.RealityTV == 1 ? "Reality TV" :
            m.Spirituality == 1 ? "Spirituality" :
            m.TVAction == 1 ? "TV Action" :
            m.TVComedies == 1 ? "TV Comedies" :
            m.TVDramas == 1 ? "TV Dramas" :
            m.TalkShowsTVComedies == 1 ? "Talk Shows TV Comedies" :
            m.Thrillers == 1 ? "Thriller" :
            "Other"
    }).ToList();

    return Ok(new
    {
        movies = movieDtos,
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
