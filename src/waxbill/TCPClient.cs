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
using ZTImage.Log;
using waxbill.Protocols;
namespace waxbill
{
    public class TCPClient:MonitorBase
    {
        private ClientSession mSession;
        private UVTCPHandle mTCPHandle;
        private UVConnectRquest mConnector;
        private bool mIsDispose = false;
        private Int32 mIsConnected = 0;
        private static SendingPool mSendPool;
        private static BufferManager mBufferManager;
        private readonly static TCPOption mOption;
        private static Int32 mConnectID = 0;
        
        static TCPClient()
        {
            mOption = TCPOption.Define;
            mBufferManager = new BufferManager(mOption.BufferSize, mOption.BufferIncemerCount);
            mSendPool = new SendingPool();
        }

        public TCPClient() : this(RealtimeProtocol.Define)
        { }

        public TCPClient(IProtocol protocol):base(protocol, mOption,mBufferManager)
        {
            this.mTCPHandle = new UVTCPHandle(UVLoopHandle.Define);
            this.mConnector = new UVConnectRquest();
        }
        
        public void Connection(string ip, Int32 port)
        {
            if (mIsDispose)
            {
                throw new Exception("已经释放的连接");
            }
            if (Interlocked.CompareExchange(ref this.mIsConnected, 1, 0) == 0)
            {
                this.mConnector.Connect(this.mTCPHandle, ip, port, this.ConnectionCallback, null);
                UVLoopHandle.Define.AsyncStart((loop)=> {
                    Trace.Info("loop return");
                });
            }   
        }

        private void ConnectionCallback(UVConnectRquest connection, Int32 retStatus, UVException exception, object state)
        {
            if (exception != null)
            {
                this.mIsConnected = 0;
                return;
            }

            Int32 cid=Interlocked.Increment(ref mConnectID);
            if (cid >=Int32.MaxValue - 1)
            {
                Volatile.Write(ref mConnectID, 0);
            }
            this.mSession = new ClientSession(this);
            this.mSession.Init(cid, this.mTCPHandle, this);
            this.mSession.RaiseOnConnected();
        }

        public void Disconnect()
        {
            if (Interlocked.CompareExchange(ref this.mIsConnected, 0, 1) == 1)
            {
                this.mSession.Close(CloseReason.Shutdown, null);
                this.mConnector.Close();
                this.mTCPHandle.Close();
                mIsDispose = true;
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
                OnConnection(session);
            }
        }

        /// <summary>
        /// 断开连接事件
        /// </summary>
        public event OnDisconnectedEvent OnDisconnected;


        internal void RaiseOnDisconnectedEvent(SessionBase session, CloseReason reason)
        {
            if (this.OnDisconnected != null)
            {
                OnDisconnected(session, reason);
            }
        }

        /// <summary>
        /// 发送事件
        /// </summary>
        public event OnSendedEvent OnSended;
        internal void RaiseOnSendedEvent(SessionBase session, IList<UVIntrop.uv_buf_t> packet, bool result)
        {
            if (this.OnSended != null)
            {
                OnSended(session, packet, result);
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
                OnReceive(session, collection);
            }
        }

        #endregion

        #region Statics
        public static void Send(string ip, Int32 port, byte[] datas)
        {
            Send(ip, port, datas, 0, datas.Length, (socket) => { });
        }

        public static void Send(string ip, Int32 port, byte[] datas, int offset, int size)
        {
            Send(ip, port, datas, offset, size, (socket) => { });
        }

        public static void Send(string ip, Int32 port, ArraySegment<byte> data)
        {
            Send(ip, port, data.Array, data.Offset, data.Count, (socket) => { });
        }

        public static void Send(string ip, Int32 port, IList<ArraySegment<byte>> datas)
        {
            byte[] data = combinDatas(datas);
            Send(ip, port, data, 0, data.Length, (socket) => { });
        }

        public static void Send(string ip, Int32 port, byte[] datas, Action<Socket> receiveAction)
        {
            Send(ip, port, datas, 0, datas.Length, receiveAction);
        }

        public static void Send(string ip, Int32 port, byte[] datas, int offset, int size, Action<Socket> receiveAction)
        {
            if (offset < 0 || size < 0 || datas.Length < (offset + size))
            {
                throw new ArgumentOutOfRangeException("发送数据大小不合理");
            }
            IPAddress address = default(IPAddress);
            if (!IPAddress.TryParse(ip, out address))
            {
                throw new Exception("ip格式不正确");
            }

            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                client.Connect(new IPEndPoint(address, port));
                client.Send(datas, offset, size, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Trace.Error("发送数据时出错", ex);
                throw;
            }

            if (receiveAction != null)
            {
                try
                {
                    client.ReceiveTimeout = 5000;
                    receiveAction(client);
                }
                catch (Exception ex)
                {
                    Trace.Error("调用接收函数出错", ex);
                    throw;
                }

            }

            try
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch
            { }
        }

        public static void Send(string ip, Int32 port, ArraySegment<byte> data, Action<Socket> receiveAction)
        {
            Send(ip, port, data.Array, data.Offset, data.Count, receiveAction);
        }

        public static void Send(string ip, Int32 port, IList<ArraySegment<byte>> datas, Action<Socket> receiveAction)
        {
            byte[] data = combinDatas(datas);
            Send(ip, port, data, 0, data.Length, receiveAction);
        }

        private static byte[] combinDatas(IList<ArraySegment<byte>> datas)
        {
            return new byte[0];
        }


        #endregion

        public override bool TryGetSendQueue(out UVWriteRequest request)
        {
            return mSendPool.TryGet(out request);
        }

        public override void ReleaseSendQueue(UVWriteRequest request)
        {
            request.Reset();
            mSendPool.Release(request);
        }

        /// <summary>
        /// 获取接收缓存
        /// </summary>
        /// <returns></returns>
        public override bool TryGetReceiveMemory(out IntPtr memory)
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
        public override void ReleaseReceiveMemory(IntPtr memory)
        {
            Marshal.FreeHGlobal(memory);
        }
        
    }
}
