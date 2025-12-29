using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authors;

public class UpdateAuthorDto
{
    [StringLength(100, ErrorMessage = "Name must not exceed 100 characters")]
    public string? Name { get; set; }

    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }
}

