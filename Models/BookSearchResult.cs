
using System.Text.Json.Serialization;

namespace OnlineBookTracker.Models;

public class BookSearchResult
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("author_name")]
    public List<string>? AuthorNames { get; set; }

    [JsonPropertyName("first_publish_year")]
    public int? FirstPublishYear { get; set; }

    [JsonPropertyName("isbn")]
    public List<string>? Isbns { get; set; }

    [JsonPropertyName("cover_i")]
    public int? CoverId { get; set; }

    public string AuthorDisplay =>
        AuthorNames is { Count: > 0 }
            ? string.Join(", ", AuthorNames)
            : "Unknown author";

    public string? FirstIsbn =>
        Isbns?.FirstOrDefault();

    public string? CoverUrl =>
        CoverId.HasValue
            ? $"https://covers.openlibrary.org/b/id/{CoverId}-M.jpg"
            : null;
}
