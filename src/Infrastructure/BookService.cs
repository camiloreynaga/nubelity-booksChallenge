using Application.DTOs;
using Application.DTOs.Books;
using Application.Interfaces;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

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

    public async Task<PagedResultDto<BookResponseDto>> GetAllAsync(GetBooksQueryDto query)
    {
        // Valores por defecto
        int page = query.Page > 0 ? query.Page : 1;
        int pageSize = query.PageSize > 0 && query.PageSize <= 100 ? query.PageSize : 10;
        
        // Construir query base
        var queryable = _context.Books
            .Include(b => b.Author)
            .AsQueryable();
        
        // Aplicar filtros
        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            // Normalizar título para búsqueda
            string normalizedTitle = _textNormalizer.Normalize(query.Title);
            queryable = queryable.Where(b => b.Title.Contains(normalizedTitle));
        }
        
        if (!string.IsNullOrWhiteSpace(query.AuthorName))
        {
            // Normalizar nombre de autor para búsqueda
            string normalizedAuthorName = _textNormalizer.Normalize(query.AuthorName);
            queryable = queryable.Where(b => b.Author.Name == normalizedAuthorName);
        }
        
        // Obtener total antes de paginar
        int totalCount = await queryable.CountAsync();
        
        // Aplicar paginación
        var books = await queryable
            .OrderBy(b => b.Title) // Ordenar por título
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        // Mapear a DTOs
        var items = books.Select(b => new BookResponseDto
        {
            Id = b.Id,
            Isbn = b.Isbn,
            Title = b.Title,
            CoverUrl = b.CoverUrl,
            PublicationYear = b.PublicationYear,
            AuthorId = b.AuthorId,
            AuthorName = b.Author.Name
        }).ToList();
        
        return new PagedResultDto<BookResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<BookResponseDto?> GetByIdAsync(Guid id)
    {
        var book = await _context.Books
            .Include(b => b.Author)
            .FirstOrDefaultAsync(b => b.Id == id);
        
        if (book == null)
        {
            return null;
        }
        
        return new BookResponseDto
        {
            Id = book.Id,
            Isbn = book.Isbn,
            Title = book.Title,
            CoverUrl = book.CoverUrl,
            PublicationYear = book.PublicationYear,
            AuthorId = book.AuthorId,
            AuthorName = book.Author.Name
        };
    }

    public async Task<BookResponseDto> UpdateAsync(Guid id, UpdateBookDto dto)
    {
        var book = await _context.Books
            .Include(b => b.Author)
            .FirstOrDefaultAsync(b => b.Id == id);
        
        if (book == null)
        {
            throw new KeyNotFoundException($"Book with ID {id} not found.");
        }
        
        // Actualizar solo campos proporcionados (actualización parcial)
        
        // Actualizar ISBN (si se proporciona)
        if (!string.IsNullOrWhiteSpace(dto.Isbn))
        {
            // Validar ISBN con SOAP si se cambia
            bool isIsbnValid = await _isbnValidator.ValidateIsbnAsync(dto.Isbn);
            if (!isIsbnValid)
            {
                throw new ArgumentException("Invalid ISBN", nameof(dto.Isbn));
            }
            
            // Verificar que el nuevo ISBN no esté en uso por otro libro
            var existingBook = await _context.Books
                .FirstOrDefaultAsync(b => b.Isbn == dto.Isbn && b.Id != id);
            
            if (existingBook != null)
            {
                throw new InvalidOperationException($"A book with ISBN '{dto.Isbn}' already exists.");
            }
            
            book.Isbn = dto.Isbn;
            
            // Si se cambia el ISBN, intentar obtener nuevo CoverUrl
            string? coverUrl = await _coverUrlService.GetCoverUrlAsync(dto.Isbn);
            if (coverUrl != null)
            {
                book.CoverUrl = coverUrl;
            }
        }
        
        // Actualizar Title (si se proporciona)
        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            string normalizedTitle = _textNormalizer.Normalize(dto.Title);
            book.Title = normalizedTitle;
        }
        
        // Actualizar PublicationYear (si se proporciona)
        if (dto.PublicationYear.HasValue)
        {
            book.PublicationYear = dto.PublicationYear.Value;
        }
        
        // Actualizar Author (si se proporciona AuthorName)
        if (!string.IsNullOrWhiteSpace(dto.AuthorName))
        {
            string normalizedAuthorName = _textNormalizer.Normalize(dto.AuthorName);
            
            // Buscar o crear autor
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
                await _context.SaveChangesAsync(); // Guardar autor primero
            }
            
            book.AuthorId = author.Id;
            book.Author = author;
        }
        
        await _context.SaveChangesAsync();
        
        // Recargar libro con autor actualizado
        await _context.Entry(book).Reference(b => b.Author).LoadAsync();
        
        return new BookResponseDto
        {
            Id = book.Id,
            Isbn = book.Isbn,
            Title = book.Title,
            CoverUrl = book.CoverUrl,
            PublicationYear = book.PublicationYear,
            AuthorId = book.AuthorId,
            AuthorName = book.Author.Name
        };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var book = await _context.Books
            .FirstOrDefaultAsync(b => b.Id == id);
        
        if (book == null)
        {
            return false; // No existe
        }
        
        // Eliminar libro
        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
        
        return true; // Eliminado correctamente
    }

    public async Task<CsvUploadResultDto> CreateBooksFromCsvAsync(Stream csvStream)
    {
        var results = new List<CsvRowResultDto>();
        int rowNumber = 0;
        
        using var reader = new StreamReader(csvStream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            HeaderValidated = null, // Ignorar validación de encabezados
            MissingFieldFound = null // Ignorar campos faltantes
        };
        
        using var csv = new CsvReader(reader, config);
        
        // Registrar mapeo
        csv.Context.RegisterClassMap<CsvBookRowMap>();
        
        // Leer encabezados
        await csv.ReadAsync();
        csv.ReadHeader();
        
        // Procesar cada fila
        while (await csv.ReadAsync())
        {
            rowNumber++;
            
            try
            {
                // Leer fila del CSV
                var csvRow = csv.GetRecord<CsvBookRowDto>();
                
                if (csvRow == null)
                {
                    results.Add(new CsvRowResultDto
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        ErrorMessage = "CSV row is null or empty",
                        Isbn = "",
                        Title = ""
                    });
                    continue;
                }
                
                // Validar campos requeridos
                if (string.IsNullOrWhiteSpace(csvRow.Isbn))
                {
                    results.Add(new CsvRowResultDto
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        ErrorMessage = "ISBN is required",
                        Isbn = csvRow.Isbn ?? "",
                        Title = csvRow.Title ?? ""
                    });
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(csvRow.Title))
                {
                    results.Add(new CsvRowResultDto
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        ErrorMessage = "Title is required",
                        Isbn = csvRow.Isbn,
                        Title = csvRow.Title ?? ""
                    });
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(csvRow.AuthorName))
                {
                    results.Add(new CsvRowResultDto
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        ErrorMessage = "AuthorName is required",
                        Isbn = csvRow.Isbn,
                        Title = csvRow.Title
                    });
                    continue;
                }
                
                if (csvRow.PublicationYear < 1000 || csvRow.PublicationYear > 2100)
                {
                    results.Add(new CsvRowResultDto
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        ErrorMessage = "PublicationYear must be between 1000 and 2100",
                        Isbn = csvRow.Isbn,
                        Title = csvRow.Title
                    });
                    continue;
                }
                
                // Crear CreateBookDto
                var createDto = new CreateBookDto
                {
                    Isbn = csvRow.Isbn.Trim(),
                    Title = csvRow.Title.Trim(),
                    PublicationYear = csvRow.PublicationYear,
                    AuthorName = csvRow.AuthorName.Trim()
                };
                
                // Intentar crear libro (reutilizar lógica existente)
                try
                {
                    var book = await CreateBookAsync(createDto);
                    
                    results.Add(new CsvRowResultDto
                    {
                        RowNumber = rowNumber,
                        IsSuccess = true,
                        BookId = book.Id,
                        Isbn = csvRow.Isbn,
                        Title = csvRow.Title
                    });
                }
                catch (ArgumentException ex) when (ex.ParamName == "Isbn")
                {
                    results.Add(new CsvRowResultDto
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        ErrorMessage = $"Invalid ISBN: {ex.Message}",
                        Isbn = csvRow.Isbn,
                        Title = csvRow.Title
                    });
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint") == true || 
                                                    ex.InnerException?.Message.Contains("Isbn") == true)
                {
                    results.Add(new CsvRowResultDto
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        ErrorMessage = "A book with this ISBN already exists",
                        Isbn = csvRow.Isbn,
                        Title = csvRow.Title
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new CsvRowResultDto
                    {
                        RowNumber = rowNumber,
                        IsSuccess = false,
                        ErrorMessage = ex.Message,
                        Isbn = csvRow.Isbn,
                        Title = csvRow.Title
                    });
                }
            }
            catch (CsvHelperException ex)
            {
                // Error al parsear la fila del CSV
                results.Add(new CsvRowResultDto
                {
                    RowNumber = rowNumber,
                    IsSuccess = false,
                    ErrorMessage = $"CSV parsing error: {ex.Message}",
                    Isbn = "",
                    Title = ""
                });
            }
            catch (Exception ex)
            {
                // Error general
                results.Add(new CsvRowResultDto
                {
                    RowNumber = rowNumber,
                    IsSuccess = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                    Isbn = "",
                    Title = ""
                });
            }
        }
        
        return new CsvUploadResultDto
        {
            TotalRows = rowNumber,
            SuccessfulRows = results.Count(r => r.IsSuccess),
            FailedRows = results.Count(r => !r.IsSuccess),
            Results = results
        };
    }
}

