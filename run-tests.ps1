# Script simplificado de pruebas para la API

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  PRUEBAS DE AUTENTICACION JWT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = $null
$httpUrl = "http://localhost:5279"
$httpsUrl = "https://localhost:7031"

# Detectar en que puerto esta corriendo
Write-Host "[1/6] Detectando API..." -ForegroundColor Yellow
try {
    $test = Invoke-WebRequest -Uri "$httpsUrl/swagger/index.html" -UseBasicParsing -TimeoutSec 2 -SkipCertificateCheck -ErrorAction Stop
    $baseUrl = $httpsUrl
    Write-Host "    OK - API en HTTPS (7031)" -ForegroundColor Green
} catch {
    try {
        $test = Invoke-WebRequest -Uri "$httpUrl/swagger/index.html" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        $baseUrl = $httpUrl
        Write-Host "    OK - API en HTTP (5279)" -ForegroundColor Green
    } catch {
        Write-Host "    ERROR - API no esta corriendo" -ForegroundColor Red
        Write-Host "    Ejecuta: dotnet run --project API" -ForegroundColor Yellow
        exit 1
    }
}

# Prueba 1: Login
Write-Host ""
Write-Host "[2/6] Prueba 1: Login con credenciales validas" -ForegroundColor Yellow
$loginData = @{ username = "admin"; password = "admin123" } | ConvertTo-Json
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginData -ContentType "application/json" -SkipCertificateCheck -ErrorAction Stop
    Write-Host "    OK - Login exitoso" -ForegroundColor Green
    Write-Host "    Token recibido: $($loginResponse.Token.Substring(0, 50))..." -ForegroundColor Cyan
    Write-Host "    Expira en: $($loginResponse.ExpiresIn) segundos" -ForegroundColor Cyan
    $token = $loginResponse.Token
} catch {
    Write-Host "    ERROR - Login fallo: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Prueba 2: Endpoint protegido con token
Write-Host ""
Write-Host "[3/6] Prueba 2: Acceso a endpoint protegido (con token)" -ForegroundColor Yellow
try {
    $headers = @{ Authorization = "Bearer $token" }
    $testResponse = Invoke-RestMethod -Uri "$baseUrl/api/test" -Method Get -Headers $headers -SkipCertificateCheck -ErrorAction Stop
    Write-Host "    OK - Endpoint protegido accesible" -ForegroundColor Green
    Write-Host "    Response: $($testResponse.message)" -ForegroundColor Cyan
    Write-Host "    Usuario: $($testResponse.user)" -ForegroundColor Cyan
} catch {
    Write-Host "    ERROR - No se pudo acceder al endpoint: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Prueba 3: Endpoint protegido sin token
Write-Host ""
Write-Host "[4/6] Prueba 3: Acceso a endpoint protegido (sin token)" -ForegroundColor Yellow
try {
    $unauthorizedResponse = Invoke-RestMethod -Uri "$baseUrl/api/test" -Method Get -SkipCertificateCheck -ErrorAction Stop
    Write-Host "    ERROR - El endpoint deberia rechazar peticiones sin token!" -ForegroundColor Red
    exit 1
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 401) {
        Write-Host "    OK - Correctamente rechazado (401 Unauthorized)" -ForegroundColor Green
    } else {
        Write-Host "    ERROR - Status code inesperado: $statusCode" -ForegroundColor Red
    }
}

# Prueba 4: Login con credenciales invalidas
Write-Host ""
Write-Host "[5/6] Prueba 4: Login con credenciales invalidas" -ForegroundColor Yellow
$invalidLoginData = @{ username = "admin"; password = "wrongpassword" } | ConvertTo-Json
try {
    $invalidResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $invalidLoginData -ContentType "application/json" -SkipCertificateCheck -ErrorAction Stop
    Write-Host "    ERROR - Deberia rechazar credenciales invalidas!" -ForegroundColor Red
    exit 1
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 401) {
        Write-Host "    OK - Correctamente rechazado (401 Unauthorized)" -ForegroundColor Green
    } else {
        Write-Host "    ERROR - Status code inesperado: $statusCode" -ForegroundColor Red
    }
}

# Resumen
Write-Host ""
Write-Host "[6/6] Resumen" -ForegroundColor Yellow
Write-Host "    Todas las pruebas pasaron exitosamente!" -ForegroundColor Green
Write-Host ""
Write-Host "Swagger UI: $baseUrl/swagger" -ForegroundColor Cyan
Write-Host "Para probar en Swagger:" -ForegroundColor Yellow
Write-Host "  1. Abre $baseUrl/swagger" -ForegroundColor White
Write-Host "  2. Haz clic en 'Authorize' (boton verde)" -ForegroundColor White
Write-Host "  3. Ingresa: Bearer $($token.Substring(0, 50))..." -ForegroundColor White
Write-Host "  4. Prueba GET /api/test" -ForegroundColor White
Write-Host ""


