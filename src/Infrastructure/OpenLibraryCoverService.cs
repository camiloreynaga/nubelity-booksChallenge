using Application.Interfaces;

namespace Infrastructure;

/// <summary>
/// Mock implementation of ICoverUrlService.
/// This will be implemented in Slice 5 using REST API (Open Library).
/// </summary>
public class OpenLibraryCoverService : ICoverUrlService
{
    public Task<string?> GetCoverUrlAsync(string isbn)
    {
        // Por ahora, siempre retorna null
        // Se implementará en el Slice 5
        return Task.FromResult<string?>(null);
    }
}

