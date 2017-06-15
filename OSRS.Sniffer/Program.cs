using System;
using System.Net;
using System.Threading;
using NLog;
using OSRS.Sniffer.Net;

namespace OSRS.Sniffer
{
    internal class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly ManualResetEvent QuitEvent = new ManualResetEvent(false);

        private static void Main(string[] args)
        {
            // Announce boot.
            Logger.Warn("Starting OSRS Sniffer, shut down with CTRL+C.");

            // Initialise servers.
            var server = new OldschoolServer(IPAddress.Loopback, 43594);

            server.Start();

            // Configure console.
            Console.Title = "OSRS.Sniffer";
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                QuitEvent.Set();
            };

            // Hold.
            QuitEvent.WaitOne();

            // Announce shutdown.
            Logger.Warn("Shutting down OSRS Sniffer.");

            // Stop servers.
            server.Stop();
        }
    }
}
