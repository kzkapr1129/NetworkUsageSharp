using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Win32;
using static Win32.ErrorCode;
using static Win32.Api;
using System.Diagnostics;
using System.Threading;

namespace NetworkMonitorSharp
{
    class NetworkMonitor
    {
        // 現在のTCP接続一覧
        private ArrayList _currentConnections = new ArrayList();
        // setPerConnectionsで待機中のコネクション一覧
        private Dictionary<string, MIB_TCPROW2> _waitingConnections = new Dictionary<string, MIB_TCPROW2>();
        // コネクションごとの通信量の最新値
        private Dictionary<string, TCP_ESTATS_DATA_ROD_v0> _computingConnections = new Dictionary<string, TCP_ESTATS_DATA_ROD_v0>();
        // トータル通信量の合算対象一覧
        private ArrayList _totalingTargets = new ArrayList();

        public NetworkMonitor()
        {

        }

        public void scan()
        {
            setMonitor();

            for (int retry = 0; retry < 3; retry++)
            {
                retriveUsage();
                Thread.Sleep(100);
            }
        }

        private void setMonitor()
        {
            // 初回呼び出し、実際に必要なバッファサイズを取得する
            var tableSize = 0;
            var ret = Api.GetTcpTable2(IntPtr.Zero, ref tableSize, true);
            if (ret != (int)ErrorCode.ERROR_INSUFFICIENT_BUFFER)
            {
                Console.WriteLine("Error in GetTcpTable2(First): error={0}", ret);
                return;
            }

            // 二回目の呼び出し、バッファを確保しコネクションテーブルを取得
            IntPtr pBuf = IntPtr.Zero;
            try {
                pBuf = Marshal.AllocHGlobal(tableSize);
                ret = Win32.Api.GetTcpTable2(pBuf, ref tableSize, true);
                if (ret != (int)ErrorCode.NO_ERROR)
                {
                    // 二回目呼び出しで失敗した場合
                    Console.WriteLine("Error in GetTcpTable2(Second): error={0}", ret);
                    return;
                }

                // 監視対象のコネクション(MIB_TCPROW2)を保存する
                _currentConnections.Clear();
                int entrySize = Marshal.SizeOf(typeof(MIB_TCPROW2));
                int nEntries = Marshal.ReadInt32(pBuf);
                IntPtr tableStartAddr = pBuf + sizeof(int);
                for (int i = 0; i < nEntries; i++)
                {
                    IntPtr pEntry = (IntPtr)(tableStartAddr + i * entrySize);
                    MIB_TCPROW2 tcpData = (MIB_TCPROW2)Marshal.PtrToStructure(pEntry, typeof(MIB_TCPROW2));
                    if (tcpData.dwOwningPid == Process.GetCurrentProcess().Id)
                    {
                        // 自プロセスのTCPコネクションを発見した場合

                        _currentConnections.Add(tcpData); // 現在の接続一覧に追加
                    }
                }
            }
            finally {
                Marshal.FreeHGlobal(pBuf);
            }

            // 通信中のTCPリストと現在のリストを比較し、今回接続していないものだけを残す
            foreach (MIB_TCPROW2 connection in _currentConnections)
            {
                String key = makeKey(connection);
                if (_waitingConnections.ContainsKey(key))
                {
                    // 待機中のコネクション一覧に今回接続しているコネクションが存在した場合
                    _waitingConnections.Remove(key);
                }
            }

            // 通信が切断したと思われるもの(_waitingConnectionsの残っているもの)を合算対象とする
            foreach (var item in _waitingConnections)
            {
                var key = item.Key;
                if (_computingConnections.ContainsKey(key))
                {
                    var stats = _computingConnections[key];
                    _totalingTargets.Add(stats);

                    _computingConnections.Remove(key);
                }
            }

            // コネクションごとに通信量の監視を開始する
            foreach (MIB_TCPROW2 connection in _currentConnections)
            {
                IntPtr buffRow = IntPtr.Zero;
                try
                {
                    buffRow = Marshal.AllocCoTaskMem(Marshal.SizeOf(connection.GetType()));
                    Marshal.StructureToPtr<MIB_TCPROW2>(connection, buffRow, false);
                    bool enable = true;

                    // 監視対象のコネクションをStats取得対象にする
                    ret = SetPerTcpConnectionEStats(buffRow, TCP_ESTATS_TYPE.TcpConnectionEstatsData, ref enable, 0, 1, 0);
                    if (ret != (int)ErrorCode.NO_ERROR)
                    {
                        continue;
                    }

                    // 待ち状態のコネクションを保存する
                    string key = makeKey(connection);
                    if (!_waitingConnections.ContainsKey(key))
                    {
                        // 新規登録の場合
                        _waitingConnections.Add(key, connection);
                    }

                } finally
                {
                    if (buffRow != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(buffRow);
                    }
                }
            }
        }

