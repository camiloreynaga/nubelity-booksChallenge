using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Authors;

public class CreateAuthorDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name must not exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }
}

