// Copyright (C) 2023 Martin Renner
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
using SimHub.Plugins.PropertyServer.ShakeIt;

namespace SimHub.Plugins.PropertyServer.Comm
{
    /// <summary>
    /// This class represents a connected client.
    /// </summary>
    public class Client
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Client));
        private readonly ISimHub _simHub;
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

        public Client(ISimHub simHub, SubscriptionManager subscriptionManager, TcpClient tcpClient)
        {
            _simHub = simHub;
            _subscriptionManager = subscriptionManager;
            _tcpClient = tcpClient;
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
                case "trigger-input":
                    if (lineItems.Length != 2)
                    {
                        Log.Warn($"Invalid 'trigger-input' command, wrong number of arguments: {line}");
                        await SendError("Invalid 'trigger-input' command, wrong number of arguments");
                        return;
                    }
                    TriggerInput(lineItems[1]);
                    return;
                case "trigger-input-pressed":
                    if (lineItems.Length != 2)
                    {
                        Log.Warn($"Invalid 'trigger-input-pressed' command, wrong number of arguments: {line}");
                        await SendError("Invalid 'trigger-input-pressed' command, wrong number of arguments");
                        return;
                    }
                    TriggerInputPressed(lineItems[1]);
                    return;
                case "trigger-input-released":
                    if (lineItems.Length != 2)
                    {
                        Log.Warn($"Invalid 'trigger-input-release' command, wrong number of arguments: {line}");
                        await SendError("Invalid 'trigger-input-release' command, wrong number of arguments");
                        return;
                    }
                    TriggerInputReleased(lineItems[1]);
                    return;
                case "shakeit-bass-structure":
                    await ShakeItBassStructure();
                    return;
                case "shakeit-motors-structure":
                    await ShakeItMotorsStructure();
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

            var property = await _subscriptionManager.Subscribe(propertyName, ValueChanged, SendError);
            // We have to check here (outside the SubscriptionManager) if the ShakeIt Bass element exists.
            if (property is SimHubPropertyShakeItBass bassProp && _simHub.FindShakeItBassEffect(bassProp.Guid) == null)
            {
                Log.Warn($"ShakeIt Bass effect or effect group with {bassProp.Guid} does not exist");
            }
            // Same for ShakeIt Motors.
            if (property is SimHubPropertyShakeItMotors motorsProp && _simHub.FindShakeItMotorsEffect(motorsProp.Guid) == null)
            {
                Log.Warn($"ShakeIt Motors effect or effect group with {motorsProp.Guid} does not exist");
            }

            if (property != null)
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

        private void TriggerInput(string inputName)
        {
            _simHub.TriggerInput(inputName);
        }

        private void TriggerInputPressed(string inputName)
        {
            _simHub.TriggerInputPressed(inputName);
        }

        private void TriggerInputReleased(string inputName)
        {
            _simHub.TriggerInputReleased(inputName);
        }

        private async Task ShakeItBassStructure()
        {
            await SendString("ShakeIt Bass structure");
            await ShakeItStructure(_simHub.ShakeItBassStructure(), "Bass");
        }

        private async Task ShakeItMotorsStructure()
        {
            await SendString("ShakeIt Motors structure");
            await ShakeItStructure(_simHub.ShakeItMotorsStructure(), "Motors");
        }

        private async Task ShakeItStructure(ICollection<Profile> profiles, string loggingName)
        {
            // Send structure, profile by profile.
            foreach (var profile in profiles)
            {
                await SendString($"0: {profile.ProfileId} {profile.GetType().Name} {profile.Name}");
                await SendEffects(1, profile.EffectsContainers);
            }
            await SendString("End");
            Log.Info($"Sent ShakeIt {loggingName} structure with {profiles.Count()} profiles to client");
        }

        private async Task SendEffects(int depth, IEnumerable<EffectsContainerBase> profileEffectsContainers)
        {
            foreach (var ecb in profileEffectsContainers)
            {
                var indent = new string(' ', depth * 2);
                await SendString($"{indent}{depth}: {ecb.ContainerId} {ecb.GetType().Name} {ecb.FullName()}");
                if (ecb is GroupContainer groupContainer)
                {
                    await SendEffects(depth + 1, groupContainer.EffectsContainers);
                }
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
            await SendString("  trigger-input inputName");
            await SendString("  trigger-input-pressed inputName");
            await SendString("  trigger-input-released inputName");
            await SendString("  shakeit-bass-structure");
            await SendString("  shakeit-motors-structure");
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