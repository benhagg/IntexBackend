using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IntexBackend.Models
{
    [Table("movies_ratings")]
    [PrimaryKey(nameof(UserId), nameof(ShowId))]
    public class MovieRating
    {
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("show_id")]
        public string? ShowId { get; set; }

        [Column("rating")]
        public int Rating { get; set; }

        // Navigation property for movie title
        [ForeignKey("ShowId")]
        public MovieTitle? MovieTitle { get; set; }

        // Navigation property for user
        [ForeignKey("UserId")]
        public MovieUser? User { get; set; }
    }
}
