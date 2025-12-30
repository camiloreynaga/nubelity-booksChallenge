namespace Application.DTOs.Books;

public class CsvUploadResultDto
{
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public List<CsvRowResultDto> Results { get; set; } = new();
}

