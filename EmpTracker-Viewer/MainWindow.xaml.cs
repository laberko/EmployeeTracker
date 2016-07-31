using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using EmpTrackerApp.EmpTrackerService;

namespace EmpTrackerApp
{
	public partial class MainWindow
	{
		private readonly List<Host> _hosts = new List<Host>();
		private Host _selectedHost;
		private readonly Timer _timer = new Timer();
		private int _interval = 5;
		private readonly FindCriteria _criteria = new FindCriteria(typeof(IEmpTrackerService));

		public MainWindow()
		{
			InitializeComponent();
			//service announcement listener
			var announcementService = new AnnouncementService();
			var announcementServiceHost = new ServiceHost(announcementService);
			announcementServiceHost.AddServiceEndpoint(new UdpAnnouncementEndpoint());
			announcementServiceHost.Open();
			announcementService.OnlineAnnouncementReceived += service_OnlineAnnouncementReceived;
			announcementService.OfflineAnnouncementReceived += service_OfflineAnnouncementReceived;
			//timer for windows listview refresh
			_timer.Elapsed += OnTimer;
			//initial hosts discovery
			DiscoverHostsAsync();
		}

		//online announcement received
		private void service_OnlineAnnouncementReceived(object sender, AnnouncementEventArgs e)
		{
			var address = e.EndpointDiscoveryMetadata.Address;
			//filter mex edpoints announcement and existing hosts
			if ((address.Uri.LocalPath != "/EmpTrackerService/tcp") || (_hosts.Any(host => host.Name == address.Uri.Host)))
				return;
			_hosts.Add(new Host(address.ToString()));
			RefreshHostsListView();
		}

		//offline announcement received
		private void service_OfflineAnnouncementReceived(object sender, AnnouncementEventArgs e)
		{
			var address = e.EndpointDiscoveryMetadata.Address;
			if (address.Uri.LocalPath != "/EmpTrackerService/tcp") return;
			_hosts.RemoveAll(host => host.Name == address.Uri.Host);
			RefreshHostsListView();
		}

		private void RefreshHostsListView()
		{
			hostsListView.ItemsSource = _hosts.OrderByDescending(host => host.Name).Select(host => host.Name);
		}

		private async void DiscoverHostsAsync()
		{
			await Task.Factory.StartNew(DiscoverHosts);
		}

		//service discovery
		private void DiscoverHosts()
		{
			Dispatcher.Invoke(() =>
			{
				refreshBar.IsIndeterminate = true;
				bottomTextBlock.Text = "Searching for computers...";
			});
			var client = new DiscoveryClient(new UdpDiscoveryEndpoint());
			var findResult = client.Find(_criteria);
			//add discovered hosts, filter mex endpoints and existing hosts
			foreach (var item in findResult.Endpoints
				.Where(item => item.Address.Uri.LocalPath == "/EmpTrackerService/tcp")
				.Where(item => _hosts.All(host => host.Name != item.Address.Uri.Host)))
			{
				_hosts.Add(new Host(item.Address.ToString()));
			}
			client.Close();
			Dispatcher.Invoke(() =>
			{
				refreshBar.IsIndeterminate = false;
				bottomTextBlock.Text = "";
				RefreshHostsListView();
			});
		}

		private void ShowHostWindows(Host host)
		{
			if (host == null) return;
			try
			{
				//ordered array of host's windows
				var hostCurrentWindows = host.GetCurrentWindows().OrderBy(w => w.Name).ToArray();
				//no windows - no host
				if (hostCurrentWindows.Length == 0)
				{
					_hosts.Remove(host);
					Dispatcher.Invoke(RefreshHostsListView);
					return;
				}
				//populate windows listview on the main thread
				Dispatcher.Invoke(() =>
				{
					//get index of active window
					var i = Array.IndexOf(hostCurrentWindows, hostCurrentWindows.FirstOrDefault(w => w.IsActive));
					windowsListView.ItemsSource = hostCurrentWindows;
					windowsListView.SelectedItem = windowsListView.Items[i];
				});
			}
			catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is TaskCanceledException)
			{
			}
			catch (Exception ex) when (ex is SocketException || ex is CommunicationException)
			{
				_hosts.Remove(host);
				Dispatcher.Invoke(RefreshHostsListView);
			}
		}

		private async void hostsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				_selectedHost = _hosts.FirstOrDefault(host => host.Name == e.AddedItems[0].ToString());
				if (_selectedHost != null)
				{
					//set timer interval for selected host
					_interval = _selectedHost.TimerInterval;
					_timer.Stop();
					_timer.Interval = _interval * 1000;
					_timer.Start();
				}
				setTimerTextBox.Text = _interval.ToString();
				await Task.Factory.StartNew(() => ShowHostWindows(_selectedHost));
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private async void getWindowsButton_Click(object sender, RoutedEventArgs e)
		{
			await Task.Factory.StartNew(() => ShowHostWindows(_selectedHost));
		}

		private void refreshHostListButton_Click(object sender, RoutedEventArgs e)
		{
			DiscoverHostsAsync();
		}

		private async void OnTimer(object sender, ElapsedEventArgs args)
		{
			await Task.Factory.StartNew(() => ShowHostWindows(_selectedHost));
		}

		private void setTimerButton_Click(object sender, RoutedEventArgs e)
		{
			//set timer interval for a host
			var previous = _selectedHost.TimerInterval;
			if ((int.TryParse(setTimerTextBox.Text, out _selectedHost.TimerInterval)) && (_selectedHost.TimerInterval > 0)) return;
			//error - revert to the previous value
			_selectedHost.TimerInterval = previous;
			MessageBox.Show("Must be a number!");
		}
	}
}
