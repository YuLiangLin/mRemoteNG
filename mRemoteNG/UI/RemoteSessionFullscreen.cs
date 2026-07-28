using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Resources.Language;
using mRemoteNG.UI.Forms;
using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace mRemoteNG.UI
{
    [SupportedOSPlatform("windows")]
    internal static class RemoteSessionFullscreen
    {
        internal static bool CanToggle(InterfaceControl interfaceControl)
        {
            return interfaceControl?.Protocol != null;
        }

        internal static void Toggle(InterfaceControl interfaceControl)
        {
            if (!CanToggle(interfaceControl))
                return;

            if (interfaceControl.Protocol is RdpProtocol rdp)
            {
                rdp.ToggleFullscreen();
                return;
            }

            RemoteSessionFullscreenForm.Toggle(interfaceControl);
        }
    }

    [SupportedOSPlatform("windows")]
    internal sealed class RemoteSessionFullscreenForm : Form
    {
        private const int WmHotKey = 0x0312;
        private const int ExitWithF11HotKeyId = 0x4D11;
        private const int ExitWithCtrlAltEnterHotKeyId = 0x4D13;
        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint ModNoRepeat = 0x4000;

        private static RemoteSessionFullscreenForm? _current;

        private readonly InterfaceControl _sessionControl;
        private readonly Control _originalParent;
        private readonly int _originalChildIndex;
        private readonly DockStyle _originalDock;
        private readonly AnchorStyles _originalAnchor;
        private readonly Rectangle _originalBounds;
        private readonly Rectangle _targetBounds;
        private readonly RemoteSessionFullscreenConnectionBar _connectionBar;
        private bool _f11HotKeyRegistered;
        private bool _ctrlAltEnterHotKeyRegistered;
        private bool _sessionRestored;

        private RemoteSessionFullscreenForm(InterfaceControl sessionControl)
        {
            _sessionControl = sessionControl ?? throw new ArgumentNullException(nameof(sessionControl));
            _originalParent = sessionControl.Parent ??
                              throw new InvalidOperationException("The remote session control has no parent.");
            _originalChildIndex = _originalParent.Controls.GetChildIndex(sessionControl);
            _originalDock = sessionControl.Dock;
            _originalAnchor = sessionControl.Anchor;
            _originalBounds = sessionControl.Bounds;
            _targetBounds = Screen.FromControl(sessionControl).Bounds;

            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            Bounds = _targetBounds;

            _connectionBar = new RemoteSessionFullscreenConnectionBar(
                _targetBounds,
                sessionControl.Info?.Name ?? Language.Fullscreen);
            _connectionBar.ExitFullscreenRequested += ConnectionBar_ExitFullscreenRequested;

            _sessionControl.Disposed += SessionControl_Disposed;
            _originalParent.Controls.Remove(_sessionControl);
            Controls.Add(_sessionControl);
            _sessionControl.Dock = DockStyle.None;
            _sessionControl.Anchor = AnchorStyles.None;
            SyncSessionBounds();
        }

        internal static void Toggle(InterfaceControl sessionControl)
        {
            if (_current is { IsDisposed: false })
            {
                _current.Close();
                return;
            }

            _current = new RemoteSessionFullscreenForm(sessionControl);
            _current.Show(FrmMain.Default);
            _current.Bounds = _current._targetBounds;
            _current.SyncSessionBounds();
            _current.BringToFront();
            _current.Activate();
            sessionControl.Protocol.Focus();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            SyncSessionBounds();

            _connectionBar.Show(this);
            _connectionBar.ShowExpandedTemporarily();
            BeginInvoke(new Action(SyncSessionBounds));
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            SyncSessionBounds();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            _f11HotKeyRegistered = NativeMethods.RegisterHotKey(
                Handle,
                ExitWithF11HotKeyId,
                ModNoRepeat,
                (uint)Keys.F11);
            _ctrlAltEnterHotKeyRegistered = NativeMethods.RegisterHotKey(
                Handle,
                ExitWithCtrlAltEnterHotKeyId,
                ModControl | ModAlt | ModNoRepeat,
                (uint)Keys.Enter);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_f11HotKeyRegistered)
                NativeMethods.UnregisterHotKey(Handle, ExitWithF11HotKeyId);
            if (_ctrlAltEnterHotKeyRegistered)
                NativeMethods.UnregisterHotKey(Handle, ExitWithCtrlAltEnterHotKeyId);

            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmHotKey &&
                (message.WParam.ToInt32() == ExitWithF11HotKeyId ||
                 message.WParam.ToInt32() == ExitWithCtrlAltEnterHotKeyId))
            {
                Close();
                return;
            }

            base.WndProc(ref message);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape ||
                keyData == Keys.F11 ||
                keyData == (Keys.Control | Keys.Alt | Keys.Enter))
            {
                Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _connectionBar.ExitFullscreenRequested -= ConnectionBar_ExitFullscreenRequested;
            if (!_connectionBar.IsDisposed)
                _connectionBar.Close();

            RestoreSessionControl();
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (ReferenceEquals(_current, this))
                _current = null;

            base.OnFormClosed(e);
        }

        private void SessionControl_Disposed(object? sender, EventArgs e)
        {
            if (!IsDisposed && IsHandleCreated)
                BeginInvoke(new Action(Close));
        }

        private void ConnectionBar_ExitFullscreenRequested(object? sender, EventArgs e)
        {
            Close();
        }

        private void RestoreSessionControl()
        {
            if (_sessionRestored)
                return;

            _sessionRestored = true;
            _sessionControl.Disposed -= SessionControl_Disposed;

            if (_sessionControl.IsDisposed || _originalParent.IsDisposed)
                return;

            _sessionControl.Dock = DockStyle.None;
            Controls.Remove(_sessionControl);
            _originalParent.Controls.Add(_sessionControl);
            _originalParent.Controls.SetChildIndex(_sessionControl, _originalChildIndex);
            _sessionControl.Bounds = _originalBounds;
            _sessionControl.Anchor = _originalAnchor;
            _sessionControl.Dock = _originalDock;
            _originalParent.PerformLayout();
            _sessionControl.Protocol.RefreshRemoteSessionSize();
            _sessionControl.BringToFront();
            _sessionControl.Protocol.Focus();
        }

        private void SyncSessionBounds()
        {
            if (_sessionControl.IsDisposed || ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            _sessionControl.SetBounds(0, 0, ClientSize.Width, ClientSize.Height);
            _sessionControl.Protocol.RefreshRemoteSessionSize();
        }
    }

    [SupportedOSPlatform("windows")]
    internal sealed class RemoteSessionFullscreenConnectionBar : Form
    {
        private const int ExpandedWidth = 320;
        private const int ExpandedHeight = 38;
        private const int CollapsedWidth = 56;
        private const int CollapsedHeight = 4;
        private static readonly TimeSpan InitialDisplayDuration = TimeSpan.FromMilliseconds(1500);
        private static readonly TimeSpan HoverGracePeriod = TimeSpan.FromMilliseconds(450);

        private readonly Rectangle _screenBounds;
        private readonly int _expandedWidth;
        private readonly TableLayoutPanel _content;
        private readonly Panel _handle;
        private readonly System.Windows.Forms.Timer _hoverTimer;
        private DateTime _keepExpandedUntil;
        private bool _expanded;

        internal event EventHandler? ExitFullscreenRequested;

        internal RemoteSessionFullscreenConnectionBar(Rectangle screenBounds, string connectionName)
        {
            _screenBounds = screenBounds;

            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.FromArgb(32, 35, 39);
            FormBorderStyle = FormBorderStyle.None;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;

            int availableWidth = Math.Max(CollapsedWidth, screenBounds.Width - 16);
            _expandedWidth = Math.Min(ExpandedWidth, availableWidth);

            _content = new TableLayoutPanel
            {
                BackColor = BackColor,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(6, 3, 6, 3),
                RowCount = 1
            };
            _content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            _content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));

            Label connectionLabel = new()
            {
                AccessibleName = connectionName,
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                Font = SystemFonts.MessageBoxFont,
                ForeColor = Color.FromArgb(205, 209, 214),
                Padding = new Padding(5, 0, 4, 0),
                Text = connectionName,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Button exitButton = new()
            {
                AccessibleName = Language.ExitFullscreen,
                BackColor = Color.FromArgb(42, 46, 51),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = SystemFonts.MessageBoxFont,
                ForeColor = Color.White,
                Margin = new Padding(3, 0, 0, 0),
                MinimumSize = new Size(0, 30),
                TabStop = true,
                Text = $"{Language.ExitFullscreen}  (F11)",
                UseVisualStyleBackColor = false
            };
            exitButton.FlatAppearance.BorderSize = 0;
            exitButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 26, 29);
            exitButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(58, 63, 69);
            exitButton.Click += ExitButton_Click;

            _handle = new Panel
            {
                AccessibleName = $"{Language.Fullscreen} connection bar",
                BackColor = Color.FromArgb(112, 116, 121),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Fill
            };
            _handle.DoubleClick += ExitButton_Click;

            _content.Controls.Add(connectionLabel, 0, 0);
            _content.Controls.Add(exitButton, 1, 0);
            Controls.Add(_content);
            Controls.Add(_handle);

            MouseEnter += KeepExpanded;
            _content.MouseEnter += KeepExpanded;
            connectionLabel.MouseEnter += KeepExpanded;
            exitButton.MouseEnter += KeepExpanded;
            _handle.MouseEnter += KeepExpanded;

            _hoverTimer = new System.Windows.Forms.Timer { Interval = 150 };
            _hoverTimer.Tick += HoverTimer_Tick;
            _hoverTimer.Start();

            Collapse();
        }

        internal void ShowExpandedTemporarily()
        {
            _keepExpandedUntil = DateTime.UtcNow + InitialDisplayDuration;
            Expand();
            BringToFront();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _hoverTimer.Stop();
            _hoverTimer.Dispose();
            base.OnFormClosed(e);
        }

        private void ExitButton_Click(object? sender, EventArgs e)
        {
            ExitFullscreenRequested?.Invoke(this, EventArgs.Empty);
        }

        private void KeepExpanded(object? sender, EventArgs e)
        {
            _keepExpandedUntil = DateTime.UtcNow + HoverGracePeriod;
            Expand();
        }

        private void HoverTimer_Tick(object? sender, EventArgs e)
        {
            if (Bounds.Contains(Cursor.Position))
            {
                _keepExpandedUntil = DateTime.UtcNow + HoverGracePeriod;
                Expand();
                return;
            }

            if (DateTime.UtcNow >= _keepExpandedUntil)
                Collapse();
        }

        private void Expand()
        {
            if (!_expanded)
            {
                _expanded = true;
                _handle.Visible = false;
                _content.Visible = true;
                ClientSize = new Size(_expandedWidth, ExpandedHeight);
                Opacity = 0.96;
            }

            SetDesktopLocation(CalculateLeft(), _screenBounds.Top);
        }

        private void Collapse()
        {
            _expanded = false;
            _content.Visible = false;
            _handle.Visible = true;
            ClientSize = new Size(Math.Min(CollapsedWidth, _screenBounds.Width), CollapsedHeight);
            Opacity = 0.42;

            SetDesktopLocation(CalculateLeft(), _screenBounds.Top);
        }

        private int CalculateLeft()
        {
            return _screenBounds.Left + (_screenBounds.Width - Width) / 2;
        }

    }
}
