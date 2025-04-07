using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IntexBackend.Models
{
    [Table("movies_ratings")]
    public class MovieRating
    {
        [Key]
        [Column("rating_id")]
        public int RatingId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("show_id")]
        public string? ShowId { get; set; }

        [Column("rating")]
        public int Rating { get; set; }

        [Column("review")]
        public string? Review { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for movie title
        [ForeignKey("ShowId")]
        public MovieTitle? MovieTitle { get; set; }

        // Navigation property for user
        [ForeignKey("UserId")]
        public MovieUser? User { get; set; }
    }
}
