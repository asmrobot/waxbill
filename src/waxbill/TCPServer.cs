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
using waxbill.Pools;
using waxbill.Sessions;
using waxbill.Utils;

namespace waxbill
{
    public class TCPServer<TSession>:MonitorBase where TSession:ServerSession,new() 
    {

        public string LocalIP { get; private set; }
        public Int32 LocalPort { get; private set; }
        internal TCPListener Listener { get; private set; }

        private ConcurrentDictionary<Int64, TSession> mSessions = new ConcurrentDictionary<Int64, TSession>();//在线会话
        private Timer mRecycleTimer = null;//异常会话回收定时器
        private Int32 IsRunning = 0;
        private SendingQueuePool mSendPool;//发送池



        public TCPServer(IProtocol protocol):this(protocol,TCPOption.Define)
        {}

        public TCPServer(IProtocol protocol,TCPOption option)
            :base(protocol, option, new BufferManager(option.BufferSize, option.BufferIncemerCount))
        {
            this.mSendPool = new SendingQueuePool();
            this.Listener = new TCPListener();
            this.Listener.OnNewConnected += OnNewConnected;
        }
        
        /// <summary>
        /// 开启服务
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
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

        /// <summary>
        /// 终止服务
        /// </summary>
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
        
        /// <summary>
        /// 新连接回调
        /// </summary>
        /// <param name="connectionID"></param>
        /// <param name="connection"></param>
        private void OnNewConnected(Int64 connectionID,UVTCPHandle connection)
        {
            TSession session = new TSession();
            session.Init(connectionID,connection,this);
            if (this.mSessions.TryAdd(session.ConnectionID, session))
            {
                session.InnerTellConnected();
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
            TSession session;
            if (mSessions.TryGetValue(sessionID, out session))
            {
                return session;
            }
            return null;
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
        

        protected internal override bool TryGetSendQueue(out UVWriteRequest reqeust)
        {
            return this.mSendPool.TryGet(out reqeust);
        }

        protected internal override void ReleaseSendQueue(UVWriteRequest request)
        {
            this.mSendPool.Release(request);
        }


        /// <summary>
        /// 获取接收缓存
        /// </summary>
        /// <returns></returns>
        protected internal override bool TryGetReceiveBuffer(out IntPtr memory)
        {
            memory = IntPtr.Zero;
            try
            {
                memory = Marshal.AllocHGlobal(this.Option.ReceiveBufferSize);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 释放缓存
        /// </summary>
        /// <param name="memory"></param>
        protected internal override void ReleaseReceiveMemory(IntPtr memory)
        {
            Marshal.FreeHGlobal(memory);
        }
    }
}
