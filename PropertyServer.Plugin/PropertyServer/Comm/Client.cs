// Copyright (C) 2026 Martin Renner
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
        private readonly object _mySubscriptionsLock = new object();
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly TcpClient _tcpClient;
        private long _running;
        private long _disconnecting;
        private StreamWriter _writer;
        private long _lastSentTicks;

        /// <summary>Ping interval: send a ping if no message has been sent for this duration.</summary>
        private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(30);

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

            await SendString("SimHub Property Server v" + ThisAssembly.AssemblyFileVersion);
            var pingTask = Task.Run(() => PingLoopAsync(token), token);
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

                if (line == null)
                {
                    // End of stream: client closed the connection.
                    Log.Info("Client closed the connection (end of stream)");
                    await Disconnect();
                }
                else
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

            try
            {
                await pingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when token is cancelled.
            }
        }

        private async Task PingLoopAsync(CancellationToken token)
        {
            // Check every 5 seconds whether a ping is due.
            while (!token.IsCancellationRequested && Running)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), token);
                if (!Running) break;

                var lastSent = new DateTime(Interlocked.Read(ref _lastSentTicks), DateTimeKind.Utc);
                if (DateTime.UtcNow - lastSent >= PingInterval)
                {
                    Log.Debug("Sending ping to client");
                    await SendString("ping");
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
            if (!Running) return;
            lock (_mySubscriptionsLock)
            {
                if (_mySubscriptions.Contains(propertyName)) return;
            }

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
                lock (_mySubscriptionsLock)
                {
                    if (Running)
                    {
                        _mySubscriptions.Add(propertyName);
                    }
                }
            }
        }

        private async Task Unsubscribe(string propertyName)
        {
            lock (_mySubscriptionsLock)
            {
                if (!_mySubscriptions.Contains(propertyName)) return;
            }

            var result = await _subscriptionManager.Unsubscribe(propertyName, ValueChanged);
            if (result)
            {
                lock (_mySubscriptionsLock)
                {
                    _mySubscriptions.Remove(propertyName);
                }
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
            try
            {
                foreach (var profile in profiles)
                {
                    await SendString($"0: {profile.ProfileId} {profile.GetType().Name} {profile.Name}");
                    await SendEffects(1, profile.EffectsContainers);
                }

                Log.Info($"Sent ShakeIt {loggingName} structure with {profiles.Count()} profiles to client");
            }
            catch (Exception e)
            {
                Log.Error($"Exception while sending ShakeIt {loggingName} structure to client", e);
            }

            await SendString("End");
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
            // Already disconnecting: don't touch the (closing) stream. Sending here would just
            // throw and re-enter Disconnect, and doing work for a dead client is pointless.
            if (!Running) return;

            var valueToSend = e.Property.ValueAsString;
            if (e.Property.RawType == typeof(string))
            {
                var hasSpecial = valueToSend.Any(c => c < 32 || c > 126);
                if (hasSpecial)
                {
                    // Encode Base64
                    var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(valueToSend));
                    valueToSend = $"{{base64}}{base64}";
                }
            }
            await SendString($"Property {e.Property.Name} {e.Property.Type} {valueToSend}");
        }

        public async Task Disconnect()
        {
            // Run the teardown exactly once. Disconnect can be triggered concurrently from several paths.
            if (Interlocked.Exchange(ref _disconnecting, 1) == 1) return;
            Running = false;

            // Iterate a snapshot: Unsubscribe is async and other paths may still touch
            // _mySubscriptions, so copy first to guarantee every subscription is removed.
            List<string> mySubscriptionsSnapshot;
            lock (_mySubscriptionsLock)
            {
                mySubscriptionsSnapshot = _mySubscriptions.ToList();
                _mySubscriptions.Clear();
            }

            foreach (var mySubscription in mySubscriptionsSnapshot)
            {
                try
                {
                    await _subscriptionManager.Unsubscribe(mySubscription, ValueChanged);
                }
                catch (Exception e)
                {
                    Log.Warn($"Exception while unsubscribing from {mySubscription} during disconnect", e);
                }
            }

            _tcpClient.Close();
        }

        private async Task SendError(string msg)
        {
            await SendString($"ERR: {msg}");
        }

        private async Task SendString(string msg)
        {
            if (!Running) return;

            // Serialize writes so concurrent callers can't overlap on the shared StreamWriter, which otherwise
            // throws "The stream is currently in use by a previous operation".
            try
            {
                await _writeLock.WaitAsync();
                try
                {
                    await _writer.WriteAsync($"{msg}\r\n");
                    await _writer.FlushAsync();
                }
                finally
                {
                    _writeLock.Release();
                }

                Interlocked.Exchange(ref _lastSentTicks, DateTime.UtcNow.Ticks);
            }
            catch (Exception ex)
            {
                Log.Warn("Exception while sending data to client. We will disconnect the client.", ex);
                await Disconnect();
            }
        }
    }
}