
using System.ComponentModel.DataAnnotations;

namespace OnlineBookTracker.Models;

public class ReviewFormViewModel
{
    [Required]
    public string OpenLibraryKey { get; set; } = string.Empty;

    public string BookTitle { get; set; } = string.Empty;

    [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10.")]
    public int Rating { get; set; } = 5;

    [Required]
    [StringLength(2000, MinimumLength = 5,
        ErrorMessage = "Review must contain between 5 and 2000 characters.")]
    [Display(Name = "Your Review")]
    public string ReviewText { get; set; } = string.Empty;
}
