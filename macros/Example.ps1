# Example.ps1
#
# Template macro, disabled by default. Enable it from the manager to try it:
# press F8 once and it types the letter "A".
#
# Required: Get-MacroInfo, Invoke-MacroTick
# Optional: Reset-Macro

$script:F8WasDown = $false

function Get-MacroInfo {
    [PSCustomObject]@{
        Id          = "Example"
        Name        = "Example - F8 types A"
        Description = "Template macro: press F8 to send the letter A."
        Enabled     = $false
    }
}

function Reset-Macro {
    $script:F8WasDown = $false
}

function Invoke-MacroTick {

    # F8 virtual-key code = 0x77
    $isDown = [MacroHost.InputBridge]::IsKeyDown(0x77)

    # Act on the down edge only, so holding the key does not retrigger.
    if ($isDown -and -not $script:F8WasDown) {
        # A virtual-key code = 0x41
        [MacroHost.InputBridge]::TapKey([UInt16]0x41)
    }

    $script:F8WasDown = $isDown
}
