using Application.Interfaces;

namespace Infrastructure;

/// <summary>
/// Mock implementation of IIsbnValidator.
/// This will be implemented in Slice 5 using SOAP.
/// </summary>
public class IsbnSoapValidator : IIsbnValidator
{
    public Task<bool> ValidateIsbnAsync(string isbn)
    {
        // Por ahora, siempre retorna true
        // Se implementará en el Slice 5
        return Task.FromResult(true);
    }
}

