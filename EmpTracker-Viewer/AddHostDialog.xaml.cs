using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace EmpTrackerApp
{
    public partial class AddHostDialog
    {
        public AddHostDialog()
        {
            InitializeComponent();
        }

        private string _hostName;
        private string _login;
        private string _password;

        public void InstallAgent()
        {
            try
            {
                MessageBox.Show(RemoteStart(@"-cih -accepteula agent_setup.exe /s /v""/qn"""));
                Thread.Sleep(5000);
                MessageBox.Show(RemoteStart(@"-id -accepteula ""C:\Program Files (x86)\Employee Tracker\Employee Tracker Agent\EmpTrackerAgentApp.exe"""));
                Thread.Sleep(5000);
                MainWindow.Hosts.Add(new Host($"net.tcp://{_hostName}:24816/EmpTrackerService/tcp"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private string RemoteStart(string procArgs)
        {
            string result;
            var args = new StringBuilder();
            //begin argument string building
            args.AppendFormat("\\\\{0} ", _hostName);
            var proc = new Process();
            //properties for all starting processes
            var processInfo = new ProcessStartInfo
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            if (_login != "")
            {
                //credentials storage needed for proper psexec authentication - run cmdkey
                processInfo.FileName = "cmdkey.exe";
                processInfo.Arguments = $"/add:{_hostName} /user:{_login} /pass:{_password}";
                proc.StartInfo = processInfo;
                proc.Start();
                //login not empty - add credentials to arguments
                args.AppendFormat("-u {0} -p {1} ", _login, _password);
            }
            args.Append(procArgs);
            processInfo.FileName = "PsExec.exe";
            processInfo.Arguments = args.ToString();
            proc.StartInfo = processInfo;
            proc.Start();
            proc.WaitForExit(10000);
            //remove first 5 strings from psexec output
            var lines = Regex.Split(proc.StandardError.ReadToEnd(), "\r\n|\r|\n").Skip(5).ToArray();
            //the last word of psexec output contains exit code
            var code = lines[lines.Length-2].Split(' ').Last();
            switch (code)
            {
                case "-532462766.":
                    result = "Агент уже запущен на " + _hostName;
                    break;
                case "255.":
                    result = "Агент уже установлен на " + _hostName;
                    break;
                case "0.":
                    result = "Агент установлен на " + _hostName;
                    break;
                case "1607.":
                case "1603.":
                case "1601.":
                    result = "Ошибка установки агента. Попробуйте установить вручную.";
                    break;
                default:
                    //return full output
                    result = string.Join(Environment.NewLine, lines);
                    break;
            }
            return result;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (hostTextBox.Text != "")
            {
                _hostName = hostTextBox.Text;
                _login = loginTextBox.Text;
                _password = passwordBox.Password;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Требуется имя компьютера или IP-адрес!");
            }
        }
    }
}
