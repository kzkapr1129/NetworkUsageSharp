using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
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

        static void sockSendInfinity()
        {
            Thread.Sleep(5 * 1000);

            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[4];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 50000);

            // Create a TCP/IP  socket.  
            Socket sender = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            sender.Connect(remoteEP);

            while (true)
            {
                byte[] msg = Encoding.ASCII.GetBytes("Hello");
                sender.Send(msg);

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

            new SockSrv().start();

            Thread t = new Thread(new ThreadStart(sockSendInfinity));
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
