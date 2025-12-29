namespace Application.DTOs.Authors;

public class AuthorResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public int BooksCount { get; set; }
}

