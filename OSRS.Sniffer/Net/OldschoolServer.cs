using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NLog;

namespace OSRS.Sniffer.Net
{
    internal class OldschoolServer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IPAddress _ipAddress;

        private readonly int _port;

        private readonly TcpListener _listener;

        private int _clientId;

        private readonly ConcurrentDictionary<int, OldschoolClient> _clients;

        public OldschoolServer(IPAddress ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
            _listener = new TcpListener(ipAddress, port);
            _clientId = 0;
            _clients = new ConcurrentDictionary<int, OldschoolClient>();
        }

        public bool Listening { get; private set; }

        public void Start()
        {
            if (Listening)
            {
                throw new Exception("Server is already listening.");
            }

            Listening = true;

            // Announce start
            Logger.Warn($"[{_port}] Starting server at {_ipAddress}:{_port}.");

            // Start server thread.
            new Thread(ListenAsync).Start();
        }

        public void Stop()
        {
            if (!Listening)
            {
                throw new Exception("Server is not listening.");
            }

            // Stop server thread.
            Listening = false;
        }

        public void RemoveClient(int clientId)
        {
            if (!_clients.ContainsKey(clientId))
            {
                return;
            }

            if (_clients.TryRemove(clientId, out OldschoolClient client))
            {
                client.Dispose();
            }
        }

        private async void ListenAsync()
        {
            _listener.Start(10);

            while (Listening)
            {
                var socket = await _listener.AcceptSocketAsync();
                var clientId = _clientId++;
                var client = new OldschoolClient(this, clientId, socket);

                if (_clients.TryAdd(clientId, client))
                {
                    Logger.Info($"[{_port}] Received connection {clientId} from {socket.RemoteEndPoint}.");
                    
                    // Connect client to runescape (Server is world 382).
                    // TODO: Receive server ip from OSRS client.
                    if (client.ConnectToServer("l3uklo11-bond0-11.jagex.com", _port))
                    {
                        // Start threads.
                        client.Start();
                    }
                }
                else
                {
                    Logger.Error($"[{_port}] Received failing connection {clientId} from {socket.RemoteEndPoint}.");

                    socket.Close();
                }
            }
        }
    }
}
