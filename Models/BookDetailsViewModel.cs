using System.Text.RegularExpressions;

namespace OnlineBookTracker.Models;

public class BookDetailsViewModel
{
    public string OpenLibraryKey { get; set; } = string.Empty;

    public string Title { get; set; } = "Untitled";

    public string Description { get; set; } =
        "Description unavailable.";

    public List<string> Authors { get; set; } = new();

    public int? CoverId { get; set; }

    public string? PublicationDate { get; set; }

    public List<string> Isbns { get; set; } = new();

    public string? CoverUrl =>
        CoverId.HasValue
            ? $"https://covers.openlibrary.org/b/id/{CoverId}-L.jpg"
            : null;

    public string? PublicationYear
    {
        get
        {
            var match = Regex.Match(
                PublicationDate ?? string.Empty,
                @"\b\d{4}\b");

            return match.Success ? match.Value : null;
        }
    }

    public string OpenLibraryUrl =>
        $"https://openlibrary.org/works/{OpenLibraryKey}";
}