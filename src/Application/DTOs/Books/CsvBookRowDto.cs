namespace Application.DTOs.Books;

public class CsvBookRowDto
{
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public int? PageNumber { get; set; }
    public string AuthorName { get; set; } = string.Empty;
}

