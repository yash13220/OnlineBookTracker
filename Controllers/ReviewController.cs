
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineBookTracker.Data;
using OnlineBookTracker.Models;
using OnlineBookTracker.Services;

namespace OnlineBookTracker.Controllers;

public class ReviewController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly OpenLibraryService _openLibrary;

    public ReviewController(
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
    public async Task<IActionResult> Index(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) ||
            !Regex.IsMatch(id, @"^OL\d+W$"))
        {
            return BadRequest("Invalid book ID.");
        }

        var reviews = await _context.Reviews
            .Include(review => review.User)
            .Where(review => review.OpenLibraryKey == id)
            .OrderByDescending(review => review.ReviewDate)
            .ToListAsync();

        string title = reviews.FirstOrDefault()?.BookTitle ?? id;

        if (reviews.Count == 0)
        {
            try
            {
                var details = await _openLibrary.GetBookDetailsAsync(id);
                if (details == null)
                    return NotFound("Book not found.");

                title = details.Title;
            }
            catch (HttpRequestException)
            {
                title = "Book " + id;
                ViewBag.Warning =
                    "Book information is temporarily unavailable.";
            }
        }

        var model = new ReviewListViewModel
        {
            OpenLibraryKey = id,
            BookTitle = title,
            Reviews = reviews
        };

        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) ||
            !Regex.IsMatch(id, @"^OL\d+W$"))
        {
            return BadRequest("Invalid book ID.");
        }

        if (!TryGetUserId(out int userId))
            return Challenge();

        bool alreadyReviewed = await _context.Reviews.AnyAsync(
            review => review.UserId == userId &&
                      review.OpenLibraryKey == id);

        if (alreadyReviewed)
        {
            TempData["ErrorMessage"] =
                "You have already reviewed this book.";

            return RedirectToAction(nameof(Index), new { id });
        }

        try
        {
            var details = await _openLibrary.GetBookDetailsAsync(id);

            if (details == null)
                return NotFound("Book not found.");

            return View(new ReviewFormViewModel
            {
                OpenLibraryKey = id,
                BookTitle = details.Title,
                Rating = 5
            });
        }
        catch (HttpRequestException)
        {
            return StatusCode(503,
                "Book information is temporarily unavailable.");
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewFormViewModel model)
    {
        if (!Regex.IsMatch(
            model.OpenLibraryKey ?? string.Empty, @"^OL\d+W$"))
        {
            return BadRequest("Invalid book ID.");
        }

        if (!TryGetUserId(out int userId))
            return Challenge();

        bool alreadyReviewed = await _context.Reviews.AnyAsync(
            review => review.UserId == userId &&
                      review.OpenLibraryKey == model.OpenLibraryKey);

        if (alreadyReviewed)
        {
            TempData["ErrorMessage"] =
                "You have already reviewed this book.";

            return RedirectToAction(
                nameof(Index), new { id = model.OpenLibraryKey });
        }

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var details = await _openLibrary.GetBookDetailsAsync(
                model.OpenLibraryKey);

            if (details == null)
                return NotFound("Book not found.");

            var review = new Review
            {
                UserId = userId,
                OpenLibraryKey = details.OpenLibraryKey,
                BookTitle = details.Title,
                Rating = model.Rating,
                ReviewText = model.ReviewText.Trim(),
                ReviewDate = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Review submitted successfully.";

            return RedirectToAction(
                nameof(Index), new { id = review.OpenLibraryKey });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("",
                "Unable to contact Open Library. Please try again.");
            return View(model);
        }
    }

    
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!TryGetUserId(out int userId))
            return Challenge();

        var review = await _context.Reviews.FirstOrDefaultAsync(
            item => item.ReviewId == id && item.UserId == userId);

        if (review == null)
            return NotFound();

        var model = new ReviewFormViewModel
        {
            OpenLibraryKey = review.OpenLibraryKey,
            BookTitle = review.BookTitle,
            Rating = review.Rating,
            ReviewText = review.ReviewText
        };

        ViewBag.ReviewId = review.ReviewId;

        return View(model);
    }


    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, ReviewFormViewModel model)
    {
        if (!TryGetUserId(out int userId))
            return Challenge();

        var review = await _context.Reviews.FirstOrDefaultAsync(
            item => item.ReviewId == id && item.UserId == userId);

        if (review == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.ReviewId = id;
            model.BookTitle = review.BookTitle;
            model.OpenLibraryKey = review.OpenLibraryKey;
            return View(model);
        }

        review.Rating = model.Rating;
        review.ReviewText = model.ReviewText.Trim();
        review.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Review updated successfully.";

        return RedirectToAction(
            nameof(Index), new { id = review.OpenLibraryKey });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetUserId(out int userId))
            return Challenge();

        var review = await _context.Reviews.FirstOrDefaultAsync(
            item => item.ReviewId == id && item.UserId == userId);

        if (review == null)
            return NotFound();

        string bookKey = review.OpenLibraryKey;

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Review deleted successfully.";

        return RedirectToAction(nameof(Index), new { id = bookKey });
    }
}
