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
        
// GET: api/MovieTitle
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

    // Genre filter mapping - Ensure exact property name matches
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

    if (!string.IsNullOrEmpty(genre))
    {
        if (genreMap.TryGetValue(genre, out var dbColumn))
        {
            // Use direct property access for common genres to avoid reflection issues
            if (genre.Equals("Action", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.Action == 1);
            }
            else if (genre.Equals("Adventure", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.Adventure == 1);
            }
            else if (genre.Equals("Comedy", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.Comedies == 1);
            }
            else if (genre.Equals("Drama", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.Dramas == 1);
            }
            else if (genre.Equals("Horror", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.HorrorMovies == 1);
            }
            else if (genre.Equals("Thriller", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.Thrillers == 1);
            }
            else
            {
                // Use reflection for other genres
                var parameter = Expression.Parameter(typeof(MovieTitle), "m");
                var property = Expression.Property(parameter, dbColumn);
                var constant = Expression.Constant(1);
                var equality = Expression.Equal(property, constant);
                var lambda = Expression.Lambda<Func<MovieTitle, bool>>(equality, parameter);
                
                query = query.Where(lambda);
            }
        }
    }

    var totalCount = await query.CountAsync();

    var movies = await query
        .OrderBy(m => m.Title)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    // Now map into the DTO and determine genre based on the filter that was applied
    var movieDtos = movies.Select(m => new MovieTitleDto
    {
        ShowId = m.ShowId,
        Title = m.Title,
        Description = m.Description,
        ImageUrl = m.ImageUrl,
        ReleaseYear = m.ReleaseYear,
        Director = m.Director,
        Cast = m.Cast,
        Type = m.Type,
        Country = m.Country,
        Duration = m.Duration,
        // If a genre filter was applied, use that genre for display
        Genre = !string.IsNullOrEmpty(genre) ? 
            // Since we've already filtered the movies by the selected genre,
            // we know this movie has that genre, so just display the selected genre
            genre :
            // If no genre filter was applied, use the default genre determination
            GetPrimaryGenre(m)
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

// Helper method to determine the primary genre of a movie
private string GetPrimaryGenre(MovieTitle m)
{
    return m.Action == 1 ? "Action" :
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
        "Other";
}


        // GET: api/MovieTitle/genres
        [HttpGet("genres")]
        public async Task<ActionResult<IEnumerable<string>>> GetGenres()
        {
            // Query the database to get all genres that have at least one movie
            var genres = new List<string>();
            
            // Check each genre column and add it to the list if there's at least one movie with that genre
            if (await _context.MovieTitles.AnyAsync(m => m.Action == 1))
                genres.Add("Action");
                
            if (await _context.MovieTitles.AnyAsync(m => m.Adventure == 1))
                genres.Add("Adventure");
                
            if (await _context.MovieTitles.AnyAsync(m => m.AnimeSeriesInternationalTVShows == 1))
                genres.Add("Anime Series International TV Shows");
                
            if (await _context.MovieTitles.AnyAsync(m => m.BritishTVShowsDocuseriesInternationalTVShows == 1))
                genres.Add("British TV Shows Docuseries International TV Shows");
                
            if (await _context.MovieTitles.AnyAsync(m => m.Children == 1))
                genres.Add("Children");
                
            if (await _context.MovieTitles.AnyAsync(m => m.Comedies == 1))
                genres.Add("Comedy");
                
            if (await _context.MovieTitles.AnyAsync(m => m.ComediesDramasInternationalMovies == 1))
                genres.Add("Comedy Dramas International Movies");
                
            if (await _context.MovieTitles.AnyAsync(m => m.ComediesRomanticMovies == 1))
                genres.Add("Comedy Romantic Movies");
                
            if (await _context.MovieTitles.AnyAsync(m => m.CrimeTVShowsDocuseries == 1))
                genres.Add("Crime TV Shows Docuseries");
                
            if (await _context.MovieTitles.AnyAsync(m => m.Documentaries == 1))
                genres.Add("Documentaries");
                
            if (await _context.MovieTitles.AnyAsync(m => m.DocumentariesInternationalMovies == 1))
                genres.Add("Documentaries International Movies");
                
            if (await _context.MovieTitles.AnyAsync(m => m.Docuseries == 1))
                genres.Add("Docuseries");
                
            if (await _context.MovieTitles.AnyAsync(m => m.Dramas == 1))
                genres.Add("Drama");
                
            if (await _context.MovieTitles.AnyAsync(m => m.DramasInternationalMovies == 1))
                genres.Add("Drama International Movies");
                
            if (await _context.MovieTitles.AnyAsync(m => m.DramasRomanticMovies == 1))
                genres.Add("Drama Romantic Movies");
                
            if (await _context.MovieTitles.AnyAsync(m => m.FamilyMovies == 1))
                genres.Add("Family Movies");
                
            if (await _context.MovieTitles.AnyAsync(m => m.Fantasy == 1))
                genres.Add("Fantasy");
                
            if (await _context.MovieTitles.AnyAsync(m => m.HorrorMovies == 1))
                genres.Add("Horror");
                
            if (await _context.MovieTitles.AnyAsync(m => m.InternationalMoviesThrillers == 1))
                genres.Add("International Movies Thrillers");
                
            if (await _context.MovieTitles.AnyAsync(m => m.InternationalTVShowsRomanticTVShowsTVDramas == 1))
                genres.Add("International TV Shows Romantic TV Shows TV Dramas");
                
            if (await _context.MovieTitles.AnyAsync(m => m.KidsTV == 1))
                genres.Add("Kids' TV");
                
            if (await _context.MovieTitles.AnyAsync(m => m.LanguageTVShows == 1))
                genres.Add("Language TV Shows");
                
            if (await _context.MovieTitles.AnyAsync(m => m.Musicals == 1))
                genres.Add("Musicals");
                
            if (await _context.MovieTitles.AnyAsync(m => m.NatureTV == 1))
                genres.Add("Nature TV");
                
            if (await _context.MovieTitles.AnyAsync(m => m.RealityTV == 1))
                genres.Add("Reality TV");
                
            if (await _context.MovieTitles.AnyAsync(m => m.Spirituality == 1))
                genres.Add("Spirituality");
                
            if (await _context.MovieTitles.AnyAsync(m => m.TVAction == 1))
                genres.Add("TV Action");
                
            if (await _context.MovieTitles.AnyAsync(m => m.TVComedies == 1))
                genres.Add("TV Comedies");
                
            if (await _context.MovieTitles.AnyAsync(m => m.TVDramas == 1))
                genres.Add("TV Dramas");
                
            if (await _context.MovieTitles.AnyAsync(m => m.TalkShowsTVComedies == 1))
                genres.Add("Talk Shows TV Comedies");
                
            if (await _context.MovieTitles.AnyAsync(m => m.Thrillers == 1))
                genres.Add("Thriller");

            // Sort the genres alphabetically
            genres.Sort();
            
            return Ok(genres);
        }

// GET: api/MovieTitle/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MovieTitleDto>> GetMovieTitle(string id)
        {
            var movie = await _context.MovieTitles.FindAsync(id);

            if (movie == null)
            {
                return NotFound();
            }

            // Fetch ratings from the database
            var ratings = await _context.MovieRatings
                .Where(r => r.ShowId == movie.ShowId)
                .ToListAsync();

            // Calculate average rating
            double averageRating = 0;
            if (ratings.Count > 0)
            {
                averageRating = ratings.Average(r => r.Rating);
            }

            var movieDto = new MovieTitleDto
            {
                ShowId = movie.ShowId,
                Title = movie.Title,
                Description = movie.Description,
                ImageUrl = movie.ImageUrl,
                ReleaseYear = movie.ReleaseYear,
                Director = movie.Director,
                Cast = movie.Cast,
                Type = movie.Type,
                Country = movie.Country,
                Duration = movie.Duration,
                Genre = GetPrimaryGenre(movie),
                AverageRating = averageRating // 👈 NEW FIELD
            };

            return Ok(movieDto);
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
public async Task<IActionResult> PutMovieTitle(string id, MovieTitleDto dto)
{
    if (id != dto.ShowId)
    {
        return BadRequest("Mismatched movie ID.");
    }

    var movie = await _context.MovieTitles.FindAsync(id);
    if (movie == null)
    {
        return NotFound("Movie not found.");
    }

    // Update other properties
    movie.Title = dto.Title;
    movie.Type = dto.Type;
    movie.ReleaseYear = dto.ReleaseYear;
    movie.Director = dto.Director;
    movie.Description = dto.Description;
    movie.ImageUrl = dto.ImageUrl;
    movie.Cast = dto.Cast;
    movie.Duration = dto.Duration;
    movie.Country = dto.Country;

    // 🔄 Reset all genre boolean flags to 0
    movie.Action = 0;
    movie.Adventure = 0;
    movie.AnimeSeriesInternationalTVShows = 0;
    movie.BritishTVShowsDocuseriesInternationalTVShows = 0;
    movie.Children = 0;
    movie.Comedies = 0;
    movie.ComediesDramasInternationalMovies = 0;
    movie.ComediesRomanticMovies = 0;
    movie.CrimeTVShowsDocuseries = 0;
    movie.Documentaries = 0;
    movie.DocumentariesInternationalMovies = 0;
    movie.Docuseries = 0;
    movie.Dramas = 0;
    movie.DramasInternationalMovies = 0;
    movie.DramasRomanticMovies = 0;
    movie.FamilyMovies = 0;
    movie.Fantasy = 0;
    movie.HorrorMovies = 0;
    movie.InternationalMoviesThrillers = 0;
    movie.InternationalTVShowsRomanticTVShowsTVDramas = 0;
    movie.KidsTV = 0;
    movie.LanguageTVShows = 0;
    movie.Musicals = 0;
    movie.NatureTV = 0;
    movie.RealityTV = 0;
    movie.Spirituality = 0;
    movie.TVAction = 0;
    movie.TVComedies = 0;
    movie.TVDramas = 0;
    movie.TalkShowsTVComedies = 0;
    movie.Thrillers = 0;

    // ✅ Map genre from string to boolean field
    switch (dto.Genre?.Trim().ToLowerInvariant())
    {
        case "action": movie.Action = 1; break;
        case "adventure": movie.Adventure = 1; break;
        case "anime series international tv shows": movie.AnimeSeriesInternationalTVShows = 1; break;
        case "british tv shows docuseries international tv shows": movie.BritishTVShowsDocuseriesInternationalTVShows = 1; break;
        case "children": movie.Children = 1; break;
        case "comedy": movie.Comedies = 1; break;
        case "comedy dramas international movies": movie.ComediesDramasInternationalMovies = 1; break;
        case "comedy romantic movies": movie.ComediesRomanticMovies = 1; break;
        case "crime tv shows docuseries": movie.CrimeTVShowsDocuseries = 1; break;
        case "documentaries": movie.Documentaries = 1; break;
        case "documentaries international movies": movie.DocumentariesInternationalMovies = 1; break;
        case "docuseries": movie.Docuseries = 1; break;
        case "drama": movie.Dramas = 1; break;
        case "drama international movies": movie.DramasInternationalMovies = 1; break;
        case "drama romantic movies": movie.DramasRomanticMovies = 1; break;
        case "family movies": movie.FamilyMovies = 1; break;
        case "fantasy": movie.Fantasy = 1; break;
        case "horror": case "horror movies": movie.HorrorMovies = 1; break;
        case "international movies thrillers": movie.InternationalMoviesThrillers = 1; break;
        case "international tv shows romantic tv shows tv dramas": movie.InternationalTVShowsRomanticTVShowsTVDramas = 1; break;
        case "kids' tv": case "kids tv": movie.KidsTV = 1; break;
        case "language tv shows": movie.LanguageTVShows = 1; break;
        case "musicals": movie.Musicals = 1; break;
        case "nature tv": movie.NatureTV = 1; break;
        case "reality tv": movie.RealityTV = 1; break;
        case "spirituality": movie.Spirituality = 1; break;
        case "tv action": movie.TVAction = 1; break;
        case "tv comedies": movie.TVComedies = 1; break;
        case "tv dramas": movie.TVDramas = 1; break;
        case "talk shows tv comedies": movie.TalkShowsTVComedies = 1; break;
        case "thriller": case "thrillers": movie.Thrillers = 1; break;
        default: return BadRequest($"Unrecognized genre: {dto.Genre}");
    }

    try
    {
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateException ex)
    {
        return StatusCode(500, $"Database update failed: {ex.Message}");
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
