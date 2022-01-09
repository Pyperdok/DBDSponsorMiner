using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Input;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace DBDSponsor
{
    public partial class MainWindow : Window
    {
        public readonly string path = $"logs/DBD_Sponsor_{DateTimeOffset.Now.ToUnixTimeSeconds()}.log";
        private bool IsUser = false;
        private bool IsUserRestartMiner = false;
        private readonly WinForms.NotifyIcon notifyIcon = new WinForms.NotifyIcon()
        {
            Visible = true,
            BalloonTipIcon = WinForms.ToolTipIcon.Info,
            Icon = new Drawing.Icon("GoldCoin.ico"),
            Text = "DBD Sponsor v0.3",
            BalloonTipText = "DBDSponsor is here"
        };

        public MainWindow()
        {
            InitializeComponent();

            if (IsDuplicate())
            {
                MessageBox.Show("DBDSponsor is already started. Please don't try to start the duplicate");
                Environment.Exit(0);
            }
            Directory.CreateDirectory("logs");

            //Events
            Miner.OutputDataReceived += MinerOutputDataReceived;
            Miner.Started += MinerStarted;
            Miner.Exited += MinerExited;
            Calculator.ProfitCoinUpdated += ProfitCoinUpdated;
            Stat.ErrorReceived += StatErrorReceived;
            Stat.Window = this;
            notifyIcon.Click += TrayClick;

            Stat.UpdateStatsAsync();
            Calculator.UpdateProfitCoinAsync();
        }

        private void ProfitCoinUpdated(string coin)
        {
            if(Miner.ProfitCoin != coin && Miner.IsWorking)
            {
                Console.WriteLine("Profit coin has changed. Restarting the miner.");
                Miner.ProfitCoin = coin; //Restart Miner if profit coin will have changed
                IsUser = true;
                Miner.Stop();
                Miner.Start();
            }
            Miner.ProfitCoin = coin;
            Dispatcher.Invoke(() =>
            {
                if (coin != null) {
                    L_Gpu.Content = "GPU: OK";
                    L_Coin.Content = $"COIN: {coin}";
                    L_Gpu.Foreground = Brushes.Lime;
                    L_Coin.Foreground = Brushes.Lime;
                }
                else if(Miner.ProfitCoin == null)
                {
                    BT_Start.IsEnabled = false;
                    L_Gpu.Content = "GPU: INVALID";
                    L_Coin.Content = "COIN: INVALID";
                    L_Gpu.Foreground = Brushes.Red;
                    L_Coin.Foreground = Brushes.Red;

                    string message = "Server can't identificate your GPU.\n";
                    message += "Probably server is shutdown. Please try again later. Try to restart miner\n";
                    message += "Please contact to admins if ONLY you see that message. Most likely your GPU doesn't support our miner. Miner can't be started";
                    MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }
            });
        }

        private void MinerStarted()
        {
            Console.WriteLine("Miner is started event");
            Dispatcher.Invoke(() =>
            {
                if (Miner.IsWorking)
                {
                    TB_Log.Clear();
                    WTB_SteamID64.IsEnabled = false;
                    BT_Start.Content = "Stop";
                    BT_Start.Background = Brushes.Red;
                }
            });
        }

        private void MinerExited()
        {
            Console.WriteLine("Miner is ended event");
            if (IsUser)
            {
                Dispatcher.Invoke(() =>
                {
                    if (!Miner.IsWorking)
                    {
                        WTB_SteamID64.IsEnabled = true;
                        BT_Start.Content = "Start";
                        BT_Start.Background = Brushes.Green;
                    }
                });
                IsUser = false;
            }
            else
            {
                Console.WriteLine("Miner is restaring");
                string messageText = "Attention. Miner was closed incorrectly.\n";
                messageText += "Miner will be restarted. If you want to close miner, use button for starting/stoping.\n";
                messageText += "If miner doens't respond then:\n";
                messageText += "First: Kill DBDSponsor process\n";
                messageText += "Second: NECESSARILY Kill miner.exe process";

                Task.Run(() =>
                MessageBox.Show(messageText, "Attention", MessageBoxButton.OK, MessageBoxImage.Warning));
                
                Miner.Start(); //Restart miner
            }
        }

        private void TrayClick(object sender, EventArgs e)
        {
            Show();
        }

        private void StatErrorReceived(Exception ex)
        {
            string output = $"Stat Error: {ex.TargetSite} {ex.Message}";
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
            return count > 1;
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
            if (!Miner.IsWorking)
            {
                Miner.Intensivity = (int)Slider_Intensivity.Value;
                Miner.Steamid = WTB_SteamID64.Text;
                Miner.Start();
            }
            else if(IsUserRestartMiner)
            {
                //Restarts Miner if scroll has been changed
                IsUser = true;
                Miner.Stop();
                Miner.Start();
                IsUserRestartMiner = false;
            }
            else
            {
                IsUser = true;
                Miner.Stop();
            }
        }

        private void Slider_Intensivity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Miner.IsWorking)
            {
                BT_Start.Content = "Apply";
                BT_Start.Background = Brushes.Orange;
                (sender as Slider).SelectionEnd = e.NewValue;
                IsUserRestartMiner = true;
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
            IsUser = true;
            Miner.Stop();
        }

        private void BT_Close(object sender, RoutedEventArgs e)
        {
            IsUser = true;
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
