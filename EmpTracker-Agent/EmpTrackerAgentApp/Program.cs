using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmpTrackerWCFLibrary;

namespace EmpTrackerAgentApp
{
	class Program
	{
		static readonly ManualResetEvent QuitEvent = new ManualResetEvent(false);
		static void Main(string[] args)
		{
			var logWriter = new WindowLogWriter();
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
