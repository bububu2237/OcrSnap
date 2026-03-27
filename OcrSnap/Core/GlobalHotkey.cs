using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;

namespace OcrSnap.Core
{
    public class GlobalHotkey : IDisposable
    {
        private HwndSource? _source;
        private readonly Dictionary<int, Action> _hotkeyActions = new();
        private int _idCounter = 9000;
        private bool _disposed;

        public void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _source = HwndSource.FromHwnd(helper.EnsureHandle());
            _source.AddHook(WndProc);
        }

        public int Register(uint modifiers, uint key, Action action)
        {
            int id = _idCounter++;
            if (NativeMethods.RegisterHotKey(_source!.Handle, id, modifiers | NativeMethods.MOD_NOREPEAT, key))
            {
                _hotkeyActions[id] = action;
                return id;
            }
            return -1;
        }

        public void Unregister(int id)
        {
            if (_source != null)
                NativeMethods.UnregisterHotKey(_source.Handle, id);
            _hotkeyActions.Remove(id);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_hotkeyActions.TryGetValue(id, out var action))
                {
                    action();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var id in _hotkeyActions.Keys)
            {
                if (_source != null)
                    NativeMethods.UnregisterHotKey(_source.Handle, id);
            }
            _hotkeyActions.Clear();
            _source?.RemoveHook(WndProc);
        }
    }
}
