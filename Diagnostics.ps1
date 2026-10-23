$ErrorActionPreference = "Continue"

Write-Host ""
Write-Host "PowerShell Diagnostics" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor DarkGray
Write-Host ""

Write-Host "PowerShell version:" -ForegroundColor Gray
$PSVersionTable | Format-List

Write-Host "PSHOME:" -ForegroundColor Gray
Write-Host $PSHOME
Write-Host ""

Write-Host "System.Management.Automation assembly:" -ForegroundColor Gray

try {
    $asm = [System.Management.Automation.PSObject].Assembly

    Write-Host "FullName:" -ForegroundColor DarkGray
    Write-Host $asm.FullName

    Write-Host ""
    Write-Host "Location:" -ForegroundColor DarkGray
    Write-Host $asm.Location

    Write-Host ""
    Write-Host "Exists:" -ForegroundColor DarkGray
    Write-Host (Test-Path $asm.Location)
}
catch {
    Write-Host $_ -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to close"
