using System;
using System.Collections.Generic;
using System.Linq;
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
		public static readonly List<Host> Hosts = new List<Host>();
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
			if ((address.Uri.LocalPath != "/EmpTrackerService/tcp") || (Hosts.Any(host => host.Name == address.Uri.Host)))
				return;
			Hosts.Add(new Host(address.ToString()));
			RefreshHostsListView();
		}

		//offline announcement received
		private void service_OfflineAnnouncementReceived(object sender, AnnouncementEventArgs e)
		{
			var address = e.EndpointDiscoveryMetadata.Address;
			if (address.Uri.LocalPath != "/EmpTrackerService/tcp") return;
			Hosts.RemoveAll(host => host.Name == address.Uri.Host);
			RefreshHostsListView();
		}

        //get hosts from list to screen
	    private void RefreshHostsListView()
		{
			hostsListView.ItemsSource = Hosts.OrderByDescending(host => host.Name).Select(host => host.Name).Distinct();
		}

        //start service discovery task
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
				bottomTextBlock.Text = "Идет поиск компьютеров в локальной сети, подождите...";
			});
			var client = new DiscoveryClient(new UdpDiscoveryEndpoint());
			var findResult = client.Find(_criteria);
			//add discovered hosts, filter mex endpoints and existing hosts
			foreach (var item in findResult.Endpoints
				.Where(item => item.Address.Uri.LocalPath == "/EmpTrackerService/tcp")
				.Where(item => Hosts.All(host => host.Name != item.Address.Uri.Host)))
			{
				Hosts.Add(new Host(item.Address.ToString()));
			}
			client.Close();
			Dispatcher.Invoke(() =>
			{
				refreshBar.IsIndeterminate = false;
				bottomTextBlock.Text = "";
				RefreshHostsListView();
			});
		}

        //get current windows information from host
		private async void ShowHostWindows(Host host)
		{
			if (host == null) return;
            try
            {
				//ordered array of host's windows
				var hostCurrentWindows = (await Task.Factory.StartNew(() => host.GetCurrentWindows())).Result.OrderBy(w => w.Name).ToArray();
				//no windows - no host
				if (!hostCurrentWindows.Any())
				{
					Hosts.Remove(host);
					Dispatcher.Invoke(RefreshHostsListView);
					return;
				}
				//populate windows listview on the main thread
				Dispatcher.Invoke(() =>
				{
					//get index of active window
					windowsListView.ItemsSource = hostCurrentWindows;
					var i = Array.IndexOf(hostCurrentWindows, hostCurrentWindows.FirstOrDefault(w => w.IsActive));
					if (i > 0)
					{
                        //select active window
						windowsListView.SelectedItem = windowsListView.Items[i];
					}
				});
			}
			catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is TaskCanceledException)
			{
			}
			catch (Exception)
			{
                //remove faulty host
				Hosts.Remove(host);
				Dispatcher.Invoke(RefreshHostsListView);
			}
		}

        //get windows statistics from host
		private async void ShowSummary(Host host)
		{
            if (host == null) return;
            try
            {
		        var summary = await host.GetSummary();
		        if (summary == null)
		        {
                    //remove faulty host
                    Hosts.Remove(host);
		            Dispatcher.Invoke(RefreshHostsListView);
		            return;
		        }
		        Dispatcher.Invoke(() =>
		        {
		            summaryTextBox.Text = summary;
		        });
		    }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is TaskCanceledException)
            {
            }
            catch (Exception)
            {
                Hosts.Remove(host);
                Dispatcher.Invoke(RefreshHostsListView);
            }
        }

        //refresh host windows information on timer
        private async void OnTimer(object sender, ElapsedEventArgs args)
        {
            await Task.Factory.StartNew(() => ShowHostWindows(_selectedHost));
        }

        //some host selected
        private async void hostsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				_selectedHost = Hosts.FirstOrDefault(host => host.Name == e.AddedItems[0].ToString());
                if (_selectedHost != null)
                {
                    //set timer interval for selected host
                    _interval = _selectedHost.TimerInterval;
                    _timer.Stop();
                    _timer.Interval = _interval * 1000;
                    _timer.Start();
                    setTimerTextBox.Text = _interval.ToString();
                    await Task.Factory.StartNew(() => ShowHostWindows(_selectedHost));
                    await Task.Factory.StartNew(() => ShowSummary(_selectedHost));
                }
                else
                {
                    windowsListView.ItemsSource = null;
                    summaryTextBox.Clear();
                }
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

        //refresh current windows information button pressed
		private async void getWindowsButton_Click(object sender, RoutedEventArgs e)
		{
			await Task.Factory.StartNew(() => ShowHostWindows(_selectedHost));
		}

        //refresh host list button pressed
        private void refreshHostListButton_Click(object sender, RoutedEventArgs e)
		{
			DiscoverHostsAsync();
		}

        //set timer for a host button pressed
        private void setTimerButton_Click(object sender, RoutedEventArgs e)
		{
			var previous = _selectedHost.TimerInterval;
			if ((int.TryParse(setTimerTextBox.Text, out _selectedHost.TimerInterval)) && (_selectedHost.TimerInterval > 0)) return;
			//error - revert to the previous value
			_selectedHost.TimerInterval = previous;
			MessageBox.Show("Неправильное значение!");
		}

        //show add host modal dialog
        private async void addHostButton_Click(object sender, RoutedEventArgs e)
        {
            var addHostDialog = new AddHostDialog {Owner = this};
            addHostDialog.ShowDialog();
            if (addHostDialog.DialogResult != true) return;
            //start agent installation
            await Task.Factory.StartNew(() =>
            {
                addHostDialog.InstallAgent();
                Dispatcher.Invoke(RefreshHostsListView);
            });
        }

        //refresh host's windows statistics
        private async void getSummaryButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Factory.StartNew(() => ShowSummary(_selectedHost));
        }
    }
}
