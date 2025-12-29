using Application.DTOs.Books;
using Application.Interfaces;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class BookService : IBookService
{
    private readonly BooksDbContext _context;
    private readonly ITextNormalizer _textNormalizer;

    public BookService(BooksDbContext context, ITextNormalizer textNormalizer)
    {
        _context = context;
        _textNormalizer = textNormalizer;
    }

    public async Task<BookResponseDto> CreateBookAsync(CreateBookDto dto)
    {
        // Normalizar el título del libro
        string normalizedTitle = _textNormalizer.Normalize(dto.Title);

        // Normalizar el nombre del autor
        string normalizedAuthorName = _textNormalizer.Normalize(dto.AuthorName);

        // Buscar autor por nombre normalizado
        var author = await _context.Authors
            .FirstOrDefaultAsync(a => a.Name == normalizedAuthorName);

        // Si no existe, crearlo
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

        // Crear el libro
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Isbn = dto.Isbn, // No normalizar ISBN (contiene números y formato específico)
            Title = normalizedTitle,
            CoverUrl = null, // Se obtendrá en el Slice 5
            PublicationYear = dto.PublicationYear,
            AuthorId = author.Id,
            Author = author
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        // Retornar BookResponseDto
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

