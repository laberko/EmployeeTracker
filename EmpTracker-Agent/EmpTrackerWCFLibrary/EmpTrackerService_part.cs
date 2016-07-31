using System;
using System.Runtime.InteropServices;
using System.Text;

namespace EmpTrackerWCFLibrary
{
	public partial class EmpTrackerWcf
	{
		private delegate bool EnumWindowsProc(IntPtr windowPointer, int lParam);

		[DllImport("USER32.DLL", SetLastError = true)]
		private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

		[DllImport("USER32.DLL", SetLastError = true)]
		private static extern int GetWindowText(IntPtr windowPointer, StringBuilder sb, int nMaxCount);

		[DllImport("USER32.DLL", SetLastError = true)]
		private static extern int GetWindowTextLength(IntPtr windowPointer);

		[DllImport("USER32.DLL", SetLastError = true)]
		private static extern bool IsWindowVisible(IntPtr windowPointer);

		[DllImport("USER32.DLL", SetLastError = true)]
		private static extern IntPtr GetShellWindow();

		[DllImport("USER32.DLL", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId(IntPtr windowPointer, out uint lpdwProcessId);

		[DllImport("USER32.DLL", SetLastError = true)]
		static extern IntPtr GetForegroundWindow();
	}
}