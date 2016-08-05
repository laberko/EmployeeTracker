using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace EmpTrackerWCFLibrary
{
	public class WindowLogging
	{
		public readonly Timer LogTimer = new Timer(Convert.ToDouble(ConfigurationManager.AppSettings["timerSeconds"]) * 1000);
		private readonly string _logPath;

		public WindowLogging(string path)
		{
			LogTimer.Elapsed += OnTimer;
		    _logPath = path;
		}

		private async void OnTimer(object sender, ElapsedEventArgs args)
		{
			await Task.Factory.StartNew(WriteLog);
		}

		private void WriteLog()
		{
			var activeWindow = EmpTrackerWcf.CurrentWindows().FirstOrDefault(w => w.IsActive);
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
                var files = Directory.EnumerateFiles(_logPath).ToList();
			    foreach (var file in files)
			    {
                    try
                    {
                        if ((DateTime.Now - DateTime.Parse(Path.GetFileName(file).Substring(0, 10))) >
                            new TimeSpan(Convert.ToInt32(ConfigurationManager.AppSettings["daysToStore"]), 0, 0, 0))
                        {
                            File.Delete(file);
                        }
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is NullReferenceException)
                    {
                    }
			    }
            }
			catch (DirectoryNotFoundException)
			{
				Directory.CreateDirectory(_logPath);
			}
		}
	}
}
