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

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
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
}

