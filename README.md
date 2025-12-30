# Books Challenge API

API REST para gestión de libros y autores desarrollada con ASP.NET Core 8.0, siguiendo principios de Clean Architecture.

> **🐳 Docker First**: Este proyecto está completamente containerizado. Puedes ejecutar **desarrollo, testing y producción** usando solo Docker, sin necesidad de instalar .NET SDK localmente.

## 🚀 Características

- **Autenticación JWT**: Endpoints protegidos con autenticación basada en tokens
- **Validación de ISBN**: Validación de ISBN vía servicio SOAP externo
- **Portadas de libros**: Obtención automática de portadas desde Open Library (REST)
- **Normalización de texto**: Normalización automática de títulos y nombres de autores
- **Carga masiva**: Importación de libros desde archivos CSV
- **Paginación**: Listado paginado de libros y autores
- **Cascade Delete**: Eliminación en cascada de libros al eliminar autores
- **Manejo de excepciones global**: Middleware con ProblemDetails (RFC 7807)

## 📋 Stack Tecnológico

- **.NET 8.0**: Framework principal
- **ASP.NET Core**: Framework web
- **Entity Framework Core**: ORM para acceso a datos
- **SQLite in-memory**: Base de datos en memoria
- **JWT Bearer**: Autenticación
- **Swagger/OpenAPI**: Documentación de API
- **xUnit**: Framework de testing
- **Docker**: Containerización

## 🏗️ Arquitectura

El proyecto sigue Clean Architecture Simple con las siguientes capas:

- **API**: Controllers, middleware, configuración
- **Application**: DTOs, interfaces de servicios
- **Infrastructure**: Implementaciones de servicios, DbContext, entidades
- **Tests**: Tests unitarios e integración

## 🛠️ Instalación y Ejecución

### Prerrequisitos

**🐳 Opción 1: Todo con Docker (Recomendado)**

- Docker Desktop o Docker Engine
- Docker Compose (incluido en Docker Desktop)
- **No requiere .NET SDK instalado** - Todo se ejecuta en contenedores

**💻 Opción 2: Desarrollo Local**

- .NET 8.0 SDK

### 🐳 Ejecución con Docker (Recomendado)

El proyecto está completamente containerizado y puede ejecutarse usando Docker para desarrollo, testing y producción.

#### 1. Clonar el repositorio:

```bash
git clone <repository-url>
cd books-challenge
```

#### 2. Ejecutar con Docker Compose

**Ejecutar en modo desarrollo:**

```bash
docker-compose up books-api-dev
```

Esto iniciará el contenedor en el puerto `8080` con el entorno de desarrollo.

**Ejecutar en modo producción:**

```bash
docker-compose --profile production up books-api-prod
```

Esto iniciará el contenedor en el puerto `8081` con el entorno de producción.

**Detener los contenedores:**

```bash
docker-compose down
```

**Ver logs:**

```bash
docker-compose logs -f
```

#### 3. Usando Docker directamente (sin Compose)

**Construir la imagen:**

```bash
docker build -t books-api .
```

**Ejecutar el contenedor:**

```bash
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development books-api
```

**Ejecutar en segundo plano:**

```bash
docker run -d -p 8080:8080 --name books-api books-api
```

**Ver logs del contenedor:**

```bash
docker logs -f books-api
```

**Detener el contenedor:**

```bash
docker stop books-api
docker rm books-api
```

#### 4. Acceder a la API:

Una vez iniciado el contenedor, accede a:

```
http://localhost:8080/swagger
```

**Notas sobre Docker:**

- El puerto puede variar según la configuración. Verificar `Dockerfile` y `docker-compose.yml`.
- En modo producción con docker-compose, el puerto es `8081`.
- La API en Docker solo expone HTTP (no HTTPS).
- **Todo el flujo de trabajo puede realizarse con Docker**: desarrollo, testing y producción.

### 💻 Ejecución Local (Sin Docker)

Si prefieres ejecutar sin Docker, necesitas .NET 8.0 SDK:

1. Clonar el repositorio:

```bash
git clone <repository-url>
cd books-challenge
```

2. Restaurar dependencias:

```bash
dotnet restore
```

3. Ejecutar la aplicación:

```bash
dotnet run --project API
```

4. Acceder a Swagger:

```
https://localhost:7031/swagger
```

## 🔐 Autenticación

La API utiliza autenticación JWT. Para acceder a endpoints protegidos:

1. **Obtener token:**

```bash
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123"
}
```

2. **Usar token en requests:**

```
Authorization: Bearer <token>
```

**Credenciales por defecto:**

- Username: `admin`
- Password: `admin123`

**Nota:** El token expira después de 1 hora.

## 📚 Endpoints Principales

### Autenticación

- `POST /api/auth/login` - Iniciar sesión y obtener token JWT

### Libros

- `GET /api/books` - Listar libros (paginado, filtros por título y autor)
- `GET /api/books/{id}` - Obtener libro por ID
- `POST /api/books` - Crear libro (requiere autenticación)
- `PATCH /api/books/{id}` - Actualizar libro parcialmente (requiere autenticación)
- `DELETE /api/books/{id}` - Eliminar libro (requiere autenticación)
- `GET /api/books/validation/{isbn}` - Validar ISBN
- `POST /api/books/upload` - Cargar libros desde CSV (requiere autenticación)

### Autores

- `GET /api/authors` - Listar autores (paginado)
- `GET /api/authors/{id}` - Obtener autor por ID
- `POST /api/authors` - Crear autor (requiere autenticación)
- `PATCH /api/authors/{id}` - Actualizar autor parcialmente (requiere autenticación)
- `DELETE /api/authors/{id}` - Eliminar autor y sus libros (requiere autenticación)

