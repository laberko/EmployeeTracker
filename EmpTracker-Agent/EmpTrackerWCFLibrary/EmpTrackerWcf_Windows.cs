using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EmpTrackerWCFLibrary
{
	public partial class EmpTrackerWcf
	{
		private delegate bool EnumWindowsProc(IntPtr windowPointer, int lParam);
		internal static ClientWindow[] CurrentWindows()
		{
			var activeWindow = GetForegroundWindow();
			var shellWindow = GetShellWindow();
			Process process;
			uint windowPid;
			var windows = new List<ClientWindow>();
			EnumWindows(delegate (IntPtr windowPointer, int lParam)
			{
				if (windowPointer == shellWindow)
					return true;
				if (!IsWindowVisible(windowPointer))
					return true;
				var length = GetWindowTextLength(windowPointer);
				if (length == 0)
					return true;
				GetWindowThreadProcessId(windowPointer, out windowPid);
				process = Process.GetProcessById(Convert.ToInt32(windowPid));
				var sb = new StringBuilder(length);
				GetWindowText(windowPointer, sb, length + 1);
				windows.Add(new ClientWindow
				{
					Name = sb.ToString(),
					ProcessName = process.ProcessName,
					IsActive = windowPointer == activeWindow
				});
				return true;
			}, 0);
			return windows.ToArray();
		}
	}
}