        private void retriveUsage()
        {
            foreach (var item in _waitingConnections)
            {
                var connection = item.Value;

                IntPtr buffRow = IntPtr.Zero;
                IntPtr rod = IntPtr.Zero;

                try
                {
                    byte state;
                    rod = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(TCP_ESTATS_DATA_ROD_v0)));
                    buffRow = Marshal.AllocCoTaskMem(Marshal.SizeOf(connection.GetType()));
                    Marshal.StructureToPtr(connection, buffRow, false);
                    int ret = GetPerTcpConnectionEStats(buffRow, TCP_ESTATS_TYPE.TcpConnectionEstatsData, out state, 0, 1, IntPtr.Zero, 0, 0, rod, 0, Marshal.SizeOf(typeof(TCP_ESTATS_DATA_ROD_v0)));
                    if (ret != (int)ErrorCode.NO_ERROR)
                    {
                        continue;
                    }

                    var rodData = (TCP_ESTATS_DATA_ROD_v0)Marshal.PtrToStructure(rod, typeof(TCP_ESTATS_DATA_ROD_v0));

                    //dumpTcpRowWithUsage(connection, rodData.DataBytesIn, rodData.DataBytesOut);

                    string key = makeKey(connection);
                    if (_computingConnections.ContainsKey(key))
                    {
                        // 既存接続の場合
                        _computingConnections[key] = rodData;
                    }
                    else
                    {
                        // 新規トランザクションの場合
                        _computingConnections.Add(key, rodData);
                    }
                }
                finally
                {
                    if (buffRow != IntPtr.Zero) {
                        Marshal.FreeCoTaskMem(buffRow);
                    }
                    if (rod != IntPtr.Zero) {
                        Marshal.FreeCoTaskMem(rod);
                    }
                }
                outputTotalBytes();
            }
        }

        private static string makeKey(MIB_TCPROW2 row)
        {
            return row.dwLocalAddr + ":" + row.dwLocalPort + "," + row.dwRemoteAddr + ":" + row.dwRemotePort;
        }

        private static void dumpTcpRow(MIB_TCPROW2 row)
        {
            Console.WriteLine("{0} -> {1}", toAddrString(row.dwLocalAddr, row.dwLocalPort), toAddrString(row.dwRemoteAddr, row.dwRemotePort));
        }

        private static void dumpTcpRowWithUsage(MIB_TCPROW2 row, UInt64 inBytes, UInt64 outBytes)
        {
            Console.WriteLine($"{toAddrString(row.dwLocalAddr, row.dwLocalPort)} -> {toAddrString(row.dwRemoteAddr, row.dwRemotePort)}, in/out: {inBytes}/{outBytes} bytes");
        }

        private static string toAddrString(Int32 addr, Int32 port)
        {
            Int32 addr1 = addr & 0xff;
            Int32 addr2 = (addr >> 8) & 0xff;
            Int32 addr3 = (addr >> 16) & 0xff;
            Int32 addr4 = (addr >> 24) & 0xff;

            return $"{addr1,000}.{addr2,000}.{addr3,000}.{addr4,000}:{port}";
        }

        private void outputTotalBytes()
        {
            UInt64 inBytes = 0;
            UInt64 outBytes = 0;
            foreach(TCP_ESTATS_DATA_ROD_v0 stats in _totalingTargets)
            {
                inBytes += stats.DataBytesIn;
                outBytes += stats.DataBytesOut;
            }
            Console.WriteLine($"Total In/Out bytes: {inBytes}/{outBytes} bytes");
        }
    }
}