## 📝 Ejemplos de Uso

### Crear un libro

```bash
POST /api/books
Authorization: Bearer <token>
Content-Type: application/json

{
  "isbn": "978-0-316-76948-0",
  "title": "El niño",
  "publicationYear": 2023,
  "authorName": "José María"
}
```

### Listar libros con filtros

```bash
GET /api/books?page=1&pageSize=10&title=el%20niño&authorName=JOSE%20MARIA
```

### Cargar libros desde CSV

```bash
POST /api/books/upload
Authorization: Bearer <token>
Content-Type: multipart/form-data

file: <archivo.csv>
```

**Formato CSV:**

```csv
ISBN,Title,PublicationYear,AuthorName
978-0-316-76948-0,Book 1,2023,Author 1
978-0-123456-78-9,Book 2,2022,Author 2
```

## 🧪 Testing

### 🐳 Testing con Docker (Recomendado)

El proyecto puede ejecutar todos los tests usando Docker, sin necesidad de tener .NET SDK instalado localmente.

#### Opción 1: Ejecutar tests unitarios dentro de un contenedor Docker

**Linux/Mac:**

```bash
docker run --rm -v ${PWD}:/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test
```

**Windows PowerShell:**

```powershell
docker run --rm -v ${PWD}:/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test
```

**Windows CMD:**

```cmd
docker run --rm -v %CD%:/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test
```

**Ejecutar tests de un proyecto específico:**

```bash
# Tests de aplicación
docker run --rm -v ${PWD}:/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test tests/Application.Tests

# Tests de API
docker run --rm -v ${PWD}:/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test tests/Api.Tests
```

#### Opción 2: Ejecutar tests contra la API en Docker

1. **Iniciar la API en Docker:**

```bash
docker-compose up books-api-dev
```

O usando Docker directamente:

```bash
docker run -d -p 8080:8080 --name books-api books-api
```

2. **Probar la API manualmente:**

Una vez que la API esté corriendo en `http://localhost:8080`, puedes probarla manualmente:

**Obtener token JWT:**

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

**Probar endpoint protegido:**

```bash
curl -X GET http://localhost:8080/api/books \
  -H "Authorization: Bearer <token>"
```

**Nota:** Los scripts de testing (`test-api.ps1` y `run-tests.ps1`) están configurados para detectar la API en los puertos locales de desarrollo (`http://localhost:5279` o `https://localhost:7031`). Para probar con Docker en el puerto 8080, puedes usar curl, Postman, o modificar los scripts para incluir el puerto 8080.

#### Opción 3: Testing de integración con Docker Compose

Puedes crear un servicio adicional en `docker-compose.yml` para ejecutar tests:

```yaml
services:
  test:
    build:
      context: .
      dockerfile: Dockerfile.test # Necesitarías crear este Dockerfile
    volumes:
      - .:/src
    command: dotnet test
```

Luego ejecutar:

```bash
docker-compose run --rm test
```

**Nota:** Esta opción requiere crear un Dockerfile específico para testing.

### 💻 Testing Local (Sin Docker)

Si prefieres ejecutar tests localmente con .NET SDK:

**Ejecutar todos los tests:**

```bash
dotnet test
```

**Ejecutar tests de un proyecto específico:**

```bash
dotnet test tests/Application.Tests
dotnet test tests/Api.Tests
```

## 📦 Estructura del Proyecto

```
books-challenge/
├── API/                    # Capa de presentación
│   ├── Controllers/        # Controladores REST
│   ├── Middleware/         # Middleware personalizado
│   └── Program.cs          # Configuración de la aplicación
├── src/
│   ├── Application/        # Capa de aplicación
│   │   ├── DTOs/          # Data Transfer Objects
│   │   └── Interfaces/    # Interfaces de servicios
│   └── Infrastructure/     # Capa de infraestructura
│       ├── Entities/      # Entidades de dominio
│       └── Services/       # Implementaciones de servicios
├── tests/                  # Tests
│   ├── Application.Tests/ # Tests unitarios de aplicación
│   └── Api.Tests/         # Tests de integración de API
├── Dockerfile             # Configuración Docker (multi-stage)
├── docker-compose.yml     # Orquestación Docker (dev y prod)
└── README.md             # Este archivo
```

## 🔧 Configuración

### appsettings.json

```json
{
  "Jwt": {
    "SecretKey": "your-secret-key-minimum-32-characters",
    "Issuer": "BooksChallenge",
    "Audience": "BooksChallenge",
    "ExpirationMinutes": 60
  }
}
```

**Nota:** En producción, usar variables de entorno o Azure Key Vault para la clave secreta.

## 🐛 Manejo de Errores

La API utiliza ProblemDetails (RFC 7807) para respuestas de error consistentes:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred",
  "status": 400,
  "detail": "Invalid ISBN",
  "instance": "/api/books"
}
```

El middleware de excepciones global captura todas las excepciones no manejadas y las convierte automáticamente en respuestas ProblemDetails.

### Tipos de Excepciones Manejadas

- **KeyNotFoundException** → 404 Not Found
- **ArgumentException** → 400 Bad Request
- **InvalidOperationException** → 400 Bad Request
- **UnauthorizedAccessException** → 401 Unauthorized
- **Otras excepciones** → 500 Internal Server Error

En modo desarrollo, se incluye información adicional como `traceId` y el nombre de la excepción.
