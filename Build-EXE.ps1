$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $root "src\MacroManager.cs"
$outputPath = Join-Path $root "SimpleMacro.exe"

if (-not (Test-Path $sourcePath)) {
    throw "Source file not found: $sourcePath"
}

Write-Host ""
Write-Host "Detecting PowerShell automation assembly..." -ForegroundColor Cyan

# Use the exact System.Management.Automation assembly loaded by
# the PowerShell process running this builder.
$sma = [System.Management.Automation.PSObject].Assembly.Location

if ([string]::IsNullOrWhiteSpace($sma) -or -not (Test-Path $sma)) {

    # Fallback: search common .NET Framework GAC locations.
    $possibleRoots = @(
        (Join-Path $env:WINDIR "Microsoft.NET\assembly\GAC_MSIL\System.Management.Automation"),
        (Join-Path $env:WINDIR "assembly\GAC_MSIL\System.Management.Automation")
    )

    $found = $null

    foreach ($candidateRoot in $possibleRoots) {

        if (-not (Test-Path $candidateRoot)) {
            continue
        }

        $found = Get-ChildItem `
            -Path $candidateRoot `
            -Filter "System.Management.Automation.dll" `
            -Recurse `
            -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty FullName

        if ($found) {
            break
        }
    }

    $sma = $found
}

if ([string]::IsNullOrWhiteSpace($sma) -or -not (Test-Path $sma)) {
    throw @"
Could not locate System.Management.Automation.dll.

PowerShell version:
$($PSVersionTable.PSVersion)

PSHOME:
$PSHOME

The builder looked at the currently loaded PowerShell assembly and the
standard .NET Framework GAC locations, but no usable assembly was found.
"@
}

Write-Host "Found:" -ForegroundColor Green
Write-Host "  $sma" -ForegroundColor White
Write-Host ""

if (Test-Path $outputPath) {
    Remove-Item $outputPath -Force
}

$source = Get-Content $sourcePath -Raw

Write-Host "Building SimpleMacro.exe..." -ForegroundColor Cyan
Write-Host ""

$references = @(
    "System.dll",
    "System.Core.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll",
    $sma
)

Add-Type `
    -TypeDefinition $source `
    -Language CSharp `
    -ReferencedAssemblies $references `
    -OutputAssembly $outputPath `
    -OutputType WindowsApplication

if (-not (Test-Path $outputPath)) {
    throw "Compilation finished without creating the EXE."
}

Write-Host ""
Write-Host "BUILD SUCCESSFUL" -ForegroundColor Green
Write-Host ""
Write-Host "EXE:" -ForegroundColor Gray
Write-Host "  $outputPath" -ForegroundColor White
Write-Host ""
Write-Host "Macros folder:" -ForegroundColor Gray
Write-Host "  $(Join-Path $root 'macros')" -ForegroundColor White
Write-Host ""
