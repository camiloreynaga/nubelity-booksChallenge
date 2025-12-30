using API.Middleware;
using Application.Interfaces;
using Infrastructure;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Data.Common;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Books Challenge API", 
        Version = "v1",
        Description = "API REST para gestión de libros y autores con autenticación JWT, validación de ISBN vía SOAP, y obtención de portadas vía REST.",
        Contact = new OpenApiContact
        {
            Name = "Books Challenge",
            Email = "support@example.com"
        }
    });
    
    // Configurar JWT en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    // Incluir comentarios XML (opcional)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Crear una conexión SQLite en memoria compartida que se mantendrá abierta
// IMPORTANTE: SQLite en memoria requiere que la conexión se mantenga abierta
// Usamos Cache=Shared para que todas las conexiones compartan la misma base de datos
var connectionString = "Data Source=:memory:;Cache=Shared";
var persistentConnection = new SqliteConnection(connectionString);
persistentConnection.Open();

// Registrar la conexión como singleton para que se mantenga abierta durante toda la vida de la app
builder.Services.AddSingleton<DbConnection>(persistentConnection);

// Register DbContext with SQLite in-memory
// Entity Framework Core creará nuevas conexiones, pero todas compartirán la misma base de datos
// gracias a Cache=Shared, y la conexión persistente mantendrá la BD viva
builder.Services.AddDbContext<BooksDbContext>(options =>
    options.UseSqlite(connectionString));

// Configurar JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// Registrar IAuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Configurar HttpClient para servicios externos
builder.Services.AddHttpClient<OpenLibraryCoverService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10); // 10 segundos timeout
    });

builder.Services.AddHttpClient<IsbnSoapValidator>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15); // 15 segundos timeout para SOAP
    });

// Registrar ITextNormalizer
builder.Services.AddScoped<ITextNormalizer, TextNormalizer>();

// Registrar IBookService
builder.Services.AddScoped<IBookService, BookService>();

// Registrar IAuthorService
builder.Services.AddScoped<IAuthorService, AuthorService>();

// Servicios externos
builder.Services.AddScoped<IIsbnValidator, IsbnSoapValidator>();
builder.Services.AddScoped<ICoverUrlService, OpenLibraryCoverService>();

var app = builder.Build();

// Initialize database and seed user
// IMPORTANTE: Usar la conexión persistente para inicializar la BD
// Esto asegura que las tablas se creen en la misma conexión que se mantendrá abierta
var optionsBuilder = new DbContextOptionsBuilder<BooksDbContext>();
optionsBuilder.UseSqlite(persistentConnection);

using (var dbContext = new BooksDbContext(optionsBuilder.Options))
{
    // Ensure database is created - this creates all tables
    dbContext.Database.EnsureCreated();
    
    // Seed user if it doesn't exist
    try
    {
        var existingUser = dbContext.Users.FirstOrDefault(u => u.Username == "admin");
        if (existingUser == null)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                PasswordHash = passwordHash
            };
            dbContext.Users.Add(adminUser);
            dbContext.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        // Log error but don't fail startup
        Console.WriteLine($"Error seeding user: {ex.Message}");
    }
}

// Registrar un shutdown hook para cerrar la conexión cuando la aplicación termine
app.Lifetime.ApplicationStopping.Register(() =>
{
    persistentConnection?.Close();
    persistentConnection?.Dispose();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Registrar middleware de excepciones (ANTES de otros middlewares)
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication(); // Debe ir ANTES de UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();

// Hacer Program público para tests de integración
public partial class Program { }
