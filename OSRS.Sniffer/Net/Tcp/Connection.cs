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

        private bool _closed;

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
                return;
            }

            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveDataCallback, null);
        }

        public void SendData(byte[] bytes)
        {
            _socket.Send(bytes);
        }

        public void Close()
        {
            if (!_socket.Connected)
            {
                return;
            }

            if (_closed)
            {
                return;
            }

            _closed = true;
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();

            OnConnectionClosed();
        }

        private void ReceiveDataCallback(IAsyncResult ar)
        {
            if (_closed)
            {
                return;
            }

            try
            {
                var bytesReceived = _socket.EndReceive(ar);
                if (bytesReceived == 0)
                {
                    Close();

                    return;
                }

                // If amount of bytes received fits in the entire buffer.
                // TODO: Packet handler to receive full packets before forwarding them.
                var bytes = new byte[bytesReceived];

                Buffer.BlockCopy(_buffer, 0, bytes, 0, bytesReceived);

                OnDataReceived(bytes);
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
