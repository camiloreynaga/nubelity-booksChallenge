using Application.Interfaces;
using System.Text;
using System.Xml;

namespace Infrastructure;

/// <summary>
/// Implementation of IIsbnValidator using SOAP service.
/// </summary>
public class IsbnSoapValidator : IIsbnValidator
{
    private readonly HttpClient _httpClient;
    private const string SoapEndpoint = "http://webservices.daehosting.com/services/isbnservice.wso";
    private const string SoapNamespace = "http://webservices.daehosting.com/services/isbnservice.wso";

    public IsbnSoapValidator(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<bool> ValidateIsbnAsync(string isbn)
    {
        try
        {
            // Limpiar ISBN
            string cleanIsbn = isbn.Replace("-", "").Replace(" ", "");

            // Intentar validar como ISBN-13 primero
            bool isValid13 = await ValidateIsbn13Async(cleanIsbn);
            if (isValid13)
            {
                return true;
            }

            // Si no es ISBN-13 válido, intentar ISBN-10
            return await ValidateIsbn10Async(cleanIsbn);
        }
        catch (Exception)
        {
            // En caso de error, retornar false (rechazar ISBN)
            return false;
        }
    }

    private async Task<bool> ValidateIsbn13Async(string isbn)
    {
        try
        {
            // Construir SOAP envelope para ISBN-13
            // El ISBN ya está limpio (solo números), pero escapamos por seguridad
            string escapedIsbn = System.Security.SecurityElement.Escape(isbn);
            string soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>
        <IsValidISBN13 xmlns=""{SoapNamespace}"">
            <sISBN>{escapedIsbn}</sISBN>
        </IsValidISBN13>
    </soap:Body>
</soap:Envelope>";

            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", $"{SoapNamespace}IsValidISBN13");

            var response = await _httpClient.PostAsync(SoapEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Parsear respuesta SOAP
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(responseContent);
            
            var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
            namespaceManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            namespaceManager.AddNamespace("ns", SoapNamespace);

            var resultNode = xmlDoc.SelectSingleNode("//ns:IsValidISBN13Result", namespaceManager);
            
            if (resultNode != null && bool.TryParse(resultNode.InnerText, out bool isValid))
            {
                return isValid;
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> ValidateIsbn10Async(string isbn)
    {
        try
        {
            // Construir SOAP envelope para ISBN-10
            // El ISBN ya está limpio (solo números), pero escapamos por seguridad
            string escapedIsbn = System.Security.SecurityElement.Escape(isbn);
            string soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soap:Body>
        <IsValidISBN10 xmlns=""{SoapNamespace}"">
            <sISBN>{escapedIsbn}</sISBN>
        </IsValidISBN10>
    </soap:Body>
</soap:Envelope>";

            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", $"{SoapNamespace}IsValidISBN10");

            var response = await _httpClient.PostAsync(SoapEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Parsear respuesta SOAP
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(responseContent);
            
            var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
            namespaceManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            namespaceManager.AddNamespace("ns", SoapNamespace);

            var resultNode = xmlDoc.SelectSingleNode("//ns:IsValidISBN10Result", namespaceManager);
            
            if (resultNode != null && bool.TryParse(resultNode.InnerText, out bool isValid))
            {
                return isValid;
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

