using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Books;

public class GetBooksQueryDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 10;

    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(200)]
    public string? AuthorName { get; set; }
}

