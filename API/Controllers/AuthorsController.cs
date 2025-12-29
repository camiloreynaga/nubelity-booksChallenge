using Application.DTOs;
using Application.DTOs.Authors;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;

    public AuthorsController(IAuthorService authorService)
    {
        _authorService = authorService;
    }

    [HttpPost]
    [Authorize] // Requiere autenticación JWT
    [ProducesResponseType(typeof(AuthorResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthorResponseDto>> CreateAuthor(CreateAuthorDto dto)
    {
        try
        {
            var author = await _authorService.CreateAsync(dto);
            return Created($"/api/authors/{author.Id}", author);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<AuthorResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<AuthorResponseDto>>> GetAuthors(
        [FromQuery] GetAuthorsQueryDto query)
    {
        var result = await _authorService.GetAllAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AuthorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthorResponseDto>> GetAuthor(Guid id)
    {
        var author = await _authorService.GetByIdAsync(id);
        
        if (author == null)
        {
            return NotFound();
        }
        
        return Ok(author);
    }

    [HttpPatch("{id}")]
    [Authorize] // Requiere autenticación JWT
    [ProducesResponseType(typeof(AuthorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthorResponseDto>> UpdateAuthor(Guid id, UpdateAuthorDto dto)
    {
        try
        {
            var author = await _authorService.UpdateAsync(id, dto);
            return Ok(author);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
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
    public async Task<IActionResult> DeleteAuthor(Guid id)
    {
        bool deleted = await _authorService.DeleteAsync(id);
        
        if (!deleted)
        {
            return NotFound();
        }
        
        return NoContent(); // 204 No Content
    }
}

