// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace SimHub.Plugins.PropertyServer
{
    public class Server
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Server));
        private static int _clientId;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private TcpListener _tcpListener;
        private long _running;
        private readonly List<Client> _clients = new List<Client>();
        private readonly SubscriptionManager _subscriptionManager;
        private readonly int _port;

        private bool Running
        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        public Server(SubscriptionManager subscriptionManager, int port)
        {
            _subscriptionManager = subscriptionManager;
            _port = port;
        }

        /// <summary>
        /// Starts the server which is listening for connections. This method is blocking until "Stop()" is getting called.
        /// </summary>
        public async Task Start()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, _port);
            _tcpListener.Start();
            Log.Info($"Listening on port {_port}");

            Running = true;
            while (Running)
            {
                try
                {
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    LogicalThreadContext.Properties["client"] = Interlocked.Increment(ref _clientId);
                    Log.Info($"New connection from client {tcpClient.Client.RemoteEndPoint}");
                    var client = new Client(_subscriptionManager, tcpClient);
                    _clients.Add(client);
                    var clientTask = client.Start(_cts.Token);

#pragma warning disable CS4014
                    clientTask.ContinueWith(t =>
#pragma warning restore CS4014
                    {
                        Log.Info("Client has finished. Removing it.");
                        _clients.Remove(client);
                    }, _cts.Token);
                }
                catch (ObjectDisposedException ode)
                {
                    // TcpListener was stopped. "_running" should be false at this moment.
                    if (Running) Log.Warn($"Server main loop got interrupted, but _running is still true: {ode}");
                    else Log.Info("Server main loop got interrupted.");
                }
            }

            Log.Info("Exiting Listener");
        }

        public async Task Stop()
        {
            Log.Info($"Ending server, disconnecting {_clients.Count} clients");

            // Clone list to avoid InvalidOperationException because of concurrent modification.
            var clients = new List<Client>(_clients);
            foreach (var client in clients)
            {
                await client.Disconnect();
            }

            Running = false;
            _cts.Cancel();
            _tcpListener.Stop();
        }
    }
}