
using System.Text.Json.Serialization;

namespace OnlineBookTracker.Models;

public class BookSearchResponse
{
    [JsonPropertyName("numFound")]
    public int TotalFound { get; set; }

    [JsonPropertyName("docs")]
    public List<BookSearchResult> Books { get; set; } = new();
}
