using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using waxbill.Client;
using waxbill.Exceptions;
using waxbill.Packets;
using waxbill.Pools;
using waxbill.Protocols;
using waxbill.Sessions;
using waxbill.Utils;

namespace waxbill
{
    

    public class TCPClient:SocketMonitor
    {
        private ConcurrentState _state=new ConcurrentState ();
        private ClientInnerSession session;//会话
        private Socket socket;//tcp连接
        private Int32 isConnected = 0;

        
        private const Int32 STATE_STARTED = 0x02;
        private const Int32 STATE_CLOSEING = 0x80;



        /// <summary>
        /// 连接
        /// </summary>
        public Action<TCPClient, Session> OnConnected { get; set; }

        /// <summary>
        /// 断开连接
        /// </summary>
        public Action<TCPClient, Session, CloseReason> OnDisconnected { get; set; }

        /// <summary>
        /// 发送
        /// </summary>
        public Action<TCPClient, Session, SendingQueue, Boolean> OnSended { get; set; }

        /// <summary>
        /// 接收
        /// </summary>
        public Action<TCPClient, Session, Packet> OnReceived { get; set; }




        public TCPClient(IProtocol protocol):base(protocol,TCPOptions.CLIENT_DEFAULT,ClientPoolProvider.Instance)
        {
            
        }
        
        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Connect(string ip, Int32 port)
        {
            Preconditions.ThrowIfZeroOrMinus(port,"port");
            IPAddress address;
            if (!IPAddress.TryParse(ip, out address))
            {
                throw new ArgumentOutOfRangeException("ip");
            }

            if (!_state.SetState(STATE_STARTED))
            {
                return;
            }

            try
            {
                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.socket.Connect(new IPEndPoint(address, port));
                this.session = new ClientInnerSession(RaiseConnected, RaiseDisconnected, RaiseSended, RaiseReceived);
                this.session.Initialize(this.socket, this);
                this.session.Start();
            }
            catch (Exception ex)
            {
                FreeResource();
                throw ex;
            }

        }


        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (!_state.GetState(STATE_STARTED))
            {
                return;
            }
            if (_state.SetState(STATE_CLOSEING))
            {
                this.session.Close(CloseReason.Default);
            }
        }


        private void FreeResource()
        {
            this.socket.Shutdown(SocketShutdown.Both);
            this.socket = null;
            this.session = null;
            _state.RemoveState(STATE_STARTED);
            _state.RemoveState(STATE_CLOSEING);
        }



        private void RaiseConnected(Session session)
        {
            if (this.OnConnected != null)
            {
                this.OnConnected(this,session);
            }
        }

        private void RaiseDisconnected(Session session,CloseReason reason)
        {
            FreeResource();
            if (this.OnDisconnected != null)
            {
                this.OnDisconnected(this,session,reason);
            }
        }

        private void RaiseReceived(Session session,Packet packet)
        {
            if (this.OnReceived != null)
            {
                this.OnReceived(this,session,packet);
            }
        }

        private void RaiseSended(Session session,SendingQueue queue, bool result)
        {
            if (this.OnSended != null)
            {
                this.OnSended(this,session,queue,result);
            }
        }
       
        #region Sends
        /// <summary>
        /// 加入到发送列表中
        /// </summary>
        /// <param name="datas"></param>
        public void Send(byte[] datas)
        {
            this.session.Send(datas);
        }

        /// <summary>
        /// 加入到发送列表中
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public void Send(byte[] datas, int offset, int size)
        {
            this.session.Send(datas, offset, size);
        }

        public void Send(ArraySegment<byte> data)
        {
            this.session.Send(data);
        }

        public void Send(IList<ArraySegment<byte>> datas)
        {
            this.session.Send(datas);
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
            return SendAndReceive(ip, port, datas, 0, datas.Length, TCPOptions.CLIENT_DEFAULT.SendTimeout);
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
            return SendAndReceive(ip, port, datas, offset, size, TCPOptions.CLIENT_DEFAULT.SendTimeout);
        }

