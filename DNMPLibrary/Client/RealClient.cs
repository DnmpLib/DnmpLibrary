using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DNMPLibrary.Messages;
using DNMPLibrary.Messages.Types;
using DNMPLibrary.Security;
using DNMPLibrary.Util;
using NLog;

namespace DNMPLibrary.Client
{
    public class RealClient : IDisposable
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        internal NetworkClient NetworkClient { get; }
        internal MessageHandler MessageHandler { get; }

        public DNMPClient SelfClient { get; set; }

        public ConcurrentDictionary<ushort, DNMPClient> ClientsById { get; } = new ConcurrentDictionary<ushort, DNMPClient>();


        internal IAsymmetricKey Key { get; private set; }

        internal ISymmetricKey DummySymmetricKey { get; private set; }

        public MessageInterface MessageInterface { get; }

        public ClientConfig Config { get; }

        public delegate void ClientConnectedEvent(ushort clientId);
        public delegate void ClientDisconnectedEvent(ushort clientId);
        public delegate void ClientParentChangedEvent(ushort clientId, ushort newParentId, ushort oldParentId);

        public delegate void ConnectedEvent();
        public delegate void ConnectionTimeoutEvent();
        public delegate void DisconnectedEvent();

        public delegate void SecondaryConnectionStartEvent(ushort fromClientId, ushort toClientId);
        public delegate void SecondaryConnectionStopEvent(ushort fromClientId, ushort toClientId);

        public event ClientConnectedEvent OnClientConnected;
        public event ClientDisconnectedEvent OnClientDisconnected;
        public event ClientParentChangedEvent OnClientParentChange;

        public event ConnectedEvent OnConnected;
        public event ConnectionTimeoutEvent OnConnectionTimeout;
        public event DisconnectedEvent OnDisconnected;

        public event SecondaryConnectionStartEvent OnSecondaryConnectionStart;
        public event SecondaryConnectionStopEvent OnSecondaryConnectionStop;

        private Guid connectionTimeoutEvent;

        public enum ClientStatus
        {
            NotConnected,
            Connected,
            Connecting,
            Handshaking,
            Disconnecting
        }

        public ClientStatus CurrentStatus { get; internal set; }

        public RealClient() : this(new DummyMessageInterface()) { }

        public RealClient(MessageInterface messageInterface) : this(messageInterface,
            new ClientConfig())
        { }

        public RealClient(MessageInterface messageInterface, ClientConfig config)
        {
            Config = config;
            MessageInterface = messageInterface;
            MessageHandler = new MessageHandler(this);
            NetworkClient = new NetworkClient(this);
        }

        public async Task<IPAddress> ResolveIpAddressAsync(string hostname)
        {
            return IPAddress.TryParse(hostname, out var ipAddress) ? ipAddress : (await Dns.GetHostEntryAsync(hostname)).AddressList.First();
        }

        public IPAddress ResolveIpAddress(string hostname)
        {
            return IPAddress.TryParse(hostname, out var ipAddress) ? ipAddress : Dns.GetHostEntry(hostname).AddressList.First();
        }

        public async Task ConnectAsync(EndPoint endPoint, EndPoint sourceEndPoint, bool invokeEvents, IAsymmetricKey key, ISymmetricKey dummySymmetricKey)
        {
            await ConnectManyAsync(new[] { endPoint }, sourceEndPoint, invokeEvents, key, dummySymmetricKey);
        }

