using Microsoft.AspNetCore.Mvc;
using OnlineBookTracker.Models;
using OnlineBookTracker.Services;
using System.Text.Json;

namespace OnlineBookTracker.Controllers;

public class BookController : Controller
{
    private readonly OpenLibraryService _openLibrary;

    public BookController(OpenLibraryService openLibrary)
    {
        _openLibrary = openLibrary;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        string? query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            ModelState.AddModelError(
                "query",
                "Enter a book title or author to search.");

            return View(new BookSearchResponse());
        }

        try
        {
            ViewBag.Query = query.Trim();

            var results = await _openLibrary.SearchBooksAsync(
                query.Trim(),
                cancellationToken);

            return View(results);
        }
        catch (HttpRequestException)
        {
            ViewBag.Error =
                "Unable to contact Open Library. Check your internet connection and try again.";

            return View(new BookSearchResponse());
        }
        catch (JsonException)
        {
            ViewBag.Error =
                "The book service returned an unexpected response. Please try again.";

            return View(new BookSearchResponse());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        string? id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("A book ID is required.");
        }

        try
        {
            var book = await _openLibrary.GetBookDetailsAsync(
                id,
                cancellationToken);

            if (book == null)
            {
                return NotFound("Book details were not found.");
            }

            return View(book);
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                503,
                "Open Library is temporarily unavailable. Please try again.");
        }
        catch (JsonException)
        {
            return StatusCode(
                502,
                "Open Library returned invalid book details.");
        }
    }

    public IActionResult About()
    {
        return View();
    }
}