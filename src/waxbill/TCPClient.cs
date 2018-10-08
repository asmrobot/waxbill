using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Libuv;
using waxbill.Packets;
using waxbill.Sessions;
using waxbill.Utils;
using waxbill.Protocols;
using waxbill.Pools;

namespace waxbill
{
    public class TCPClient:MonitorBase
    {
        private const Int32 DEFAULT_MILLISECONDS_TIMEOUT = 15000;
        private static SendingQueuePool mSendPool;
        private static BufferManager mBufferManager;
        private readonly static TCPOption mOption;
        private static Int32 mConnectID = 0;
        
        
        static TCPClient()
        {
            mOption = TCPOption.Define;
            mBufferManager = new BufferManager(mOption.BufferSize, mOption.BufferIncemerCount);
            mSendPool = new SendingQueuePool();
        }
        
        private ClientSession mSession;//会话
        private UVTCPHandle mTCPHandle;//tcp连接
        private Int32 mIsConnected = 0;

        public TCPClient() : this(RealtimeProtocol.Define)
        { }

        public TCPClient(IProtocol protocol):base(protocol, mOption,mBufferManager)
        {}
        
        public void Connection(string ip, Int32 port)
        {
            if (Interlocked.CompareExchange(ref this.mIsConnected, 1, 0) == 0)
            {
                this.mTCPHandle = new UVTCPHandle(UVLoopHandle.Define);
                this.mTCPHandle.Connect(ip, port, this.ConnectionCallback, null);
                UVLoopHandle.Define.AsyncStart((loop)=> {
                    Trace.Info("loop return");
                });
            }
            else
            {
                throw new Exception("重复连接");
            }
        }

        private void ConnectionCallback(UVException exception, object state)
        {
            if (exception != null)
            {
                Disconnect(exception);
                return;
            }

            Int32 cid=Interlocked.Increment(ref mConnectID);
            if (cid >=Int32.MaxValue - 1)
            {
                Volatile.Write(ref mConnectID, 0);
            }
            this.mSession = new ClientSession(this);
            this.mSession.Init(cid, this.mTCPHandle, this);
            this.mSession.InnerTellConnected();
        }

        public void Disconnect(Exception exception=null)
        {
            if (Interlocked.CompareExchange(ref this.mIsConnected, 0, 1) == 1)
            {
                if (this.mSession != null)
                {
                    this.mSession.Close(CloseReason.Shutdown, exception);
                }
                else
                {
                    RaiseOnDisconnectedEvent(null, exception);
                }
            }
        }

        #region Sends
        /// <summary>
        /// 加入到发送列表中
        /// </summary>
        /// <param name="datas"></param>
        public void Send(byte[] datas)
        {
            this.mSession.Send(datas);
        }

        /// <summary>
        /// 加入到发送列表中
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public void Send(byte[] datas, int offset, int size)
        {
            this.mSession.Send(datas, offset, size);
        }

        public void Send(ArraySegment<byte> data)
        {
            this.mSession.Send(data);
        }

        public void Send(IList<ArraySegment<byte>> datas)
        {
            this.mSession.Send(datas);
        }


        #endregion

        #region Events
        /// <summary>
        /// 连接事件
        /// </summary>
        public event OnConnectionEvent OnConnection;
        internal void RaiseOnConnectionEvent(SessionBase session)
        {
            if (OnConnection != null)
            {
                OnConnection(this,session);
            }
        }

        /// <summary>
        /// 断开连接事件
        /// </summary>
        public event OnDisconnectedEvent OnDisconnected;


        internal void RaiseOnDisconnectedEvent(SessionBase session, Exception exception)
        {
            if (this.OnDisconnected != null)
            {
                OnDisconnected(this,session, exception);
            }
        }

        /// <summary>
        /// 发送事件
        /// </summary>
        public event OnSendedEvent OnSended;
        internal void RaiseOnSendedEvent(SessionBase session, PlatformBuf packet, bool result)
        {
            if (this.OnSended != null)
            {
                OnSended(this,session, packet, result);
            }
        }
        
        /// <summary>
        /// 接收事件
        /// </summary>
        public event OnReceiveEvent OnReceive;
        internal void RaiseOnReceiveEvent(SessionBase session, Packet collection)
        {
            if (OnReceive != null)
            {
                OnReceive(this,session, collection);
            }
        }

        #endregion

        #region Syncs
        /// <summary>
        /// 同步发送接收
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="datas"></param>
        public static byte[] SendAndReceive(string ip, Int32 port, byte[] datas)
        {
            return SendAndReceive(ip, port, datas, 0, datas.Length, DEFAULT_MILLISECONDS_TIMEOUT);
        }

