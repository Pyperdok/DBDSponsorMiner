using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Input;
using WinForms = System.Windows.Forms;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;

namespace DBDSponsor
{
    public partial class MainWindow : Window
    {
        public readonly string path = $"logs/DBD_Sponsor_{DateTimeOffset.Now.ToUnixTimeSeconds()}.log";
        private bool IsUserRestartMiner = false;
        private bool IsSubmit = true;
        private bool IsPlaceholder = true;
        
        private readonly WinForms.NotifyIcon notifyIcon = new WinForms.NotifyIcon()
        {
            Visible = true,
            BalloonTipIcon = WinForms.ToolTipIcon.Info,
            Icon = Properties.Resources.GoldCoin,
            Text = "DBD Sponsor v0.5",
            BalloonTipText = "DBDSponsor is here"
        };

        private readonly Dictionary<Brush, Brush> verifyColorPair = new Dictionary<Brush, Brush>()
        {
            {Brushes.Green,  Brushes.DarkGreen},
            {Brushes.Yellow,  Brushes.Gold},
            {Brushes.DarkGreen,  Brushes.Green},
            {Brushes.Gold,  Brushes.Yellow},
        };

        private readonly Dictionary<Brush, Brush> MinimizeColorPair = new Dictionary<Brush, Brush>()
        {
            {Brushes.DarkGreen,  Brushes.Transparent},
            { Brushes.Transparent,  Brushes.DarkGreen},
        };

        private readonly Dictionary<Brush, Brush> CloseColorPair = new Dictionary<Brush, Brush>()
        {
            {Brushes.DarkRed,  Brushes.Transparent},
            { Brushes.Transparent,  Brushes.DarkRed},
        };

        public MainWindow()
        {
            InitializeComponent();
            ST_Error.Visibility = Visibility.Hidden;
            BT_Start.IsEnabled = false;
            
            var providerName = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "ProviderName", RegistryValueKind.String);
            if (providerName.ToString().ToLower().IndexOf("advanced") != -1)
            {
                MessageBox.Show("Is your GPU by AMD? For AMD gpu PLEASE set COMPUTING MODE of your GPU. If you won't do that then your gpu effcienty has been extremely reduced.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (IsDuplicate())
            {
                MessageBox.Show("DBDSponsor is already started. Please don't try to start the duplicate");
                Environment.Exit(0);
            }
            Directory.CreateDirectory("logs");

            using (StreamReader reader = new StreamReader(File.Open("steamid.cfg", FileMode.OpenOrCreate)))
            {
                string steamid = reader.ReadLine();
                if (!string.IsNullOrEmpty(steamid))
                {
                    WTB_SteamID64.Text = steamid;
                    BT_Verify_Click(BT_Verify, null);
                }
            }         

            //Events
            Miner.OutputDataReceived += MinerOutputDataReceived;
            Miner.Started += MinerStarted;
            Miner.Exited += MinerExited;
            Calculator.ProfitCoinUpdated += ProfitCoinUpdated;
            Stat.ErrorReceived += StatErrorReceived;
            Stat.Updated += StatsUpdated;
            notifyIcon.Click += TrayClick;

            Stat.UpdateStatsAsync();
            Stat.UpdateBalanceAsync();
            L_GPU.Content = Calculator.GpuName;
            Calculator.UpdateProfitCoinAsync();
        }

        private void StatsUpdated(bool good)
        {
            Action updateCallback = () =>
            {
                L_Temperature.Content = $"Temperature GPU: -°C";
                L_Fan.Content = $"Fan Speed: -%";
                L_Uptime.Content = $"Uptime: -:-";
                L_Hashrate.Content = $"Hashrate: -";
                L_Balance.Content = $"Balance: {Stat.Balance}";
            };

            if(good)
            {
                updateCallback = () =>
                {
                    L_Temperature.Content = $"Temperature GPU: {Stat.Temperature}°C";
                    L_Fan.Content = $"Fan Speed: {Stat.Fan}%";
                    L_Uptime.Content = $"Uptime: {Stat.Uptime / 60}:{Stat.Uptime % 60}";
                    L_Hashrate.Content = $"Hashrate: {Stat.Hashrate}";
                    L_Balance.Content = $"Balance: {Stat.Balance}";
                };
            }
            Dispatcher.Invoke(updateCallback);
        }

        private void ProfitCoinUpdated(string coin)
        {
            Miner.ProfitCoin = coin;
            if(Miner.ProfitCoin == null)
            {
                string message = "Server can't identificate your GPU.\n";
                message += "Probably server is shutdown. Please try again later. Try to restart miner\n";
                message += "Please contact to admins if ONLY you see that message. Most likely your GPU doesn't support our miner. Miner can't be started";
                
                StreamWriter log = File.AppendText(path);
                log.WriteLine(message);
                log.Close();

                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }

            if (Miner.ProfitCoin != coin && Miner.IsWorking)
            {
                Console.WriteLine("Profit coin has changed. Restarting the miner.");
                Miner.ProfitCoin = coin; //Restart Miner if profit coin will have changed
                Miner.Restart();
            }

            Dispatcher.Invoke(() => L_Coin.Content = $"Coin: {coin}");
        }

        private void MinerStarted()
        {
            Console.WriteLine("Miner is started event");
            Dispatcher.Invoke(() =>
            {
                if (Miner.IsWorking)
                {
                    BT_Start.Content = "STOP";
                    BT_Verify.IsEnabled = false;
                }
            });
            using (StreamWriter writer = new StreamWriter("steamid.cfg", false))
                writer.WriteLine(Miner.Steamid);
        }

        private void MinerExited()
        {
            Console.WriteLine("Miner is ended event");
            if (!Miner.IsWorking)
            {
                Dispatcher.Invoke(() => {
                    BT_Start.Content = "START";
                    BT_Verify.IsEnabled = true;
                });
            }
            else if(!IsUserRestartMiner)
            {
                Console.WriteLine("Miner is restaring");
                string messageText = "Attention. Miner was closed incorrectly.\n";
                messageText += "Miner will be restarted. If you want to close miner, use button for starting/stoping.\n";

                Task.Run(() =>
                MessageBox.Show(messageText, "Attention", MessageBoxButton.OK, MessageBoxImage.Warning));

                Miner.Restart(); //Restart miner
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
                    //if (TB_Log.Text.Length > 1048576)
                    //{
                    //    TB_Log.Clear();
                    //}
                    StreamWriter log = File.AppendText(path);
                    log.WriteLineAsync(e.Data);
                    log.Close();
                    //TB_Log.AppendText("\n" + e.Data);
                });
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {          
            if (!Miner.IsWorking && Miner.ProfitCoin != null)
            {
                WTB_SteamID64.IsEnabled = false;
                Miner.Start();
            }
            else if (IsUserRestartMiner && Miner.ProfitCoin != null)
            {
                //Restarts Miner if scroll has been changed
                Miner.Intensivity = (int)Slider_Intensivity.Value;
                Miner.Restart();
                IsUserRestartMiner = false;
            }
            else
            {
                Miner.Stop();
            }
        }

        private void Slider_Intensivity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Miner.IsWorking)
            {
                BT_Start.Content = "APPLY";
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
            Miner.Stop();
        }

