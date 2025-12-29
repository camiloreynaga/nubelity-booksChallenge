using Application.DTOs;
using Application.DTOs.Authors;
using Application.Interfaces;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class AuthorService : IAuthorService
{
    private readonly BooksDbContext _context;
    private readonly ITextNormalizer _textNormalizer;

    public AuthorService(BooksDbContext context, ITextNormalizer textNormalizer)
    {
        _context = context;
        _textNormalizer = textNormalizer;
    }

    public async Task<AuthorResponseDto> CreateAsync(CreateAuthorDto dto)
    {
        // Normalizar Name
        string normalizedName = _textNormalizer.Normalize(dto.Name);
        
        // Verificar si ya existe un autor con ese nombre normalizado
        var existingAuthor = await _context.Authors
            .FirstOrDefaultAsync(a => a.Name == normalizedName);
        
        if (existingAuthor != null)
        {
            throw new InvalidOperationException($"An author with the name '{dto.Name}' already exists.");
        }
        
        // Crear autor
        var author = new Author
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            BirthDate = dto.BirthDate
        };
        
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();
        
        return new AuthorResponseDto
        {
            Id = author.Id,
            Name = author.Name,
            BirthDate = author.BirthDate,
            BooksCount = 0 // Nuevo autor, no tiene libros aún
        };
    }

    public async Task<PagedResultDto<AuthorResponseDto>> GetAllAsync(GetAuthorsQueryDto query)
    {
        // Valores por defecto
        int page = query.Page > 0 ? query.Page : 1;
        int pageSize = query.PageSize > 0 && query.PageSize <= 100 ? query.PageSize : 10;
        
        // Construir query base
        var queryable = _context.Authors
            .Include(a => a.Books) // Incluir libros para contar
            .AsQueryable();
        
        // Obtener total antes de paginar
        int totalCount = await queryable.CountAsync();
        
        // Aplicar paginación
        var authors = await queryable
            .OrderBy(a => a.Name) // Ordenar por nombre
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        // Mapear a DTOs
        var items = authors.Select(a => new AuthorResponseDto
        {
            Id = a.Id,
            Name = a.Name,
            BirthDate = a.BirthDate,
            BooksCount = a.Books.Count
        }).ToList();
        
        return new PagedResultDto<AuthorResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AuthorResponseDto?> GetByIdAsync(Guid id)
    {
        var author = await _context.Authors
            .Include(a => a.Books) // Incluir libros para contar
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (author == null)
        {
            return null;
        }
        
        return new AuthorResponseDto
        {
            Id = author.Id,
            Name = author.Name,
            BirthDate = author.BirthDate,
            BooksCount = author.Books.Count
        };
    }

    public async Task<AuthorResponseDto> UpdateAsync(Guid id, UpdateAuthorDto dto)
    {
        var author = await _context.Authors
            .Include(a => a.Books)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (author == null)
        {
            throw new KeyNotFoundException($"Author with ID {id} not found.");
        }
        
        // Actualizar solo campos proporcionados (actualización parcial)
        if (dto.Name != null)
        {
            // Normalizar nuevo nombre
            string normalizedName = _textNormalizer.Normalize(dto.Name);
            
            // Verificar si otro autor ya tiene ese nombre
            var existingAuthor = await _context.Authors
                .FirstOrDefaultAsync(a => a.Name == normalizedName && a.Id != id);
            
            if (existingAuthor != null)
            {
                throw new InvalidOperationException($"An author with the name '{dto.Name}' already exists.");
            }
            
            author.Name = normalizedName;
        }
        
        if (dto.BirthDate.HasValue)
        {
            author.BirthDate = dto.BirthDate;
        }
        
        await _context.SaveChangesAsync();
        
        return new AuthorResponseDto
        {
            Id = author.Id,
            Name = author.Name,
            BirthDate = author.BirthDate,
            BooksCount = author.Books.Count
        };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var author = await _context.Authors
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (author == null)
        {
            return false; // No existe
        }
        
        // Eliminar autor (cascade delete eliminará automáticamente los libros asociados)
        _context.Authors.Remove(author);
        await _context.SaveChangesAsync();
        
        return true; // Eliminado correctamente
    }
}

