namespace Application.Interfaces;

/// <summary>
/// Interface for cover URL service.
/// This will be implemented in Slice 5 using REST API (Open Library).
/// </summary>
public interface ICoverUrlService
{
    /// <summary>
    /// Gets the cover URL for a book by its ISBN.
    /// </summary>
    /// <param name="isbn">The ISBN of the book</param>
    /// <returns>The URL of the book cover, or null if not found</returns>
    Task<string?> GetCoverUrlAsync(string isbn);
}

