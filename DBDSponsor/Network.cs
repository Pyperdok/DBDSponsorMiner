using System.Net;
using System.Text;

namespace DBDSponsor
{
    public static class Network
    {
        public static HttpWebResponse Http(string method, string url, string body = "")
        {            
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
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            return response;
        }
    }
}
