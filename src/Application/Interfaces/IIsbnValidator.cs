namespace Application.Interfaces;

/// <summary>
/// Interface for ISBN validation service.
/// This will be implemented in Slice 5 using SOAP.
/// </summary>
public interface IIsbnValidator
{
    /// <summary>
    /// Validates an ISBN.
    /// </summary>
    /// <param name="isbn">The ISBN to validate</param>
    /// <returns>True if the ISBN is valid, false otherwise</returns>
    Task<bool> ValidateIsbnAsync(string isbn);
}

