# AI prompt: generate a SimpleMacro macro

Copy everything inside the box below into any AI tool (ChatGPT, Claude, Gemini, Copilot, ...),
then replace the text after **MY MACRO REQUEST** with what you want the macro to do.
Save the script the AI gives you as a `.ps1` file in the `macros` folder next to the EXE
(see [Where to put the macro](#where-to-put-the-macro)).

````text
You are writing a macro script for SimpleMacro, a Windows tray app that hosts PowerShell macros.
Write ONE complete PowerShell (.ps1) file that follows the rules below exactly. Output only the
code in a single code block, followed by a short plain-language explanation of how to use it.

## How the host works
- The host loads the .ps1 into its own Windows PowerShell 5.1 runspace, then calls
  Invoke-MacroTick roughly every 10 ms on the UI thread while the macro is enabled.
- Because of that, Invoke-MacroTick must return immediately. NEVER use Start-Sleep, Thread.Sleep,
  while/for loops that wait, or anything blocking inside it. Do timing with timestamps and a
  small state machine kept in $script: variables.
- Any error written to the error stream (even non-terminating) disables the macro, so avoid
  commands that write errors; guard with checks instead.

## Required file structure
The file must define these functions:

1. Get-MacroInfo (required) - returns a [PSCustomObject] with:
   Id (unique, no spaces), Name (shown in the list), Description (one line),
   Enabled ($true or $false; use $false for risky macros).
2. Invoke-MacroTick (required) - called every ~10 ms, must be fast and non-blocking.
3. Reset-Macro (optional but recommended) - set ALL $script: state back to its initial values.
   The host calls it when the macro is toggled, reloaded, errors, the manager window is shown or
   hidden, and on exit.

Keep all state in $script: variables declared at the top of the file.

## Helper functions available (already loaded by the host - do not define or import them)
[MacroHost.InputBridge]::IsKeyDown([int]$virtualKey)       -> bool, true while the physical key is held
[MacroHost.InputBridge]::TapKey([UInt16]$virtualKey)       -> presses and releases a key (cast the code to [UInt16])
[MacroHost.InputBridge]::LeftClick()                       -> left click at the CURRENT cursor position
[MacroHost.WindowInput]::IsForegroundProcess([string]$processName)
      -> bool, true if that process owns the foreground window (".exe" is optional, case-insensitive)
[MacroHost.WindowInput]::ClickForegroundProcessPercent([string]$processName, [double]$xPercent, [double]$yPercent, [bool]$restoreCursor)
      -> moves the cursor to x%/y% (0-100) of that window's client area, clicks, returns $true;
         returns $false (and does nothing) if that process is not in the foreground

There is NO API for: holding a key down, typing text strings, scrolling, right-click, mouse-move
only, reading the screen, or sleeping. For text, tap one key per step with the state machine.
Only use virtual-key codes (e.g. F5 = 0x74, F6 = 0x75, A = 0x41, Space = 0x20, Enter = 0x0D).

## Coding rules
- Trigger on the key DOWN EDGE (down now, not down last tick) unless the macro is meant to repeat
  while a key is held. Store the previous state in a $script: variable.
- Use [DateTime]::UtcNow and a $script:NextActionAt timestamp for delays between steps.
- For multi-step sequences use a $script:Stage integer (0 = idle) and advance one stage per
  due timestamp. If anything fails or the target app loses focus, set Stage back to 0.
- If the macro should only work in one application, check IsForegroundProcess before every
  action, not just the first.
- Do not use Add-Type, external modules, files, the network, or elevated commands.
- Put plain comments at the top explaining what the macro does, the hotkey, and any timing or
  coordinate values the user may want to change. Put those values in clearly named $script:
  variables at the top.
- Compatible with Windows PowerShell 5.1 only (no ternary operator, no ?? operator, no &&).

## Minimal working example (follow this shape)
```powershell
# F8 types the letter A once per press.
$script:F8WasDown = $false

function Get-MacroInfo {
    [PSCustomObject]@{
        Id          = "ExampleTypeA"
        Name        = "Example - F8 types A"
        Description = "Press F8 to send the letter A."
        Enabled     = $true
    }
}

function Reset-Macro {
    $script:F8WasDown = $false
}

function Invoke-MacroTick {
    $isDown = [MacroHost.InputBridge]::IsKeyDown(0x77)

    if ($isDown -and -not $script:F8WasDown) {
        [MacroHost.InputBridge]::TapKey([UInt16]0x41)
    }

    $script:F8WasDown = $isDown
}
```

## If my request is unclear
Ask me up to 3 short questions BEFORE writing code (which key triggers it, which application it
should work in, what delays are needed). If my request needs something the helper functions above
cannot do, tell me that instead of inventing functions.

## MY MACRO REQUEST
(Describe here, in plain words, what the macro should do. Include: the trigger key, the
application it should work in (process name, e.g. notepad.exe), the sequence of keys/clicks,
and the delay between steps in milliseconds.)
````

## Where to put the macro

1. Save the AI's code as a file ending in `.ps1`, for example `MyMacro.ps1`.
2. Put it in the **`macros`** folder that sits **next to `SimpleMacro.exe`** (created automatically on first start).
3. Open the manager (`F12`) and press **Reload Macros** (or right-click the tray icon, Reload Macros).
4. Select the macro and press `Enter` to enable it. Check the **Runtime** column.

If the macro shows `ERROR`, or a "Could not load macro" message appears, open `crash.log` next to the EXE,
paste its contents back into the AI chat, and ask it to fix the script.
