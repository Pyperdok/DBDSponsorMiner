using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace DBDSponsor
{
    public partial class MainWindow : Window
    {
        private string path = $"logs/DBD_Sponsor_{DateTimeOffset.Now.ToUnixTimeSeconds()}.log";
        private WinForms.NotifyIcon notifyIcon = new WinForms.NotifyIcon();
        public MainWindow()
        {
            InitializeComponent();
            if(IsDuplicate())
            {
                MessageBox.Show("DBDSponsor is already started. Please don't try to start the duplicate");
                Environment.Exit(0);
            }
            Directory.CreateDirectory("logs");

            Miner.OutputDataReceived += MinerOutputDataReceived;
            Network.NetworkErrorReceived += NetworkErrorReceived;
            notifyIcon.Click += TrayClick;
            notifyIcon.Visible = true;
            notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.Info;
            notifyIcon.Icon = new Drawing.Icon("GoldCoin.ico");
            notifyIcon.Text = "DBD Sponsor v0.2";
            notifyIcon.BalloonTipText = "DBDSponsor is here";

            UpdateStats();
        }

        private void TrayClick(object sender, EventArgs e)
        {
            Show();
        }

        private void NetworkErrorReceived(Exception ex)
        {
            string output = $"Network Error: {ex.TargetSite} {ex.Message}";
            Console.WriteLine(output);
            StreamWriter log = File.AppendText(path);
            log.WriteLine(output);
            log.Close();
        }

        private bool IsDuplicate()
        {
            Process[] processes = Process.GetProcesses();
            string myProcName = Process.GetCurrentProcess().ProcessName;
            int count = 0;
            for (int i = 0; i < processes.Length; i++)
            {
                string procName = processes[i].ProcessName;
                if (procName == myProcName)
                {
                    count++;
                }
            }
            return count > 1 ? true : false;
        }
        
        private async void UpdateStats()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    Dispatcher.Invoke(() => {
                        string hashrate = Network.UpdatePoolHashrate();
                        string balance = Network.UpdatePoolBalance();
                        string voteWeight = Network.UpdateVoteWeight(WTB_SteamID64.Text);

                        L_HashratePool.Content = $"Hashrate Pool: {hashrate} h/s";
                        L_BalancePool.Content = $"Balance: {balance} (Aion)";
                        L_VoteWeight.Content = $"Vote Weight: {voteWeight} %";

                        StreamWriter log = File.AppendText(path);
                        log.WriteLine($"Hashrate Pool: {hashrate} h/s");
                        log.WriteLine($"Balance: {balance} (Aion)");
                        log.WriteLine($"Vote Weight: {voteWeight} %");
                        log.Close();
                    });
                    Thread.Sleep(60000);
                }
            });
        }

        private void MinerOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Dispatcher.Invoke(() =>
                {
                    if (TB_Log.Text.Length > 1048576)
                    {
                        TB_Log.Clear();
                    }
                    StreamWriter log = File.AppendText(path);
                    log.WriteLineAsync(e.Data);
                    log.Close();
                    TB_Log.AppendText("\n" + e.Data);
                });
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!Miner.IsStarted)
            {
                WTB_SteamID64.IsEnabled = false;
                BT_Start.Content = "Stop";
                BT_Start.Background = Brushes.Red;
                Miner.Start(WTB_SteamID64.Text, (int)Slider_Intensivity.Value);
            }
            else
            {
                WTB_SteamID64.IsEnabled = true;
                BT_Start.Content = "Start";
                BT_Start.Background = Brushes.Green;
                Miner.Stop();
            }
        }

        private void Slider_Intensivity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Miner.IsStarted)
            {
                BT_Start.Content = "Apply";
                BT_Start.Background = Brushes.Orange;
                (sender as Slider).SelectionEnd = e.NewValue;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void TB_Log_TextChanged(object sender, TextChangedEventArgs e)
        {
            (sender as TextBox).ScrollToEnd();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Miner.Stop();
        }

        private void BT_Close(object sender, RoutedEventArgs e)
        {
            Miner.Stop();
            Environment.Exit(0);
        }
        private void BT_Minimize(object sender, RoutedEventArgs e)
        {
            notifyIcon.ShowBalloonTip(5000);
            Hide();
        }
    }
}
