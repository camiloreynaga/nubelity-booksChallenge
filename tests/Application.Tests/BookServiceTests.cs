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
}

