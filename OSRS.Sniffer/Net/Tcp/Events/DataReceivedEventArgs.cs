using System;

namespace OSRS.Sniffer.Net.Tcp.Events
{
    internal class DataReceivedEventArgs : EventArgs
    {
        public DataReceivedEventArgs(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; }
    }
}
