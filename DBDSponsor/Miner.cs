using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace DBDSponsor
{
    public class Miner
    {
        public delegate string CoinParameter();
        private static Logger log = LogManager.GetCurrentClassLogger();
        private readonly static ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "miner",
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };
        private static Process internalMiner;
        private static int intensivity = 50;
        private static string steamid = null;
        private static IntPtr handle = Process.GetCurrentProcess().CreateHandle();

        private readonly static Dictionary<string, CoinParameter> coinParams = new Dictionary<string, CoinParameter>
        {
            { "ETH",  () => $"-i {Intensivity} --api 10050 --tfan 60 --tfan_min 50 -t 70 -w 0 --algo ethash --server ethash.poolbinance.com:1800 --user DBDSponsorMiner.{Steamid} --pass x #{handle}" },
            { "FLUX", () => $"-i {Intensivity} --api 10050 --tfan 60 --tfan_min 50 -t 70 -w 0 --algo 125_4 --pers ZelProof --server flux-eu.minerpool.org:2032 --user t1LcJ2xMbV3ZRqvjxHe8y8fN8un4tfVYVzV.{Steamid} --pass 12345 #{handle}" },
            { "RVN", () => $"-i {Intensivity} --api 10050 --tfan 60 --tfan_min 50 -t 70 -w 0 --algo kawpow --server stratum.ravenminer.com:3838 --user RT7EVdQ5XhLV6q7isVT2fcSWQQ3J2zdZSP.{Steamid} --pass pps #{handle}" },
            { "AION", () => $"-i {Intensivity} --api 10050 --tfan 60 --tfan_min 50 -t 70 -w 0 --algo 210_9 --pers AION0PoW --server cluster.aionpool.tech:2222 --user 0xa004cd90142988890dc8ef1372adea6531617155a41fa98ccb45fee1771d588e.{Steamid} --pass x #{handle}" },
        };
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        public static int Intensivity { get => intensivity; set => intensivity = Math.Max(value, 50); }
        public static string Steamid { get => steamid; set => steamid = value.Trim();}
        public static bool IsWorking { get; private set; }
        public static string ProfitCoin;

        public static event DataReceivedEventHandler OutputDataReceived;
        public static event Action Started;
        public static event Action Exited;

        public static void Start()
        {
            log.Info("Miner is starting");
            startInfo.Arguments = coinParams[ProfitCoin].Invoke();
            log.Info(startInfo.Arguments + $"SteamID64 {Steamid}");

            internalMiner = Process.Start(startInfo);
            IsWorking = true;
            log.Info("Miner is started");

            log.Debug("Miner is started. Waiting of valid handle");
            while (internalMiner.MainWindowHandle == IntPtr.Zero) ;
            ShowWindowAsync(internalMiner.MainWindowHandle, 0);

            log.Debug("Handle is valid. Console window is hidden");

            log.Debug("Injection AutoExit.dll");
            if (!internalMiner.InjectDLL("AutoExit.dll"))
            {
                string message = "Injection Failed. Stopping the miner";
                log.Fatal(message);
                Stop();
                MessageBox.Show(message);
                return;
            }

            internalMiner.EnableRaisingEvents = true;
            internalMiner.OutputDataReceived += OutputDataReceived;
            internalMiner.Exited += MinerExited;
            internalMiner.BeginOutputReadLine();
            log.Debug("Output is redirected to this process");

            Started?.Invoke();
            log.Info($"Miner is started PID: {internalMiner.Id}");
        }

        public static void Restart()
        {
            log.Info("Miner is restaring");
            internalMiner.Exited -= MinerExited;
            Stop();
            Start();
            log.Info("Miner is restarted");
        }

        public static void Stop()
        {
            if (IsWorking)
            {
                log.Info($"Miner is stopping");
                internalMiner.CloseMainWindow();
                internalMiner = null;
                IsWorking = false;
                log.Info("Miner was stopped");
            }
        }

        private static void MinerExited(object sender, EventArgs e)
        {
            log.Debug("Miner is exited internal event");
            Exited?.Invoke();
        }
    }
}
