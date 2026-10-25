[CmdletBinding()]
param(
    [string]$ServidorApi
)

$ErrorActionPreference = 'Stop'

$rutaCliente = Split-Path -Parent $PSScriptRoot
$rutaEntorno = Join-Path $rutaCliente '.env.android.local'

if ([string]::IsNullOrWhiteSpace($ServidorApi) -and (Test-Path -LiteralPath $rutaEntorno))
{
    $lineaServidor = Get-Content -LiteralPath $rutaEntorno |
        Where-Object { $_ -match '^VITE_API_URL=' } |
        Select-Object -First 1

    if ($lineaServidor)
    {
        $ServidorApi = $lineaServidor.Substring('VITE_API_URL='.Length).Trim()
    }
}

$direccionServidor = $null

if (-not [Uri]::TryCreate($ServidorApi, [UriKind]::Absolute, [ref]$direccionServidor))
{
    throw 'Indica la API de la PC: npm run android:apk -- -ServidorApi http://IP-DE-TU-PC:5253'
}

if ($direccionServidor.Scheme -notin @('http', 'https') -or
    $direccionServidor.IsLoopback -or
    $direccionServidor.Host -eq '0.0.0.0' -or
    $direccionServidor.AbsolutePath -ne '/' -or
    $direccionServidor.Query -or
    $direccionServidor.Fragment -or
    $direccionServidor.UserInfo)
{
    throw 'Usa una dirección HTTP o HTTPS de la PC accesible desde el celular, sin rutas ni credenciales.'
}

$ServidorApi = $direccionServidor.GetLeftPart([UriPartial]::Authority)

$candidatosJava = @($env:JAVA_HOME)

foreach ($carpetaJava in @(
    (Join-Path $env:ProgramFiles 'Java'),
    (Join-Path $env:ProgramFiles 'Microsoft')
))
{
    if (Test-Path -LiteralPath $carpetaJava)
    {
        $candidatosJava += Get-ChildItem -LiteralPath $carpetaJava -Directory |
            Select-Object -ExpandProperty FullName
    }
}

$candidatosJava += Join-Path $env:ProgramFiles 'Android\Android Studio\jbr'
$rutaJava = $null

foreach ($candidatoJava in $candidatosJava)
{
    if ([string]::IsNullOrWhiteSpace($candidatoJava))
    {
        continue
    }

    $rutaVersionJava = Join-Path $candidatoJava 'release'
    $rutaEjecutableJava = Join-Path $candidatoJava 'bin\java.exe'

    if (-not (Test-Path -LiteralPath $rutaVersionJava) -or
        -not (Test-Path -LiteralPath $rutaEjecutableJava))
    {
        continue
    }

    $versionJava = [regex]::Match(
        [IO.File]::ReadAllText($rutaVersionJava),
        'JAVA_VERSION="(?<principal>\d+)'
    )

    if ($versionJava.Success)
    {
        $versionPrincipal = [int]$versionJava.Groups['principal'].Value

        if ($versionPrincipal -ge 21 -and $versionPrincipal -le 24)
        {
            $rutaJava = $candidatoJava
            break
        }
    }
}

if (-not $rutaJava)
{
    throw 'Este proyecto usa Gradle 8.14.3: instala un JDK entre 21 y 24, o indica su carpeta en JAVA_HOME.'
}

$rutaSdk = $env:ANDROID_HOME

if ([string]::IsNullOrWhiteSpace($rutaSdk))
{
    $rutaSdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk'
}

if (-not (Test-Path -LiteralPath (Join-Path $rutaSdk 'platforms\android-36\android.jar')))
{
    throw 'Falta Android SDK Platform 36. Instálalo desde SDK Manager de Android Studio.'
}

$entornoAnterior = @{
    JAVA_HOME = $env:JAVA_HOME
    ANDROID_HOME = $env:ANDROID_HOME
    VITE_API_URL = $env:VITE_API_URL
}

Push-Location $rutaCliente

try
{
    $env:JAVA_HOME = $rutaJava
    $env:ANDROID_HOME = $rutaSdk
    $env:VITE_API_URL = $ServidorApi

    Write-Host "API incluida en el APK: $ServidorApi"

    & node node_modules/typescript/bin/tsc --noEmit

    if ($LASTEXITCODE -ne 0)
    {
        throw 'No se pudo verificar TypeScript. No se generó un APK nuevo.'
    }

    & npm.cmd run build -- --mode android

    if ($LASTEXITCODE -ne 0)
    {
        throw 'No se pudo compilar el cliente web.'
    }

    & node node_modules/@capacitor/cli/bin/capacitor sync android

    if ($LASTEXITCODE -ne 0)
    {
        throw 'No se pudieron sincronizar los archivos con Android.'
    }

    & '.\android\gradlew.bat' -p '.\android' assembleDebug --no-daemon --console=plain

    if ($LASTEXITCODE -ne 0)
    {
        throw 'Gradle no pudo generar el APK de prueba.'
    }

    $rutaApkGenerado = Join-Path $rutaCliente 'android\app\build\outputs\apk\debug\app-debug.apk'
    $rutaDescargas = Join-Path $rutaCliente 'descargas'
    $rutaApkEntregado = Join-Path $rutaDescargas 'PacmanMultijugador-debug.apk'

    if (-not (Test-Path -LiteralPath $rutaApkGenerado))
    {
        throw 'La compilación terminó sin encontrar app-debug.apk.'
    }

    [IO.Directory]::CreateDirectory($rutaDescargas) | Out-Null
    Copy-Item -LiteralPath $rutaApkGenerado -Destination $rutaApkEntregado -Force

    $algoritmoHash = [Security.Cryptography.SHA256]::Create()
    $flujoApk = [IO.File]::OpenRead($rutaApkEntregado)

    try
    {
        $huellaSha256 = [BitConverter]::ToString($algoritmoHash.ComputeHash($flujoApk)).Replace('-', '')
    }
    finally
    {
        $flujoApk.Dispose()
        $algoritmoHash.Dispose()
    }

    @{
        archivo = 'PacmanMultijugador-debug.apk'
        aplicacion = 'com.pacman.multijugador'
        tipo = 'debug'
        servidorApi = $ServidorApi
        fechaUtc = [DateTime]::UtcNow.ToString('o')
        sha256 = $huellaSha256
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $rutaDescargas 'prueba-android.json') -Encoding UTF8

    Write-Host ''
    Write-Host 'APK generado correctamente:' -ForegroundColor Green
    Write-Host $rutaApkEntregado
    Write-Host 'Inicia PacmanApi con el perfil red-local y conecta el celular a la misma red Wi-Fi.'
}
finally
{
    Pop-Location

    foreach ($variableAnterior in $entornoAnterior.GetEnumerator())
    {
        [Environment]::SetEnvironmentVariable($variableAnterior.Key, $variableAnterior.Value, 'Process')
    }
}
