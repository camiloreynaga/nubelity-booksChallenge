using Application.DTOs;
using Application.DTOs.Books;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IIsbnValidator _isbnValidator;

    public BooksController(IBookService bookService, IIsbnValidator isbnValidator)
    {
        _bookService = bookService;
        _isbnValidator = isbnValidator;
    }

    [HttpPost]
    [Authorize] // Requiere autenticación JWT
    [ProducesResponseType(typeof(BookResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BookResponseDto>> CreateBook(CreateBookDto dto)
    {
        try
        {
            var book = await _bookService.CreateBookAsync(dto);
            return Created($"/api/books/{book.Id}", book);
        }
        catch (ArgumentException ex) when (ex.ParamName == "Isbn")
        {
            return BadRequest(new { error = "Invalid ISBN", details = ex.Message });
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint") == true || 
                                            ex.InnerException?.Message.Contains("Isbn") == true)
        {
            // Manejo específico para ISBN duplicado
            return BadRequest(new { error = "A book with this ISBN already exists." });
        }
        catch (Exception ex)
        {
            // Manejo de errores (se mejorará en fases siguientes con ProblemDetails)
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<BookResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<BookResponseDto>>> GetBooks(
        [FromQuery] GetBooksQueryDto query)
    {
        var result = await _bookService.GetAllAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponseDto>> GetBook(Guid id)
    {
        var book = await _bookService.GetByIdAsync(id);
        
        if (book == null)
        {
            return NotFound();
        }
        
        return Ok(book);
    }

    [HttpGet("validation/{isbn}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> ValidateIsbn(string isbn)
    {
        var isValid = await _isbnValidator.ValidateIsbnAsync(isbn);
        
        return Ok(new { isbn, isValid });
    }

    [HttpPatch("{id}")]
    [Authorize] // Requiere autenticación JWT
    [ProducesResponseType(typeof(BookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BookResponseDto>> UpdateBook(Guid id, UpdateBookDto dto)
    {
        try
        {
            var book = await _bookService.UpdateAsync(id, dto);
            return Ok(book);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex) when (ex.ParamName == "Isbn")
        {
            return BadRequest(new { error = "Invalid ISBN", details = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint") == true || 
                                            ex.InnerException?.Message.Contains("Isbn") == true)
        {
            return BadRequest(new { error = "A book with this ISBN already exists." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize] // Requiere autenticación JWT
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteBook(Guid id)
    {
        bool deleted = await _bookService.DeleteAsync(id);
        
        if (!deleted)
        {
            return NotFound();
        }
        
        return NoContent(); // 204 No Content
    }

    [HttpPost("upload")]
    [Authorize] // Requiere autenticación JWT
    [ProducesResponseType(typeof(CsvUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CsvUploadResultDto>> UploadBooksCsv(IFormFile file)
    {
        // Validar que se proporcionó un archivo
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded or file is empty" });
        }
        
        // Validar extensión (opcional)
        var allowedExtensions = new[] { ".csv" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest(new { error = "Invalid file type. Only CSV files are allowed." });
        }
        
        // Validar tamaño máximo (opcional, ej: 10MB)
        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { error = $"File size exceeds maximum allowed size of {maxFileSize / 1024 / 1024}MB" });
        }
        
        try
        {
            // Procesar CSV
            using var stream = file.OpenReadStream();
            var result = await _bookService.CreateBooksFromCsvAsync(stream);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Error processing CSV file: {ex.Message}" });
        }
    }
}

