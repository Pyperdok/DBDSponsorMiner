using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;

namespace DBDSponsor
{
    public class Miner
    {
        private static ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "miner",
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        public static bool IsStarted { get; private set; } 
        public static DataReceivedEventHandler OutputDataReceived = null;

        public static void Start(string steamid, int intensivity)
        {
            Stop(); //Stop miner if user try to double start
            if(!IsValidSteamID64(steamid))
            {
                MessageBox.Show("SteamID64 is incorrect", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            startInfo.Arguments = $"-i {intensivity} --tfan 60 --tfan_min 50 -t 70 --algo 210_9 --pers AION0PoW --server eu.aionpool.tech:2222 --user 0xa030ba8b2742fa1e41e10a982b91806f279dd6b90b46552144f2dfe1fe48d37e.{steamid} --pass x";
            Process internalMiner = Process.Start(startInfo);
            internalMiner.OutputDataReceived += OutputDataReceived;
            internalMiner.BeginOutputReadLine();
            IsStarted = true;
        }

        public static void Stop()
        {
            Process[] processes = Process.GetProcesses();
            for (int i = 0; i < processes.Length; i++)
            {
                string procName = processes[i].ProcessName;
                if (procName == startInfo.FileName)
                {
                    processes[i].Kill();
                }
            }
            IsStarted = false;
        }
        private static bool IsValidSteamID64(string steamid)
        {
            HttpWebResponse result = Network.Http($"https://steamcommunity.com/profiles/{steamid}/?xml=1");
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
