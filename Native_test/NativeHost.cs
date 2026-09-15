using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Androidplayer_wpf.Native_test
{
    /// <summary>
    /// HwndHost that creates a child native HWND suitable for Direct2D/Direct3D rendering.
    /// Use the HandleCreated event to initialize native rendering (InitD2D / create swapchain, etc).
    /// </summary>
    public class NativeHost : HwndHost
    {
        public IntPtr Handle { get; private set; } = IntPtr.Zero;

        // Raised once the child HWND has been created and is usable.
        // Subscribe to this and initialize your native renderer there.
        public event Action<IntPtr>? HandleCreated;

        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;

        // BuildWindowCore is called by WPF when it's time to create the native window.
        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            // Create the child window with a tiny default size — we'll immediately move/resize it.
            Handle = CreateWindowEx(
                0,
                "STATIC",    // pre-registered class name (uppercase)
                "",
                WS_CHILD | WS_VISIBLE,
                0, 0,
                1, 1,
                hwndParent.Handle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (Handle == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to create native HWND. Win32 error = {err}");
            }

            // Fire the event so caller can Init rendering at the correct time.
            // Use Dispatcher to ensure this runs on UI thread and after WPF finishes its layout.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                HandleCreated?.Invoke(Handle);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);

            return new HandleRef(this, Handle);
        }

        // Destroy the window when WPF disposes this HwndHost.
        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            if (hwnd.Handle != IntPtr.Zero)
            {
                DestroyWindow(hwnd.Handle);
                Handle = IntPtr.Zero;
            }
        }

        // Called when WPF moves/resizes the host. Keep the native child sized to the same client area.
        protected override void OnWindowPositionChanged(Rect rcBoundingBox)
        {
            // rcBoundingBox contains the new size in device-independent units (DIPs).
            // Convert to int pixels (WPF typically uses device-independent pixels; this is fine for layout).
            int w = Math.Max(1, (int)Math.Round(rcBoundingBox.Width));
            int h = Math.Max(1, (int)Math.Round(rcBoundingBox.Height));

            if (Handle != IntPtr.Zero)
            {
                MoveWindow(Handle, 0, 0, w, h, true);
            }
            base.OnWindowPositionChanged(rcBoundingBox);
        }

        // If WPF calls Dispose on this host, ensure native window is destroyed.
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (Handle != IntPtr.Zero)
                {
                    DestroyWindow(Handle);
                    Handle = IntPtr.Zero;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // ---- Win32 imports ----
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(
            IntPtr hWnd,
            int X,
            int Y,
            int nWidth,
            int nHeight,
            bool bRepaint
        );
    }
}
