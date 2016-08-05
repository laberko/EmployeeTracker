using System;
using System.IO;
using System.ServiceModel;
using System.Threading;
using EmpTrackerWCFLibrary;

namespace EmpTrackerAgentApp
{
	class Program
	{
		static readonly ManualResetEvent QuitEvent = new ManualResetEvent(false);
        static readonly string LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "EmpTracker");
        static void Main(string[] args)
		{
			var logWriter = new WindowLogging(LogPath);
			logWriter.LogTimer.Start();
			using (var host = new ServiceHost(typeof(EmpTrackerWcf)))
			{
				host.Open();
				//keep program running
				Console.CancelKeyPress += (sender, eArgs) => 
				{
					QuitEvent.Set();
					eArgs.Cancel = true;
				};
				QuitEvent.WaitOne();
				host.Close();
			}
		}
	}
}
