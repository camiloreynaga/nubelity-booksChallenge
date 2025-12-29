using Application.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace Infrastructure;

public class TextNormalizer : ITextNormalizer
{
    public string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // 1. Convertir a mayúsculas
        var normalized = text.ToUpperInvariant();

        // 2. Eliminar números
        normalized = new string(normalized.Where(c => !char.IsDigit(c)).ToArray());

        // 3. Reemplazar caracteres especiales y acentuados
        var sb = new StringBuilder(normalized);
        
        // Reemplazos de caracteres acentuados y especiales
        sb.Replace("Á", "A").Replace("À", "A").Replace("Ä", "A").Replace("Â", "A");
        sb.Replace("É", "E").Replace("È", "E").Replace("Ë", "E").Replace("Ê", "E");
        sb.Replace("Í", "I").Replace("Ì", "I").Replace("Ï", "I").Replace("Î", "I");
        sb.Replace("Ó", "O").Replace("Ò", "O").Replace("Ö", "O").Replace("Ô", "O");
        sb.Replace("Ú", "U").Replace("Ù", "U").Replace("Ü", "U").Replace("Û", "U");
        sb.Replace("Ñ", "N");
        sb.Replace("Ç", "C");
        
        normalized = sb.ToString();

        // 4. Normalizar espacios: reemplazar múltiples espacios por uno solo y trim
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }
}

