using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Books;

public class UpdateBookDto
{
    [StringLength(17, ErrorMessage = "ISBN must not exceed 17 characters")]
    public string? Isbn { get; set; }

    [StringLength(200, ErrorMessage = "Title must not exceed 200 characters")]
    public string? Title { get; set; }

    [Range(1000, 2100, ErrorMessage = "Publication year must be between 1000 and 2100")]
    public int? PublicationYear { get; set; }

    [Range(1, 10000, ErrorMessage = "Page number must be between 1 and 10000")]
    public int? PageNumber { get; set; }

    [StringLength(100, ErrorMessage = "Author name must not exceed 100 characters")]
    public string? AuthorName { get; set; }
}

