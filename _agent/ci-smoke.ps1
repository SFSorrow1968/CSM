param(
    [switch]$Strict
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$repoRootPath = $repoRoot.Path
$projectPath = Join-Path $repoRootPath "CSM.csproj"
$testProjectPath = Join-Path $repoRootPath "CSM.Tests\CSM.Tests.csproj"

$libsPath = Join-Path (Split-Path $repoRootPath -Parent) "libs"
$requiredDlls = @(
    "ThunderRoad.dll",
    "Assembly-CSharp.dll",
    "Assembly-CSharp-firstpass.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.TextRenderingModule.dll"
)

$missingDlls = @()
foreach ($dll in $requiredDlls) {
    $dllPath = Join-Path $libsPath $dll
    if (-not (Test-Path $dllPath)) {
        $missingDlls += $dll
    }
}

if ($missingDlls.Count -gt 0) {
    $msg = "[CSM-CI] Missing game libraries in ${libsPath}: $($missingDlls -join ', ')"
    if ($Strict) {
        Write-Error $msg
        exit 1
    }

    Write-Warning $msg
    Write-Warning "[CSM-CI] Skipping Release/Nomad build in non-strict mode."
}
else {
    Write-Host "[CSM-CI] Building Release..."
    dotnet build $projectPath -c Release | Out-Host
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "[CSM-CI] Building Nomad..."
    dotnet build $projectPath -c Nomad | Out-Host
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (Test-Path $testProjectPath) {
    Write-Host "[CSM-CI] Running tests..."
    dotnet test $testProjectPath -c Release --nologo -v minimal | Out-Host
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "[CSM-CI] Smoke checks complete."
exit 0
