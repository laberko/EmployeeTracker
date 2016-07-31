using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace EmpTrackerWCFLibrary
{
	[ServiceKnownType(typeof(ClientWindow))]
	[ServiceKnownType(typeof(WindowSummary))]
	[ServiceContract]
	public interface IEmpTrackerService
	{
		[OperationContract]
		ClientWindow[] CurrentWindows();

		[OperationContract]
		WindowSummary[] Summary();
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
		public string Date;
		[DataMember]
		public Dictionary<string, int> TopActiveWindows;
		[DataMember]
		public Dictionary<string, int> TopActiveProcesses;
	}
}
