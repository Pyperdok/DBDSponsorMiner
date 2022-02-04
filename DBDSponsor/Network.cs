using System.Net;
using System.Text;
using NLog;

namespace DBDSponsor
{
    public static class Network
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        public static HttpWebResponse Http(string method, string url, string body = "")
        {
            log.Debug("Http Request");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "DBDSponsor";
            request.ContentType = "application/json";
            if(method == "POST" || method == "PUT")
            {
                request.Method = method;
                byte[] data = Encoding.UTF8.GetBytes(body);
                request.ContentLength = data.Length;
                request.GetRequestStream().Write(data, 0, data.Length);
            }

            log.Debug($"Method: {request.Method} | Url: {url}");
            log.Debug($"Body: {body}");

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            log.Debug($"Http Response");
            log.Debug($"Status: {response.StatusCode}");

            return response;
        }
    }
}
