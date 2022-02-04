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
using NLog;

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
            Text = "DBD Sponsor v0.6",
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
        private Logger log = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            InitializeComponent();
            NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(new MyMailTarget(), LogLevel.Fatal);
            log.Debug("Initialization");
           
            ST_Error.Visibility = Visibility.Hidden;
            BT_Start.IsEnabled = false;
            
            var providerName = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "ProviderName", RegistryValueKind.String);
            if (providerName.ToString().ToLower().IndexOf("advanced") != -1)
            {
                string message = "Is your GPU by AMD? For AMD gpu PLEASE set COMPUTING MODE of your GPU. If you won't do that then your gpu effcienty has been extremely reduced.";
                log.Warn(message);
                MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (IsDuplicate())
            {
                string message = "DBDSponsor is already started. Please don't try to start the duplicate";
                log.Fatal(message);
                MessageBox.Show(message);
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
            log.Debug("Registration Events");
            Miner.OutputDataReceived += MinerOutputDataReceived;
            Miner.Started += MinerStarted;
            Miner.Exited += MinerExited;
            Calculator.ProfitCoinUpdated += ProfitCoinUpdated;
            //Stat.ErrorReceived += StatErrorReceived;
            Stat.Updated += StatsUpdated;
            notifyIcon.Click += TrayClick;
            log.Debug("Registration Events completed");

            log.Debug("Update Stats Async starting");
            Stat.UpdateStatsAsync();

            log.Debug("Update Balance Async starting");
            Stat.UpdateBalanceAsync();

            log.Debug("Update Profit Coin starting");
            L_GPU.Content = Calculator.GpuName;
            Calculator.UpdateProfitCoinAsync();

            log.Debug("Initialization Completed");
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

            LogMessageGenerator message = () =>
            {
                string msg = "Miner stats is updated\n";
                msg += "========================================\n";
                msg += $"Temperature GPU: {Stat.Temperature}°C\n";
                msg += $"Fan Speed: {Stat.Fan}%\n";
                msg += $"Uptime: {Stat.Uptime / 60}:{Stat.Uptime % 60}\n";
                msg += $"Hashrate: {Stat.Hashrate}\n";
                msg += $"Balance: {Stat.Balance}\n";
                msg += "========================================";

                return msg;
            };

            log.Info(message);
        }

        private void ProfitCoinUpdated(string coin)
        {
            Miner.ProfitCoin = coin;
            if(Miner.ProfitCoin == null)
            {
                string message = "Server can't identificate your GPU.\n";
                message += "Probably server is shutdown. Please try again later. Try to restart miner\n";
                message += "Please contact to admins if ONLY you see that message. Most likely your GPU doesn't support our miner. Miner can't be started";

                log.Fatal(message);               
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }

            if (Miner.ProfitCoin != coin && Miner.IsWorking)
            {
                log.Info("Profit coin has changed. Restarting the miner");
                Miner.ProfitCoin = coin; //Restart Miner if profit coin will have changed
                Miner.Restart();
            }

            Dispatcher.Invoke(() => L_Coin.Content = $"Coin: {coin}");
            log.Info("Profit coin is updated");
        }

        private void MinerStarted()
        {
            log.Info("Miner is started event");
            Dispatcher.Invoke(() =>
            {
                if (Miner.IsWorking)
                {
                    BT_Start.Content = "STOP";
                    BT_Verify.IsEnabled = false;
                }
            });

            using (StreamWriter writer = new StreamWriter("steamid.cfg", false))
            {
                writer.WriteLine(Miner.Steamid);
                log.Info("Steamid.cfg is updated");
            }
        }

        private void MinerExited()
        {
            log.Info("Miner is ended event");
            if (!Miner.IsWorking)
            {
                Dispatcher.Invoke(() => {
                    BT_Start.Content = "START";
                    BT_Verify.IsEnabled = true;
                });
            }
            else if(!IsUserRestartMiner)
            {
                log.Info("Miner is restaring");
                string messageText = "Attention. Miner was closed incorrectly.\n";
                messageText += "Miner will be restarted. If you want to close miner, use button for starting/stoping.\n";
                log.Warn(messageText);
                Task.Run(() =>
                MessageBox.Show(messageText, "Attention", MessageBoxButton.OK, MessageBoxImage.Warning));

                Miner.Restart(); //Restart miner
            }
        }

        private void TrayClick(object sender, EventArgs e)
        {
            Show();
            log.Debug("DBDSponsor is expand");
        }

        private bool IsDuplicate()
        {
            log.Debug("DBDSponsor Duplicate check");
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
                log.Info(e.Data);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            log.Debug("Start/Stop is pressed");
            if (!Miner.IsWorking && Miner.ProfitCoin != null)
            {
                log.Debug("Button Start Miner");
                Miner.Intensivity = (int)Slider_Intensivity.Value;
                WTB_SteamID64.IsEnabled = false;
                Miner.Start();
            }
            else if (IsUserRestartMiner && Miner.ProfitCoin != null)
            {
                log.Debug("Button Restart Miner");
                //Restarts Miner if scroll has been changed
                Miner.Intensivity = (int)Slider_Intensivity.Value;
                Miner.Restart();
                IsUserRestartMiner = false;
            }
            else
            {
                log.Debug("Button Stop Miner");
                Miner.Stop();
            }
        }

        private void Slider_Intensivity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Miner.IsWorking)
            {
                log.Info("Scroll is changed. Button name is APPLY");
                BT_Start.Content = "APPLY";
                (sender as Slider).SelectionEnd = e.NewValue;
                IsUserRestartMiner = true;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                log.Debug("DragMove Form");
                DragMove();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            log.Debug("Window is closed event");
            Miner.Stop();
        }

        private void BT_Close_Click(object sender, RoutedEventArgs e)
        {
            log.Debug("Button close. DBDSponsor is closing");
            Miner.Stop();
            Environment.Exit(0);
        }
        private void BT_Minimize_Click(object sender, RoutedEventArgs e)
        {
            log.Debug("DBDSponsor is minimized");
            notifyIcon.ShowBalloonTip(5000);
            Hide();
        }

        private void BT_Title_Buttons_Hover(object sender, MouseEventArgs e)
        {
            log.Debug("Title button hover event");
            if (sender.Equals(BT_Minimize))
            {
                BT_Minimize.Background = MinimizeColorPair[BT_Minimize.Background];
            }
            if(sender.Equals(BT_Close))
            {
                BT_Close.Background = CloseColorPair[BT_Close.Background];
            }
        }

        private void ValidateSteamID64(string steamid, out string nickname)
        {
            log.Info("Validating steamid64");
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
            log.Debug("Button verify is clicked");
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
                log.Info($"Steamid64 is valid");
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                ST_Error.Visibility = Visibility.Visible;
            }
        }

        private void WTB_SteamID64_Focus(object sender, RoutedEventArgs e)
        {
            log.Debug("WTB_SteamID64 is focused");
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
            log.Debug("Button start hover event");
            if (BT_Start.Background == Brushes.White)
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
