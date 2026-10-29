using System.Net.Http.Json;
using System.Text.Json;
using OnlineBookTracker.Models;

namespace OnlineBookTracker.Services;

public class OpenLibraryService
{
    private readonly HttpClient _httpClient;

    public OpenLibraryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BookSearchResponse> SearchBooksAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new BookSearchResponse();
        }

        string url =
            $"search.json?q={Uri.EscapeDataString(query)}&limit=20";

        return await _httpClient.GetFromJsonAsync<BookSearchResponse>(
            url,
            cancellationToken) ?? new BookSearchResponse();
    }

    public async Task<JsonDocument?> GetWorkAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id) ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                id, @"^OL\d+W$"))
        {
            return null;
        }

        using var response = await _httpClient.GetAsync(
            $"works/{id}.json",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);
    }

    public async Task<BookDetailsViewModel?> GetBookDetailsAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        using var work = await GetWorkAsync(id, cancellationToken);

        if (work == null)
        {
            return null;
        }

        var root = work.RootElement;

        var book = new BookDetailsViewModel
        {
            OpenLibraryKey = id,
            Title = GetText(root, "title") ?? "Untitled",
            Description = GetDescription(root),
            PublicationDate = GetText(root, "first_publish_date"),
            CoverId = GetFirstCoverId(root)
        };

        var authorKeys = GetAuthorKeys(root);

        var authorTasks = authorKeys
            .Take(4)
            .Select(key => GetAuthorNameAsync(key, cancellationToken));

        var authorNames = await Task.WhenAll(authorTasks);

        book.Authors = authorNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct()
            .ToList();

        await AddEditionDetailsAsync(book, cancellationToken);

        return book;
    }

    private async Task AddEditionDetailsAsync(
        BookDetailsViewModel book,
        CancellationToken cancellationToken)
    {
        using var editions = await GetOptionalJsonAsync(
            $"works/{book.OpenLibraryKey}/editions.json?limit=20",
            cancellationToken);

        if (editions == null ||
            !editions.RootElement.TryGetProperty("entries", out var entries) ||
            entries.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var edition in entries.EnumerateArray())
        {
            if (string.IsNullOrWhiteSpace(book.PublicationDate))
            {
                book.PublicationDate = GetText(edition, "publish_date");
            }

            if (book.CoverId == null)
            {
                book.CoverId = GetFirstCoverId(edition);
            }

            var isbn13 = GetStringArray(edition, "isbn_13");
            var isbn10 = GetStringArray(edition, "isbn_10");

            if (book.Isbns.Count == 0)
            {
                book.Isbns = isbn13
                    .Concat(isbn10)
                    .Distinct()
                    .Take(5)
                    .ToList();
            }

            if (book.CoverId != null && book.Isbns.Count > 0)
            {
                break;
            }
        }
    }

    private async Task<string?> GetAuthorNameAsync(
        string authorKey,
        CancellationToken cancellationToken)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(
            authorKey, @"^OL\d+A$"))
        {
            return null;
        }

        using var author = await GetOptionalJsonAsync(
            $"authors/{authorKey}.json",
            cancellationToken);

        if (author == null)
        {
            return null;
        }

        return GetText(author.RootElement, "name");
    }

    private async Task<JsonDocument?> GetOptionalJsonAsync(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                relativeUrl,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            return await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetText(
        JsonElement element,
        string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        if (property.ValueKind == JsonValueKind.Object &&
            property.TryGetProperty("value", out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    private static string GetDescription(JsonElement root)
    {
        return GetText(root, "description")
            ?? "Description unavailable.";
    }

    private static int? GetFirstCoverId(JsonElement element)
    {
        if (!element.TryGetProperty("covers", out var covers) ||
            covers.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var cover in covers.EnumerateArray())
        {
            if (cover.ValueKind == JsonValueKind.Number &&
                cover.TryGetInt32(out int coverId) &&
                coverId > 0)
            {
                return coverId;
            }
        }

        return null;
    }

    private static List<string> GetStringArray(
        JsonElement element,
        string propertyName)
    {
        var result = new List<string>();

        if (!element.TryGetProperty(propertyName, out var values) ||
            values.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var value in values.EnumerateArray())
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    result.Add(text);
                }
            }
        }

        return result;
    }

    private static List<string> GetAuthorKeys(JsonElement root)
    {
        var result = new List<string>();

        if (!root.TryGetProperty("authors", out var authors) ||
            authors.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in authors.EnumerateArray())
        {
            string? key = null;

            if (item.TryGetProperty("author", out var author) &&
                author.ValueKind == JsonValueKind.Object)
            {
                key = GetText(author, "key");
            }
            else
            {
                key = GetText(item, "key");
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                string authorId = key.Split('/').Last();

                if (System.Text.RegularExpressions.Regex.IsMatch(
                    authorId, @"^OL\d+A$"))
                {
                    result.Add(authorId);
                }
            }
        }

        return result.Distinct().ToList();
    }
}