using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBookTracker.Data;
using OnlineBookTracker.Models;
using OnlineBookTracker.Services;

namespace OnlineBookTracker.Controllers;

[Authorize]
public class LibraryController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly OpenLibraryService _openLibrary;

    public LibraryController(
        ApplicationDbContext context,
        OpenLibraryService openLibrary)
    {
        _context = context;
        _openLibrary = openLibrary;
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryGetUserId(out int userId))
            return Challenge();

        var books = await _context.UserBooks
            .Where(book => book.UserId == userId)
            .OrderByDescending(book => book.DateAdded)
            .ToListAsync();

        return View(books);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        string? id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out int userId))
            return Challenge();

        if (string.IsNullOrWhiteSpace(id) ||
            !System.Text.RegularExpressions.Regex.IsMatch(id, @"^OL\d+W$"))
        {
            TempData["ErrorMessage"] = "Invalid book ID.";
            return RedirectToAction(nameof(Index));
        }

        bool alreadyAdded = await _context.UserBooks
            .AnyAsync(
                book => book.UserId == userId &&
                        book.OpenLibraryKey == id,
                cancellationToken);

        if (alreadyAdded)
        {
            TempData["ErrorMessage"] =
                "This book is already in your library.";

            return RedirectToAction(nameof(Index));
        }

        try
        {
            var details = await _openLibrary.GetBookDetailsAsync(
                id,
                cancellationToken);

            if (details == null)
            {
                TempData["ErrorMessage"] =
                    "Book details could not be retrieved.";

                return RedirectToAction(nameof(Index));
            }

            var userBook = new UserBook
            {
                UserId = userId,
                OpenLibraryKey = details.OpenLibraryKey,
                BookTitle = details.Title,
                AuthorName = string.Join(", ", details.Authors),
                CoverId = details.CoverId,
                ReadingStatus = "Want to Read",
                DateAdded = DateTime.UtcNow
            };

            _context.UserBooks.Add(userBook);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Book added to your library!";
        }
        catch (HttpRequestException)
        {
            TempData["ErrorMessage"] =
                "Open Library is unavailable. Please try again.";
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "The book could not be added. It may already be in your library.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        string? status)
    {
        if (!TryGetUserId(out int userId))
            return Challenge();

        string[] allowedStatuses =
        {
            "Want to Read",
            "Reading",
            "Completed"
        };

        if (string.IsNullOrWhiteSpace(status) ||
            !allowedStatuses.Contains(status))
        {
            TempData["ErrorMessage"] = "Invalid reading status.";
            return RedirectToAction(nameof(Index));
        }

        var book = await _context.UserBooks
            .FirstOrDefaultAsync(
                item => item.UserBookId == id &&
                        item.UserId == userId);

        if (book == null)
            return NotFound();

        book.ReadingStatus = status;
        book.DateCompleted = status == "Completed"
            ? DateTime.UtcNow
            : null;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Reading status updated.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        if (!TryGetUserId(out int userId))
            return Challenge();

        var book = await _context.UserBooks
            .FirstOrDefaultAsync(
                item => item.UserBookId == id &&
                        item.UserId == userId);

        if (book == null)
            return NotFound();

        _context.UserBooks.Remove(book);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Book removed from your library.";

        return RedirectToAction(nameof(Index));
    }
}