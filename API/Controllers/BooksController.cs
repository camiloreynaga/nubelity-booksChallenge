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
}

