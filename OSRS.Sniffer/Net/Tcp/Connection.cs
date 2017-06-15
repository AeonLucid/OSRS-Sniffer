using System;
using System.Net.Sockets;
using NLog;
using OSRS.Sniffer.Net.Tcp.Events;

namespace OSRS.Sniffer.Net.Tcp
{
    internal class Connection : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Socket _socket;

        private readonly byte[] _buffer;

        public Connection(Socket socket, int bufferSize = 1024)
        {
            _socket = socket;
            _buffer = new byte[bufferSize];
        }

        public event EventHandler ConnectionClosed;

        public event EventHandler<DataReceivedEventArgs> DataReceived;

        public void ReceiveData()
        {
            if (!_socket.Connected)
            {
                OnConnectionClosed();
                return;
            }

            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveDataCallback, null);
        }

        private void ReceiveDataCallback(IAsyncResult ar)
        {
            try
            {
                var bytesReceived = _socket.EndReceive(ar);

                // If amount of bytes received fits in the entire buffer.
                if (bytesReceived < _buffer.Length)
                {
                    var bytes = new byte[bytesReceived];

                    Buffer.BlockCopy(_buffer, 0, bytes, 0, bytesReceived);
                    
                    OnDataReceived(bytes);
                }
                else
                {
                    Logger.Error($"[{_socket.RemoteEndPoint}] Received too much data.");
                }
            }
            finally
            {
                ReceiveData();
            }
        }

        private void OnConnectionClosed()
        {
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }

        private void OnDataReceived(byte[] data)
        {
            DataReceived?.Invoke(this, new DataReceivedEventArgs(data));
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }
    }
}
