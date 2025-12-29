# Script de prueba para la API de autenticacion JWT

Write-Host ""
Write-Host "=== PRUEBA DE AUTENTICACION JWT ===" -ForegroundColor Cyan
Write-Host ""

# Esperar a que la API inicie
Write-Host "Esperando a que la API inicie..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

# URLs a probar
$httpUrl = "http://localhost:5279"
$httpsUrl = "https://localhost:7031"

# Datos de login
$loginData = @{
    username = "admin"
    password = "admin123"
} | ConvertTo-Json

$baseUrl = $null

# Probar HTTPS primero
try {
    Write-Host "Probando conexion HTTPS (7031)..." -ForegroundColor Yellow
    $test = Invoke-WebRequest -Uri "$httpsUrl/swagger/index.html" -UseBasicParsing -TimeoutSec 3 -SkipCertificateCheck -ErrorAction Stop
    $baseUrl = $httpsUrl
    Write-Host "OK - API detectada en HTTPS (7031)" -ForegroundColor Green
} catch {
    try {
        Write-Host "Probando conexion HTTP (5279)..." -ForegroundColor Yellow
        $test = Invoke-WebRequest -Uri "$httpUrl/swagger/index.html" -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
        $baseUrl = $httpUrl
        Write-Host "OK - API detectada en HTTP (5279)" -ForegroundColor Green
    } catch {
        Write-Host "ERROR - No se pudo conectar a la API" -ForegroundColor Red
        Write-Host "Asegurate de ejecutar: dotnet run --project API" -ForegroundColor Yellow
        exit 1
    }
}

Write-Host ""
Write-Host "=== PRUEBA 1: LOGIN ===" -ForegroundColor Cyan
Write-Host "POST $baseUrl/api/auth/login" -ForegroundColor Gray

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginData -ContentType "application/json" -SkipCertificateCheck -ErrorAction Stop
    
    Write-Host "OK - Login exitoso!" -ForegroundColor Green
    $tokenPreview = $loginResponse.Token.Substring(0, [Math]::Min(80, $loginResponse.Token.Length))
    Write-Host "  Token (primeros 80 chars): $tokenPreview..." -ForegroundColor Cyan
    Write-Host "  Expires in: $($loginResponse.ExpiresIn) seconds" -ForegroundColor Cyan
    
    $token = $loginResponse.Token
    
    Write-Host ""
    Write-Host "=== PRUEBA 2: ENDPOINT PROTEGIDO (con token valido) ===" -ForegroundColor Cyan
    Write-Host "GET $baseUrl/api/test" -ForegroundColor Gray
    
    $headers = @{
        Authorization = "Bearer $token"
    }
    
    $testResponse = Invoke-RestMethod -Uri "$baseUrl/api/test" -Method Get -Headers $headers -SkipCertificateCheck -ErrorAction Stop
    
    Write-Host "OK - Endpoint protegido accesible!" -ForegroundColor Green
    $responseJson = $testResponse | ConvertTo-Json -Compress
    Write-Host "  Response: $responseJson" -ForegroundColor Cyan
    
    Write-Host ""
    Write-Host "=== PRUEBA 3: ENDPOINT PROTEGIDO (sin token) ===" -ForegroundColor Cyan
    Write-Host "GET $baseUrl/api/test (sin Authorization header)" -ForegroundColor Gray
    
    try {
        $unauthorizedResponse = Invoke-RestMethod -Uri "$baseUrl/api/test" -Method Get -SkipCertificateCheck -ErrorAction Stop
        Write-Host "ERROR - El endpoint deberia requerir autenticacion!" -ForegroundColor Red
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 401) {
            Write-Host "OK - Correctamente rechazado (401 Unauthorized)" -ForegroundColor Green
        } else {
            Write-Host "ERROR - Error inesperado: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    Write-Host "=== PRUEBA 4: LOGIN CON CREDENCIALES INVALIDAS ===" -ForegroundColor Cyan
    Write-Host "POST $baseUrl/api/auth/login" -ForegroundColor Gray
    
    $invalidLoginData = @{
        username = "admin"
        password = "wrongpassword"
    } | ConvertTo-Json
    
    try {
        $invalidResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $invalidLoginData -ContentType "application/json" -SkipCertificateCheck -ErrorAction Stop
        Write-Host "ERROR - Deberia rechazar credenciales invalidas!" -ForegroundColor Red
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 401) {
            Write-Host "OK - Correctamente rechazado (401 Unauthorized)" -ForegroundColor Green
        } else {
            Write-Host "ERROR - Error inesperado: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    Write-Host "=== TODAS LAS PRUEBAS COMPLETADAS ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Swagger UI disponible en: $baseUrl/swagger" -ForegroundColor Cyan
    Write-Host "Para probar desde Swagger:" -ForegroundColor Yellow
    Write-Host "  1. Haz clic en 'Authorize' (boton verde)" -ForegroundColor Yellow
    $tokenShort = $token.Substring(0, [Math]::Min(50, $token.Length))
    Write-Host "  2. Ingresa: Bearer $tokenShort..." -ForegroundColor Yellow
    Write-Host "  3. Prueba el endpoint GET /api/test" -ForegroundColor Yellow
    
} catch {
    Write-Host "ERROR - Error en las pruebas: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "  Response: $responseBody" -ForegroundColor Red
        } catch {
            Write-Host "  No se pudo leer la respuesta" -ForegroundColor Red
        }
    }
    exit 1
}
