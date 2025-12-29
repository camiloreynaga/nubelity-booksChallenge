using Application.DTOs.Books;

namespace Application.Interfaces;

public interface IBookService
{
    Task<BookResponseDto> CreateBookAsync(CreateBookDto dto);
}