        /// <summary>
        /// 同步发送
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="data"></param>
        public static byte[] SendAndReceive(string ip, Int32 port, ArraySegment<byte> data)
        {
            return SendAndReceive(ip, port, data.Array, data.Offset, data.Count, TCPOptions.CLIENT_DEFAULT.SendTimeout);
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
            Preconditions.ThrowIfNullOrWhite(ip, "ip");
            Preconditions.ThrowIfZeroOrMinus(port, "port");
            Preconditions.ThrowIfNull(datas, "datas");

            if (offset + count > datas.Length)
            {
                throw new ArgumentOutOfRangeException("offset+size>datas.length");
            }

            if (count <= 0)
            {
                return new byte[0];
            }

            byte[] retDatas = null;
            CloseReason closeReason = CloseReason.Default;
            TCPClient client = new TCPClient(RealtimeProtocol.Define);
            ManualResetEvent mre = new ManualResetEvent(false);
            
            client.OnConnected += (tcp,session) =>
            {
                session.Send(datas, offset, count);
            };

            client.OnDisconnected += (tcp,session, reason) =>
            {
                closeReason = reason;
                mre.Set();
            };
            client.OnReceived += (tcp,session, packet) =>
            {
                retDatas = packet.Read();
                client.OnDisconnected = null;
                mre.Set();
            };

            client.Connect(ip, port);
            if (!mre.WaitOne(millisecondsTimeout))
            {
                throw new TimeoutException("send or receive timeout");
            }

            mre.Close();
            client.Disconnect();
            if (closeReason ==CloseReason.Exception)
            {
                throw new Exception("不正常的关闭");
            }


            if (retDatas == null)
            {
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
            SendAndReceive(ip, port, datas, 0, datas.Length, TCPOptions.CLIENT_DEFAULT.SendTimeout);
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
            SendAndReceive(ip, port, datas, offset, size, TCPOptions.CLIENT_DEFAULT.SendTimeout);
        }

        /// <summary>
        /// 同步发送
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="data"></param>
        public static void SendOnly(string ip, Int32 port, ArraySegment<byte> data)
        {
            SendAndReceive(ip, port, data.Array, data.Offset, data.Count, TCPOptions.CLIENT_DEFAULT.SendTimeout);
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
            Preconditions.ThrowIfNullOrWhite(ip, "ip");
            Preconditions.ThrowIfZeroOrMinus(port, "port");
            Preconditions.ThrowIfNull(datas, "datas");

            if (offset + count > datas.Length)
            {
                throw new ArgumentOutOfRangeException("offset+size>datas.length");
            }

            if (count <= 0)
            {
                return;
            }

            CloseReason closeReason = CloseReason.Default;
            TCPClient client = new TCPClient(RealtimeProtocol.Define);
            ManualResetEvent mre = new ManualResetEvent(false);
            client.OnConnected += (tcp,session) =>
            {
                session.Send(datas, offset, count);
            };
            client.OnDisconnected += (tcp,session, reason) =>
            {
                closeReason = reason;
                mre.Set();
            };

            client.OnSended += (tcp,session, queue, result) => {
                mre.Set();
            };

            client.Connect(ip, port);
            if (!mre.WaitOne(millisecondsTimeout))
            {
                throw new TimeoutException("send or receive timeout");
            }

            mre.Close();
            client.Disconnect();
            if (closeReason ==CloseReason.Exception)
            {
                //连不上
                throw new Exception("连接失败") ;
            }
        }
        #endregion
        
        #region static method
        private static byte[] combinDatas(IList<ArraySegment<byte>> datas)
        {
            return new byte[0];
        }

        public static void Send(string ip, int port, byte[] datas)
        {
            Send(ip, port, datas, 0, datas.Length);
        }

        public static void Send(string ip, int port, ArraySegment<byte> data)
        {
            Send(ip, port, data.Array, data.Offset, data.Count);
        }

        public static void Send(string ip, int port, IList<ArraySegment<byte>> datas)
        {
            byte[] buffer = combinDatas(datas);
            Send(ip, port, buffer, 0, buffer.Length);
        }

        public static void Send(string ip, int port, byte[] datas, Action<Socket> receiveAction)
        {
            Send(ip, port, datas, 0, datas.Length, receiveAction);
        }

        public static void Send(string ip, int port, ArraySegment<byte> data, Action<Socket> receiveAction)
        {
            Send(ip, port, data.Array, data.Offset, data.Count, receiveAction);
        }

        public static void Send(string ip, int port, IList<ArraySegment<byte>> datas, Action<Socket> receiveAction)
        {
            byte[] buffer = combinDatas(datas);
            Send(ip, port, buffer, 0, buffer.Length, receiveAction);
        }

        public static void Send(string ip, int port, byte[] datas, int offset, int size)
        {
            Send(ip, port, datas, offset, size, (socket)=>{ });
        }

        public static void Send(string ip, int port, byte[] datas, int offset, int size, Action<Socket> receiveAction)
        {
            if (((offset < 0) || (size < 0)) || (datas.Length < (offset + size)))
            {
                throw new ArgumentOutOfRangeException("发送数据大小不合理");
            }
            IPAddress address = null;
            if (!IPAddress.TryParse(ip, out address))
            {
                throw new ArgumentOutOfRangeException("ip");
            }
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(new IPEndPoint(address, port));
                socket.Send(datas, offset, size, SocketFlags.None);
            }
            catch (Exception exception)
            {
                Trace.Error("发送数据时出错", exception);
                throw new NoNetException();
            }
            if (receiveAction != null)
            {
                try
                {
                    socket.ReceiveTimeout = 0x1388;
                    receiveAction(socket);
                }
                catch (Exception exception2)
                {
                    Trace.Error("调用接收函数出错", exception2);
                    throw new Exception("");
                }
            }
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch
            {
            }
        }
        #endregion
    }
}

