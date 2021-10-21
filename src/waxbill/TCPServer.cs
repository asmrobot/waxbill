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
    

    public class TCPServer<TSession> : SocketMonitor where TSession: Session,new()
    {
        private SocketListener<TSession> _Listeners;
        private Timer _RecycleTimer;
        private ConcurrentDictionary<long, TSession> _Sessions;

        public Int32 Port { get; private set; }

        public TCPServer(IProtocol protocol) : this(protocol, TCPOptions.SERVER_DEFAULT)
        {}

        public TCPServer(IProtocol protocol, TCPOptions option) : base(protocol, option,new PoolProvider(option))
        {
            this._Sessions = new ConcurrentDictionary<long, TSession>();
        }

        /// <summary>
        /// 接入一个会话
        /// </summary>
        /// <param name="session"></param>
        internal void Accept(TSession session)
        {
            if (this._Sessions.TryAdd(session.ConnectionID, session))
            {
                session.Start();
            }
        }

        private void AutoRecycleSessionThread(object state)
        {
            List<long> list = new List<long>();
            foreach (KeyValuePair<long, TSession> pair in this._Sessions)
            {
                if (pair.Value.IsClosed)
                {
                    list.Add(pair.Key);
                }
            }
            TSession local = default(TSession);
            for (int i = 0; i < list.Count; i++)
            {
                this._Sessions.TryRemove(list[i], out local);
            }
            this._RecycleTimer.Change(base.Option.RecycleSecond, -1);
        }

        public TSession GetSession(Func<TSession, bool> predicate)
        {
            return this._Sessions.Values.FirstOrDefault<TSession>(predicate);
        }

        public TSession GetSession(long sessionID)
        {
            return this._Sessions.Values.FirstOrDefault<TSession>(item => (item.ConnectionID == sessionID));
        }

        public ICollection<TSession> GetSessions()
        {
            return this._Sessions.Values;
        }

        /// <summary>
        /// 启动服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
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
            this.Port = endpoint.Port;
            this._Listeners = new SocketListener<TSession>(endpoint,this);
            this._Listeners.Start();
            if (Option.AutoRecycleSession)
            {
                _RecycleTimer = new Timer(this.AutoRecycleSessionThread, null, Timeout.Infinite,Timeout.Infinite);
                _RecycleTimer.Change(Option.RecycleSecond, -1);
            }
        }

        public void Stop()
        {
            this._Listeners.Stop();
            foreach (KeyValuePair<long, TSession> pair in this._Sessions)
            {
                pair.Value.Close(CloseReason.Default);
            }
        }
    }
}

