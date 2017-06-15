using System;
using System.Net.Sockets;
using NLog;

namespace OSRS.Sniffer.Net
{
    internal class OldschoolClient : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly OldschoolServer _server;

        private readonly Socket _client;

        private readonly int _clientId;

        private readonly byte[] _buffer;

        public OldschoolClient(OldschoolServer server, int clientId, Socket client)
        {
            _server = server;
            _clientId = clientId;
            _client = client;
            _buffer = new byte[1024];
        }

        public void ReceiveData()
        {
            if (!_client.Connected)
            {
                _server.RemoveClient(_clientId);
                return;
            }

            _client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveDataCallback, null);
        }

        private void ReceiveDataCallback(IAsyncResult ar)
        {
            try
            {
                var bytesReceived = _client.EndReceive(ar);

                // If amount of bytes received fits in the entire buffer.
                if (bytesReceived < _buffer.Length)
                {
                    var bytes = new byte[bytesReceived];

                    Buffer.BlockCopy(_buffer, 0, bytes, 0, bytesReceived);

                    Logger.Info($"[Client][{_clientId}] Received {bytesReceived} bytes.");
                    Logger.Info($"[Client][{_clientId}] Hex: '{BitConverter.ToString(bytes)}'.");
                }
                else
                {
                    Logger.Error($"[Client][{_clientId}] Received too much data.");
                }
            }
            finally
            {
                ReceiveData();
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
