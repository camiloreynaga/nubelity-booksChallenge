using Infrastructure;

namespace Application.Tests;

public class TextNormalizerTests
{
    private readonly TextNormalizer _normalizer;

    public TextNormalizerTests()
    {
        _normalizer = new TextNormalizer();
    }

    [Fact]
    public void Normalize_WithLowerCase_ShouldConvertToUpperCase()
    {
        // Arrange
        var input = "hola mundo";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("HOLA MUNDO", result);
    }

    [Fact]
    public void Normalize_WithNumbers_ShouldRemoveNumbers()
    {
        // Arrange
        var input = "ABC123DEF456";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("ABCDEF", result);
    }

    [Fact]
    public void Normalize_WithAccentedCharacters_ShouldReplaceWithNonAccented()
    {
        // Arrange
        var input = "José María";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("JOSE MARIA", result);
    }

    [Fact]
    public void Normalize_WithNino_ShouldReplaceNWithN()
    {
        // Arrange
        var input = "niño";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("NINO", result);
    }

    [Fact]
    public void Normalize_WithCafe_ShouldReplaceEWithE()
    {
        // Arrange
        var input = "café";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("CAFE", result);
    }

    [Fact]
    public void Normalize_WithFrancois_ShouldReplaceCWithC()
    {
        // Arrange
        var input = "François";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("FRANCOIS", result);
    }

    [Fact]
    public void Normalize_WithMultipleSpaces_ShouldNormalizeToSingleSpace()
    {
        // Arrange
        var input = "  HOLA   MUNDO  ";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("HOLA MUNDO", result);
    }

    [Fact]
    public void Normalize_WithMultipleSpacesAndAccents_ShouldNormalizeCorrectly()
    {
        // Arrange
        var input = "Múltiples    espacios";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("MULTIPLES ESPACIOS", result);
    }

    [Fact]
    public void Normalize_WithCombinedCase_ShouldNormalizeCorrectly()
    {
        // Arrange
        var input = "José123 María456";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("JOSE MARIA", result);
    }

    [Fact]
    public void Normalize_WithCombinedCaseAndSpaces_ShouldNormalizeCorrectly()
    {
        // Arrange
        var input = "  El niño 789  ";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("EL NINO", result);
    }

    [Fact]
    public void Normalize_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var input = "";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Normalize_WithOnlySpaces_ShouldReturnEmptyString()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Normalize_WithOnlyNumbers_ShouldReturnEmptyString()
    {
        // Arrange
        var input = "123456";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Normalize_WithNull_ShouldReturnEmptyString()
    {
        // Arrange
        string? input = null;

        // Act
        var result = _normalizer.Normalize(input!);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Normalize_WithAllAccentedVowels_ShouldReplaceAll()
    {
        // Arrange
        var input = "áàäâ éèëê íìïî óòöô úùüû";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("AAAA EEEE IIII OOOO UUUU", result);
    }

    [Fact]
    public void Normalize_WithComplexExample_ShouldNormalizeCorrectly()
    {
        // Arrange
        var input = "José María 123";

        // Act
        var result = _normalizer.Normalize(input);

        // Assert
        Assert.Equal("JOSE MARIA", result);
    }
}

