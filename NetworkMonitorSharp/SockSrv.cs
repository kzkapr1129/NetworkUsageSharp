using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetworkMonitorSharp
{
    class SockSrv
    {
        public SockSrv()
        {

        }

        public void start()
        {
            Thread t = new Thread(new ThreadStart(doStart));
            t.Start();
        }

        public static void doStart()
        {
            IPAddress[] localIP = Dns.GetHostAddresses(Dns.GetHostName());
            int portNo = 50000;
            IPEndPoint ep = new IPEndPoint(localIP[4], portNo);
            Socket listener = new Socket(AddressFamily.InterNetwork,
                                         SocketType.Stream,
                                         ProtocolType.Tcp);

            listener.Bind(ep);
            listener.Listen(1);

            while (true)
            {
                Socket connection = listener.Accept();

                byte[] receiveData = new byte[1000];
                connection.Receive(receiveData);
            }
        }
    }
}
