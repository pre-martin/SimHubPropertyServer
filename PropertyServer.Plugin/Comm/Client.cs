// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using SimHub.Plugins.PropertyServer.Property;

namespace SimHub.Plugins.PropertyServer.Comm
{
    /// <summary>
    /// This class represents a connected client.
    /// </summary>
    public class Client
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Client));
        private readonly SubscriptionManager _subscriptionManager;
        private readonly HashSet<string> _mySubscriptions = new HashSet<string>();
        private readonly TcpClient _tcpClient;
        private long _running;
        private StreamWriter _writer;

        private bool Running
        {
            get => Interlocked.Read(ref _running) == 1;
            set => Interlocked.Exchange(ref _running, Convert.ToInt64(value));
        }

        public Client(SubscriptionManager subscriptionManager, TcpClient tcpClient)
        {
            _subscriptionManager = subscriptionManager;
            this._tcpClient = tcpClient;
        }

        public async Task Start(CancellationToken token)
        {
            Running = true;
            var stream = _tcpClient.GetStream();
            var reader = new StreamReader(stream);
            _writer = new StreamWriter(stream);

            await SendString("SimHub Property Server");
            while (Running && !token.IsCancellationRequested)
            {
                string line = null;
                try
                {
                    line = await reader.ReadLineAsync();
                }
                catch (IOException ioe)
                {
                    Log.Warn($"IOException while waiting for client data. Probably the client closed the connection: {ioe.Message}");
                    await Disconnect();
                }

                if (line != null)
                {
                    try
                    {
                        await HandleClientCommand(line);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Unhandled exception while handling command from client: {e}");
                    }
                }
            }
        }

        private async Task HandleClientCommand(string line)
        {
            var lineItems = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (lineItems.Length == 0) return;

            Log.Debug($"Received from client: {line}");
            var command = lineItems[0];
            switch (command)
            {
                case "disconnect":
                    await Disconnect();
                    return;
                case "subscribe":
                    if (lineItems.Length != 2)
                    {
                        Log.Warn($"Invalid 'subscribe' command, wrong number of arguments: {line}");
                        await SendError("Invalid 'subscribe' command, wrong number of arguments");
                        return;
                    }

                    await Subscribe(lineItems[1]);
                    return;
                case "unsubscribe":
                    if (lineItems.Length != 2)
                    {
                        Log.Warn($"Invalid 'subscribe' command, wrong number of arguments: {line}");
                        await SendError("Invalid 'subscribe' command, wrong number of arguments");
                        return;
                    }

                    await Unsubscribe(lineItems[1]);
                    return;
                case "help":
                    await Help();
                    return;
                default:
                    Log.Warn($"Received unknown command: {line}");
                    await SendError("Received unknown command");
                    return;
            }
        }

        private async Task Subscribe(string propertyName)
        {
            if (_mySubscriptions.Contains(propertyName)) return;

            var result = await _subscriptionManager.Subscribe(propertyName, ValueChanged, SendError);
            if (result)
            {
                _mySubscriptions.Add(propertyName);
            }
        }

        private async Task Unsubscribe(string propertyName)
        {
            if (!_mySubscriptions.Contains(propertyName)) return;

            var result = await _subscriptionManager.Unsubscribe(propertyName, ValueChanged);
            if (result)
            {
                _mySubscriptions.Remove(propertyName);
            }
        }

        private async Task Help()
        {
            var propertyList = PropertyAccessor.GetAvailableProperties();
            await SendString("Available properties:");
            foreach (var p in propertyList.OrderBy(s => s.Name).ToList())
            {
                await SendString($"  {p.Name} {p.Type}");
            }

            await SendString("Available commands:");
            await SendString("  subscribe propertyName");
            await SendString("  unsubscribe propertyName");
            await SendString("  disconnect");
        }

        private async Task ValueChanged(ValueChangedEventArgs e)
        {
            await SendString($"Property {e.Property.Name} {e.Property.Type} {e.Property.ValueAsString}");
        }

        public async Task Disconnect()
        {
            Running = false;
            foreach (var mySubscription in _mySubscriptions)
            {
                await _subscriptionManager.Unsubscribe(mySubscription, ValueChanged);
            }

            _tcpClient.Close();
        }

        private async Task SendError(string msg)
        {
            await SendString($"ERR: {msg}");
        }

        private async Task SendString(string msg)
        {
            await _writer.WriteAsync($"{msg}\r\n");
            await _writer.FlushAsync();
        }
    }
}