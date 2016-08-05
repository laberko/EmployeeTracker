using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using EmpTrackerApp.EmpTrackerService;

namespace EmpTrackerApp
{
	//monitored host class
    public class Host
	{
		private readonly EndpointAddress _address;
		public int TimerInterval = 5;
		public string Name => _address.Uri.Host;

		public Host(string address)
		{
			_address = new EndpointAddress(address);
		}

        //invoke method CurrentWindowsAsync() on wcf host
        public async Task<ClientWindow[]> GetCurrentWindows()
		{
			var factory = new ChannelFactory<IEmpTrackerService>("TcpEndpoint", _address);
			var client = factory.CreateChannel();
			try
			{
				var windows = await client.CurrentWindowsAsync();
				CloseClient(factory, client);
				return windows;
			}
			catch
			{
				CloseClient(factory, client);
				return null;
			}
		}

        //invoke method SummaryAsync() on wcf host
        public async Task<string> GetSummary()
		{
			var factory = new ChannelFactory<IEmpTrackerService>("TcpEndpoint", _address);
			var client = factory.CreateChannel();
			try
			{
				var sb = new StringBuilder();
				var summary = (await client.SummaryAsync()).OrderByDescending(s => s.Date).ToArray();
				CloseClient(factory, client);
				foreach (var item in summary)
				{
					var date = item.Date;
					var totalWinLogs = item.TopActiveWindows.Sum(l => l.Value);
					var totalProcLogs = item.TopActiveProcesses.Sum(l => l.Value);
					sb.AppendFormat("\n\tСтатистика за {0}\n", date.ToString("D"));
					sb.AppendLine("\t(в % от времени наблюдения)\n");
					sb.AppendLine("\tПо процессам:\n");
					foreach (var process in item.TopActiveProcesses.OrderByDescending(p => p.Value))
					{
						sb.AppendFormat("{0} - {1:0.00}%\n", process.Key, (double)process.Value * 100 / totalProcLogs);
					}
					sb.AppendLine("\n\tПо окнам:\n");
					foreach (var process in item.TopActiveWindows.OrderByDescending(p => p.Value))
					{
						sb.AppendFormat("{0} - {1:0.00}%\n", process.Key, (double)process.Value * 100 / totalWinLogs);
					}
				}
				return sb.ToString();
			}
			catch
			{
				CloseClient(factory, client);
				return null;
			}
		}

        //close or abort connection
		private static void CloseClient(ICommunicationObject factory, IEmpTrackerService client)
		{
			var clientInstance = ((IClientChannel)client);
			if (clientInstance.State == CommunicationState.Faulted)
			{
				clientInstance.Abort();
				factory.Abort();
			}
			else if (clientInstance.State != CommunicationState.Closed)
			{
				clientInstance.Close();
				factory.Close();
			}
		}

	}
}
