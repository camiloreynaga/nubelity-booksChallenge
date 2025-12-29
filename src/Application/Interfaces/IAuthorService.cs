using Application.DTOs;
using Application.DTOs.Authors;

namespace Application.Interfaces;

public interface IAuthorService
{
    Task<AuthorResponseDto> CreateAsync(CreateAuthorDto dto);
    Task<PagedResultDto<AuthorResponseDto>> GetAllAsync(GetAuthorsQueryDto query);
    Task<AuthorResponseDto?> GetByIdAsync(Guid id);
    Task<AuthorResponseDto> UpdateAsync(Guid id, UpdateAuthorDto dto);
    Task<bool> DeleteAsync(Guid id);
}

