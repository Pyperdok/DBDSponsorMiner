using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace DBDSponsor
{
    public class Stat
    {
        private static Logger log = LogManager.GetCurrentClassLogger();   
        public static Action<bool> Updated;
        
        public static int Temperature { get; private set; }
        public static int Fan { get; private set; }
        public static int Uptime { get; private set; }
        public static string Hashrate { get; private set; }
        public static string Balance { get; private set; } = "$0.00 + $0.00";

        private static void UpdateStats()
        {
           HttpWebResponse response = Network.Http("GET", "http://127.0.0.1:10050/stat");

           using(StreamReader reader = new StreamReader(response.GetResponseStream()))
           {
                JsonDocument json = JsonDocument.Parse(reader.ReadToEnd());

                json.RootElement.TryGetProperty("devices", out JsonElement devices);
                devices[0].TryGetProperty("temperature", out JsonElement temperature);
                devices[0].TryGetProperty("fan", out JsonElement fan);
                devices[0].TryGetProperty("speed", out JsonElement speed);

                json.RootElement.TryGetProperty("uptime", out JsonElement uptime);
                json.RootElement.TryGetProperty("speed_unit", out JsonElement speed_unit);

                Temperature = temperature.GetInt32();
                Fan = fan.GetInt32();
                Uptime = uptime.GetInt32();
                Hashrate = $"{speed.GetUInt64()} {speed_unit.GetString()}";
            }
        }

        private static void UpdateBalance()
        {
            HttpWebResponse response = Network.Http("GET", $"http://dbd-mix.xyz/api");
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                JsonDocument json = JsonDocument.Parse(reader.ReadToEnd());

                json.RootElement.TryGetProperty("address", out JsonElement address);
                json.RootElement.TryGetProperty("binance", out JsonElement binance);
                string add = Math.Round(address.GetSingle(), 2).ToString().Replace(",", ".");
                string bin = Math.Round(binance.GetSingle(), 2).ToString().Replace(",", ".");
                Balance = $"${add} + ${bin}";
            }
        }
        public static async void UpdateBalanceAsync()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        UpdateBalance();
                    }
                    catch(Exception ex)
                    {
                        log.Error(ex.ToString());
                        Balance = $"$x + $x";
                    }
                    Thread.Sleep(5000);
                }
            });
        }

        public static async void UpdateStatsAsync()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        UpdateStats();
                        Updated?.Invoke(true);
                    }
                    catch(Exception ex) 
                    {
                        log.Error(ex.ToString());
                        Updated?.Invoke(false);
                    }
                    Thread.Sleep(1000);
                }
            });
        }
    }
}
