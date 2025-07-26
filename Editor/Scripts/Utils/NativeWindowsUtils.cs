using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HMSUnitySDK.Editor
{
    public static class NativeWindowsUtils
    {
        [DllImport("user32.dll")]
        private static extern uint GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);

        private static IntPtr _hWnd_UnityWindow;
        private const int SW_RESTORE = 9;

        public static void CacheUnityWindowHandle()
        {
            _hWnd_UnityWindow = (IntPtr)GetActiveWindow();
            Debug.Log($"Unity window handle: {_hWnd_UnityWindow}");
        }

        public static void FocusUnityWindow()
        {
            Debug.Log("Tentando restaurar o foco da janela do Unity Editor...");

            if (_hWnd_UnityWindow != System.IntPtr.Zero)
            {
                // Restaura a janela se ela estiver minimizada e a traz para o primeiro plano
                ShowWindow(_hWnd_UnityWindow, SW_RESTORE);
                SetForegroundWindow(_hWnd_UnityWindow);
                Debug.Log("Foco da janela do Unity Editor restaurado (tentativa).");
            }
            else
            {
                Debug.LogError("Não foi possível obter o handle da janela ativa. O Unity Editor pode não estar focado ou visível.");
            }
        }
    }
}