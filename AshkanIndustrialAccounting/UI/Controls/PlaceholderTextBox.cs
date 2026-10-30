using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AshkanIndustrialAccounting.UI.Controls
{
    /// <summary>Native Win32 cue-banner based placeholder compatible with .NET Framework 4.8.</summary>
    public sealed class PlaceholderTextBox : TextBox
    {
        private const int EmSetCueBanner = 0x1501;
        private string _placeholder = string.Empty;

        public string Placeholder
        {
            get { return _placeholder; }
            set { _placeholder = value ?? string.Empty; ApplyPlaceholder(); }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyPlaceholder();
        }

        private void ApplyPlaceholder()
        {
            if (IsHandleCreated)
                SendMessage(Handle, EmSetCueBanner, (IntPtr)1, _placeholder);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
    }
}
