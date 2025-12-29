using Application.DTOs.Authors;
using Application.DTOs.Books;
using System.ComponentModel.DataAnnotations;

namespace Application.Tests;

public class DtoValidationTests
{
    [Fact]
    public void CreateBookDto_WithEmptyIsbn_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateBookDto
        {
            Isbn = "",
            Title = "Test Title",
            PublicationYear = 2020,
            AuthorName = "Test Author"
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBookDto.Isbn)));
    }

    [Fact]
    public void CreateBookDto_WithEmptyTitle_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateBookDto
        {
            Isbn = "1234567890123",
            Title = "",
            PublicationYear = 2020,
            AuthorName = "Test Author"
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBookDto.Title)));
    }

    [Fact]
    public void CreateBookDto_WithPublicationYearOutOfRange_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateBookDto
        {
            Isbn = "1234567890123",
            Title = "Test Title",
            PublicationYear = 500, // Below minimum
            AuthorName = "Test Author"
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBookDto.PublicationYear)));
    }

    [Fact]
    public void CreateBookDto_WithPublicationYearTooHigh_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateBookDto
        {
            Isbn = "1234567890123",
            Title = "Test Title",
            PublicationYear = 3000, // Above maximum
            AuthorName = "Test Author"
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBookDto.PublicationYear)));
    }

    [Fact]
    public void CreateBookDto_WithTitleTooLong_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateBookDto
        {
            Isbn = "1234567890123",
            Title = new string('A', 201), // Exceeds 200 characters
            PublicationYear = 2020,
            AuthorName = "Test Author"
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateBookDto.Title)));
    }

    [Fact]
    public void CreateBookDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new CreateBookDto
        {
            Isbn = "1234567890123",
            Title = "Test Title",
            PublicationYear = 2020,
            AuthorName = "Test Author"
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateBookDto_WithAllFieldsNull_ShouldPassValidation()
    {
        // Arrange
        var dto = new UpdateBookDto();

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateBookDto_WithTitleTooLong_ShouldFailValidation()
    {
        // Arrange
        var dto = new UpdateBookDto
        {
            Title = new string('A', 201) // Exceeds 200 characters
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateBookDto.Title)));
    }

    [Fact]
    public void UpdateBookDto_WithPublicationYearOutOfRange_ShouldFailValidation()
    {
        // Arrange
        var dto = new UpdateBookDto
        {
            PublicationYear = 500 // Below minimum
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateBookDto.PublicationYear)));
    }

    [Fact]
    public void UpdateBookDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new UpdateBookDto
        {
            Title = "Updated Title",
            PublicationYear = 2021
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void CreateAuthorDto_WithEmptyName_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateAuthorDto
        {
            Name = ""
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAuthorDto.Name)));
    }

    [Fact]
    public void CreateAuthorDto_WithNameTooLong_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateAuthorDto
        {
            Name = new string('A', 101) // Exceeds 100 characters
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAuthorDto.Name)));
    }

    [Fact]
    public void CreateAuthorDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new CreateAuthorDto
        {
            Name = "Test Author",
            BirthDate = new DateTime(1980, 1, 1)
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateAuthorDto_WithAllFieldsNull_ShouldPassValidation()
    {
        // Arrange
        var dto = new UpdateAuthorDto();

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateAuthorDto_WithNameTooLong_ShouldFailValidation()
    {
        // Arrange
        var dto = new UpdateAuthorDto
        {
            Name = new string('A', 101) // Exceeds 100 characters
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAuthorDto.Name)));
    }

    [Fact]
    public void UpdateAuthorDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new UpdateAuthorDto
        {
            Name = "Updated Author"
        };

        // Act
        var isValid = TryValidateObject(dto, out var results);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    private static bool TryValidateObject(object instance, out List<ValidationResult> validationResults)
    {
        var context = new ValidationContext(instance, serviceProvider: null, items: null);
        validationResults = new List<ValidationResult>();
        return Validator.TryValidateObject(instance, context, validationResults, validateAllProperties: true);
    }
}

