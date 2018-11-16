using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using waxbill.Sessions;

namespace waxbill
{
    

    public class SocketServer<TSession> : SocketMonitor where TSession: SessionBase, new()
    {
        private SocketListener<TSession> _Listeners;
        private Timer m_RecycleTimer;
        private ConcurrentDictionary<long, TSession> m_Session;

        public SocketServer(IProtocol protocol) : base(protocol, SocketConfiguration.Default)
        {
            this.m_Session = new ConcurrentDictionary<long, TSession>();
        }

        public SocketServer(IProtocol protocol, SocketConfiguration configuration) : base(protocol, configuration)
        {
            this.m_Session = new ConcurrentDictionary<long, TSession>();
        }

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
            this.m_RecycleTimer.Change(base.Config.RecycleSessionFrequency, -1);
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

        public void Start(IPEndPoint endpoint)
        {
            this._Listeners = new SocketListener<TSession>(endpoint, (SocketServer<TSession>) this);
            this._Listeners.Start();
            if (base.Config.AutoRecycleSession)
            {
                this.m_RecycleTimer = new System.Threading.Timer(new TimerCallback(this.AutoRecycleSessionThread), null, -1, -1);
                this.m_RecycleTimer.Change(base.Config.RecycleSessionFrequency, -1);
            }
        }

        public void Stop()
        {
            this._Listeners.Stop();
            foreach (KeyValuePair<long, TSession> pair in this.m_Session)
            {
                pair.Value.Close(CloseReason.Shutdown);
            }
        }
    }
}

