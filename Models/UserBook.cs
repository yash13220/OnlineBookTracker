using System.ComponentModel.DataAnnotations;

namespace OnlineBookTracker.Models;

public class UserBook
{
    public int UserBookId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string OpenLibraryKey { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string BookTitle { get; set; } = string.Empty;

    [StringLength(500)]
    public string AuthorName { get; set; } = string.Empty;

    public int? CoverId { get; set; }

    [Required]
    [RegularExpression("^(Want to Read|Reading|Completed)$")]
    public string ReadingStatus { get; set; } = "Want to Read";

    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    public DateTime? DateCompleted { get; set; }

    public User? User { get; set; }
}