        /// <summary>
        /// 同步发送
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="datas"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public static byte[] SendAndReceive(string ip, Int32 port, byte[] datas, int offset, int size)
        {
            return SendAndReceive(ip, port, datas, offset, size, DEFAULT_MILLISECONDS_TIMEOUT);
        }

        /// <summary>
        /// 同步发送
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="data"></param>
        public static byte[] SendAndReceive(string ip, Int32 port, ArraySegment<byte> data)
        {
            return SendAndReceive(ip, port, data.Array, data.Offset, data.Count, DEFAULT_MILLISECONDS_TIMEOUT);
        }
        
        /// <summary>
        /// 同步发送与接收
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="datas"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="millisecondsTimeout"></param>
        /// <returns></returns>
        public static byte[] SendAndReceive(string ip, Int32 port, byte[] datas, int offset, int count, Int32 millisecondsTimeout)
        {
            Validate.ThrowIfNullOrWhite(ip, "ip");
            Validate.ThrowIfZeroOrMinus(port, "port");
            Validate.ThrowIfNull(datas, "datas");

            if (offset + count > datas.Length)
            {
                throw new ArgumentOutOfRangeException("offset+size>datas.length");
            }
            
            if (count <= 0)
            {
                return new byte[0];
            }

            byte[] retDatas=null;
            Exception exception = null;
            TCPClient client = new TCPClient();
            ManualResetEvent mre = new ManualResetEvent(false);
            client.OnConnection += (c, session) =>
            {
                session.Send(datas, offset, count);
            };
            client.OnDisconnected += (c, session, ex) =>
            {
                exception = ex;
                mre.Set();
            };
            client.OnReceive += (c, s, p) =>
            {
                retDatas = p.Read();
                client.OnDisconnected = null;
                mre.Set();
            };

            client.Connection(ip, port);
            if (!mre.WaitOne(millisecondsTimeout))
            {
                throw new TimeoutException("send or receive timeout");
            }

            mre.Close();
            client.Disconnect();
            if (exception != null)
            {
                //连不上
                throw exception;
            }

            
            if (retDatas == null)
            {
                //连接关闭
                throw new Exception("连接关闭");
            }
            
            return retDatas;
        }

        /// <summary>
        /// 同步发送接收
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="datas"></param>
        public static void SendOnly(string ip, Int32 port, byte[] datas)
        {
            SendAndReceive(ip, port, datas, 0, datas.Length, DEFAULT_MILLISECONDS_TIMEOUT);
        }

        /// <summary>
        /// 同步发送
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="datas"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public static void SendOnly(string ip, Int32 port, byte[] datas, int offset, int size)
        {
            SendAndReceive(ip, port, datas, offset, size, DEFAULT_MILLISECONDS_TIMEOUT);
        }

        /// <summary>
        /// 同步发送
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="data"></param>
        public static void SendOnly(string ip, Int32 port, ArraySegment<byte> data)
        {
            SendAndReceive(ip, port, data.Array, data.Offset, data.Count, DEFAULT_MILLISECONDS_TIMEOUT);
        }

        /// <summary>
        /// 同步发送与接收
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="datas"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="millisecondsTimeout"></param>
        /// <returns></returns>
        public static void SendOnly(string ip, Int32 port, byte[] datas, int offset, int count, Int32 millisecondsTimeout)
        {
            Validate.ThrowIfNullOrWhite(ip, "ip");
            Validate.ThrowIfZeroOrMinus(port, "port");
            Validate.ThrowIfNull(datas, "datas");

            if (offset + count > datas.Length)
            {
                throw new ArgumentOutOfRangeException("offset+size>datas.length");
            }

            if (count <= 0)
            {
                return;
            }

            byte[] retDatas = null;
            Exception exception = null;
            TCPClient client = new TCPClient();
            ManualResetEvent mre = new ManualResetEvent(false);
            client.OnConnection += (c, session) =>
            {
                session.Send(datas, offset, count);
            };
            client.OnDisconnected += (c, session, ex) =>
            {
                exception = ex;
                mre.Set();
            };
            
            client.OnSended += (cleint, session, send_datas, result) => {
                mre.Set();
            };

            client.Connection(ip, port);
            if (!mre.WaitOne(millisecondsTimeout))
            {
                throw new TimeoutException("send or receive timeout");
            }

            mre.Close();
            client.Disconnect();
            if (exception != null)
            {
                //连不上
                throw exception;
            }
        }
        #endregion

        #region Override
        /// <summary>
        /// 获取发送队列
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected internal override bool TryGetSendQueue(out UVWriteRequest request)
        {
            return mSendPool.TryGet(out request);
        }

        /// <summary>
        /// 回收发送队列
        /// </summary>
        /// <param name="request"></param>
        protected internal override void ReleaseSendQueue(UVWriteRequest request)
        {
            
            mSendPool.Release(request);
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

        #endregion
    }
}
