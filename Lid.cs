using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VSIXTimeTracker
{
	internal class Lid
	{
		public event Action<bool> StatusChanged;

		public Lid(Window window)
		{
			var source = (HwndSource) PresentationSource.FromVisual(window);
			Debug.Assert(source != null, "source != null");

			source.AddHook(MessageProc);

			IntPtr hMonitorOn = RegisterPowerSettingNotification(source.Handle, ref GUID_LIDSWITCH_STATE_CHANGE,
				DEVICE_NOTIFY_WINDOW_HANDLE);
			window.Closing += (s, a) => UnregisterPowerSettingNotification(hMonitorOn);
		}

		private IntPtr MessageProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_POWERBROADCAST && (int) wParam == PBT_POWERSETTINGCHANGE)
			{
				var ps = (POWERBROADCAST_SETTING) Marshal.PtrToStructure(lParam, typeof(POWERBROADCAST_SETTING));
				if (ps.PowerSetting == GUID_LIDSWITCH_STATE_CHANGE)
				{
					bool isLidOpen = ps.Data != 0;
					StatusChanged?.Invoke(isLidOpen);
				}
			}

			return IntPtr.Zero;
		}

// ReSharper disable InconsistentNaming

		[DllImport("User32", SetLastError = true, EntryPoint = "RegisterPowerSettingNotification",
			CallingConvention = CallingConvention.StdCall)]
		private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid powerSettingGuid, int flags);

		[DllImport("User32", EntryPoint = "UnregisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall)
		]
		private static extern bool UnregisterPowerSettingNotification(IntPtr handle);

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		internal struct POWERBROADCAST_SETTING
		{
			public Guid PowerSetting;
			public uint DataLength;
			public byte Data;
		}

		private const int WM_POWERBROADCAST = 0x0218;
		private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;
		private const int PBT_POWERSETTINGCHANGE = 0x8013;

		private static Guid GUID_LIDSWITCH_STATE_CHANGE = new Guid(0xBA3E0F4D, 0xB817, 0x4094, 0xA2, 0xD1, 0xD5, 0x63, 0x79,
			0xE6, 0xA0, 0xF3);

// ReSharper restore InconsistentNaming
	}
}