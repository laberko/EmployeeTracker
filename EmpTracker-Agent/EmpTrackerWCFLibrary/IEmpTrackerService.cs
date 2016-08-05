using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;

namespace EmpTrackerWCFLibrary
{
	[ServiceKnownType(typeof(ClientWindow))]
	[ServiceKnownType(typeof(WindowSummary))]
	[ServiceContract]
	public interface IEmpTrackerService
	{
		[OperationContract]
		Task<ClientWindow[]> CurrentWindowsAsync();

		[OperationContract]
		Task<WindowSummary[]> SummaryAsync();

	}
	[DataContract]
	public class ClientWindow
	{
		[DataMember]
		public string Name;
		[DataMember]
		public string ProcessName;
		[DataMember]
		public bool IsActive;
	}
	[DataContract]
	public class WindowSummary
	{
		[DataMember]
		public DateTime Date;
		[DataMember]
		public Dictionary<string, int> TopActiveWindows;
		[DataMember]
		public Dictionary<string, int> TopActiveProcesses;
	}
}
