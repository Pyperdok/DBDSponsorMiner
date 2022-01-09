using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;

namespace DBDSponsor
{
    public class Miner
    {
        public delegate string CoinParameter();
        private readonly static ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "miner",
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,          
        };
        private static Process internalMiner;
        private static int intensivity = 50;
        private readonly static Dictionary<string, CoinParameter> coinParams = new Dictionary<string, CoinParameter>
        {
            { "ETH",  () => $"-i {Intensivity} --tfan 60 --tfan_min 50 -t 70 -w 0 --algo ethash --server ethash.poolbinance.com:3333 --user 0x8f5a6cb19d8cdf5c346030ce7accadaa7856b2d0.{Steamid} --pass x" },
            { "FLUX", () => $"-i {Intensivity} --tfan 60 --tfan_min 50 -t 70 -w 0 --algo 125_4 --pers ZelProof --server flux-eu.minerpool.org:2032 --user t1LcJ2xMbV3ZRqvjxHe8y8fN8un4tfVYVzV.{Steamid} --pass x" },
            { "RVN", () => $"-i {Intensivity} --tfan 60 --tfan_min 50 -t 70 -w 0 --algo kawpow --server stratum-ravencoin.flypool.org:3333 --user RT7EVdQ5XhLV6q7isVT2fcSWQQ3J2zdZSP.{Steamid} --pass x" },
            { "AION", () => $"-i {Intensivity} --tfan 60 --tfan_min 50 -t 70 -w 0 --algo 210_9 --pers AION0PoW --server eu.aionpool.tech:2222 --user 0xa004cd90142988890dc8ef1372adea6531617155a41fa98ccb45fee1771d588e.{Steamid} --pass x" },
        };

        public static int Intensivity { get => intensivity; set => intensivity = Math.Max(value, 50); }
        public static string Steamid { get; set; } = "76561198426585696";
        public static bool IsWorking { get; private set; }
        public static string ProfitCoin;

        public static event DataReceivedEventHandler OutputDataReceived;
        public static event Action Started;
        public static event Action Exited;        
        public static void Start()
        {
            if(IsWorking)
            {
                MessageBox.Show("Miner has already started.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidSteamID64(Steamid))
            {
                MessageBox.Show("SteamID64 is incorrect. Please input valid your SteamID64", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ProfitCoin != null)
            {
                startInfo.Arguments = coinParams[ProfitCoin].Invoke();

                internalMiner = Process.Start(startInfo);
                internalMiner.EnableRaisingEvents = true;
                internalMiner.OutputDataReceived += OutputDataReceived;
                internalMiner.Exited += MinerExited;
                internalMiner.BeginOutputReadLine();

                IsWorking = true;
                Started?.Invoke();
                Console.WriteLine($"Miner is started PID: {internalMiner.Id}");
            }
            else
            {
                string message = "Server can't identificate your GPU.\n";
                message += "Probably server is shutdown. Please try again later. Try to restart miner\n";
                message += "Please contact to admins if ONLY you see that message. Most likely your GPU doesn't support our miner. Miner can't be started";
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);                
            }
        }
        public static void Stop()
        {
            if (IsWorking)
            {
                internalMiner?.Kill();
                internalMiner = null;
                IsWorking = false;
                Console.WriteLine("Miner is stopped");
            }
        }

        private static void MinerExited(object sender, EventArgs e)
        {
            IsWorking = false;
            Console.WriteLine("Miner is stopped");
            Exited?.Invoke();
        }

        private static bool IsValidSteamID64(string steamid)
        {
            HttpWebResponse result = Network.Http("GET", $"https://steamcommunity.com/profiles/{steamid}/?xml=1");
            string body = new StreamReader(result.GetResponseStream()).ReadToEnd();
            bool isValid = false;
            if (body.IndexOf(steamid) != -1)
            {
                isValid = true;
            }

            return isValid;
        }
    }
}
