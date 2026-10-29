using System.ComponentModel.DataAnnotations;

namespace OnlineBookTracker.Models;

public class Review
{
    public int ReviewId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string OpenLibraryKey { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string BookTitle { get; set; } = string.Empty;

    [Range(1, 10)]
    public int Rating { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 5)]
    public string ReviewText { get; set; } = string.Empty;

    public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    public User? User { get; set; }
}