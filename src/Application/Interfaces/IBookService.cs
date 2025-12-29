using Application.DTOs;
using Application.DTOs.Books;

namespace Application.Interfaces;

public interface IBookService
{
    Task<BookResponseDto> CreateBookAsync(CreateBookDto dto);
    Task<PagedResultDto<BookResponseDto>> GetAllAsync(GetBooksQueryDto query);
    Task<BookResponseDto?> GetByIdAsync(Guid id);
}

