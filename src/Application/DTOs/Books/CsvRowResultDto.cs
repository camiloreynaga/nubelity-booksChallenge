namespace Application.DTOs.Books;

public class CsvRowResultDto
{
    public int RowNumber { get; set; }
    public bool IsSuccess { get; set; }
    public Guid? BookId { get; set; }
    public string? ErrorMessage { get; set; }
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

