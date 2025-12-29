using Application.Interfaces;
using System.Text.Json;

namespace Infrastructure;

/// <summary>
/// Implementation of ICoverUrlService using Open Library REST API.
/// </summary>
public class OpenLibraryCoverService : ICoverUrlService
{
    private readonly HttpClient _httpClient;
    private const string OpenLibraryApiUrl = "https://openlibrary.org/api/books";

    public OpenLibraryCoverService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<string?> GetCoverUrlAsync(string isbn)
    {
        try
        {
            // Limpiar ISBN (remover guiones y espacios)
            string cleanIsbn = isbn.Replace("-", "").Replace(" ", "");
            
            string url = $"{OpenLibraryApiUrl}?bibkeys=ISBN:{cleanIsbn}&format=json";
            
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(json);
            
            // Buscar la clave que empieza con "ISBN:"
            string isbnKey = $"ISBN:{cleanIsbn}";
            if (jsonDoc.RootElement.TryGetProperty(isbnKey, out var bookData))
            {
                // Intentar obtener thumbnail_url o cover_url
                if (bookData.TryGetProperty("thumbnail_url", out var thumbnailUrl))
                {
                    return thumbnailUrl.GetString();
                }
                
                // Si no hay thumbnail, intentar construir URL de cover
                // Open Library usa: https://covers.openlibrary.org/b/isbn/{isbn}-M.jpg
                return $"https://covers.openlibrary.org/b/isbn/{cleanIsbn}-M.jpg";
            }

            return null;
        }
        catch (Exception)
        {
            // En caso de error (servicio caído, timeout, etc.), retornar null
            // El libro se puede crear sin cover URL
            return null;
        }
    }
}

