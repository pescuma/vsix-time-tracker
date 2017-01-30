using System;
using System.Runtime.InteropServices;

namespace VSIXTimeTracker
{
	// https://www.codeproject.com/Articles/17067/Controlling-The-Screen-Saver-With-C
	public static class ScreenSaver
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int flags);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool SystemParametersInfo(int uAction, int uParam, ref bool lpvParam, int flags);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern int PostMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr OpenDesktop(string hDesktop, int flags, bool inherit, uint desiredAccess);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool CloseDesktop(IntPtr hDesktop);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDesktopWindowsProc callback, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr GetForegroundWindow();

		private delegate bool EnumDesktopWindowsProc(IntPtr hDesktop, IntPtr lParam);

		private const int SPI_GETSCREENSAVERACTIVE = 16;
		private const int SPI_SETSCREENSAVERACTIVE = 17;
		private const int SPI_GETSCREENSAVERTIMEOUT = 14;
		private const int SPI_SETSCREENSAVERTIMEOUT = 15;
		private const int SPI_GETSCREENSAVERRUNNING = 114;
		private const int SPIF_SENDWININICHANGE = 2;

		private const uint DESKTOP_WRITEOBJECTS = 0x0080;
		private const uint DESKTOP_READOBJECTS = 0x0001;
		private const int WM_CLOSE = 16;

		// Returns TRUE if the screen saver is active 
		// (enabled, but not necessarily running).
		public static bool GetScreenSaverActive()
		{
			var isActive = false;

			SystemParametersInfo(SPI_GETSCREENSAVERACTIVE, 0, ref isActive, 0);
			return isActive;
		}

		// Pass in TRUE(1) to activate or FALSE(0) to deactivate
		// the screen saver.
		public static void SetScreenSaverActive(int Active)
		{
			var nullVar = 0;

			SystemParametersInfo(SPI_SETSCREENSAVERACTIVE, Active, ref nullVar, SPIF_SENDWININICHANGE);
		}

		// Returns the screen saver timeout setting, in seconds
		public static int GetScreenSaverTimeout()
		{
			var value = 0;

			SystemParametersInfo(SPI_GETSCREENSAVERTIMEOUT, 0, ref value, 0);
			return value;
		}

		// Pass in the number of seconds to set the screen saver
		// timeout value.
		public static void SetScreenSaverTimeout(int Value)
		{
			var nullVar = 0;

			SystemParametersInfo(SPI_SETSCREENSAVERTIMEOUT, Value, ref nullVar, SPIF_SENDWININICHANGE);
		}

		// Returns TRUE if the screen saver is actually running
		public static bool GetScreenSaverRunning()
		{
			var isRunning = false;

			SystemParametersInfo(SPI_GETSCREENSAVERRUNNING, 0, ref isRunning, 0);
			return isRunning;
		}

		// From Microsoft's Knowledge Base article #140723: 
		// http://support.microsoft.com/kb/140723
		// "How to force a screen saver to close once started 
		// in Windows NT, Windows 2000, and Windows Server 2003"
		public static void KillScreenSaver()
		{
			IntPtr hDesktop = OpenDesktop("Screen-saver", 0, false, DESKTOP_READOBJECTS | DESKTOP_WRITEOBJECTS);
			if (hDesktop != IntPtr.Zero)
			{
				EnumDesktopWindows(hDesktop, KillScreenSaverFunc, IntPtr.Zero);
				CloseDesktop(hDesktop);
			}
			else
			{
				PostMessage(GetForegroundWindow(), WM_CLOSE, 0, 0);
			}
		}

		private static bool KillScreenSaverFunc(IntPtr hWnd, IntPtr lParam)
		{
			if (IsWindowVisible(hWnd))
				PostMessage(hWnd, WM_CLOSE, 0, 0);
			return true;
		}
	}
}