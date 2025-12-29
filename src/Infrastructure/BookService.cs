using Application.DTOs.Books;
using Application.Interfaces;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class BookService : IBookService
{
    private readonly BooksDbContext _context;
    private readonly ITextNormalizer _textNormalizer;
    private readonly IIsbnValidator _isbnValidator;
    private readonly ICoverUrlService _coverUrlService;

    public BookService(
        BooksDbContext context, 
        ITextNormalizer textNormalizer,
        IIsbnValidator isbnValidator,
        ICoverUrlService coverUrlService)
    {
        _context = context;
        _textNormalizer = textNormalizer;
        _isbnValidator = isbnValidator;
        _coverUrlService = coverUrlService;
    }

    public async Task<BookResponseDto> CreateBookAsync(CreateBookDto dto)
    {
        // 1. Validar ISBN con SOAP
        bool isIsbnValid = await _isbnValidator.ValidateIsbnAsync(dto.Isbn);
        if (!isIsbnValid)
        {
            throw new ArgumentException("Invalid ISBN", nameof(dto.Isbn));
        }

        // 2. Normalizar Title
        string normalizedTitle = _textNormalizer.Normalize(dto.Title);

        // 3. Buscar o crear Author
        string normalizedAuthorName = _textNormalizer.Normalize(dto.AuthorName);
        var author = await _context.Authors
            .FirstOrDefaultAsync(a => a.Name == normalizedAuthorName);

        if (author == null)
        {
            author = new Author
            {
                Id = Guid.NewGuid(),
                Name = normalizedAuthorName
            };
            _context.Authors.Add(author);
            await _context.SaveChangesAsync();
        }

        // 4. Obtener CoverUrl con REST
        string? coverUrl = await _coverUrlService.GetCoverUrlAsync(dto.Isbn);
        // Si no se obtiene, puede ser null (no es crítico)

        // 5. Crear Book
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = dto.Isbn,
            Title = normalizedTitle,
            CoverUrl = coverUrl, // Ahora se obtiene del servicio REST
            PublicationYear = dto.PublicationYear,
            AuthorId = author.Id,
            Author = author
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        // 6. Retornar BookResponseDto
        return new BookResponseDto
        {
            Id = book.Id,
            Isbn = book.Isbn,
            Title = book.Title,
            CoverUrl = book.CoverUrl,
            PublicationYear = book.PublicationYear,
            AuthorId = book.AuthorId,
            AuthorName = author.Name
        };
    }
}

