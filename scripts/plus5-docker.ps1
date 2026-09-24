<#
.SYNOPSIS
Starts, stops, restarts, or inspects the PLUS 5 local Docker Compose stack.

.DESCRIPTION
Uses the repository Docker Compose contract and validates the resulting HTTP endpoints.
When Docker Desktop reports the two known Windows stale runtime-path failures, the script
stops Docker Desktop and recoverably renames only the affected runtime directories before
retrying. That repair does not delete or reset images, containers, named volumes, project
files, or Docker settings.

.PARAMETER Action
Start (default), Stop, Restart, or Status.

.PARAMETER NoBuild
Starts the existing images without running a Compose build.

.PARAMETER SkipDockerDesktopRepair
Disables automatic recovery of the two known stale Docker Desktop runtime directories.

.EXAMPLE
.\scripts\plus5-docker.ps1

.EXAMPLE
.\scripts\plus5-docker.ps1 Start -NoBuild

.EXAMPLE
.\scripts\plus5-docker.ps1 Stop
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('Start', 'Stop', 'Restart', 'Status')]
    [string] $Action = 'Start',

    [switch] $NoBuild,

    [switch] $SkipDockerDesktopRepair,

    [ValidateRange(30, 600)]
    [int] $EngineTimeoutSeconds = 180,

    [ValidateRange(30, 900)]
    [int] $ComposeTimeoutSeconds = 300
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue) {
    $PSNativeCommandUseErrorActionPreference = $false
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$composeFile = Join-Path $repositoryRoot 'docker-compose.yml'
$environmentFile = Join-Path $repositoryRoot '.env'
$knownRuntimeErrorPattern = '(?is)(dockerInference|docker-secrets-engine).*(cannot be accessed|filename, directory name, or volume label syntax)'

function Write-Step {
    param([Parameter(Mandatory)][string] $Message)

    Write-Host "[PLUS 5 Docker] $Message" -ForegroundColor Cyan
}

function Assert-LastExitCode {
    param(
        [Parameter(Mandatory)][string] $Operation,
        [Parameter(Mandatory)][int] $ExitCode
    )

    if ($ExitCode -ne 0) {
        throw "$Operation failed with exit code $ExitCode."
    }
}

function Test-DockerEngine {
    & docker info --format '{{.ServerVersion}}' *> $null
    return $LASTEXITCODE -eq 0
}

function Wait-DockerEngine {
    param([Parameter(Mandatory)][int] $TimeoutSeconds)

    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        if (Test-DockerEngine) {
            return $true
        }

        Start-Sleep -Seconds 2
    }

    return $false
}

function Get-EnvironmentValues {
    $requiredNames = @(
        'PLUS5_SQL_SA_PASSWORD',
        'PLUS5_SQL_MIGRATION_PASSWORD',
        'PLUS5_SQL_APP_PASSWORD'
    )

    if (-not (Test-Path -LiteralPath $environmentFile -PathType Leaf)) {
        throw "Missing $environmentFile. Copy .env.example to .env and set the three local SQL passwords."
    }

    $values = @{}
    foreach ($line in Get-Content -LiteralPath $environmentFile) {
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)\s*$') {
            $name = $Matches[1]
            $value = $Matches[2].Trim()
            if ($value.Length -ge 2 -and $value.StartsWith('"') -and $value.EndsWith('"')) {
                $value = $value.Substring(1, $value.Length - 2)
            }

            $values[$name] = $value
        }
    }

    $missingNames = @($requiredNames | Where-Object {
        -not $values.ContainsKey($_) -or [string]::IsNullOrWhiteSpace([string] $values[$_])
    })
    if ($missingNames.Count -gt 0) {
        throw "The local .env file has missing values: $($missingNames -join ', ')."
    }

    foreach ($name in $requiredNames) {
        $value = [string] $values[$name]
        if ($value.Contains("'") -or $value.Contains(';')) {
            throw "$name contains a character that is not supported by the local SQL initialization contract (' or ;)."
        }
    }

    $distinctCount = @($requiredNames | ForEach-Object { [string] $values[$_] } | Sort-Object -Unique).Count
    if ($distinctCount -ne $requiredNames.Count) {
        throw 'The three local SQL passwords in .env must be different.'
    }
}

