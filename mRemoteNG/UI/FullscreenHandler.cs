using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI
{
    public class FullscreenHandler(
        Form handledForm,
        ToolStripContainer toolStripContainer = null,
        DockPanel dockPanel = null)
    {
        private readonly Form _handledForm = handledForm;
        private readonly ToolStripContainer _toolStripContainer = toolStripContainer;
        private readonly DockPanel _dockPanel = dockPanel;
        private readonly List<DockContent> _hiddenDockContents = new();
        private FormWindowState _savedWindowState;
        private FormBorderStyle _savedBorderStyle;
        private Rectangle _savedBounds;
        private bool _savedTopToolStripPanelVisible;
        private DocumentStyle _savedDocumentStyle;
        private DockContent _savedActiveDocument;
        private bool _focusModeActive;
        private bool _value;

        public bool Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                if (!_value)
                    EnterFullscreen();
                else
                    ExitFullscreen();
                _value = value;
            }
        }

        private void EnterFullscreen()
        {
            _savedBorderStyle = _handledForm.FormBorderStyle;
            _savedWindowState = _handledForm.WindowState;
            _savedBounds = _handledForm.Bounds;

            EnterFocusMode();

            _handledForm.FormBorderStyle = FormBorderStyle.None;
            if (_handledForm.WindowState == FormWindowState.Maximized)
            {
                _handledForm.WindowState = FormWindowState.Normal;
            }

            _handledForm.WindowState = FormWindowState.Maximized;
        }

        private void ExitFullscreen()
        {
            _handledForm.FormBorderStyle = _savedBorderStyle;
            _handledForm.WindowState = _savedWindowState;
            _handledForm.Bounds = _savedBounds;

            ExitFocusMode();
        }

        private void EnterFocusMode()
        {
            if (_toolStripContainer == null || _dockPanel?.ActiveDocument is not DockContent activeDocument)
                return;

            _focusModeActive = true;
            _savedActiveDocument = activeDocument;
            _savedTopToolStripPanelVisible = _toolStripContainer.TopToolStripPanelVisible;
            _savedDocumentStyle = _dockPanel.DocumentStyle;

            _toolStripContainer.TopToolStripPanelVisible = false;

            _hiddenDockContents.Clear();
            foreach (IDockContent dockContent in _dockPanel.Contents)
            {
                if (dockContent is not DockContent content)
                    continue;

                DockState state = content.DockState;
                if (state == DockState.Document ||
                    state == DockState.Hidden ||
                    state == DockState.Unknown)
                {
                    continue;
                }

                _hiddenDockContents.Add(content);
            }

            foreach (DockContent content in _hiddenDockContents)
                content.DockHandler.Hide();

            _dockPanel.DocumentStyle = DocumentStyle.DockingSdi;
            _savedActiveDocument.DockHandler.Activate();
        }

        private void ExitFocusMode()
        {
            if (!_focusModeActive)
                return;

            _dockPanel.DocumentStyle = _savedDocumentStyle;

            foreach (DockContent content in _hiddenDockContents)
            {
                if (!content.IsDisposed)
                    content.DockHandler.Show();
            }

            _toolStripContainer.TopToolStripPanelVisible = _savedTopToolStripPanelVisible;

            if (_savedActiveDocument is { IsDisposed: false })
                _savedActiveDocument.DockHandler.Activate();

            _hiddenDockContents.Clear();
            _savedActiveDocument = null;
            _focusModeActive = false;
        }
    }
}
