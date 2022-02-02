using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Win32;
using System.Windows;
using System.Text;

namespace DBDSponsor
{
    public static class Calculator
    {
        private readonly static string url = "http://dbd-mix.xyz/profit";
        public static object GpuName =
            Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", 
                "HardwareInformation.AdapterString", 
                RegistryValueKind.String);

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
                var regRam = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "HardwareInformation.qwMemorySize", RegistryValueKind.QWord);
                long ram = 0;
                if (!long.TryParse(regRam.ToString(), out ram))
                {
                    ram = 2147483648;
                    MessageBox.Show("Miner can't get gpu ram. Miner set default ram 2048MB");
                }
                ram /= 1048576;

                if (GpuName is byte[])
                {
                    GpuName = Encoding.ASCII.GetString((byte[])GpuName);
                }
                Console.WriteLine(GpuName);
                Console.WriteLine(ram);

                string json = $"{{\"gpu\": \"{GpuName}\"}}";

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
                        catch(Exception ex) 
                        {
                            Console.WriteLine(ex.Message);
                        }
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
