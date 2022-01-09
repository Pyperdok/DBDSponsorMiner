using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DBDSponsor
{
    public class Stat
    {
        private readonly static string poolHashrateUrl = "https://api.aionpool.tech/api/pools/AionPool/miners/0xa030ba8b2742fa1e41e10a982b91806f279dd6b90b46552144f2dfe1fe48d37e";
        private readonly static string voteWeightUrl = "http://dbd-mix.xyz/stat";
        private readonly static string poolBalanceUrl = "https://mainnet-api.theoan.com/aion/dashboard/getAccountDetails?accountAddress=a030ba8b2742fa1e41e10a982b91806f279dd6b90b46552144f2dfe1fe48d37e";
        
        public delegate void NetworkErrorHandler(Exception ex);
        public static event NetworkErrorHandler ErrorReceived = null;
        public static MainWindow Window;
        private static string UpdatePoolBalance()
        {
            HttpWebResponse response = Network.Http("GET", poolBalanceUrl);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Console.WriteLine(body);
                JsonDocument json = JsonDocument.Parse(body);
                if (json.RootElement.TryGetProperty("content", out JsonElement content))
                {
                    try
                    {
                        string currency = content[0].GetProperty("balance").GetString();
                        double balance = float.Parse(currency, NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo);
                        balance = Math.Round(balance, 3);
                        return balance.ToString().Replace(",", ".");
                    }
                    catch (Exception ex)
                    {
                        ErrorReceived?.Invoke(ex);
                    }
                }

            }
            return "00000.000";
        }

        private static string UpdateVoteWeight(string SteamID)
        {
            string url = voteWeightUrl + $"?steamid={SteamID}";
            HttpWebResponse response = Network.Http("GET", url);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    if (float.TryParse(body, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out float voteWeight))
                    {
                        voteWeight = (float)Math.Round(voteWeight, 5);
                        return (voteWeight * 100).ToString().Replace(",", ".");
                    }
                }
                catch (Exception ex)
                {
                    ErrorReceived?.Invoke(ex);
                }
            }
            return "-";
        }
        private static string UpdatePoolHashrate()
        {
            HttpWebResponse response = Network.Http("GET", poolHashrateUrl);

            double hashrate = 0;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                JsonDocument json = JsonDocument.Parse(body);
                if (json.RootElement.TryGetProperty("performance", out JsonElement perfomance))
                {
                    try
                    {
                        JsonElement workers = perfomance.GetProperty("workers");
                        foreach (var el in workers.EnumerateObject())
                        {
                            hashrate += el.Value.GetProperty("hashrate").GetSingle();
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorReceived?.Invoke(ex);
                    }
                }
            }

            hashrate = Math.Round(hashrate, 1);
            Console.WriteLine($"Updated Hashrate Pool: {hashrate}");
            return hashrate.ToString().Replace(",", ".");
        }
        public static async void UpdateStatsAsync()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    Window.Dispatcher.Invoke(() => {
                        string hashrate = UpdatePoolHashrate();
                        string balance = UpdatePoolBalance();
                        string voteWeight = UpdateVoteWeight(Window.WTB_SteamID64.Text);

                        Window.L_HashratePool.Content = $"Hashrate Pool: {hashrate} h/s";
                        Window.L_BalancePool.Content = $"Balance: {balance} (Aion)";
                        Window.L_VoteWeight.Content = $"Vote Weight: {voteWeight} %";

                        StreamWriter log = File.AppendText(Window.path);
                        log.WriteLine($"Hashrate Pool: {hashrate} h/s");
                        log.WriteLine($"Balance: {balance} (Aion)");
                        log.WriteLine($"Vote Weight: {voteWeight} %");
                        log.Close();
                    });
                    Thread.Sleep(60000);
                }
            });
        }
    }
}
