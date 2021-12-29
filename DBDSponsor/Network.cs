using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Net;
using System.IO;
using System.Globalization;

namespace DBDSponsor
{
    class Network
    {
        static string poolHashrateUrl = "https://api.aionpool.tech/api/pools/AionPool/miners/0xa030ba8b2742fa1e41e10a982b91806f279dd6b90b46552144f2dfe1fe48d37e";
        static string voteWeightUrl = "http://dbd-mix.xyz/stat";
        static string poolBalanceUrl = "https://mainnet-api.theoan.com/aion/dashboard/getAccountDetails?accountAddress=a030ba8b2742fa1e41e10a982b91806f279dd6b90b46552144f2dfe1fe48d37e";
        public static HttpWebResponse Http(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "DBDSponsor";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            return response;
        }
        public static string UpdatePoolBalance()
        {
            HttpWebResponse response = Http(poolBalanceUrl);
            if(response.StatusCode == HttpStatusCode.OK)
            {
                string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Console.WriteLine(body);
                JsonDocument json = JsonDocument.Parse(body);
                JsonElement content;
                if(json.RootElement.TryGetProperty("content", out content))
                {
                    try
                    {
                        string currency = content[0].GetProperty("balance").GetString();
                        
                        double balance = float.Parse(currency, NumberStyles.AllowDecimalPoint, NumberFormatInfo.InvariantInfo);
                        balance = Math.Round(balance, 3);
                        return balance.ToString().Replace(",", ".");
                    }
                    catch(Exception ex) 
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

            }
            return "00000.000";
        }

        public static string UpdateVoteWeight(string SteamID)
        {           
            string url = voteWeightUrl + $"?steamid={SteamID}";
            HttpWebResponse response = Http(url);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                float voteWeight;
                if (float.TryParse(body, out voteWeight))
                {
                    return (voteWeight * 100).ToString().Replace(",", ".");
                }
            }
            return "-";
        }
        public static string UpdatePoolHashrate()
        {
            HttpWebResponse response = Http(poolHashrateUrl);

            double hashrate = 0;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string body = new StreamReader(response.GetResponseStream()).ReadToEnd();
                JsonDocument json = JsonDocument.Parse(body);
                JsonElement perfomance;
                if(json.RootElement.TryGetProperty("performance", out perfomance))
                {
                    try
                    {
                        JsonElement workers = perfomance.GetProperty("workers");
                        foreach (var el in workers.EnumerateObject())
                        {
                            hashrate += el.Value.GetProperty("hashrate").GetSingle();
                        }
                    }
                    catch { }
                }                             
            }
            hashrate = Math.Round(hashrate, 1);

            Console.WriteLine($"Updated Hashrate Pool: {hashrate}");
            return hashrate.ToString().Replace(",",".");
        }
    }
}
