using System.Threading.Tasks;

namespace EmpTrackerWCFLibrary
{
	public partial class EmpTrackerWcf : IEmpTrackerService
	{
		public async Task<ClientWindow[]> CurrentWindowsAsync()
		{
			return await Task.Factory.StartNew(() => CurrentWindows());
		}

		public async Task<WindowSummary[]> SummaryAsync()
		{
			return await Task.Factory.StartNew(() => ReadLog());
		}
	}
}
