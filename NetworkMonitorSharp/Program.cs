using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;

namespace NetworkMonitorSharp
{
    class Program
    {
        static List<string> URLs = new List<string>() {
            "http://www.google.com",
            "http://www.yahoo.co.jp",
            "http://github.co.jp",
            "http://qiita.com/",
            "http://www.bing.com/"
        };

        static Random rand = new Random();
        static async void httpGet()
        {
            using (var client = new HttpClient())
            {
                var url = URLs[rand.Next(0, URLs.Count-1)];
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var result = await client.SendAsync(request);
                Console.WriteLine($"GET: {url}");
            }
        }

        static void httpGetInfinity()
        {
            while (true) {
                httpGet();
                Thread.Sleep(2000);
            }
        }

        static void Main(string[] args)
        {
            Thread.GetDomain().SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            var pri = (WindowsPrincipal)Thread.CurrentPrincipal;
            if (!pri.IsInRole(WindowsBuiltInRole.Administrator))
            {
                Console.WriteLine("このアプリは管理者権限が必要です。");
                return;
            }

            Thread t = new Thread(new ThreadStart(httpGetInfinity));
            t.Start();

            NetworkMonitor monitor = new NetworkMonitor();

            while (true)
            {
                monitor.scan();
                Thread.Sleep(100);
            }
        }
    }
}
