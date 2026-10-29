using System.ComponentModel.DataAnnotations;

namespace OnlineBookTracker.Models;

public class User
{
    public int UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<UserBook> UserBooks { get; set; } = new List<UserBook>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}