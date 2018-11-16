namespace ZTImage.Net
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using ZTImage.Net.Utils;

    public abstract class SocketMonitor
    {
        private long _ConnectionIncremer;
        internal ZTImage.Net.Utils.BufferManager BufferManager;
        internal SocketConfiguration Config;
        internal ZTImage.Net.Utils.SendingPool SendingPool;
        internal EventArgPool SocketEventArgsPool;

        [field: CompilerGenerated]
        public event OnConnectionEvent OnConnection;

        [field: CompilerGenerated]
        public event OnDisconnectedEvent OnDisconnected;

        [field: CompilerGenerated]
        public event OnReceiveEvent OnReceive;

        [field: CompilerGenerated]
        public event OnSendedEvent OnSended;

        public SocketMonitor(IProtocol protocol, SocketConfiguration config)
        {
            if (protocol == null)
            {
                throw new ArgumentNullException("protocol");
            }
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            if (config.BufferSize <= 0)
            {
                throw new ArgumentNullException("buffersize");
            }
            this.Config = config;
            this._Protocol = protocol;
            this.MinSendingPoolSize = config.MinSendingPoolSize;
            this.MaxSendingPoolSize = config.MaxSendingPoolSize;
            this.SendingQueueSize = config.SendQueueSize;
            this.SendingPool = new ZTImage.Net.Utils.SendingPool();
            this.SendingPool.Initialize(this.MinSendingPoolSize, this.MaxSendingPoolSize, this.SendingQueueSize);
            this.BufferManager = new ZTImage.Net.Utils.BufferManager(config);
            this.SocketEventArgsPool = new EventArgPool(config);
        }

        internal long GetNextConnectionID()
        {
            return Interlocked.Increment(ref this._ConnectionIncremer);
        }

        internal void RaiseOnConnectionEvent(SocketSession session)
        {
            if (this.OnConnection != null)
            {
                this.OnConnection(session);
            }
        }

        internal void RaiseOnDisconnectedEvent(SocketSession session, CloseReason reason)
        {
            if (this.OnDisconnected != null)
            {
                this.OnDisconnected(session, reason);
            }
        }

        internal void RaiseOnReceiveEvent(SocketSession session, Packet collection)
        {
            if (this.OnReceive != null)
            {
                this.OnReceive(session, collection);
            }
        }

        internal void RaiseOnSendedEvent(SocketSession session, SendingQueue packet, bool result)
        {
            if (this.OnSended != null)
            {
                this.OnSended(session, packet, result);
            }
        }

        internal IProtocol _Protocol { get; set; }

        public int BufferSize { get; private set; }

        public int MaxSendingPoolSize { get; private set; }

        public int MinSendingPoolSize { get; set; }

        public int SendingQueueSize { get; private set; }
    }
}