function Move-RuntimeDirectoryToBackup {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter(Mandatory)][string] $ExpectedPath,
        [Parameter(Mandatory)][string] $Timestamp
    )

    $normalizedPath = [System.IO.Path]::GetFullPath($Path).TrimEnd('\')
    $normalizedExpectedPath = [System.IO.Path]::GetFullPath($ExpectedPath).TrimEnd('\')
    if (-not $normalizedPath.Equals($normalizedExpectedPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to move unexpected Docker runtime path: $normalizedPath"
    }

    if (-not (Test-Path -LiteralPath $normalizedPath)) {
        return $null
    }

    $backupPath = "$normalizedPath.stale-$Timestamp"
    $suffix = 1
    while (Test-Path -LiteralPath $backupPath) {
        $backupPath = "$normalizedPath.stale-$Timestamp-$suffix"
        $suffix++
    }

    $lastError = $null
    for ($attempt = 1; $attempt -le 6; $attempt++) {
        try {
            Move-Item -LiteralPath $normalizedPath -Destination $backupPath
            return $backupPath
        }
        catch {
            $lastError = $_
            if ($attempt -lt 6) {
                Start-Sleep -Seconds 2
            }
        }
    }

    throw "Could not recoverably rename $normalizedPath. Last error: $($lastError.Exception.Message)"
}

function Repair-KnownDockerDesktopRuntimeFailure {
    if (-not $env:LOCALAPPDATA) {
        throw 'LOCALAPPDATA is not available; the Docker Desktop runtime repair cannot be scoped safely.'
    }

    $dockerRunPath = [System.IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Docker\run'))
    $secretsEnginePath = [System.IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'docker-secrets-engine'))

    Write-Step 'Stopping Docker Desktop before recoverable runtime-path repair...'
    & docker desktop stop *> $null

    $dockerProcessNames = @(
        'Docker Desktop',
        'com.docker.backend',
        'com.docker.build',
        'com.docker.proxy'
    )
    foreach ($processName in $dockerProcessNames) {
        Get-Process -Name $processName -ErrorAction SilentlyContinue |
            Stop-Process -Force -ErrorAction SilentlyContinue
    }

    Start-Sleep -Seconds 2

    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backups = @(
        Move-RuntimeDirectoryToBackup -Path $dockerRunPath -ExpectedPath $dockerRunPath -Timestamp $timestamp
        Move-RuntimeDirectoryToBackup -Path $secretsEnginePath -ExpectedPath $secretsEnginePath -Timestamp $timestamp
    ) | Where-Object { $_ }

    if ($backups.Count -eq 0) {
        throw 'Docker Desktop reported the known runtime-path failure, but neither exact runtime directory was available for recovery.'
    }

    foreach ($backup in $backups) {
        Write-Host "  preserved backup: $backup"
    }
}

function Start-DockerEngine {
    if (Test-DockerEngine) {
        Write-Step 'Docker engine is already ready.'
        return
    }

    Write-Step 'Starting Docker Desktop...'
    $startOutput = (& docker desktop start 2>&1 | Out-String)
    $startExitCode = $LASTEXITCODE

    if ($startExitCode -eq 0 -and (Wait-DockerEngine -TimeoutSeconds $EngineTimeoutSeconds)) {
        Write-Step 'Docker engine is ready.'
        return
    }

    $hasKnownRuntimeFailure = $startOutput -match $knownRuntimeErrorPattern
    if (-not $hasKnownRuntimeFailure) {
        $safeOutput = $startOutput.Trim()
        if ([string]::IsNullOrWhiteSpace($safeOutput)) {
            $safeOutput = 'Docker Desktop did not report additional details.'
        }

        throw "Docker engine did not become ready within $EngineTimeoutSeconds seconds. $safeOutput"
    }

    if ($SkipDockerDesktopRepair) {
        throw 'Docker Desktop reported the known stale runtime-path failure and automatic repair was disabled.'
    }

    Write-Step 'Detected the known stale Docker Desktop runtime-path failure.'
    Repair-KnownDockerDesktopRuntimeFailure

    Write-Step 'Retrying Docker Desktop startup after recovery...'
    $retryOutput = (& docker desktop start 2>&1 | Out-String)
    $retryExitCode = $LASTEXITCODE
    if ($retryExitCode -ne 0 -or -not (Wait-DockerEngine -TimeoutSeconds $EngineTimeoutSeconds)) {
        $safeRetryOutput = $retryOutput.Trim()
        throw "Docker Desktop recovery completed, but the engine still did not become ready. $safeRetryOutput"
    }

    Write-Step 'Docker engine is ready after recovery.'
}

function Invoke-Compose {
    param([Parameter(Mandatory)][string[]] $Arguments)

    & docker compose --project-directory $repositoryRoot --file $composeFile @Arguments
    $exitCode = $LASTEXITCODE
    Assert-LastExitCode -Operation "docker compose $($Arguments -join ' ')" -ExitCode $exitCode
}

function Test-HttpEndpoint {
    param(
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][uri] $Uri
    )

    $response = Invoke-WebRequest -Uri $Uri -Method Get -TimeoutSec 15 -UseBasicParsing
    if ($response.StatusCode -ne 200) {
        throw "${Name} returned HTTP $($response.StatusCode)."
    }

    Write-Host "  ${Name}: HTTP 200"
}

if (-not (Test-Path -LiteralPath $composeFile -PathType Leaf)) {
    throw "Docker Compose file was not found at $composeFile."
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker CLI is not available on PATH. Install or repair Docker Desktop before using this script.'
}

Set-Location -LiteralPath $repositoryRoot

switch ($Action) {
    'Stop' {
        if (-not (Test-DockerEngine)) {
            throw 'Docker engine is not available, so the Compose stack cannot be stopped cleanly.'
        }

        & docker compose version *> $null
        Assert-LastExitCode -Operation 'docker compose version' -ExitCode $LASTEXITCODE
        Get-EnvironmentValues
        Write-Step 'Stopping the PLUS 5 stack without deleting the SQL named volume...'
        Invoke-Compose -Arguments @('down')
        Write-Step 'PLUS 5 stack stopped. The plus5-sql-data volume was preserved.'
        break
    }
    'Status' {
        if (-not (Test-DockerEngine)) {
            throw 'Docker engine is not available.'
        }

        & docker compose version *> $null
        Assert-LastExitCode -Operation 'docker compose version' -ExitCode $LASTEXITCODE
        Get-EnvironmentValues
        Invoke-Compose -Arguments @('ps', '--all')
        break
    }
    'Restart' {
        Start-DockerEngine
        & docker compose version *> $null
        Assert-LastExitCode -Operation 'docker compose version' -ExitCode $LASTEXITCODE
        Get-EnvironmentValues
        Write-Step 'Restarting the PLUS 5 Compose services without deleting the SQL named volume...'
        Invoke-Compose -Arguments @('down')
    }
    'Start' {
        Start-DockerEngine
        & docker compose version *> $null
        Assert-LastExitCode -Operation 'docker compose version' -ExitCode $LASTEXITCODE
        Get-EnvironmentValues
    }
}

if ($Action -in @('Start', 'Restart')) {
    Write-Step 'Validating Docker Compose configuration...'
    Invoke-Compose -Arguments @('config', '--quiet')

    $upArguments = @('up', '--detach')
    if (-not $NoBuild) {
        $upArguments += '--build'
    }

    $upArguments += @('--wait', '--wait-timeout', [string] $ComposeTimeoutSeconds)

    Write-Step 'Starting the database, migrations, API, and frontend...'
    Invoke-Compose -Arguments $upArguments

    Write-Step 'Compose service state:'
    Invoke-Compose -Arguments @('ps', '--all')

    Write-Step 'Validating application endpoints...'
    Test-HttpEndpoint -Name 'API liveness' -Uri 'http://localhost:8080/health/live'
    Test-HttpEndpoint -Name 'API readiness' -Uri 'http://localhost:8080/health/ready'
    Test-HttpEndpoint -Name 'Frontend' -Uri 'http://localhost:8081/'

    Write-Step 'PLUS 5 is ready: http://localhost:8081'
}
