using Application.DTOs.Authors;
using Application.Interfaces;
using Infrastructure;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests;

public class AuthorServiceTests
{
    private BooksDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BooksDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new BooksDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_WithNewAuthor_ShouldCreateAuthor()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        var dto = new CreateAuthorDto
        {
            Name = "José María",
            BirthDate = new DateTime(1980, 1, 1)
        };

        // Act
        var result = await authorService.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("JOSE MARIA", result.Name);
        Assert.Equal(new DateTime(1980, 1, 1), result.BirthDate);
        Assert.Equal(0, result.BooksCount);
        Assert.Single(context.Authors);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ShouldThrowException()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        var dto1 = new CreateAuthorDto
        {
            Name = "José María"
        };
        
        await authorService.CreateAsync(dto1);
        
        var dto2 = new CreateAuthorDto
        {
            Name = "Jose Maria" // Variación del nombre
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => authorService.CreateAsync(dto2));
        
        Assert.Contains("already exists", exception.Message);
        Assert.Single(context.Authors); // Solo debe haber un autor
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        // Crear múltiples autores con nombres únicos (sin números porque TextNormalizer los elimina)
        var names = new[] { "Alice", "Bob", "Charlie", "David", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack", "Kate", "Liam", "Mia", "Noah", "Olivia" };
        foreach (var name in names)
        {
            var dto = new CreateAuthorDto
            {
                Name = name
            };
            await authorService.CreateAsync(dto);
        }
        
        var query = new GetAuthorsQueryDto { Page = 1, PageSize = 5 };

        // Act
        var result = await authorService.GetAllAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public async Task GetAllAsync_WithSecondPage_ShouldReturnSecondPage()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        // Crear múltiples autores con nombres únicos (sin números porque TextNormalizer los elimina)
        var names = new[] { "Alice", "Bob", "Charlie", "David", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack" };
        foreach (var name in names)
        {
            var dto = new CreateAuthorDto
            {
                Name = name
            };
            await authorService.CreateAsync(dto);
        }
        
        var query = new GetAuthorsQueryDto { Page = 2, PageSize = 5 };

        // Act
        var result = await authorService.GetAllAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingAuthor_ShouldReturnAuthor()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        var createDto = new CreateAuthorDto
        {
            Name = "Test Author",
            BirthDate = new DateTime(1980, 1, 1)
        };
        var created = await authorService.CreateAsync(createDto);

        // Act
        var result = await authorService.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("TEST AUTHOR", result.Name);
        Assert.Equal(new DateTime(1980, 1, 1), result.BirthDate);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingAuthor_ShouldReturnNull()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await authorService.GetByIdAsync(nonExistingId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithBooks_ShouldReturnBooksCount()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        var createDto = new CreateAuthorDto { Name = "Test Author" };
        var author = await authorService.CreateAsync(createDto);
        
        // Agregar libros al autor
        context.Books.Add(new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "Test Book 1",
            PublicationYear = 2023,
            AuthorId = author.Id
        });
        
        context.Books.Add(new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-79-0",
            Title = "Test Book 2",
            PublicationYear = 2023,
            AuthorId = author.Id
        });
        
        await context.SaveChangesAsync();

        // Act
        var result = await authorService.GetByIdAsync(author.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.BooksCount);
    }

    [Fact]
    public async Task UpdateAsync_WithNameOnly_ShouldUpdateOnlyName()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        var createDto = new CreateAuthorDto
        {
            Name = "Original Name",
            BirthDate = new DateTime(1980, 1, 1)
        };
        var author = await authorService.CreateAsync(createDto);
        
        var updateDto = new UpdateAuthorDto
        {
            Name = "Updated Name"
        };

        // Act
        var result = await authorService.UpdateAsync(author.Id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("UPDATED NAME", result.Name);
        Assert.Equal(new DateTime(1980, 1, 1), result.BirthDate); // BirthDate no cambió
    }

    [Fact]
    public async Task UpdateAsync_WithBirthDateOnly_ShouldUpdateOnlyBirthDate()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        var createDto = new CreateAuthorDto
        {
            Name = "Test Author",
            BirthDate = new DateTime(1980, 1, 1)
        };
        var author = await authorService.CreateAsync(createDto);
        
        var updateDto = new UpdateAuthorDto
        {
            BirthDate = new DateTime(1990, 5, 15)
        };

        // Act
        var result = await authorService.UpdateAsync(author.Id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TEST AUTHOR", result.Name); // Name no cambió
        Assert.Equal(new DateTime(1990, 5, 15), result.BirthDate);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingAuthor_ShouldThrowException()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        var nonExistingId = Guid.NewGuid();
        var updateDto = new UpdateAuthorDto
        {
            Name = "Updated Name"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => authorService.UpdateAsync(nonExistingId, updateDto));
        
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateName_ShouldThrowException()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        var author1 = await authorService.CreateAsync(new CreateAuthorDto { Name = "Author One" });
        var author2 = await authorService.CreateAsync(new CreateAuthorDto { Name = "Author Two" });
        
        var updateDto = new UpdateAuthorDto
        {
            Name = "Author One" // Intentar usar el nombre del primer autor
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => authorService.UpdateAsync(author2.Id, updateDto));
        
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingAuthor_ShouldReturnTrue()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        var createDto = new CreateAuthorDto { Name = "Test Author" };
        var author = await authorService.CreateAsync(createDto);

        // Act
        bool deleted = await authorService.DeleteAsync(author.Id);

        // Assert
        Assert.True(deleted);
        Assert.Null(await context.Authors.FindAsync(author.Id));
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingAuthor_ShouldReturnFalse()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        var nonExistingId = Guid.NewGuid();

        // Act
        bool deleted = await authorService.DeleteAsync(nonExistingId);

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithBooks_ShouldDeleteAuthorAndBooks()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var authorService = new AuthorService(context, textNormalizer);
        
        // Crear autor
        var createDto = new CreateAuthorDto { Name = "Test Author" };
        var author = await authorService.CreateAsync(createDto);
        
        // Crear libros asociados
        context.Books.Add(new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "Test Book",
            PublicationYear = 2023,
            AuthorId = author.Id
        });
        await context.SaveChangesAsync();
        
        // Act
        bool deleted = await authorService.DeleteAsync(author.Id);
        
        // Assert
        Assert.True(deleted);
        Assert.Null(await context.Authors.FindAsync(author.Id));
        Assert.Empty(context.Books); // Los libros también deben eliminarse
    }
}

