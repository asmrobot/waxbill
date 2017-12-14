using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Libuv;
using waxbill.Packets;
using waxbill.Utils;

namespace waxbill
{
    public class TCPServer<TSession>:TCPMonitor where TSession:SocketSession,new() 
    {

        public TCPServer(IProtocol protocol):this(protocol,ServerOption.Define)
        {}

        public TCPServer(IProtocol protocol,ServerOption option):base(protocol,option)
        {
            this.Listener = new TCPListener();
            this.Listener.OnStartSession += Listener_OnStartSession;
        }
        
        public string LocalIP { get; private set; }

        public Int32 LocalPort { get; private set; }

        public TCPListener Listener { get; private set; }

        private ConcurrentDictionary<Int64, TSession> mSessions = new ConcurrentDictionary<Int64, TSession>();

        private Timer mRecycleTimer=null;

        private Int32 IsRunning = 0;

        public void Start(string ip, Int32 port)
        {
            if (Interlocked.CompareExchange(ref this.IsRunning, 1, 0) == 0)
            {
                Validate.ThrowIfZeroOrMinus(port, "端口号不正确");

                this.LocalIP = ip;
                if (string.IsNullOrEmpty(ip))
                {
                    this.LocalIP = "0.0.0.0";
                }
                this.LocalPort = port;

                this.Listener.Start(this.LocalIP, this.LocalPort,this.Option.ListenBacklog);
                //开始自动回收
                if (Option.AutoRecycleSession)
                {
                    mRecycleTimer = new Timer(new TimerCallback(AutoRecycleSessionThread), null, Timeout.Infinite, Timeout.Infinite);
                    mRecycleTimer.Change(Option.RecycleSessionFrequency, Timeout.Infinite);
                }
            }
            
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref this.IsRunning, 0, 1) == 1)
            {
                foreach (var item in mSessions)
                {
                    item.Value.Close(CloseReason.Shutdown, null);
                }
                this.Listener.Stop();
                //开始自动回收
                if (Option.AutoRecycleSession)
                {
                    mRecycleTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
            

        }
        
        private void Listener_OnStartSession(Int64 connectionID,UVTCPHandle connection)
        {
            TSession session = new TSession();
            session.Init(connectionID,connection,this,this.Option);
            if (this.mSessions.TryAdd(session.ConnectionID, session))
            {
                //添加到队列中
                session.RaiseAccept();
                connection.ReadStart(session.AllocMemoryCallback, session.ReadCallback, session, session);
            }
            else
            {
                connection.Close();
            }
        }

        #region session相关

        /// <summary>
        /// 通过sessionid得到session
        /// </summary>
        /// <param name="sessionID"></param>
        /// <returns></returns>
        public TSession GetSession(long sessionID)
        {
            return mSessions.Values.FirstOrDefault<TSession>((item) => item.ConnectionID == sessionID);
        }

        /// <summary>
        /// get session by predicate
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public TSession GetSession(Func<TSession, bool> predicate)
        {
            return mSessions.Values.FirstOrDefault<TSession>(predicate);
        }

        /// <summary>
        /// get all session
        /// </summary>
        /// <returns></returns>
        public ICollection<TSession> GetSessions()
        {
            return mSessions.Values;
        }

        /// <summary>
        /// 所有符合条件的session
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TSession> GetSessions(Func<TSession, bool> predicate)
        {
            return mSessions.Values.Where<TSession>(predicate);
        }

        /// <summary>
        /// 自动回收断开的会话
        /// </summary>
        /// <param name="state"></param>
        private void AutoRecycleSessionThread(object state)
        {
            
            List<Int64> removesConnectionID = new List<Int64>();

            foreach (var item in mSessions)
            {
                if (item.Value.IsClosed)
                {
                    removesConnectionID.Add(item.Key);
                }
            }

            TSession session = null;
            for (int i = 0; i < removesConnectionID.Count; i++)
            {
                mSessions.TryRemove(removesConnectionID[i], out session);
            }
            if (this.IsRunning == 1)
            {
                mRecycleTimer.Change(Option.RecycleSessionFrequency, Timeout.Infinite);
            }
        }
        #endregion
    }
}
