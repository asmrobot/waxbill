using System;
using System.Runtime.CompilerServices;
using System.Threading;
using waxbill.Packets;
using waxbill.Pools;
using waxbill.Sessions;
using waxbill.Utils;



namespace waxbill
{
    

    public abstract class SocketMonitor
    {
        private long _ConnectionIncremer;
        internal BufferManager BufferManager;
        internal SocketConfiguration Config;
        internal SendingQueuePool SendingPool;
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
            this.SendingPool = new SendingQueuePool();
            this.SendingPool.Initialize(this.MinSendingPoolSize, this.MaxSendingPoolSize, this.SendingQueueSize);
            this.BufferManager = new BufferManager(config);
            this.SocketEventArgsPool = new EventArgPool(config);
        }

        internal long GetNextConnectionID()
        {
            return Interlocked.Increment(ref this._ConnectionIncremer);
        }

        internal void RaiseOnConnectionEvent(SessionBase session)
        {
            if (this.OnConnection != null)
            {
                this.OnConnection(session);
            }
        }

        internal void RaiseOnDisconnectedEvent(SessionBase session, CloseReason reason)
        {
            if (this.OnDisconnected != null)
            {
                this.OnDisconnected(session, reason);
            }
        }

        internal void RaiseOnReceiveEvent(SessionBase session, Packet collection)
        {
            if (this.OnReceive != null)
            {
                this.OnReceive(session, collection);
            }
        }

        internal void RaiseOnSendedEvent(SessionBase session, SendingQueue packet, bool result)
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

