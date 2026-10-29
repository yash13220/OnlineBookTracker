
namespace OnlineBookTracker.Models;

public class ReviewListViewModel
{
    public string OpenLibraryKey { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public List<Review> Reviews { get; set; } = new();
}
