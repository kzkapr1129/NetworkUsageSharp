using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Win32
{
    public enum TCP_ESTATS_TYPE: int
    {
        TcpConnectionEstatsSynOpts = 0,
        TcpConnectionEstatsData,
        TcpConnectionEstatsSndCong,
        TcpConnectionEstatsPath,
        TcpConnectionEstatsSendBuff,
        TcpConnectionEstatsRec,
        TcpConnectionEstatsObsRec,
        TcpConnectionEstatsBandwidth,
        TcpConnectionEstatsFineRtt,
        TcpConnectionEstatsMaximum,
    }

    public enum TCP_CONNECTION_OFFLOAD_STATE
    {
        TcpConnectionOffloadStateInHost = 0,
        TcpConnectionOffloadStateOffloading,
        TcpConnectionOffloadStateOffloaded,
        TcpConnectionOffloadStateUploading,
        TcpConnectionOffloadStateMax
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPTABLE2
    {
        public Int32 dwNumEntries;
        MIB_TCPROW2 table;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW2
    {
#pragma warning disable CS0649
        public Int32 dwState;
        public Int32 dwLocalAddr;
        public Int32 dwLocalPort;
        public Int32 dwRemoteAddr;
        public Int32 dwRemotePort;
        public Int32 dwOwningPid;
        public TCP_CONNECTION_OFFLOAD_STATE dwOffloadState;
#pragma warning restore CS0649

        public MIB_TCPROW2(MIB_TCPROW2 row)
        {
            dwState = row.dwState;
            dwLocalAddr = row.dwLocalAddr;
            dwLocalPort = row.dwLocalPort;
            dwRemoteAddr = row.dwRemoteAddr;
            dwRemotePort = row.dwRemotePort;
            dwOwningPid = row.dwOwningPid;
            dwOffloadState = row.dwOffloadState;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TCP_ESTATS_DATA_RW_v0
    {
        public bool EnableCollection;

        public TCP_ESTATS_DATA_RW_v0(bool enable)
        {
            EnableCollection = enable;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TCP_ESTATS_DATA_ROD_v0
    {
        public ulong DataBytesOut;
        public ulong DataSegsOut;
        public ulong DataBytesIn;
        public ulong DataSegsIn;
        public ulong SegsOut;
        public ulong SegsIn;
        public uint SoftErrors;
        public uint SoftErrorReason;
        public uint SndUna;
        public uint SndNxt;
        public uint SndMax;
        public ulong ThruBytesAcked;
        public uint RcvNxt;
        public ulong ThruBytesReceived;
    }

    public enum ErrorCode
    {
        NO_ERROR = 0,
        ERROR_INSUFFICIENT_BUFFER = 122
    }

    class Api
    {
        [DllImport("iphlpapi.dll", EntryPoint = "GetTcpTable2")]
        public static extern int GetTcpTable2(IntPtr TcpTable, ref int SizePointer, bool order);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int GetPerTcpConnectionEStats(
            IntPtr Row, TCP_ESTATS_TYPE EstatsType,
            out byte Rw, int RwVersion, int RwSize,
            IntPtr Ros, int RosVersion, int RosSize,
            IntPtr Rod, int RodVersion, int RodSize);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int SetPerTcpConnectionEStats(
            IntPtr Row, TCP_ESTATS_TYPE EstatsType,
            ref bool Rw, int RwVersion, int RwSize,
            int Offset);
    }
}
