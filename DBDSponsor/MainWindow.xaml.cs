using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.OLE.Interop;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Windows.Input;

namespace DBDSponsor
{
    public partial class MainWindow : Window
    {
        Process miner;
        int intensivity = 50;
        string minerName = "miner";
        string SteamID64 { get => WTB_SteamID64.Text; }
        public MainWindow()
        {
            InitializeComponent();
            //KillMiner();
            UpdateStats();
            StreamWriter writer = new StreamWriter("DBDSponsor.log");
            
            writer.WriteLine("Hello");
            writer.Close();
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
                        string voteWeight = Network.UpdateVoteWeight(SteamID64);

                        L_HashratePool.Content = $"Hashrate Pool: {hashrate} h/s";
                        L_BalancePool.Content = $"Balance: {balance} (Aion)";
                        L_VoteWeight.Content = $"Vote Weight: {voteWeight} %";
                    });
                    Thread.Sleep(60000);
                }
            });
        }

        private void Miner_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (TB_Log.Text.Length > 1048576)
                {
                    TB_Log.Clear();
                }

                TB_Log.AppendText("\n" + e.Data);
            });
        }

        public void KillMiner()
        {
            Process[] processes = Process.GetProcesses();
            for (int i = 0; i < processes.Length; i++)
            {
                string procName = processes[i].ProcessName;
                if (procName == minerName)
                {
                    processes[i].Kill();
                }
            }
        }
        private bool IsValidSteamID64()
        {
            HttpWebResponse result = Network.Http($"https://steamcommunity.com/profiles/{SteamID64}/?xml=1");
            string body = new StreamReader(result.GetResponseStream()).ReadToEnd();
            bool isValid = false;
            if (body.IndexOf(SteamID64) != -1)
            {
                isValid = true;
            }

            return isValid;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (miner == null)
            {
                if (!IsValidSteamID64())
                {
                    MessageBox.Show("SteamID64 is incorrect", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                WTB_SteamID64.IsEnabled = false;
                KillMiner();
                BT_Start.Content = "Stop";
                BT_Start.Background = Brushes.Red;
                intensivity = (int)Slider_Intensivity.Value;

                ProcessStartInfo startInfo = new ProcessStartInfo(minerName);
                startInfo.Arguments = $"-i {intensivity} --tfan 60 -t 70 --algo 210_9 --pers AION0PoW --server eu.aionpool.tech:2222 --user 0xa030ba8b2742fa1e41e10a982b91806f279dd6b90b46552144f2dfe1fe48d37e.{SteamID64} --pass x";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;

                miner = Process.Start(startInfo);
                miner.OutputDataReceived += Miner_OutputDataReceived;
                miner.BeginOutputReadLine();
            }
            else
            {
                WTB_SteamID64.IsEnabled = true;
                KillMiner();
                miner = null;
                BT_Start.Content = "Start";
                BT_Start.Background = Brushes.Green;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            KillMiner();
            miner = null;
        }

        private void Slider_Intensivity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (miner != null)
            {
                BT_Start.Content = "Apply";
                BT_Start.Background = Brushes.Orange;
            }
            (sender as Slider).SelectionEnd = e.NewValue;
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        private void WTB_SteamID64_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {          
        }

        private void TB_Log_TextChanged(object sender, TextChangedEventArgs e)
        {
            (sender as TextBox).ScrollToEnd();
        }

        private void BT_Close(object sender, RoutedEventArgs e)
        {
            KillMiner();
            Environment.Exit(0);
        }
    }
}
