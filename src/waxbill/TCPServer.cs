using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using waxbill.Pools;
using waxbill.Sessions;

namespace waxbill
{
    

    public class TCPServer<TSession> : SocketMonitor where TSession: SessionBase,new()
    {
        private SocketListener<TSession> _Listeners;
        private Timer m_RecycleTimer;
        private ConcurrentDictionary<long, TSession> m_Session;

        public TCPServer(IProtocol protocol) : this(protocol, TCPOption.SERVER_DEFAULT)
        {}

        public TCPServer(IProtocol protocol, TCPOption option) : base(protocol, option,new PoolProvider(option))
        {
            this.m_Session = new ConcurrentDictionary<long, TSession>();
        }

        /// <summary>
        /// 接入一个会话
        /// </summary>
        /// <param name="session"></param>
        internal void Accept(TSession session)
        {
            if (this.m_Session.TryAdd(session.ConnectionID, session))
            {
                session.Start();
            }
        }

        private void AutoRecycleSessionThread(object state)
        {
            List<long> list = new List<long>();
            foreach (KeyValuePair<long, TSession> pair in this.m_Session)
            {
                if (pair.Value.IsClosed)
                {
                    list.Add(pair.Key);
                }
            }
            TSession local = default(TSession);
            for (int i = 0; i < list.Count; i++)
            {
                this.m_Session.TryRemove(list[i], out local);
            }
            this.m_RecycleTimer.Change(base.Option.RecycleSecond, -1);
        }

        public TSession GetSession(Func<TSession, bool> predicate)
        {
            return this.m_Session.Values.FirstOrDefault<TSession>(predicate);
        }

        public TSession GetSession(long sessionID)
        {
            return this.m_Session.Values.FirstOrDefault<TSession>(item => (item.ConnectionID == sessionID));
        }

        public ICollection<TSession> GetSessions()
        {
            return this.m_Session.Values;
        }


        public void Start(string ip, Int32 port)
        {
            IPAddress address;
            if (!IPAddress.TryParse(ip, out address))
            {
                throw new ArgumentOutOfRangeException("ip");
            }
            if (port <= 0 || port >= 65536)
            {
                throw new ArgumentOutOfRangeException("port");
            }

            Start(new IPEndPoint(address, port));
        }

        public void Start(IPEndPoint endpoint)
        {
            this._Listeners = new SocketListener<TSession>(endpoint, (TCPServer<TSession>) this);
            this._Listeners.Start();
            if (base.Option.AutoRecycleSession)
            {
                this.m_RecycleTimer = new System.Threading.Timer(new TimerCallback(this.AutoRecycleSessionThread), null, -1, -1);
                this.m_RecycleTimer.Change(base.Option.RecycleSecond, -1);
            }
        }

        public void Stop()
        {
            this._Listeners.Stop();
            foreach (KeyValuePair<long, TSession> pair in this.m_Session)
            {
                pair.Value.Close(CloseReason.Default);
            }
        }
    }
}

