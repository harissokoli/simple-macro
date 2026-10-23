# SimpleMacro

A small Windows tray application that hosts PowerShell macros. Each macro is a
plain `.ps1` file dropped into the `macros` folder; the manager loads it, polls
it on a 10 ms timer, and lets you enable or disable it from a simple UI.

- Single-file C# source, compiled locally into one EXE (no SDK or Visual Studio needed)
- Each macro runs in its own isolated PowerShell runspace
- Runs from the system tray; macros keep running whether the window is visible, hidden or in the tray
- Optional "Start with Windows"
- Errors disable the offending macro and are logged to `crash.log` instead of failing silently

## Requirements

- Windows 10 / 11
- Windows PowerShell 5.1 (preinstalled on Windows). The build uses `powershell.exe`, not PowerShell 7 (`pwsh`).

## Build and run

1. Close any running copy from its tray icon (right-click, Exit).
2. Double-click `Build-EXE.bat`. It compiles `src/MacroManager.cs` into `SimpleMacro.exe` and starts it.
3. Next time, start it with `Run.bat` or the EXE directly.

If the build cannot find `System.Management.Automation.dll`, run `Diagnostics.bat` and check the reported location.

## Using the manager

| Action | How |
| --- | --- |
| Show / hide the window | `F12` (global hotkey), tray icon double-click, or tray menu |
| Navigate the list | `Up` / `Down` |
| Enable / disable a macro | `Enter`, `Space` or double-click |
| Hide the window | `Esc`, `F12` or the Hide button |
| Reload macros from disk | Reload Macros button or tray menu |
| Run at logon | Tray menu, Start with Windows (writes `HKCU\...\Run`) |
| Quit | Tray menu, Exit (closing the window only hides it) |

The **Runtime** column shows `Ready`, `Running`, `Disabled` or `ERROR`.
If a macro throws, it is reset, disabled and marked `ERROR`; details go to `crash.log` next to the EXE.

## Where macros go

> **Put your macro `.ps1` files in the `macros` folder located next to the EXE**
> (`<folder containing the EXE>\macros\`). The folder is created automatically on first start.
>
> - Only `.ps1` files directly inside `macros` are detected. Files in subfolders are ignored.
> - New or changed files are picked up at startup or when you press **Reload Macros** (button or tray menu).

## Generate a macro with AI

Don't want to write PowerShell yourself? Open **[docs/AI-MACRO-PROMPT.md](docs/AI-MACRO-PROMPT.md)**,
copy the prompt into any AI tool (ChatGPT, Claude, Gemini, ...), describe what you want the macro to do,
then save the result as a `.ps1` file in the `macros` folder.

## Writing a macro

Every `*.ps1` file directly inside `macros/` is loaded at startup (and on reload).
A macro must define:

| Function | Required | Purpose |
| --- | --- | --- |
| `Get-MacroInfo` | yes | Returns an object with `Id`, `Name`, `Description`, `Enabled` |
| `Invoke-MacroTick` | yes | Called every ~10 ms while the macro is enabled |
| `Reset-Macro` | no | Called when the window is shown/hidden, the macro is toggled, reloaded, errors, or the app exits. Clear your state here. |

Start from [`macros/Example.ps1`](macros/Example.ps1). Notes:

- State lives in `$script:` variables; each macro has its own runspace, so macros cannot clash.
- `Invoke-MacroTick` runs on the UI thread. Keep it fast and never sleep or loop in it; use timestamps and a small state machine instead.
- Any error written to the error stream (including non-terminating ones) counts as a failure and disables the macro.
- Detect key presses by edge (down now, up last tick) so holding a key does not retrigger.

### Helper API available to macros

```powershell
[MacroHost.InputBridge]::IsKeyDown(0x74)          # bool, virtual-key code (0x74 = F5)
[MacroHost.InputBridge]::TapKey([UInt16]0x41)     # key down + up via SendInput
[MacroHost.InputBridge]::LeftClick()              # left click at current cursor position

[MacroHost.WindowInput]::IsForegroundProcess("notepad.exe")   # ".exe" optional
[MacroHost.WindowInput]::ClickForegroundProcessPercent(
    "notepad.exe", 50.0, 50.0, $true)             # x%, y% of the client area; $true restores the cursor
```

`ClickForegroundProcessPercent` only clicks when the named process owns the foreground window, and
returns `$false` otherwise. Virtual-key codes: <https://learn.microsoft.com/windows/win32/inputdev/virtual-key-codes>

## Project layout

```
src/MacroManager.cs   whole application (input bridge, macro host, UI, tray)
macros/               your *.ps1 macros (Example.ps1 is a disabled template)
Build-EXE.bat/.ps1    compile to SimpleMacro.exe
Run.bat               start the built EXE
Diagnostics.bat/.ps1  print PowerShell / assembly info for build problems
```

## Limitations

- Input is injected with `SendInput`, so it will not work against elevated (administrator) windows unless the manager is also run as administrator.
- Keys are observed, not intercepted: a macro cannot block the physical key from reaching the active application.
- Mouse coordinates are not DPI-aware (the EXE has no DPI manifest); at display scaling other than 100% click targets may be offset.
- Enabled/disabled state is not persisted; each macro's `Enabled` value in `Get-MacroInfo` applies at every start.
- There is no single-instance guard; running two copies runs every macro twice.

## Responsible use

This tool synthesizes keyboard and mouse input. Many online games and anti-cheat systems prohibit
macros or automation and may ban accounts for using them. You are responsible for following the rules
of any software you use it with.
