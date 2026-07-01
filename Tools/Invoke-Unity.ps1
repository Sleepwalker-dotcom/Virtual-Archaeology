[CmdletBinding()]
param(
    [ValidateSet("Compile", "EditMode", "PlayMode")]
    [string]$Task = "Compile",
    [string]$UnityPath = "E:\unity\6000.0.58f2\Editor\Unity.exe",
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity executable not found: $UnityPath"
}

if (-not (Test-Path -LiteralPath (Join-Path $ProjectPath "ProjectSettings\ProjectVersion.txt") -PathType Leaf)) {
    throw "Not a Unity project: $ProjectPath"
}

$resultDirectory = Join-Path $ProjectPath "TestResults"
New-Item -ItemType Directory -Force -Path $resultDirectory | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logPath = Join-Path $resultDirectory "$($Task.ToLowerInvariant())-$timestamp.log"
$arguments = @(
    "-batchmode"
    "-nographics"
    "-projectPath", $ProjectPath
    "-logFile", $logPath
)

if ($Task -eq "Compile") {
    $arguments += "-quit"
}
else {
    $resultPath = Join-Path $resultDirectory "$($Task.ToLowerInvariant())-$timestamp.xml"
    $arguments += @(
        "-runTests"
        "-testPlatform", $Task
        "-testResults", $resultPath
        "-quit"
    )
}

Write-Host "Running Unity $Task verification..."
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow

if (Test-Path -LiteralPath $logPath) {
    $errors = Select-String -LiteralPath $logPath -Pattern "error CS\d+|Compilation failed|Aborting batchmode|Fatal Error" -CaseSensitive:$false
    if ($errors) {
        $errors | ForEach-Object { Write-Error $_.Line }
    }
}

if ($process.ExitCode -ne 0) {
    throw "Unity exited with code $($process.ExitCode). See $logPath"
}

Write-Host "Unity $Task verification completed. Log: $logPath"

