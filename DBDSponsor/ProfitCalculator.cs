using System;
using System.Collections.Generic;
using System.Management;
using System.Text.Json;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace DBDSponsor
{
    public static class Calculator
    {
        private readonly static string url = "http://dbd-mix.xyz/profit";
        private readonly static Dictionary<string, long> memoryRequires = new Dictionary<string, long>
        {
            { "ETH",  5632 },
            { "FLUX", 3584 },
            { "RVN",  3891 },
            { "AION", 1126 }
        };
        public delegate void ProfitCoinHandler(string coin);
        public static event ProfitCoinHandler ProfitCoinUpdated = null;
        public static async void UpdateProfitCoinAsync()  //Return: ETH, FLUX, RVN, AION, INVALID
        {
            await Task.Run(() =>
            {
                string gpu = "";
                long ram = 0;
                var searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["CurrentBitsPerPixel"] != null && obj["CurrentHorizontalResolution"] != null)
                    {
                        gpu = obj["Name"].ToString();
                        long.TryParse(obj["AdapterRAM"].ToString(), out ram);
                        ram /= 1048576; //Bytes to MB
                    }
                }

                string json = $"{{\"gpu\": \"{gpu}\"}}";

                while (true)
                {
                    var response = Network.Http("POST", url, json);
                    string profitCoin = null;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        try
                        {
                            string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                            JsonDocument coins = JsonDocument.Parse(body);

                            foreach (var coin in coins.RootElement.EnumerateArray())
                            {
                                if (ram > memoryRequires[coin.GetString()])
                                {
                                    profitCoin = coin.GetString();
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
                    Console.WriteLine($"Profit Coin: {profitCoin}");
                    ProfitCoinUpdated?.Invoke(profitCoin);
                    Thread.Sleep(3600000); //1 hour
                }
            });
            //return profitCoin;
        }
    }
}
