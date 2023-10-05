using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BooruDatasetTagManager
{
    /// <summary>
    /// Global keyboard event interception for the program
    /// </summary>
    /// 
    /// Allows for zero changes to existing code in order to intercept key-events from anywhere in the app.
    /// 
    /// Q: Why this heavy handed low level non-C# idiomatic approach to event subscription?
    /// A: Needed to avoid unintended side-effects where the original app-code has event handlers already 
    /// registered, and we want to override them completely in certain cases. The original event handlers
    /// in the program often make a lot of assumptions regarding the application state (which control has
    /// focus, what specific values are at a given location etc); all too messy to untangle. Easier to
    /// just disable those completely while our own key-bound shortcut actions are in effect.
    /// 
    public static class InterceptKeys
    {
        private const int WH_KEYBOARD_LL = 0xd;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static bool CtrlPressed = false;
        private static bool ShiftPressed = false;
        private static bool AltPressed = false;

        // required signature for event handlers that register for key events
        public static event Action<object, KeyEventArgs> keyEventHandlers;

        /// <summary>
        /// Ensures first-dibs on handling any keyboard event
        /// </summary>
        public static void AddKeyEventListener(Action<object, KeyEventArgs> callback)
        {
            keyEventHandlers += callback;
        }

        static InterceptKeys()
        {
            _hookID = SetHook(_proc);
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (System.Diagnostics.Process curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (System.Diagnostics.ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0 || keyEventHandlers == null || !isEventFromOurApp()) {
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }

            int vk = Marshal.ReadInt32(lParam);
            var keyDown = wParam == (IntPtr)WM_KEYDOWN;

            if (isCtrlKey(vk))   CtrlPressed  = keyDown;
            if (isShiftKey(vk))  ShiftPressed = keyDown;
            if (isAltKey(vk))    AltPressed   = keyDown;

            var csKey = ToKeys(vk);
            var evt = new KeyEventArgs(csKey);

            if (keyDown)                                             // only handle key-down events
            {
                keyEventHandlers.Invoke(null, evt);                  // call the registered custom callbacks "C#-Form-like"

                if (evt.Handled || evt.SuppressKeyPress)
                {
                    return (IntPtr)1;                                // swallow the event, blocking it from entering the app
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);   // let the event enter the app normally
        }

        private static bool isEventFromOurApp() {
            IntPtr hwnd = GetForegroundWindow();                     // whatever window is in the foreground
            GetWindowThreadProcessId(hwnd, out uint processId);      // pid for the window
            return processId == GetCurrentProcessId();               // if it's our pid, then our app's in the foreground and thus key events come from it.
        }


        private static bool isCtrlKey(int vk) {
            return vk == (int)Keys.ControlKey || vk == (int)Keys.LControlKey || vk == (int)Keys.RControlKey;
        }

        private static bool isShiftKey(int vk)
        {
            return vk == (int)Keys.Shift || vk == (int)Keys.LShiftKey || vk == (int)Keys.RShiftKey || vk == (int)Keys.ShiftKey;
        }

        private static bool isAltKey(int vk)
        {
            return vk == (int)Keys.Menu || vk == (int)Keys.LMenu || vk == (int)Keys.RMenu || vk == (int)Keys.Alt;
        }

        private static Keys ToKeys(int vkCode)
        {
            Keys k = (Keys)vkCode;
            if (CtrlPressed)  k |= Keys.Control;
            if (ShiftPressed) k |= Keys.Shift;
            if (AltPressed)   k |= Keys.Alt;
            return k;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentProcessId();
    }
}
