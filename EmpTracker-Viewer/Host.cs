using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using EmpTrackerApp.EmpTrackerService;

namespace EmpTrackerApp
{
	//monitored host class
	internal class Host
	{
		private readonly EndpointAddress _address;
		public int TimerInterval = 5;
		public string Name => _address.Uri.Host;

		public Host(string address)
		{
			_address = new EndpointAddress(address);
		}

		//invoke method CurrentWindows() on wcf host
		public IEnumerable<ClientWindow> GetCurrentWindows()
		{
			var windows = new List<ClientWindow>();
			try
			{
				Job((delegate(IEmpTrackerService client)
				{
					windows = client.CurrentWindows().ToList();
				}), _address);
			}
			catch
			{
				return null;
			}
			return windows;
		}

		//invoke action delegate on wcf host
		private static void Job<T>(Action<T> action, EndpointAddress address)
		{

			var factory = new ChannelFactory<T>("TcpEndpoint", address);
			var client = factory.CreateChannel();
			try
			{
				action(client);
				((IClientChannel)client).Close();
				factory.Close();
			}
			catch
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
}
