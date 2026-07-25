using System;
using System.Drawing;
using System.Windows.Forms;
using AxMSTSCLib;
using mRemoteNG.App;
using mRemoteNG.Messages;
using MSTSCLib;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace mRemoteNG.Connection.Protocol.RDP
{
    [SupportedOSPlatform("windows")]
    /* RDP v8 requires Windows 7 with:
		* https://support.microsoft.com/en-us/kb/2592687 
		* OR
		* https://support.microsoft.com/en-us/kb/2923545
		* 
		* Windows 8+ support RDP v8 out of the box.
		*/
    public class RdpProtocol8 : RdpProtocol7
    {
        private MsRdpClient8NotSafeForScripting RdpClient8 => (MsRdpClient8NotSafeForScripting)((AxHost)Control).GetOcx();

        protected override RdpVersion RdpProtocolVersion => RDP.RdpVersion.Rdc8;
        protected FormWindowState LastWindowState = FormWindowState.Minimized;

        // Debounce timer to reduce flickering during resize
        private System.Timers.Timer _resizeDebounceTimer;
        private Size _pendingResizeSize;
        private bool _hasPendingResize = false;

        public RdpProtocol8()
        {
            // Initialize debounce timer (300ms delay).
            // Keep this in the constructor because it doesn't root the instance in any
            // external static object – it's safe for the temporary probing instances
            // created by RdpProtocolFactory.
            _resizeDebounceTimer = new System.Timers.Timer(300);
            _resizeDebounceTimer.AutoReset = false;
            _resizeDebounceTimer.Elapsed += ResizeDebounceTimer_Elapsed;
        }

        public override bool Initialize()
        {
            if (!base.Initialize())
                return false;

            if (RdpVersion < Versions.RDC81) return false; // minimum dll version checked, loaded MSTSCLIB dll version is not capable

            // Subscribe to static/external events here (not in the constructor) so that
            // temporary probing instances created by RdpProtocolFactory.RdpVersionSupported()
            // are not rooted and do not accumulate memory leaks or spurious callbacks.
            _frmMain.ResizeEnd += ResizeEnd;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            // https://learn.microsoft.com/en-us/windows/win32/termserv/imsrdpextendedsettings-property
            if (connectionInfo.UseRestrictedAdmin)
            {
                SetExtendedProperty("RestrictedLogon", true);
            }
            else if (connectionInfo.UseRCG)
            {
                SetExtendedProperty("DisableCredentialsDelegation", true);
                SetExtendedProperty("RedirectedAuthentication", true);
            }
            
            return true;
        }

        public override bool Fullscreen
        {
            get => base.Fullscreen;
            protected set
            {
                base.Fullscreen = value;
                DoResizeClient();
            }
        }

        protected override void Resize(object sender, EventArgs e)
        {
            if (_frmMain == null) return;

            // Skip resize entirely when minimized or minimizing
            if (_frmMain.WindowState == FormWindowState.Minimized) return;

            Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                $"Resize() called - WindowState={_frmMain.WindowState}, LastWindowState={LastWindowState}");

            // Update control size during resize to keep UI synchronized
            // Actual RDP session resize is deferred to ResizeEnd() to prevent flickering
            DoResizeControl();

            // Window state changes fire before the child layout has settled.
            // Debounce them just like manual drag-resizing so the session receives
            // the final panel size instead of the previous window size.
            if (LastWindowState != _frmMain.WindowState)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                    $"Resize() - Window state changed from {LastWindowState} to {_frmMain.WindowState}, scheduling final session resize");
                LastWindowState = _frmMain.WindowState;
                ScheduleDebouncedResize();
            }
            else
            {
                Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                    $"Resize() - Window state unchanged ({_frmMain.WindowState}), deferring to ResizeEnd()");
            }
        }

        protected override void ResizeEnd(object sender, EventArgs e)
        {
            if (_frmMain == null) return;

            // Skip resize when minimized
            if (_frmMain.WindowState == FormWindowState.Minimized) return;

            Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                $"ResizeEnd() called - WindowState={_frmMain.WindowState}");

            // Update window state tracking
            LastWindowState = _frmMain.WindowState;

            // Update control size immediately (no flicker)
            DoResizeControl();

            // Debounce the RDP session resize to reduce flickering
            ScheduleDebouncedResize();
        }

        private void ScheduleDebouncedResize()
        {
            if (InterfaceControl == null) return;

            // Store the pending size
            _pendingResizeSize = InterfaceControl.Size;
            _hasPendingResize = true;

            // Reset the timer (this delays the resize if called repeatedly)
            _resizeDebounceTimer?.Stop();
            _resizeDebounceTimer?.Start();

            Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                $"Resize debounced - will resize to {_pendingResizeSize.Width}x{_pendingResizeSize.Height} after 300ms");
        }

        private void ResizeDebounceTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_hasPendingResize) return;

            // Check if controls are still valid (not disposed during shutdown)
            if (Control == null || Control.IsDisposed || InterfaceControl == null || InterfaceControl.IsDisposed)
            {
                _hasPendingResize = false;
                return;
            }

            // Guard against the window handle not yet being created or already destroyed
            if (!InterfaceControl.IsHandleCreated)
            {
                _hasPendingResize = false;
                return;
            }

            _hasPendingResize = false;

            Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                $"Debounce timer fired - executing delayed resize to {_pendingResizeSize.Width}x{_pendingResizeSize.Height}");

            // Marshal to the UI thread because the pending resize accesses WinForms and COM objects.
            // Wrap in try/catch: even after the guards above, there is a disposal race between
            // this timer thread and the UI thread that can cause ObjectDisposedException or
            // InvalidOperationException from BeginInvoke.
            try
            {
                if (InterfaceControl.InvokeRequired)
                {
                    InterfaceControl.BeginInvoke(new Action(ApplyPendingResize));
                }
                else
                {
                    ApplyPendingResize();
                }
            }
            catch (ObjectDisposedException ex)
            {
                Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                    $"ResizeDebounceTimer_Elapsed: control disposed during BeginInvoke ({ex.GetType().Name})");
            }
            catch (InvalidOperationException ex)
            {
                Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                    $"ResizeDebounceTimer_Elapsed: control handle unavailable during BeginInvoke ({ex.GetType().Name})");
            }
        }

        private void ApplyPendingResize()
        {
            // The first Resize event can arrive before WinForms has finished laying out the
            // connection panel. Re-apply the aspect-fit bounds after the debounce interval
            // even while the RDP login sequence is still running. The session resize itself
            // remains guarded by loginComplete inside DoResizeClient().
            DoResizeControl();
            DoResizeClient();
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            // When display settings change (e.g., outer RDP session reconnects with a different
            // resolution/viewport), schedule a debounced resize so the inner RDP session is
            // updated to match the new panel dimensions once the display has settled.
            // SystemEvents.DisplaySettingsChanged can fire on a non-UI thread, so marshal
            // ScheduleDebouncedResize() back to the UI thread before touching UI state.
            if (!loginComplete) return;
            if (InterfaceControl == null || InterfaceControl.IsDisposed) return;

            if (InterfaceControl.InvokeRequired)
            {
                InterfaceControl.BeginInvoke(new Action(ScheduleDebouncedResize));
            }
            else
            {
                ScheduleDebouncedResize();
            }
        }

        protected override AxHost CreateActiveXRdpClientControl()
        {
            return new AxMsRdpClient8NotSafeForScripting();
        }

        protected override void RDPEvent_OnLoginComplete()
        {
            base.RDPEvent_OnLoginComplete();
            DoResizeControl();
            ScheduleDebouncedResize();
        }

        private void DoResizeClient()
        {
            if (!loginComplete)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                    $"Resize skipped for '{connectionInfo.Hostname}': Login not complete");
                return;
            }

            if (!InterfaceControl.Info.AutomaticResize)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                    $"Resize skipped for '{connectionInfo.Hostname}': AutomaticResize is disabled");
                return;
            }

            // FitToWindow keeps its connect-time resolution and uses scrollbars.
            // SmartSize also keeps its connect-time resolution because renegotiating
            // Linux/xRDP session dimensions can create or resume a different working
            // session. Only true fullscreen mode updates the remote session size.
            if (!RdpResizePolicy.UsesDynamicSessionResize(InterfaceControl.Info.Resolution))
            {
                Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                    $"Resize skipped for '{connectionInfo.Hostname}': Resolution is {InterfaceControl.Info.Resolution} (dynamic resize is disabled for this mode)");
                return;
            }

            Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                $"Resizing RDP connection to host '{connectionInfo.Hostname}'");

            try
            {
                // True fullscreen follows the monitor bounds.
                Size size = RdpResizePolicy.NormalizeDesktopSize(
                    Screen.FromControl(Control).Bounds.Size);
                Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                    $"Calling UpdateSessionDisplaySettings({size.Width}, {size.Height}) for '{connectionInfo.Hostname}' (Control.Size={Control.Size}, InterfaceControl.Size={InterfaceControl.Size})");

                UpdateSessionDisplaySettings((uint)size.Width, (uint)size.Height);
                RemoteDesktopSize = size;
                Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                    $"Successfully resized RDP session for '{connectionInfo.Hostname}' to {size.Width}x{size.Height}");
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage(
                    string.Format(Language.ChangeConnectionResolutionError, connectionInfo.Hostname),
                    ex, MessageClass.WarningMsg, false);
            }
        }

        private bool DoResizeControl()
        {
            if (Control == null || InterfaceControl == null) return false;

            // Check if controls are being disposed during shutdown
            if (Control.IsDisposed || InterfaceControl.IsDisposed) return false;

            // FitToWindow: control is undocked at a fixed size with scrollbars; don't touch it.
            if (InterfaceControl.Info.Resolution == RDPResolutions.FitToWindow)
                return false;

            Rectangle viewport = InterfaceControl.DisplayRectangle;
            if (viewport.Size == Size.Empty) return false;

            Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                $"DoResizeControl - Before: Control.Bounds={Control.Bounds}, Viewport={viewport}, Control.Dock={Control.Dock}");

            if (InterfaceControl.Info.Resolution == RDPResolutions.SmartSize)
            {
                Rectangle targetBounds = RdpResizePolicy.CalculateAspectFitBounds(
                    viewport,
                    RemoteDesktopSize);

                Control.Dock = DockStyle.None;
                Control.Anchor = AnchorStyles.None;

                bool boundsChanged = Control.Bounds != targetBounds;
                if (boundsChanged)
                    Control.Bounds = targetBounds;

                // Reapply this after setting the AxHost bounds. MSTSC can keep its
                // previous renderer size during the connection/login animation unless
                // SmartSizing is refreshed after the resize.
                SmartSize = true;
                Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                    $"DoResizeControl - SmartSize aspect-fit bounds={Control.Bounds}, RemoteDesktopSize={RemoteDesktopSize}");
                return boundsChanged;
            }

            // Fullscreen uses the complete viewport.
            bool wasDocked = Control.Dock == DockStyle.Fill;
            if (wasDocked)
                Control.Dock = DockStyle.None;

            if (Control.Bounds == viewport)
            {
                if (wasDocked)
                    Control.Dock = DockStyle.Fill;

                Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                    "DoResizeControl - Skipped: bounds already match viewport");
                return false;
            }

            Control.Bounds = viewport;

            if (wasDocked)
                Control.Dock = DockStyle.Fill;

            Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                $"DoResizeControl - After: Control.Bounds={Control.Bounds}, Control.Dock={Control.Dock}");

            return true;
        }

        protected virtual void UpdateSessionDisplaySettings(uint width, uint height)
        {
            if (RdpClient8 != null)
            {
                RdpClient8.Reconnect(width, height);
            }
        }

        public override void Close()
        {
            // Unsubscribe from external/static events to prevent memory leaks
            _frmMain.ResizeEnd -= ResizeEnd;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

            // Clean up debounce timer
            if (_resizeDebounceTimer != null)
            {
                _resizeDebounceTimer.Stop();
                _resizeDebounceTimer.Elapsed -= ResizeDebounceTimer_Elapsed;
                _resizeDebounceTimer.Dispose();
                _resizeDebounceTimer = null;
            }

            base.Close();
        }

    }
}
