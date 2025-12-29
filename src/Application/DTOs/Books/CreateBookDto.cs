using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Books;

public class CreateBookDto
{
    [Required(ErrorMessage = "ISBN is required")]
    [StringLength(17, ErrorMessage = "ISBN must not exceed 17 characters")]
    public string Isbn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title must not exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Publication year is required")]
    [Range(1000, 2100, ErrorMessage = "Publication year must be between 1000 and 2100")]
    public int PublicationYear { get; set; }

    [Required(ErrorMessage = "Author name is required")]
    [StringLength(100, ErrorMessage = "Author name must not exceed 100 characters")]
    public string AuthorName { get; set; } = string.Empty;
}

