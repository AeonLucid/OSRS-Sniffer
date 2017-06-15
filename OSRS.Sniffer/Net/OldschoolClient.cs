using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NLog;
using OSRS.Sniffer.Net.Tcp;
using OSRS.Sniffer.Net.Tcp.Events;

namespace OSRS.Sniffer.Net
{
    /// <summary>
    ///     This represents a player using his oldschool client
    ///     to connect to runescape.
    /// </summary>
    internal class OldschoolClient : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly OldschoolServer _oldschoolServer;

        private readonly Connection _client;

        private readonly int _clientId;

        private Connection _server;

        public OldschoolClient(OldschoolServer oldschoolServer, int clientId, Socket client)
        {
            _oldschoolServer = oldschoolServer;
            _clientId = clientId;

            _client = new Connection(client);
            _client.ConnectionClosed += ClientOnConnectionClosed;
            _client.DataReceived += ClientOnDataReceived;
        }

        public bool ConnectToServer(string host, int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var connect = socket.BeginConnect(new DnsEndPoint(host, port), null, null);

            var success = connect.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5), true);
            if (success)
            {
                Logger.Info($"[{_clientId}][Server] Connected to {host}:{port}.");

                socket.EndConnect(connect);

                _server = new Connection(socket);
                _server.ConnectionClosed += ServerOnConnectionClosed;
                _server.DataReceived += ServerOnDataReceived;
                ;

                return true;
            }

            socket.Close();

            return false;
        }

        public void Start()
        {
            new Thread(_server.ReceiveData).Start();
            new Thread(_client.ReceiveData).Start();
        }

        private void ClientOnConnectionClosed(object sender, EventArgs e)
        {
            Logger.Info($"[{_clientId}][Client] Connection closed.");

            _oldschoolServer.RemoveClient(_clientId);
        }

        private void ClientOnDataReceived(object sender, DataReceivedEventArgs e)
        {
            Logger.Info($"[{_clientId}][Client] Received {e.Data.Length} bytes.");
            Logger.Info($"[{_clientId}][Client] Hex: '{BitConverter.ToString(e.Data)}'.");

            // Forward to server.
            _server.SendData(e.Data);
        }

        private void ServerOnConnectionClosed(object sender, EventArgs e)
        {
            Logger.Info($"[{_clientId}][Server] Connection closed.");

            _oldschoolServer.RemoveClient(_clientId);
        }

        private void ServerOnDataReceived(object sender, DataReceivedEventArgs e)
        {
            Logger.Info($"[{_clientId}][Server] Received {e.Data.Length} bytes.");
            Logger.Info($"[{_clientId}][Server] Hex: '{BitConverter.ToString(e.Data)}'.");

            // Forward to client.
            _client.SendData(e.Data);
        }

        public void Dispose()
        {
            _client?.Close();
            _client?.Dispose();

            _server?.Close();
            _server?.Dispose();

            Logger.Info($"[{_clientId}] Disposed.");
        }
    }
}
