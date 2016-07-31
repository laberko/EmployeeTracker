using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using EmpTrackerWCFLibrary;

namespace EmpTrackerAgentApp
{
	class WindowLogWriter
	{
		public readonly Timer LogTimer = new Timer(5000);
		private readonly string _logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EmpTracker\\Log");

		public WindowLogWriter()
		{
			LogTimer.Elapsed += OnTimer;
		}

		private void WriteLog()
		{
			var tracker = new EmpTrackerWcf();
			var activeWindow = tracker.CurrentWindows().FirstOrDefault(w => w.IsActive);
			try
			{
				if (activeWindow == null) return;
				using (var writer = File.AppendText(Path.Combine(_logPath, $"{DateTime.Now:yyyy-MM-dd}-processes.txt")))
				{
					writer.WriteLine(activeWindow.ProcessName);
				}
				using (var writer = File.AppendText(Path.Combine(_logPath, $"{DateTime.Now:yyyy-MM-dd}-windows.txt")))
				{
					writer.WriteLine(activeWindow.Name);
				}
			}
			catch (DirectoryNotFoundException)
			{
				Directory.CreateDirectory(_logPath);
			}
			catch (NullReferenceException)
			{
			}
		}

		private async void OnTimer(object sender, ElapsedEventArgs args)
		{
			await Task.Factory.StartNew(WriteLog);
		}
	}
}
