using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace EmpTrackerWCFLibrary
{
	public partial class EmpTrackerWcf
	{
        private static readonly string LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "EmpTracker");
        private static WindowSummary[] ReadLog()
		{
			var logEntries = new List<LogEntry>();
			var files = Directory.EnumerateFiles(LogPath).ToList();
			foreach (var file in files)
			{
				try
				{
					var date = DateTime.Parse(Path.GetFileName(file).Substring(0, 10));
					logEntries.AddRange(File.ReadLines(file).Select(line => new LogEntry
					{
						Date = date,
						Name = line,
						LType = file.Contains("processes") ? LogEntry.LogType.Process : LogEntry.LogType.Window
					}));
				}
				catch (Exception ex) when (ex is ArgumentException || ex is NullReferenceException)
				{
				}
			}
			return logEntries.GroupBy(l => l.Date).Select(date => new WindowSummary
			{
				Date = date.Key,
				TopActiveWindows = date.Where(l => l.LType == LogEntry.LogType.Window).GroupBy(l => l.Name).ToDictionary(l => l.Key, l => l.Count()),
				TopActiveProcesses = date.Where(l => l.LType == LogEntry.LogType.Process).GroupBy(l => l.Name).ToDictionary(l => l.Key, l => l.Count())
			}).ToArray();
		}

		private struct LogEntry
		{
			public DateTime Date;
			public string Name;
			public LogType LType;
			public enum LogType { Process, Window };
		}
	}
}
