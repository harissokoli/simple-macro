using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MacroHost
{
    public static class InputBridge
    {
        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(
            uint nInputs,
            INPUT[] pInputs,
            int cbSize
        );

        public static bool IsKeyDown(int virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        public static void TapKey(ushort virtualKey)
        {
            INPUT[] inputs = new INPUT[2];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = virtualKey;
            inputs[0].U.ki.wScan = 0;
            inputs[0].U.ki.dwFlags = 0;
            inputs[0].U.ki.time = 0;
            inputs[0].U.ki.dwExtraInfo = IntPtr.Zero;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki.wVk = virtualKey;
            inputs[1].U.ki.wScan = 0;
            inputs[1].U.ki.dwFlags = KEYEVENTF_KEYUP;
            inputs[1].U.ki.time = 0;
            inputs[1].U.ki.dwExtraInfo = IntPtr.Zero;

            int inputSize = Marshal.SizeOf(typeof(INPUT));

            uint result = SendInput(
                (uint)inputs.Length,
                inputs,
                inputSize
            );

            if (result != inputs.Length)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "SendInput failed"
                );
            }
        }

        public static void LeftClick()
        {
            INPUT[] inputs = new INPUT[2];

            inputs[0].type = INPUT_MOUSE;
            inputs[0].U.mi.dx = 0;
            inputs[0].U.mi.dy = 0;
            inputs[0].U.mi.mouseData = 0;
            inputs[0].U.mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
            inputs[0].U.mi.time = 0;
            inputs[0].U.mi.dwExtraInfo = IntPtr.Zero;

            inputs[1].type = INPUT_MOUSE;
            inputs[1].U.mi.dx = 0;
            inputs[1].U.mi.dy = 0;
            inputs[1].U.mi.mouseData = 0;
            inputs[1].U.mi.dwFlags = MOUSEEVENTF_LEFTUP;
            inputs[1].U.mi.time = 0;
            inputs[1].U.mi.dwExtraInfo = IntPtr.Zero;

            int inputSize = Marshal.SizeOf(typeof(INPUT));

            uint result = SendInput(
                (uint)inputs.Length,
                inputs,
                inputSize
            );

            if (result != inputs.Length)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "Mouse SendInput failed"
                );
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }
    }

    public static class WindowInput
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            out uint processId
        );

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(
            IntPtr hWnd,
            out RECT lpRect
        );

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(
            IntPtr hWnd,
            ref POINT lpPoint
        );

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(
            out POINT lpPoint
        );

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(
            int X,
            int Y
        );

        private static string NormalizeProcessName(
            string processName
        )
        {
            if (String.IsNullOrWhiteSpace(processName))
            {
                return String.Empty;
            }

            string value = processName.Trim();

            if (value.EndsWith(
                ".exe",
                StringComparison.OrdinalIgnoreCase
            ))
            {
                value = value.Substring(
                    0,
                    value.Length - 4
                );
            }

            return value;
        }

        public static bool IsForegroundProcess(
            string processName
        )
        {
            IntPtr hWnd = GetForegroundWindow();

            if (hWnd == IntPtr.Zero)
            {
                return false;
            }

            uint processId;

            GetWindowThreadProcessId(
                hWnd,
                out processId
            );

            if (processId == 0)
            {
                return false;
            }

            string expected =
                NormalizeProcessName(processName);

            try
            {
                using (
                    Process process =
                        Process.GetProcessById(
                            (int)processId
                        )
                )
                {
                    return String.Equals(
                        process.ProcessName,
                        expected,
                        StringComparison.OrdinalIgnoreCase
                    );
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool ClickForegroundProcessPercent(
            string processName,
            double xPercent,
            double yPercent,
            bool restoreCursor
        )
        {
            if (
                xPercent < 0.0 ||
                xPercent > 100.0
            )
            {
                throw new ArgumentOutOfRangeException(
                    "xPercent",
                    "X percent must be between 0 and 100."
                );
            }

            if (
                yPercent < 0.0 ||
                yPercent > 100.0
            )
            {
                throw new ArgumentOutOfRangeException(
                    "yPercent",
                    "Y percent must be between 0 and 100."
                );
            }

            IntPtr hWnd = GetForegroundWindow();

            if (hWnd == IntPtr.Zero)
            {
                return false;
            }

            uint processId;

            GetWindowThreadProcessId(
                hWnd,
                out processId
            );

            if (processId == 0)
            {
                return false;
            }

            string expected =
                NormalizeProcessName(processName);

            try
            {
                using (
                    Process process =
                        Process.GetProcessById(
                            (int)processId
                        )
                )
                {
                    if (!String.Equals(
                        process.ProcessName,
                        expected,
                        StringComparison.OrdinalIgnoreCase
                    ))
                    {
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }

            RECT clientRect;

            if (!GetClientRect(
                hWnd,
                out clientRect
            ))
            {
                return false;
            }

            int width =
                clientRect.Right -
                clientRect.Left;

            int height =
                clientRect.Bottom -
                clientRect.Top;

            if (
                width <= 0 ||
                height <= 0
            )
            {
                return false;
            }

            int clientX = (int)Math.Round(
                (width - 1) *
                (xPercent / 100.0)
            );

            int clientY = (int)Math.Round(
                (height - 1) *
                (yPercent / 100.0)
            );

            POINT target = new POINT();
            target.X = clientX;
            target.Y = clientY;

            if (!ClientToScreen(
                hWnd,
                ref target
            ))
            {
                return false;
            }

            POINT original = new POINT();
            bool haveOriginal =
                GetCursorPos(out original);

            if (!SetCursorPos(
                target.X,
                target.Y
            ))
            {
                return false;
            }

            // Give Windows/game a brief moment to observe the cursor
            // at the intended target before sending the click.
            Thread.Sleep(20);

            InputBridge.LeftClick();

            Thread.Sleep(20);

            if (
                restoreCursor &&
                haveOriginal
            )
            {
                SetCursorPos(
                    original.X,
                    original.Y
                );
            }

            return true;
        }
    }

    public sealed class PowerShellMacro : IDisposable
    {
        private Runspace _runspace;

        public string FilePath { get; private set; }
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public bool Enabled { get; set; }
        public string RuntimeStatus { get; set; }

        public PowerShellMacro(string filePath)
        {
            FilePath = filePath;
            Load();
        }

        private void Load()
        {
            _runspace = RunspaceFactory.CreateRunspace();
            _runspace.Open();

            string script = File.ReadAllText(FilePath, Encoding.UTF8);

            using (PowerShell ps = PowerShell.Create())
            {
                ps.Runspace = _runspace;
                ps.AddScript(script);

                ps.Invoke();

                ThrowIfErrors(ps, "loading macro");
            }

            PSObject info = InvokeSingle("Get-MacroInfo");

            if (info == null)
            {
                throw new InvalidOperationException(
                    "Get-MacroInfo returned no object."
                );
            }

            Id = ReadString(info, "Id", Path.GetFileNameWithoutExtension(FilePath));
            Name = ReadString(info, "Name", Id);
            Description = ReadString(info, "Description", "");

            PSPropertyInfo enabledProperty = info.Properties["Enabled"];

            if (enabledProperty == null || enabledProperty.Value == null)
            {
                Enabled = true;
            }
            else
            {
                Enabled = Convert.ToBoolean(enabledProperty.Value);
            }

            RuntimeStatus = "Ready";
        }

        private static string ReadString(
            PSObject obj,
            string propertyName,
            string fallback
        )
        {
            PSPropertyInfo property = obj.Properties[propertyName];

            if (property == null || property.Value == null)
            {
                return fallback;
            }

            return Convert.ToString(property.Value);
        }

        private PSObject InvokeSingle(string commandName)
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.Runspace = _runspace;
                ps.AddCommand(commandName);

                var results = ps.Invoke();

                ThrowIfErrors(ps, commandName);

                if (results == null || results.Count == 0)
                {
                    return null;
                }

                return results[0];
            }
        }

        private void InvokeVoid(string commandName, bool ignoreMissing)
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.Runspace = _runspace;
                ps.AddCommand(commandName);

                ps.Invoke();

                if (ignoreMissing && ps.HadErrors)
                {
                    bool onlyCommandMissing = true;

                    foreach (ErrorRecord error in ps.Streams.Error)
                    {
                        if (!(error.Exception is CommandNotFoundException))
                        {
                            onlyCommandMissing = false;
                            break;
                        }
                    }

                    if (onlyCommandMissing)
                    {
                        return;
                    }
                }

                ThrowIfErrors(ps, commandName);
            }
        }

        private static void ThrowIfErrors(
            PowerShell ps,
            string operation
        )
        {
            if (!ps.HadErrors)
            {
                return;
            }

            StringBuilder builder = new StringBuilder();

            foreach (ErrorRecord error in ps.Streams.Error)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(error.ToString());
            }

            throw new InvalidOperationException(
                "PowerShell error while " + operation + ":" +
                Environment.NewLine +
                builder.ToString()
            );
        }

        public void Tick()
        {
            if (!Enabled)
            {
                return;
            }

            InvokeVoid("Invoke-MacroTick", false);
        }

        public void Reset()
        {
            try
            {
                InvokeVoid("Reset-Macro", true);
            }
            catch
            {
                // Reset must never take the manager down.
            }
        }

        public void Dispose()
        {
            Reset();

            if (_runspace != null)
            {
                try
                {
                    _runspace.Close();
                }
                catch
                {
                }

                _runspace.Dispose();
                _runspace = null;
            }
        }
    }

    public sealed class ManagerForm : Form
    {
        private readonly ListView _list;
        private readonly Label _statusLabel;
        private readonly Button _reloadButton;
        private readonly Button _hideButton;

        public event EventHandler ToggleSelectedRequested;
        public event EventHandler ReloadRequested;
        public event EventHandler HideRequested;

        public ManagerForm()
        {
            Text = "SimpleMacro";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1000, 620);
            MinimumSize = new Size(760, 460);
            FormBorderStyle = FormBorderStyle.Sizable;
            BackColor = Color.FromArgb(12, 12, 12);
            ForeColor = Color.White;
            KeyPreview = true;
            ShowInTaskbar = true;

            Font normalFont = new Font(
                "Consolas",
                10.5f,
                FontStyle.Regular
            );

            Font titleFont = new Font(
                "Consolas",
                20.0f,
                FontStyle.Bold
            );

            Label title = new Label();
            title.Text = "SIMPLEMACRO";
            title.AutoSize = true;
            title.Font = titleFont;
            title.ForeColor = Color.FromArgb(70, 220, 220);
            title.Location = new Point(28, 24);
            Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text =
                "WinForms EXE + PowerShell macro engine";
            subtitle.AutoSize = true;
            subtitle.Font = normalFont;
            subtitle.ForeColor = Color.Gray;
            subtitle.Location = new Point(31, 63);
            Controls.Add(subtitle);

            _statusLabel = new Label();
            _statusLabel.Text =
                "UP/DOWN Navigate   ENTER/SPACE Toggle   F12/ESC Hide";
            _statusLabel.AutoSize = true;
            _statusLabel.Font = new Font(
                "Consolas",
                10.5f,
                FontStyle.Bold
            );
            _statusLabel.ForeColor = Color.Khaki;
            _statusLabel.Location = new Point(31, 98);
            Controls.Add(_statusLabel);

            _list = new ListView();
            _list.Location = new Point(31, 137);
            _list.Size = new Size(920, 365);
            _list.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Bottom |
                AnchorStyles.Left |
                AnchorStyles.Right;

            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.HideSelection = false;
            _list.MultiSelect = false;
            _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _list.BackColor = Color.FromArgb(17, 17, 17);
            _list.ForeColor = Color.White;
            _list.Font = normalFont;
            _list.BorderStyle = BorderStyle.FixedSingle;

            _list.Columns.Add("Macro", 260);
            _list.Columns.Add("Status", 120);
            _list.Columns.Add("Description", 430);
            _list.Columns.Add("Runtime", 150);

            Controls.Add(_list);

            _reloadButton = new Button();
            _reloadButton.Text = "Reload Macros";
            _reloadButton.Size = new Size(140, 34);
            _reloadButton.Location = new Point(31, 520);
            _reloadButton.Anchor =
                AnchorStyles.Left |
                AnchorStyles.Bottom;

            _reloadButton.Click += delegate
            {
                EventHandler handler = ReloadRequested;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            };

            Controls.Add(_reloadButton);

            _hideButton = new Button();
            _hideButton.Text = "Hide";
            _hideButton.Size = new Size(100, 34);
            _hideButton.Location = new Point(181, 520);
            _hideButton.Anchor =
                AnchorStyles.Left |
                AnchorStyles.Bottom;

            _hideButton.Click += delegate
            {
                EventHandler handler = HideRequested;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            };

            Controls.Add(_hideButton);

            Label footer = new Label();
            footer.Text =
                "Enabled macros run continuously, whether this window is visible or hidden.";
            footer.AutoSize = true;
            footer.Font = new Font(
                "Consolas",
                9.0f,
                FontStyle.Regular
            );
            footer.ForeColor = Color.Gray;
            footer.Location = new Point(300, 529);
            footer.Anchor =
                AnchorStyles.Left |
                AnchorStyles.Bottom;

            Controls.Add(footer);

            _list.KeyDown += OnListKeyDown;
            _list.DoubleClick += delegate
            {
                RequestToggle();
            };

            KeyDown += OnFormKeyDown;
        }

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12 ||
                e.KeyCode == Keys.Escape)
            {
                EventHandler handler = HideRequested;

                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void OnListKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter ||
                e.KeyCode == Keys.Space)
            {
                RequestToggle();

                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.F12 ||
                e.KeyCode == Keys.Escape)
            {
                EventHandler handler = HideRequested;

                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void RequestToggle()
        {
            EventHandler handler = ToggleSelectedRequested;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public int SelectedMacroIndex
        {
            get
            {
                if (_list.SelectedItems.Count == 0)
                {
                    return -1;
                }

                object tag = _list.SelectedItems[0].Tag;

                if (tag == null)
                {
                    return -1;
                }

                return Convert.ToInt32(tag);
            }
        }

        public void RefreshMacros(
            IList<PowerShellMacro> macros
        )
        {
            int selected = SelectedMacroIndex;

            _list.BeginUpdate();
            _list.Items.Clear();

            for (int i = 0; i < macros.Count; i++)
            {
                PowerShellMacro macro = macros[i];

                ListViewItem item =
                    new ListViewItem(macro.Name);

                ListViewItem.ListViewSubItem status =
                    item.SubItems.Add(
                        macro.Enabled
                            ? "ENABLED"
                            : "DISABLED"
                    );

                item.UseItemStyleForSubItems = false;

                if (macro.Enabled)
                {
                    status.ForeColor = Color.LimeGreen;
                }
                else
                {
                    status.ForeColor = Color.IndianRed;
                }

                item.SubItems.Add(macro.Description);

                ListViewItem.ListViewSubItem runtime =
                    item.SubItems.Add(
                        String.IsNullOrWhiteSpace(macro.RuntimeStatus)
                            ? "Ready"
                            : macro.RuntimeStatus
                    );

                if (String.Equals(
                    macro.RuntimeStatus,
                    "ERROR",
                    StringComparison.OrdinalIgnoreCase
                ))
                {
                    runtime.ForeColor = Color.OrangeRed;
                }
                else
                {
                    runtime.ForeColor = Color.Gray;
                }

                item.Tag = i;

                _list.Items.Add(item);
            }

            _list.EndUpdate();

            if (_list.Items.Count > 0)
            {
                if (selected < 0 ||
                    selected >= _list.Items.Count)
                {
                    selected = 0;
                }

                _list.Items[selected].Selected = true;
                _list.Items[selected].Focused = true;
                _list.Items[selected].EnsureVisible();
            }
        }

        public void FocusMacroList()
        {
            if (_list.Items.Count > 0)
            {
                _list.Focus();
            }
        }
    }

    public sealed class MacroApplicationContext :
        ApplicationContext
    {
        private const int VK_F12 = 0x7B;
        private const string StartupName =
            "SimpleMacro";

        private readonly string _baseDirectory;
        private readonly string _macroDirectory;
        private readonly string _logFile;

        private readonly List<PowerShellMacro> _macros;
        private readonly ManagerForm _form;
        private readonly NotifyIcon _tray;
        private readonly System.Windows.Forms.Timer _timer;

        private bool _f12WasDown;
        private bool _exitRequested;

        public MacroApplicationContext()
        {
            _baseDirectory =
                AppDomain.CurrentDomain.BaseDirectory;

            _macroDirectory =
                Path.Combine(
                    _baseDirectory,
                    "macros"
                );

            _logFile =
                Path.Combine(
                    _baseDirectory,
                    "crash.log"
                );

            _macros =
                new List<PowerShellMacro>();

            if (!Directory.Exists(_macroDirectory))
            {
                Directory.CreateDirectory(
                    _macroDirectory
                );
            }

            LoadMacros();

            _form = new ManagerForm();

            _form.ToggleSelectedRequested +=
                delegate
                {
                    ToggleSelectedMacro();
                };

            _form.ReloadRequested +=
                delegate
                {
                    ReloadMacros();
                };

            _form.HideRequested +=
                delegate
                {
                    HideManager();
                };

            _form.FormClosing += OnFormClosing;

            _tray = new NotifyIcon();
            _tray.Text = "SimpleMacro";
            _tray.Icon = SystemIcons.Application;
            _tray.Visible = true;

            ContextMenuStrip menu =
                new ContextMenuStrip();

            ToolStripMenuItem showItem =
                new ToolStripMenuItem(
                    "Show Manager"
                );

            showItem.Click += delegate
            {
                ShowManager();
            };

            ToolStripMenuItem reloadItem =
                new ToolStripMenuItem(
                    "Reload Macros"
                );

            reloadItem.Click += delegate
            {
                ReloadMacros();
            };

            ToolStripMenuItem startupItem =
                new ToolStripMenuItem(
                    "Start with Windows"
                );

            startupItem.CheckOnClick = true;
            startupItem.Checked =
                IsStartupEnabled();

            startupItem.Click += delegate
            {
                try
                {
                    SetStartupEnabled(
                        startupItem.Checked
                    );
                }
                catch (Exception ex)
                {
                    LogException(ex);

                    MessageBox.Show(
                        "Could not change startup setting:" +
                        Environment.NewLine +
                        ex.Message,
                        "SimpleMacro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    startupItem.Checked =
                        IsStartupEnabled();
                }
            };

            ToolStripMenuItem exitItem =
                new ToolStripMenuItem(
                    "Exit"
                );

            exitItem.Click += delegate
            {
                ExitManager();
            };

            menu.Items.Add(showItem);
            menu.Items.Add(reloadItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(startupItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            _tray.ContextMenuStrip = menu;

            _tray.DoubleClick += delegate
            {
                ShowManager();
            };

            _timer =
                new System.Windows.Forms.Timer();

            _timer.Interval = 10;
            _timer.Tick += OnTimerTick;
            _timer.Start();

            _f12WasDown = false;

            _form.RefreshMacros(_macros);

            // Start completely hidden.
            // ApplicationContext keeps the message loop alive,
            // so F12 polling continues even when no form is visible.
        }

        private void OnFormClosing(
            object sender,
            FormClosingEventArgs e
        )
        {
            if (_exitRequested)
            {
                return;
            }

            e.Cancel = true;
            HideManager();
        }

        private void OnTimerTick(
            object sender,
            EventArgs e
        )
        {
            try
            {
                bool f12Down =
                    InputBridge.IsKeyDown(VK_F12);

                if (f12Down &&
                    !_f12WasDown)
                {
                    if (_form.Visible)
                    {
                        HideManager();
                    }
                    else
                    {
                        ShowManager();
                    }
                }

                _f12WasDown = f12Down;

                for (int i = 0;
                     i < _macros.Count;
                     i++)
                {
                    PowerShellMacro macro =
                        _macros[i];

                    if (!macro.Enabled)
                    {
                        continue;
                    }

                    try
                    {
                        macro.Tick();

                        if (!String.Equals(
                            macro.RuntimeStatus,
                            "Running",
                            StringComparison.Ordinal
                        ))
                        {
                            macro.RuntimeStatus = "Running";

                            if (_form.Visible)
                            {
                                _form.RefreshMacros(_macros);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                        macro.Reset();
                        macro.Enabled = false;
                        macro.RuntimeStatus = "ERROR";

                        if (_form.Visible)
                        {
                            _form.RefreshMacros(_macros);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private void ShowManager()
        {
            ResetAllMacros();

            _form.RefreshMacros(_macros);

            if (_form.WindowState ==
                FormWindowState.Minimized)
            {
                _form.WindowState =
                    FormWindowState.Normal;
            }

            _form.Show();
            _form.Activate();
            _form.BringToFront();
            _form.FocusMacroList();
        }

        private void HideManager()
        {
            ResetAllMacros();
            _form.Hide();
        }

        private void ToggleSelectedMacro()
        {
            int index =
                _form.SelectedMacroIndex;

            if (index < 0 ||
                index >= _macros.Count)
            {
                return;
            }

            PowerShellMacro macro =
                _macros[index];

            macro.Reset();
            macro.Enabled = !macro.Enabled;
            macro.RuntimeStatus =
                macro.Enabled
                    ? "Ready"
                    : "Disabled";

            _form.RefreshMacros(_macros);
        }

        private void LoadMacros()
        {
            string[] files =
                Directory.GetFiles(
                    _macroDirectory,
                    "*.ps1",
                    SearchOption.TopDirectoryOnly
                );

            Array.Sort(
                files,
                StringComparer.OrdinalIgnoreCase
            );

            for (int i = 0;
                 i < files.Length;
                 i++)
            {
                try
                {
                    PowerShellMacro macro =
                        new PowerShellMacro(files[i]);

                    macro.RuntimeStatus =
                        macro.Enabled
                            ? "Ready"
                            : "Disabled";

                    _macros.Add(macro);
                }
                catch (Exception ex)
                {
                    LogException(ex);

                    MessageBox.Show(
                        "Could not load macro:" +
                        Environment.NewLine +
                        files[i] +
                        Environment.NewLine +
                        Environment.NewLine +
                        ex.Message,
                        "SimpleMacro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
        }

        private void ReloadMacros()
        {
            ResetAllMacros();

            for (int i = 0;
                 i < _macros.Count;
                 i++)
            {
                _macros[i].Dispose();
            }

            _macros.Clear();

            LoadMacros();

            _form.RefreshMacros(_macros);
        }

        private void ResetAllMacros()
        {
            for (int i = 0;
                 i < _macros.Count;
                 i++)
            {
                _macros[i].Reset();
            }
        }

        private bool IsStartupEnabled()
        {
            using (
                RegistryKey key =
                    Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Run",
                        false
                    )
            )
            {
                if (key == null)
                {
                    return false;
                }

                object value =
                    key.GetValue(StartupName);

                if (value == null)
                {
                    return false;
                }

                string expected =
                    "\"" +
                    Application.ExecutablePath +
                    "\"";

                return string.Equals(
                    Convert.ToString(value),
                    expected,
                    StringComparison.OrdinalIgnoreCase
                );
            }
        }

        private void SetStartupEnabled(
            bool enabled
        )
        {
            using (
                RegistryKey key =
                    Registry.CurrentUser.CreateSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Run"
                    )
            )
            {
                if (key == null)
                {
                    throw new InvalidOperationException(
                        "Could not open the Windows startup registry key."
                    );
                }

                if (enabled)
                {
                    string value =
                        "\"" +
                        Application.ExecutablePath +
                        "\"";

                    key.SetValue(
                        StartupName,
                        value,
                        RegistryValueKind.String
                    );
                }
                else
                {
                    key.DeleteValue(
                        StartupName,
                        false
                    );
                }
            }
        }

        private void LogException(Exception ex)
        {
            try
            {
                string text =
                    "==================================================" +
                    Environment.NewLine +
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss"
                    ) +
                    Environment.NewLine +
                    ex.ToString() +
                    Environment.NewLine;

                File.AppendAllText(
                    _logFile,
                    text,
                    Encoding.UTF8
                );
            }
            catch
            {
            }
        }

        private void ExitManager()
        {
            _exitRequested = true;

            _timer.Stop();

            ResetAllMacros();

            for (int i = 0;
                 i < _macros.Count;
                 i++)
            {
                _macros[i].Dispose();
            }

            _macros.Clear();

            _tray.Visible = false;
            _tray.Dispose();

            if (!_form.IsDisposed)
            {
                _form.Close();
                _form.Dispose();
            }

            ExitThread();
        }

        protected override void ExitThreadCore()
        {
            if (!_exitRequested)
            {
                _exitRequested = true;

                try
                {
                    _timer.Stop();
                }
                catch
                {
                }

                ResetAllMacros();

                for (int i = 0;
                     i < _macros.Count;
                     i++)
                {
                    try
                    {
                        _macros[i].Dispose();
                    }
                    catch
                    {
                    }
                }

                _macros.Clear();

                try
                {
                    _tray.Visible = false;
                    _tray.Dispose();
                }
                catch
                {
                }
            }

            base.ExitThreadCore();
        }
    }

    public static class Program
    {
        private static string _logFile;

        [STAThread]
        public static void Main()
        {
            _logFile =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "crash.log"
                );

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException +=
                delegate(
                    object sender,
                    System.Threading.ThreadExceptionEventArgs e
                )
                {
                    LogFatal(e.Exception);
                };

            AppDomain.CurrentDomain.UnhandledException +=
                delegate(
                    object sender,
                    UnhandledExceptionEventArgs e
                )
                {
                    Exception ex =
                        e.ExceptionObject as Exception;

                    if (ex != null)
                    {
                        LogFatal(ex);
                    }
                };

            try
            {
                MacroApplicationContext context =
                    new MacroApplicationContext();

                Application.Run(context);
            }
            catch (Exception ex)
            {
                LogFatal(ex);

                MessageBox.Show(
                    "SimpleMacro crashed." +
                    Environment.NewLine +
                    Environment.NewLine +
                    ex.Message +
                    Environment.NewLine +
                    Environment.NewLine +
                    "See crash.log next to the EXE.",
                    "SimpleMacro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private static void LogFatal(
            Exception ex
        )
        {
            try
            {
                string text =
                    "==================================================" +
                    Environment.NewLine +
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss"
                    ) +
                    Environment.NewLine +
                    ex.ToString() +
                    Environment.NewLine;

                File.AppendAllText(
                    _logFile,
                    text,
                    Encoding.UTF8
                );
            }
            catch
            {
            }
        }
    }
}