        private void BT_Close_Click(object sender, RoutedEventArgs e)
        {
            Miner.Stop();
            Environment.Exit(0);
        }
        private void BT_Minimize_Click(object sender, RoutedEventArgs e)
        {
            notifyIcon.ShowBalloonTip(5000);
            Hide();
        }

        private void BT_Title_Buttons_Hover(object sender, MouseEventArgs e)
        {
            if(sender.Equals(BT_Minimize))
            {
                BT_Minimize.Background = MinimizeColorPair[BT_Minimize.Background];
            }
            if(sender.Equals(BT_Close))
            {
                BT_Close.Background = CloseColorPair[BT_Close.Background];
            }
        }

        private static void ValidateSteamID64(string steamid, out string nickname)
        {
            HttpWebResponse response = Network.Http("GET", $"http://dbd-mix.xyz/steam?steamid={steamid}");
            nickname = null;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                JsonDocument json = JsonDocument.Parse(body);
                if (json.RootElement.TryGetProperty("nickname", out JsonElement value))
                {
                    nickname = value.GetString();
                }
            }
        }

        private void BT_Verify_Hover(object sender, MouseEventArgs e) =>
            BT_Verify.Background = verifyColorPair[BT_Verify.Background];
        
        private void BT_Verify_Edit()
        {
            WTB_SteamID64.Text = Miner.Steamid;
            WTB_SteamID64.IsEnabled = true;
            WTB_SteamID64.Background = Brushes.White;
            BT_Start.IsEnabled = false;
        }

        private void BT_Verify_Submit()
        {
            ValidateSteamID64(WTB_SteamID64.Text, out string nickname);
            WTB_SteamID64.Foreground = Brushes.Black;
            Miner.Steamid = WTB_SteamID64.Text;
            WTB_SteamID64.Text = nickname;
            WTB_SteamID64.IsEnabled = false;
            WTB_SteamID64.Background = Brushes.LightGray;
            BT_Start.IsEnabled = true;
        }

        private void BT_Verify_Click(object sender, RoutedEventArgs e)
        {
            string icon = "/Resources/Arrow.png";
            Brush color = Brushes.DarkGreen;
            Action verifyCallback = BT_Verify_Edit;

            if (IsSubmit)
            {
                icon = "/Resources/Pensil.png";
                color = Brushes.Gold;
                verifyCallback = BT_Verify_Submit;
            }
            try
            {
                verifyCallback.Invoke();
                (BT_Verify.Content as Image).Source = new BitmapImage(new Uri(icon, UriKind.Relative));
                BT_Verify.Background = color;
                ST_Error.Visibility = Visibility.Hidden;
                IsSubmit = !IsSubmit;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ST_Error.Visibility = Visibility.Visible;
            }
        }

        private void WTB_SteamID64_Focus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (sender as TextBox);
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                WTB_SteamID64.Text = "Enter SteamID64";
                WTB_SteamID64.Foreground = Brushes.Gray;
                IsPlaceholder = true;
            }
            else if(IsPlaceholder)
            {
                WTB_SteamID64.Text = "";
                WTB_SteamID64.Foreground = Brushes.Black;
                IsPlaceholder = false;
            }
        }

        private void L_Balance_MouseDown(object sender, MouseButtonEventArgs e) =>
            Process.Start("https://www.bscscan.com/tokenholdings?a=0x2CD2cABd93496A85295EC992857d68EbF086bD78&q=BUSD");

        private void BT_Start_Hover(object sender, MouseEventArgs e)
        {
            if(BT_Start.Background == Brushes.White)
            {
                BT_Start.Background = Brushes.LightGray;
            }
            else
            {
                BT_Start.Background = Brushes.White;
            }
        }

        private void IMG_Discord_MouseDown(object sender, MouseButtonEventArgs e) =>
            Process.Start("https://discord.gg/xQH4bSeUwz");
    }
}
