using Application.DTOs;
using Application.DTOs.Books;
using Application.Interfaces;
using Infrastructure;
using Infrastructure.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Application.Tests;

public class BookServiceTests
{
    private BooksDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BooksDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new BooksDbContext(options);
    }

    private (Mock<IIsbnValidator>, Mock<ICoverUrlService>) CreateMockServices(bool isValidIsbn = true, string? coverUrl = null)
    {
        var mockIsbnValidator = new Mock<IIsbnValidator>();
        mockIsbnValidator.Setup(x => x.ValidateIsbnAsync(It.IsAny<string>()))
            .ReturnsAsync(isValidIsbn);
        
        var mockCoverService = new Mock<ICoverUrlService>();
        mockCoverService.Setup(x => x.GetCoverUrlAsync(It.IsAny<string>()))
            .ReturnsAsync(coverUrl);
        
        return (mockIsbnValidator, mockCoverService);
    }

    [Fact]
    public async Task CreateBookAsync_WithExistingAuthor_ShouldUseExistingAuthor()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);

        // Crear un autor primero
        var existingAuthor = new Author
        {
            Id = Guid.NewGuid(),
            Name = "JOSE MARIA" // Nombre ya normalizado
        };
        context.Authors.Add(existingAuthor);
        await context.SaveChangesAsync();

        var dto = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9",
            Title = "El niño 123",
            PublicationYear = 2023,
            AuthorName = "José María" // Variación del nombre (con acentos)
        };

        // Act
        var result = await bookService.CreateBookAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EL NINO", result.Title);
        Assert.Equal("JOSE MARIA", result.AuthorName);
        Assert.Equal(existingAuthor.Id, result.AuthorId);
        Assert.Single(context.Authors); // No se creó un autor duplicado
        Assert.Single(context.Books);
    }

    [Fact]
    public async Task CreateBookAsync_WithNewAuthor_ShouldCreateAuthorAndBook()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);

        var dto = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9",
            Title = "El niño 123",
            PublicationYear = 2023,
            AuthorName = "José María"
        };

        // Act
        var result = await bookService.CreateBookAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EL NINO", result.Title);
        Assert.Equal("JOSE MARIA", result.AuthorName);
        Assert.Single(context.Authors);
        Assert.Single(context.Books);
        
        var author = await context.Authors.FirstAsync();
        Assert.Equal("JOSE MARIA", author.Name);
    }

    [Fact]
    public async Task CreateBookAsync_ShouldNormalizeTitle()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);

        var dto = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9",
            Title = "  El niño 789  ", // Con espacios, acentos y números
            PublicationYear = 2023,
            AuthorName = "José María"
        };

        // Act
        var result = await bookService.CreateBookAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EL NINO", result.Title);
        
        var book = await context.Books.FirstAsync();
        Assert.Equal("EL NINO", book.Title);
    }

    [Fact]
    public async Task CreateBookAsync_ShouldNormalizeAuthorName()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);

        var dto = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9",
            Title = "Test Book",
            PublicationYear = 2023,
            AuthorName = "  José123 María456  " // Con espacios, acentos y números
        };

        // Act
        var result = await bookService.CreateBookAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("JOSE MARIA", result.AuthorName);
        
        var author = await context.Authors.FirstAsync();
        Assert.Equal("JOSE MARIA", author.Name);
    }

    [Fact]
    public async Task CreateBookAsync_WithDuplicateIsbn_ShouldThrowException()
    {
        // Arrange - Usar SQLite en memoria para validar restricción única
        // InMemory database no respeta índices únicos, por lo que usamos SQLite
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BooksDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new BooksDbContext(options);
        context.Database.EnsureCreated();

        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);

        var dto1 = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9",
            Title = "First Book",
            PublicationYear = 2023,
            AuthorName = "Author One"
        };

        var dto2 = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9", // Mismo ISBN
            Title = "Second Book",
            PublicationYear = 2024,
            AuthorName = "Author Two"
        };

        // Act & Assert
        await bookService.CreateBookAsync(dto1);
        
        // Intentar crear otro libro con el mismo ISBN debe lanzar excepción
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await bookService.CreateBookAsync(dto2);
        });
    }

    [Fact]
    public async Task CreateBookAsync_WithDifferentAuthorNameVariations_ShouldUseSameAuthor()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);

        var dto1 = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9",
            Title = "First Book",
            PublicationYear = 2023,
            AuthorName = "José María"
        };

        var dto2 = new CreateBookDto
        {
            Isbn = "978-0-123456-78-0",
            Title = "Second Book",
            PublicationYear = 2024,
            AuthorName = "Jose Maria" // Sin acentos, pero se normaliza igual
        };

        // Act
        var result1 = await bookService.CreateBookAsync(dto1);
        var result2 = await bookService.CreateBookAsync(dto2);

        // Assert
        Assert.Equal(result1.AuthorId, result2.AuthorId); // Mismo autor
        Assert.Single(context.Authors); // Solo un autor creado
        Assert.Equal(2, context.Books.Count()); // Dos libros
    }

    [Fact]
    public async Task CreateBookAsync_ShouldNotNormalizeIsbn()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);

        var dto = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9", // ISBN con números y guiones
            Title = "Test Book",
            PublicationYear = 2023,
            AuthorName = "Test Author"
        };

        // Act
        var result = await bookService.CreateBookAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("978-0-123456-78-9", result.Isbn); // ISBN no debe normalizarse
        
        var book = await context.Books.FirstAsync();
        Assert.Equal("978-0-123456-78-9", book.Isbn);
    }

    [Fact]
    public async Task CreateBookAsync_ShouldSetCoverUrlToNull()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices(coverUrl: null);
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);

        var dto = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9",
            Title = "Test Book",
            PublicationYear = 2023,
            AuthorName = "Test Author"
        };

        // Act
        var result = await bookService.CreateBookAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.CoverUrl); // CoverUrl debe ser null cuando el servicio retorna null
    }

    [Fact]
    public async Task CreateBookAsync_WithInvalidIsbn_ShouldThrowException()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices(isValidIsbn: false);
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        var dto = new CreateBookDto
        {
            Isbn = "invalid-isbn",
            Title = "Test Book",
            PublicationYear = 2023,
            AuthorName = "Test Author"
        };
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => bookService.CreateBookAsync(dto));
    }

    [Fact]
    public async Task CreateBookAsync_WithValidIsbn_ShouldCreateBook()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices(isValidIsbn: true);
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        var dto = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9",
            Title = "Test Book",
            PublicationYear = 2023,
            AuthorName = "Test Author"
        };
        
        // Act
        var result = await bookService.CreateBookAsync(dto);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("978-0-123456-78-9", result.Isbn);
        Assert.Single(context.Books);
        
        // Verificar que se llamó al validador
        mockIsbnValidator.Verify(x => x.ValidateIsbnAsync("978-0-123456-78-9"), Times.Once);
    }

    [Fact]
    public async Task CreateBookAsync_WithCoverUrl_ShouldSetCoverUrl()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var expectedCoverUrl = "https://covers.openlibrary.org/b/id/123456-M.jpg";
        var (mockIsbnValidator, mockCoverService) = CreateMockServices(coverUrl: expectedCoverUrl);
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        var dto = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9",
            Title = "Test Book",
            PublicationYear = 2023,
            AuthorName = "Test Author"
        };
        
        // Act
        var result = await bookService.CreateBookAsync(dto);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCoverUrl, result.CoverUrl);
        
        var book = await context.Books.FirstAsync();
        Assert.Equal(expectedCoverUrl, book.CoverUrl);
        
        // Verificar que se llamó al servicio de cover
        mockCoverService.Verify(x => x.GetCoverUrlAsync("978-0-123456-78-9"), Times.Once);
    }

    [Fact]
    public async Task CreateBookAsync_WhenCoverServiceReturnsNull_ShouldCreateBookWithNullCoverUrl()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices(coverUrl: null);
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        var dto = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9",
            Title = "Test Book",
            PublicationYear = 2023,
            AuthorName = "Test Author"
        };
        
        // Act
        var result = await bookService.CreateBookAsync(dto);
        
        // Assert
        Assert.NotNull(result);
        Assert.Null(result.CoverUrl); // Debe ser null cuando el servicio retorna null
        Assert.Single(context.Books); // El libro se debe crear de todas formas
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<BooksDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var context = new BooksDbContext(options);
        var textNormalizer = new TextNormalizer();
        
        // Crear 25 libros
        var author = new Author { Id = Guid.NewGuid(), Name = "TEST AUTHOR" };
        context.Authors.Add(author);
        
        for (int i = 1; i <= 25; i++)
        {
            context.Books.Add(new Book
            {
                Id = Guid.NewGuid(),
                Isbn = $"978-0-123456-{i:D2}-0",
                Title = $"Book {i}",
                PublicationYear = 2023,
                AuthorId = author.Id,
                Author = author
            });
        }
        await context.SaveChangesAsync();
        
        var bookService = new BookService(context, textNormalizer, 
            Mock.Of<IIsbnValidator>(), Mock.Of<ICoverUrlService>());
        
        var query = new GetBooksQueryDto { Page = 1, PageSize = 10 };
        
        // Act
        var result = await bookService.GetAllAsync(query);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetAllAsync_WithTitleFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        var author = new Author { Id = Guid.NewGuid(), Name = "TEST AUTHOR" };
        context.Authors.Add(author);
        
        context.Books.Add(new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "EL NINO",
            PublicationYear = 2023,
            AuthorId = author.Id,
            Author = author
        });
        
        context.Books.Add(new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-79-0",
            Title = "LA NINA",
            PublicationYear = 2023,
            AuthorId = author.Id,
            Author = author
        });
        
        await context.SaveChangesAsync();
        
        var query = new GetBooksQueryDto { Page = 1, PageSize = 10, Title = "el niño" };
        
        // Act
        var result = await bookService.GetAllAsync(query);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("EL NINO", result.Items[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_WithAuthorNameFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        var author1 = new Author { Id = Guid.NewGuid(), Name = "JOSE MARIA" };
        var author2 = new Author { Id = Guid.NewGuid(), Name = "MARIA JOSE" };
        context.Authors.AddRange(author1, author2);
        
        context.Books.Add(new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "BOOK 1",
            PublicationYear = 2023,
            AuthorId = author1.Id,
            Author = author1
        });
        
        context.Books.Add(new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-79-0",
            Title = "BOOK 2",
            PublicationYear = 2023,
            AuthorId = author2.Id,
            Author = author2
        });
        
        await context.SaveChangesAsync();
        
        var query = new GetBooksQueryDto { Page = 1, PageSize = 10, AuthorName = "José María" };
        
        // Act
        var result = await bookService.GetAllAsync(query);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("JOSE MARIA", result.Items[0].AuthorName);
    }

    [Fact]
    public async Task GetAllAsync_WithCombinedFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        var author1 = new Author { Id = Guid.NewGuid(), Name = "JOSE MARIA" };
        var author2 = new Author { Id = Guid.NewGuid(), Name = "MARIA JOSE" };
        context.Authors.AddRange(author1, author2);
        
        context.Books.Add(new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "EL NINO",
            PublicationYear = 2023,
            AuthorId = author1.Id,
            Author = author1
        });
        
        context.Books.Add(new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-79-0",
            Title = "EL NINO",
            PublicationYear = 2023,
            AuthorId = author2.Id,
            Author = author2
        });
        
        context.Books.Add(new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-80-0",
            Title = "LA NINA",
            PublicationYear = 2023,
            AuthorId = author1.Id,
            Author = author1
        });
        
        await context.SaveChangesAsync();
        
        var query = new GetBooksQueryDto 
        { 
            Page = 1, 
            PageSize = 10, 
            Title = "el niño",
            AuthorName = "José María"
        };
        
        // Act
        var result = await bookService.GetAllAsync(query);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("EL NINO", result.Items[0].Title);
        Assert.Equal("JOSE MARIA", result.Items[0].AuthorName);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingBook_ShouldReturnBook()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        var author = new Author { Id = Guid.NewGuid(), Name = "TEST AUTHOR" };
        context.Authors.Add(author);
        
        var bookId = Guid.NewGuid();
        var book = new Book
        {
            Id = bookId,
            Isbn = "978-0-123456-78-9",
            Title = "TEST BOOK",
            PublicationYear = 2023,
            AuthorId = author.Id,
            Author = author
        };
        context.Books.Add(book);
        await context.SaveChangesAsync();
        
        // Act
        var result = await bookService.GetByIdAsync(bookId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookId, result.Id);
        Assert.Equal("978-0-123456-78-9", result.Isbn);
        Assert.Equal("TEST BOOK", result.Title);
        Assert.Equal("TEST AUTHOR", result.AuthorName);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingBook_ShouldReturnNull()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        var nonExistingId = Guid.NewGuid();
        
        // Act
        var result = await bookService.GetByIdAsync(nonExistingId);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WithOnlyTitle_ShouldUpdateOnlyTitle()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear autor y libro
        var author = new Author { Id = Guid.NewGuid(), Name = "TEST AUTHOR" };
        context.Authors.Add(author);
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "ORIGINAL TITLE",
            PublicationYear = 2020,
            AuthorId = author.Id,
            Author = author
        };
        context.Books.Add(book);
        await context.SaveChangesAsync();
        
        var updateDto = new UpdateBookDto { Title = "New Title 123" };
        
        // Act
        var result = await bookService.UpdateAsync(book.Id, updateDto);
        
        // Assert
        Assert.Equal("NEW TITLE", result.Title); // Normalizado
        Assert.Equal(book.Isbn, result.Isbn); // No cambió
        Assert.Equal(book.PublicationYear, result.PublicationYear); // No cambió
        Assert.Equal(author.Id, result.AuthorId); // No cambió
    }

    [Fact]
    public async Task UpdateAsync_WithOnlyIsbn_ShouldUpdateIsbnAndCoverUrl()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var newIsbn = "978-0-316-76948-0";
        var expectedCoverUrl = "https://covers.openlibrary.org/b/id/123456-M.jpg";
        var (mockIsbnValidator, mockCoverService) = CreateMockServices(coverUrl: expectedCoverUrl);
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear autor y libro
        var author = new Author { Id = Guid.NewGuid(), Name = "TEST AUTHOR" };
        context.Authors.Add(author);
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "ORIGINAL TITLE",
            PublicationYear = 2020,
            AuthorId = author.Id,
            Author = author
        };
        context.Books.Add(book);
        await context.SaveChangesAsync();
        
        var updateDto = new UpdateBookDto { Isbn = newIsbn };
        
        // Act
        var result = await bookService.UpdateAsync(book.Id, updateDto);
        
        // Assert
        Assert.Equal(newIsbn, result.Isbn); // Cambió
        Assert.Equal(expectedCoverUrl, result.CoverUrl); // Se obtuvo nuevo CoverUrl
        Assert.Equal(book.Title, result.Title); // No cambió
        Assert.Equal(book.PublicationYear, result.PublicationYear); // No cambió
        mockIsbnValidator.Verify(x => x.ValidateIsbnAsync(newIsbn), Times.Once);
        mockCoverService.Verify(x => x.GetCoverUrlAsync(newIsbn), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithOnlyAuthorName_ShouldUpdateAuthor()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear autor original y libro
        var authorA = new Author { Id = Guid.NewGuid(), Name = "AUTHOR A" };
        context.Authors.Add(authorA);
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "ORIGINAL TITLE",
            PublicationYear = 2020,
            AuthorId = authorA.Id,
            Author = authorA
        };
        context.Books.Add(book);
        await context.SaveChangesAsync();
        
        var updateDto = new UpdateBookDto { AuthorName = "Author B" };
        
        // Act
        var result = await bookService.UpdateAsync(book.Id, updateDto);
        
        // Assert
        Assert.Equal("AUTHOR B", result.AuthorName); // Normalizado
        Assert.NotEqual(authorA.Id, result.AuthorId); // Cambió el autor
        Assert.Equal(book.Isbn, result.Isbn); // No cambió
        Assert.Equal(book.Title, result.Title); // No cambió
    }

    [Fact]
    public async Task UpdateAsync_WithMultipleFields_ShouldUpdateAllFields()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear autor y libro
        var author = new Author { Id = Guid.NewGuid(), Name = "TEST AUTHOR" };
        context.Authors.Add(author);
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "ORIGINAL TITLE",
            PublicationYear = 2020,
            AuthorId = author.Id,
            Author = author
        };
        context.Books.Add(book);
        await context.SaveChangesAsync();
        
        var updateDto = new UpdateBookDto 
        { 
            Title = "Updated Title",
            PublicationYear = 2024,
            AuthorName = "New Author"
        };
        
        // Act
        var result = await bookService.UpdateAsync(book.Id, updateDto);
        
        // Assert
        Assert.Equal("UPDATED TITLE", result.Title); // Normalizado
        Assert.Equal(2024, result.PublicationYear);
        Assert.Equal("NEW AUTHOR", result.AuthorName); // Normalizado
        Assert.Equal(book.Isbn, result.Isbn); // No cambió
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingBook_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        var nonExistingId = Guid.NewGuid();
        var updateDto = new UpdateBookDto { Title = "New Title" };
        
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            bookService.UpdateAsync(nonExistingId, updateDto));
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidIsbn_ShouldThrowArgumentException()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices(isValidIsbn: false);
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear autor y libro
        var author = new Author { Id = Guid.NewGuid(), Name = "TEST AUTHOR" };
        context.Authors.Add(author);
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "ORIGINAL TITLE",
            PublicationYear = 2020,
            AuthorId = author.Id,
            Author = author
        };
        context.Books.Add(book);
        await context.SaveChangesAsync();
        
        var updateDto = new UpdateBookDto { Isbn = "invalid-isbn" };
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            bookService.UpdateAsync(book.Id, updateDto));
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateIsbn_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear dos libros con ISBNs diferentes
        var author = new Author { Id = Guid.NewGuid(), Name = "TEST AUTHOR" };
        context.Authors.Add(author);
        var book1 = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "BOOK 1",
            PublicationYear = 2020,
            AuthorId = author.Id,
            Author = author
        };
        var book2 = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-79-0",
            Title = "BOOK 2",
            PublicationYear = 2021,
            AuthorId = author.Id,
            Author = author
        };
        context.Books.AddRange(book1, book2);
        await context.SaveChangesAsync();
        
        // Intentar actualizar el ISBN del primer libro al del segundo
        var updateDto = new UpdateBookDto { Isbn = book2.Isbn };
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            bookService.UpdateAsync(book1.Id, updateDto));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingBook_ShouldReturnTrueAndDeleteBook()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear autor y libro
        var author = new Author { Id = Guid.NewGuid(), Name = "TEST AUTHOR" };
        context.Authors.Add(author);
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = "978-0-123456-78-9",
            Title = "TEST BOOK",
            PublicationYear = 2020,
            AuthorId = author.Id,
            Author = author
        };
        context.Books.Add(book);
        await context.SaveChangesAsync();
        
        // Act
        var result = await bookService.DeleteAsync(book.Id);
        
        // Assert
        Assert.True(result);
        var deletedBook = await context.Books.FindAsync(book.Id);
        Assert.Null(deletedBook); // El libro debe estar eliminado
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingBook_ShouldReturnFalse()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        var nonExistingId = Guid.NewGuid();
        
        // Act
        var result = await bookService.DeleteAsync(nonExistingId);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CreateBooksFromCsvAsync_WithValidCsv_ShouldCreateAllBooks()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices(isValidIsbn: true);
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear CSV en memoria
        var csvContent = "ISBN,Title,PublicationYear,AuthorName\n" +
                         "978-0-316-76948-0,Book 1,2023,Author 1\n" +
                         "978-0-123456-78-9,Book 2,2022,Author 2\n";
        var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        var csvStream = new MemoryStream(csvBytes);
        
        // Act
        var result = await bookService.CreateBooksFromCsvAsync(csvStream);
        
        // Assert
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.SuccessfulRows);
        Assert.Equal(0, result.FailedRows);
        Assert.All(result.Results, r => Assert.True(r.IsSuccess));
        Assert.Equal(2, context.Books.Count());
    }

    [Fact]
    public async Task CreateBooksFromCsvAsync_WithInvalidRows_ShouldReportErrors()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices(isValidIsbn: true);
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear CSV con filas inválidas
        var csvContent = "ISBN,Title,PublicationYear,AuthorName\n" +
                         "978-0-316-76948-0,Book 1,2023,Author 1\n" + // Válido
                         ",Book 2,2022,Author 2\n" + // ISBN faltante
                         "978-0-123456-78-9,,2022,Author 3\n" + // Title faltante
                         "978-0-123456-79-0,Book 4,500,Author 4\n"; // Año fuera de rango
        var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        var csvStream = new MemoryStream(csvBytes);
        
        // Act
        var result = await bookService.CreateBooksFromCsvAsync(csvStream);
        
        // Assert
        Assert.Equal(4, result.TotalRows);
        Assert.Equal(1, result.SuccessfulRows);
        Assert.Equal(3, result.FailedRows);
        Assert.Single(context.Books); // Solo se creó un libro
    }

    [Fact]
    public async Task CreateBooksFromCsvAsync_WithDuplicateIsbn_ShouldReportError()
    {
        // Arrange - Usar SQLite en memoria para validar restricción única
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<BooksDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new BooksDbContext(options);
        context.Database.EnsureCreated();

        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices(isValidIsbn: true);
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear CSV con ISBNs duplicados
        var csvContent = "ISBN,Title,PublicationYear,AuthorName\n" +
                         "978-0-316-76948-0,Book 1,2023,Author 1\n" +
                         "978-0-316-76948-0,Book 2,2022,Author 2\n"; // Mismo ISBN
        var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        var csvStream = new MemoryStream(csvBytes);
        
        // Act
        var result = await bookService.CreateBooksFromCsvAsync(csvStream);
        
        // Assert
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.SuccessfulRows);
        Assert.Equal(1, result.FailedRows);
        Assert.Single(context.Books); // Solo se creó un libro
        Assert.Contains(result.Results, r => !r.IsSuccess && r.ErrorMessage != null && r.ErrorMessage.Contains("ISBN already exists"));
    }

    [Fact]
    public async Task CreateBooksFromCsvAsync_WithInvalidIsbn_ShouldReportError()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices(isValidIsbn: false);
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear CSV con ISBN inválido
        var csvContent = "ISBN,Title,PublicationYear,AuthorName\n" +
                         "invalid-isbn,Book 1,2023,Author 1\n";
        var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        var csvStream = new MemoryStream(csvBytes);
        
        // Act
        var result = await bookService.CreateBooksFromCsvAsync(csvStream);
        
        // Assert
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.SuccessfulRows);
        Assert.Equal(1, result.FailedRows);
        Assert.Empty(context.Books);
        Assert.Contains(result.Results, r => !r.IsSuccess && r.ErrorMessage != null && r.ErrorMessage.Contains("Invalid ISBN"));
    }

    [Fact]
    public async Task CreateBooksFromCsvAsync_WithEmptyCsv_ShouldReturnZeroRows()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices();
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear CSV vacío (solo encabezados)
        var csvContent = "ISBN,Title,PublicationYear,AuthorName\n";
        var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        var csvStream = new MemoryStream(csvBytes);
        
        // Act
        var result = await bookService.CreateBooksFromCsvAsync(csvStream);
        
        // Assert
        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.SuccessfulRows);
        Assert.Equal(0, result.FailedRows);
        Assert.Empty(result.Results);
        Assert.Empty(context.Books);
    }

    [Fact]
    public async Task CreateBooksFromCsvAsync_WithMissingAuthorName_ShouldReportError()
    {
        // Arrange
        var context = CreateContext();
        var textNormalizer = new TextNormalizer();
        var (mockIsbnValidator, mockCoverService) = CreateMockServices(isValidIsbn: true);
        var bookService = new BookService(context, textNormalizer, mockIsbnValidator.Object, mockCoverService.Object);
        
        // Crear CSV con AuthorName faltante
        var csvContent = "ISBN,Title,PublicationYear,AuthorName\n" +
                         "978-0-316-76948-0,Book 1,2023,\n"; // AuthorName vacío
        var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        var csvStream = new MemoryStream(csvBytes);
        
        // Act
        var result = await bookService.CreateBooksFromCsvAsync(csvStream);
        
        // Assert
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.SuccessfulRows);
        Assert.Equal(1, result.FailedRows);
        Assert.Empty(context.Books);
        Assert.Contains(result.Results, r => !r.IsSuccess && r.ErrorMessage != null && r.ErrorMessage.Contains("AuthorName is required"));
    }
}