        public async Task ConnectManyAsync(EndPoint[] endPoints, EndPoint sourceEndPoint, bool invokeEvents, IAsymmetricKey key, ISymmetricKey dummySymmetricKey)
        {
            if (CurrentStatus != ClientStatus.NotConnected)
                return;
            Initialize(sourceEndPoint, key, dummySymmetricKey);
            CurrentStatus = ClientStatus.Connecting;
            if (invokeEvents)
            {
                connectionTimeoutEvent = EventQueue.AddEvent(_ =>
                {
                    NetworkClient.Stop();
                    MessageHandler.Stop();
                    ClientsById.Clear();
                    CurrentStatus = ClientStatus.NotConnected;
                    OnConnectionTimeout?.Invoke();
                }, null, DateTime.Now.AddMilliseconds(Config.ConnectionTimeout));
            }
            else
            {
                connectionTimeoutEvent = EventQueue.AddEvent(_ =>
                {
                    CurrentStatus = ClientStatus.NotConnected;
                }, null, DateTime.Now.AddMilliseconds(Config.ConnectionTimeout));
            }

            logger.Debug($"Trying to connect to {endPoints.Length} endpoints. First: {endPoints.FirstOrDefault()}");

            foreach (var endPoint in endPoints)
            {
                logger.Debug($"Trying to connect to {endPoint}");
                NetworkClient.SendBaseMessage(new BaseMessage(new ConnectionRequestMessage(key.GetNetworkId()), 0xFFFF, 0xFFFF),
                    endPoint);
            }

            if (invokeEvents)
                return;
            SpinWait.SpinUntil(() => CurrentStatus == ClientStatus.Connecting || CurrentStatus == ClientStatus.Handshaking);
            if (CurrentStatus == ClientStatus.NotConnected)
                throw new TimeoutException("Connection timeout");
            await Task.Delay(0);
        }

        public async Task StartAsFirstNodeAsync(EndPoint sourceEndPoint, EndPoint publicEndPoint, IAsymmetricKey key, ISymmetricKey dummySymmetricKey)
        {
            if (CurrentStatus != ClientStatus.NotConnected)
                return;
            SelfClient = new DNMPClient
            {
                Id = 0,
                EndPoint = publicEndPoint
            };
            Initialize(sourceEndPoint, key, dummySymmetricKey);
            CurrentStatus = ClientStatus.Connected;
            logger.Info($"Started as first node on {sourceEndPoint} [{publicEndPoint}]");
            OnClientConnected?.Invoke(SelfClient.Id);
            OnConnected?.Invoke();
            MessageInterface.Initialize(SelfClient.Id);
            await Task.Delay(0);
        }
        
        private void Initialize(EndPoint sourceEndPoint, IAsymmetricKey key, ISymmetricKey dummySymmetricKey)
        {
            Key = key;
            DummySymmetricKey = dummySymmetricKey;
            MessageHandler.Start();
            NetworkClient.Start(sourceEndPoint);
        }

        internal void AddClient(DNMPClient client)
        {
            logger.Debug($"Added client #{client.Id}");
            ClientsById.TryAdd(client.Id, client);
            OnClientConnected?.Invoke(client.Id);
        }

        internal void RemoveClient(DNMPClient client)
        {
            logger.Debug($"Removed client #{client.Id}");
            ClientsById.TryRemove(client.Id, out _);
            OnClientDisconnected?.Invoke(client.Id);
        }
        
        internal void ClientParentChanged(ushort clientId, ushort newParentId, ushort oldParentId) =>
            OnClientParentChange?.Invoke(clientId, newParentId, oldParentId);

        internal void SecondaryConnectionStart(ushort toClientId) =>
            OnSecondaryConnectionStart?.Invoke(SelfClient.Id, toClientId);

        internal void SecondaryConnectionStop(ushort toClientId) =>
            OnSecondaryConnectionStop?.Invoke(SelfClient.Id, toClientId);


        internal void SelfConnected()
        {
            CurrentStatus = ClientStatus.Connected;
            EventQueue.RemoveEvent(connectionTimeoutEvent);
            OnConnected?.Invoke();
        }

        public void Stop()
        {
            if (CurrentStatus == ClientStatus.NotConnected)
                return;
            CurrentStatus = ClientStatus.Disconnecting;
            EventQueue.RemoveEvent(connectionTimeoutEvent);
            NetworkClient.Stop();
            MessageHandler.Stop();
            ClientsById.Clear();
            CurrentStatus = ClientStatus.NotConnected;
            OnDisconnected?.Invoke();
        }

        public void Dispose()
        {
            NetworkClient.Dispose();
            MessageHandler.Dispose();
        }
    }
}
