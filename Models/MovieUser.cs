using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntexBackend.Models
{
    [Table("movies_users")]
    public class MovieUser
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("age")]
        public int Age { get; set; }

        [Column("gender")]
        public string? Gender { get; set; }

        [Column("Netflix")]
        public int Netflix { get; set; }

        [Column("Amazon Prime")]
        public int AmazonPrime { get; set; }

        [Column("Disney+")]
        public int DisneyPlus { get; set; }

        [Column("Paramount+")]
        public int ParamountPlus { get; set; }

        [Column("Max")]
        public int Max { get; set; }

        [Column("Hulu")]
        public int Hulu { get; set; }

        [Column("Apple TV+")]
        public int AppleTVPlus { get; set; }

        [Column("Peacock")]
        public int Peacock { get; set; }

        [Column("city")]
        public string? City { get; set; }

        [Column("state")]
        public string? State { get; set; }

        [Column("zip")]
        public int Zip { get; set; }

        // Navigation property for ratings
        public ICollection<MovieRating>? MovieRatings { get; set; }
    }
}
