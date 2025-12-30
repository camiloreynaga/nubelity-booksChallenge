using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.DTOs.Books;
using Infrastructure;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remover el DbContext existente
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<BooksDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Crear una nueva conexión SQLite en memoria para testing
            var connectionString = "Data Source=:memory:;Cache=Shared";
            var connection = new SqliteConnection(connectionString);
            connection.Open();

            services.AddSingleton(connection);
            services.AddDbContext<BooksDbContext>(options =>
            {
                options.UseSqlite(connection);
            });

            // Asegurar que la BD se inicialice
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<BooksDbContext>();
                db.Database.EnsureCreated();

                // Seed user
                var existingUser = db.Users.FirstOrDefault(u => u.Username == "admin");
                if (existingUser == null)
                {
                    var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
                    var adminUser = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = "admin",
                        PasswordHash = passwordHash
                    };
                    db.Users.Add(adminUser);
                    db.SaveChanges();
                }
            }
        });
    }
}

public class BooksControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BooksControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task BooksController_CreateBook_WithoutToken_ShouldReturn401()
    {
        // Arrange
        var dto = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9",
            Title = "Test Book",
            PublicationYear = 2023,
            AuthorName = "Test Author"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(dto),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/books", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BooksController_CreateBook_WithValidToken_ShouldNotReturn401()
    {
        // Arrange - Login para obtener token
        var loginDto = new { username = "admin", password = "admin123" };
        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginDto),
            Encoding.UTF8,
            "application/json"
        );

        var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
        
        // Si el login falla, puede ser por problemas de inicialización de BD en testing
        // En ese caso, el test no puede continuar, pero al menos verificamos que el endpoint responde
        if (loginResponse.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            // El test falla pero con un mensaje más claro
            Assert.Fail($"Login failed with status {loginResponse.StatusCode}. Response: {errorContent}");
        }

        var loginResult = await loginResponse.Content.ReadAsStringAsync();
        var loginData = JsonSerializer.Deserialize<JsonElement>(loginResult);
        var token = loginData.GetProperty("token").GetString();

        Assert.NotNull(token);

        // Configurar token en el cliente
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        // Crear libro con token
        var bookDto = new CreateBookDto
        {
            Isbn = "978-0-123456-78-9",
            Title = "Test Book",
            PublicationYear = 2023,
            AuthorName = "Test Author"
        };

        var bookContent = new StringContent(
            JsonSerializer.Serialize(bookDto),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/books", bookContent);

        // Assert
        // Lo importante es que NO sea 401 (autenticación funcionó)
        // Puede ser 400 (ISBN inválido), 500 (servicio externo), o 201 (éxito)
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        
        // Si es éxito, verificar estructura de respuesta
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var book = JsonSerializer.Deserialize<JsonElement>(responseContent);
            Assert.True(book.TryGetProperty("id", out _));
            Assert.True(book.TryGetProperty("title", out var titleProp));
            Assert.Equal("Test Book", titleProp.GetString());
        }
    }
}

