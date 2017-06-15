using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
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

        private readonly Connection _server;

        private readonly int _clientId;

        public OldschoolClient(OldschoolServer oldschoolServer, int clientId, Socket client)
        {
            _oldschoolServer = oldschoolServer;
            _clientId = clientId;

            _client = new Connection(client);
            _client.ConnectionClosed += ClientOnConnectionClosed;
            _client.DataReceived += ClientOnDataReceived;
        }

        public void ReceiveData()
        {
            _client.ReceiveData();
        }

        private void ClientOnConnectionClosed(object sender, EventArgs eventArgs)
        {
            _server.RemoveClient(_clientId);
        }

        private void ClientOnDataReceived(object sender, DataReceivedEventArgs e)
        {
            Logger.Info($"[Client][{_clientId}] Received {e.Data.Length} bytes.");
            Logger.Info($"[Client][{_clientId}] Hex: '{BitConverter.ToString(e.Data)}'.");
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
