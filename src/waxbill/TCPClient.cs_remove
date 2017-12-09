using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Libuv;
using waxbill.Utils;

namespace waxbill
{
    public class TCPClient:TCPMonitor
    {
        private MClientSession mSession;
        private UVLoopHandle mLoopHandle;
        private UVTCPHandle mServerHandle;
        private UVConnect mConnect;
        private bool mIsDispose = false;
        private Int32 mIsConnected = 0;

        public MClientSession Session
        {
            get
            {
                return this.mSession;
            }
        }
        
        public TCPClient(IProtocol protocol):base(protocol,ServerOption.Define)
        {
            Init();
        }

        public TCPClient(IProtocol protocol,ServerOption option):base(protocol,option)
        {
            Init();
        }

        private void Init()
        {
            this.mLoopHandle = new UVLoopHandle();
            this.mServerHandle = new UVTCPHandle(this.mLoopHandle);
            this.mConnect = new UVConnect();
        }

        public void Connection(string ip, Int32 port)
        {
            if (mIsDispose)
            {
                throw new Exception("已经释放的连接");
            }
            if (Interlocked.CompareExchange(ref this.mIsConnected, 1, 0) == 0)
            {
                this.mConnect.Connect(this.mServerHandle, ip, port, this.ConnectionCallback, null);
                Thread thread = new Thread(ConnectionThread);
                thread.IsBackground = true;
                thread.Start();
            }   
        }

        private void ConnectionThread(object state)
        {
            this.mLoopHandle.Start();
        }

        private void ConnectionCallback(UVConnect connection, Int32 retStatus, UVException exception, object state)
        {
            if (exception != null)
            {
                this.mIsConnected = 0;
                return;
            }
            this.mSession = new MClientSession(this);
            this.mSession.Init(0, this.mServerHandle, this, this.Option);
            this.mSession.RaiseAccept();
            this.mServerHandle.ReadStart(mSession.AllocMemoryCallback, mSession.ReadCallback, mSession, mSession);
        }

        public void Disconnect()
        {
            if (Interlocked.CompareExchange(ref this.mIsConnected, 0, 1) == 1)
            {
                this.mSession.Close(CloseReason.Shutdown, null);
                this.mConnect.Close();
                this.mServerHandle.Close();
                this.mLoopHandle.Stop();
                this.mLoopHandle.Stop();
                this.mLoopHandle.Close();
                mIsDispose = true;
            }
        }
        
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


        public class MClientSession : SocketSession
        {
            private TCPClient mClient;
            public MClientSession(TCPClient client)
            {
                this.mClient = client;
            }
            protected override void ConnectedCallback()
            {}

            protected override void DisconnectedCallback(CloseReason reason)
            {
                if (mClient != null)
                {
                    mClient.Disconnect();
                }
            }

            protected override void ReceiveCallback(Packet packet)
            {}

            protected override void SendedCallback(IList<UVIntrop.uv_buf_t> packet, bool result)
            {}
        }

        #region static
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

    }
}
