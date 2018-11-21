using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using waxbill.Exceptions;
using waxbill.Packets;
using waxbill.Pools;
using waxbill.Utils;


namespace waxbill.Sessions
{
    

    public abstract class SessionBase
    {
        protected SocketMonitor Monitor;

        private Socket connector;
        private Packet packet;
        private SocketAsyncEventArgs receiveSAE;
        private SendingQueue sendingQueue;
        private SocketAsyncEventArgs sendSAE;
        private ConcurrentState state;//状态

        private const Int32 STATE_CLOSED = 0x80;
        private const Int32 STATE_CLOSING = 0x40;
        private const Int32 STATE_RECEIVEING = 0x02;
        private const Int32 STATE_SENDING = 0x01;


        /// <summary>
        /// 连接ID
        /// </summary>
        public long ConnectionID { get; private set; }

        /// <summary>
        /// 就否关闭
        /// </summary>
        public bool IsClosed
        {
            get
            {
                return (this.state >= STATE_CLOSED);
            }
        }

        /// <summary>
        /// 是否关闭或正在关闭
        /// </summary>
        public bool IsClosingOrClosed
        {
            get
            {
                return (this.state >= STATE_CLOSING);
            }
        }

        /// <summary>
        /// 远程结点
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                try
                {
                    return (this.connector.RemoteEndPoint as IPEndPoint);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="client"></param>
        /// <param name="monitor"></param>
        internal void Initialize(Socket client, SocketMonitor monitor)
        {
            Preconditions.ThrowIfNull(client, "client");
            Preconditions.ThrowIfNull(monitor, "monitor");

            this.Monitor = monitor;
            this.connector = client;
            this.ConnectionID = monitor.GetNextConnectionID();

            //set send SAE
            if (!this.Monitor.SocketEventArgsPool.TryGet(out this.sendSAE))
            {
                throw new Exception("无法获取接收SocketAsyncEventArgs对象");
            }
            this.sendSAE.Completed += new EventHandler<SocketAsyncEventArgs>(this.SAE_SendCompleted);

            //set receive SAE
            if (!this.Monitor.SocketEventArgsPool.TryGet(out this.receiveSAE))
            {
                throw new Exception("无法获取接收SocketAsyncEventArgs对象");
            }
            ArraySegment<byte> receiveBuffer;
            if (!this.Monitor.ReceiveBufferPool.TryGet(out receiveBuffer))
            {
                throw new Exception("无法获取发送Buffer");
            }
            this.receiveSAE.SetBuffer(receiveBuffer.Array, receiveBuffer.Offset, receiveBuffer.Count);
            this.receiveSAE.Completed += new EventHandler<SocketAsyncEventArgs>(this.SAE_ReceiveCompleted);

            this.packet = this.Monitor.Protocol.CreatePacket(this.Monitor.PacketBufferPool);
        }
        

        /// <summary>
        /// 开始
        /// </summary>
        public void Start()
        {
            this.RaiseConnected();
            if (!this.Monitor.SendingPool.TryGet(out this.sendingQueue))
            {
                Trace.Error("无法获取可用的发送池");
            }
            else
            {
                this.sendingQueue.StartQueue();
                if (this.state.TrySetState(STATE_RECEIVEING))
                {
                    this.InternalReceive();
                }
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="reason"></param>
        public void Close(CloseReason reason)
        {
            if (this.IsClosingOrClosed)
            {
                SpinWait wait = new SpinWait();
                while (true)
                {
                    if (!this.state.GetState(STATE_SENDING))
                    {
                        break;
                    }
                    wait.SpinOnce();
                }
                this.RaiseDisconnected(reason);
                try
                {
                    this.connector.Shutdown(SocketShutdown.Both);
                    this.connector.Close();
                    this.connector = null;
                }
                catch (Exception exception)
                {
                    Trace.Error("关闭连接失败", exception);
                }
                this.FreeResource(reason);
            }
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="reason"></param>
        private void FreeResource(CloseReason reason)
        {
            if (this.packet != null)
            {
                this.packet.Reset();
            }
            if (this.sendingQueue != SendingQueue.Null)
            {
                if (this.sendingQueue.Count > 0)
                {
                    this.RaiseSended(this.sendingQueue, false);
                }
                this.sendingQueue.Clear();
                this.Monitor.SendingPool.Release(this.sendingQueue);
            }
            this.sendSAE.Completed -= new EventHandler<SocketAsyncEventArgs>(this.SAE_SendCompleted);
            this.sendSAE.UserToken = null;
            this.sendSAE.SetBuffer(null, 0, 0);
            this.Monitor.SocketEventArgsPool.Release(this.sendSAE);
            this.sendSAE = null;




            this.receiveSAE.Completed -= new EventHandler<SocketAsyncEventArgs>(this.SAE_ReceiveCompleted);
            this.receiveSAE.UserToken = null;
            this.Monitor.ReceiveBufferPool.Release(new ArraySegment<byte>(this.receiveSAE.Buffer, this.receiveSAE.Offset, this.receiveSAE.Count));
            this.receiveSAE.SetBuffer(null, 0, 0);
            this.Monitor.SocketEventArgsPool.Release(this.receiveSAE);
            this.receiveSAE = null;


            this.connector = null;
            this.state.SetState(STATE_CLOSED);
        }
        
        
        #region send
        private bool InternalSend(SendingQueue queue)
        {
            if (this.IsClosingOrClosed)
            {
                this.SendEnd(queue, CloseReason.Default,null);
                return false;
            }
            bool flag = true;
            try
            {
                if (queue.Count > 1)
                {
                    this.sendSAE.BufferList = queue;
                }
                else
                {
                    ArraySegment<byte> segment = queue[0];
                    this.sendSAE.SetBuffer(segment.Array, segment.Offset, segment.Count);
                }
                this.sendSAE.UserToken = queue;
                flag = this.connector.SendAsync(this.sendSAE);
            }
            catch (Exception exception)
            {
                this.SendEnd(queue, CloseReason.Exception,exception);
                return false;
            }
            if (!flag)
            {
                this.SAE_SendCompleted(this, this.sendSAE);
            }
            return true;
        }

        private bool PreSend()
        {
            SendingQueue queue2;
            SendingQueue mSendingQueue = this.sendingQueue;
            if (mSendingQueue.Count <= 0)
            {
                return true;
            }
            if (!this.state.TrySetState(STATE_SENDING))
            {
                return true;
            }
            if (!this.Monitor.SendingPool.TryGet(out queue2))
            {
                this.SendEnd(SendingQueue.Null, CloseReason.Exception,new waxbill.Exceptions.WaxbillException("没有分配到发送queue"));
                return false;
            }
            queue2.StartQueue();
            this.sendingQueue = queue2;
            mSendingQueue.StopQueue();
            return this.InternalSend(mSendingQueue);
        }

        private void SAE_SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            SendingQueue sendQueue = e.UserToken as SendingQueue;

            if (sendQueue == SendingQueue.Null)
            {
                Trace.Error("未知错误help!~");
            }
            else if (e.SocketError != SocketError.Success)
            {
                this.RaiseSended(sendQueue, false);
                this.SendEnd(sendQueue, CloseReason.Exception, new Exception("send complete error"));
            }
            else if (sendQueue.Count <= e.BytesTransferred)
            {

                e.SetBuffer(null, 0, 0);
                e.BufferList = null;
                this.RaiseSended(sendQueue, true);
                sendQueue.Clear();
                this.Monitor.SendingPool.Release(sendQueue);
                this.state.RemoveState(STATE_SENDING);
                this.PreSend();
            }
            else
            {
                sendQueue.TrimByte(e.BytesTransferred);
                this.InternalSend(sendQueue);
            }
        }

        public void Send(byte[] datas)
        {
            this.Send(new ArraySegment<byte>(datas, 0, datas.Length));
        }

        public void Send(ArraySegment<byte> data)
        {
            bool reTry = false;
            if (this.TrySend(data, out reTry))
            {
                return;
            }
            if (!reTry)
            {
                return;
            }
            SpinWait wait = new SpinWait();
            DateTime time = DateTime.Now.AddMilliseconds((double)this.Monitor.Option.SendTimeout);
            while (true)
            {
                wait.SpinOnce();
                if (this.TrySend(data, out reTry) || (DateTime.Now >= time))
                {
                    return;
                }
                if (!reTry)
                {
                }
            }
        }

        public void Send(IList<ArraySegment<byte>> datas)
        {
            if (datas.Count > this.sendingQueue.Capacity)
            {
                throw new ArgumentOutOfRangeException("发送内容大于缓存池");
            }
            bool reTry = false;
            if (this.TrySend(datas, out reTry))
            {
                return;
            }
            if (!reTry)
            {
                return;
            }
            SpinWait wait = new SpinWait();
            DateTime time = DateTime.Now.AddMilliseconds((double)this.Monitor.Option.SendTimeout);
            while (true)
            {
                wait.SpinOnce();
                if (this.TrySend(datas, out reTry) || (DateTime.Now >= time))
                {
                    return;
                }
                if (!reTry)
                {
                }
            }
        }

        public void Send(byte[] datas, int offset, int size)
        {
            this.Send(new ArraySegment<byte>(datas, offset, size));
        }
        
        private void SendEnd(SendingQueue queue, CloseReason reason, Exception exception)
        {
            if (queue != null)
            {
                queue.Clear();
                this.Monitor.SendingPool.Release(queue);
            }
            this.state.RemoveState(STATE_SENDING);
            this.Close(reason);
        }
        
        private bool TrySend(ArraySegment<byte> data, out bool reTry)
        {
            reTry = false;
            SendingQueue mSendingQueue = this.sendingQueue;
            if (mSendingQueue == null)
            {
                return false;
            }
            if (!mSendingQueue.Enqueue(data))
            {
                reTry = true;
                return false;
            }
            return this.PreSend();
        }

        private bool TrySend(IList<ArraySegment<byte>> datas, out bool reTry)
        {
            reTry = false;
            SendingQueue mSendingQueue = this.sendingQueue;
            if (mSendingQueue == null)
            {
                return false;
            }
            if (!mSendingQueue.Enqueue(datas))
            {
                reTry = true;
                return false;
            }
            return this.PreSend();
        }
        #endregion
        

        #region receive
        private void InternalReceive()
        {
            if (!this.IsClosingOrClosed)
            {
                bool flag = true;
                try
                {
                    flag = this.connector.ReceiveAsync(this.receiveSAE);
                }
                catch (Exception exception)
                {

                    this.ReceiveEnd(CloseReason.Exception, exception);
                }
                if (!flag)
                {
                    this.SAE_ReceiveCompleted(this, this.receiveSAE);
                }
            }
        }

        private void ReceiveCompleted(ArraySegment<byte> datas)
        {
            bool flag = false;
            int readlen = 0;
            try
            {
                flag = this.Monitor.Protocol.TryToPacket(this.packet, datas, out readlen);
            }
            catch (Exception exception)
            {
                this.ReceiveEnd(CloseReason.Exception,exception);
            }
            if (flag)
            {
                Packet oldPacket = this.packet;
                this.packet = this.Monitor.Protocol.CreatePacket(this.Monitor.PacketBufferPool);
                ThreadPool.QueueUserWorkItem(delegate (object obj) {
                    try
                    {
                        this.RaiseReceived(oldPacket);
                        this.ReceiveCompletedLoop(datas, readlen);
                    }
                    catch (Exception exception)
                    {
                        this.ReceiveEnd(CloseReason.Exception,exception);
                    }
                    finally
                    {
                        oldPacket.Reset();
                    }
                });
            }
            else
            {
                this.ReceiveCompletedLoop(datas, readlen);
            }
        }

        private void ReceiveCompletedLoop(ArraySegment<byte> datas, int readlen)
        {
            if ((readlen < 0) || (readlen > datas.Count))
            {
                throw new ArgumentOutOfRangeException("readlen", "readlen < 0 or > payload.Count.");
            }
            if ((readlen == 0) || (readlen == datas.Count))
            {
                this.InternalReceive();
            }
            else
            {
                this.ReceiveCompleted(new ArraySegment<byte>(datas.Array, datas.Offset + readlen, datas.Count - readlen));
            }
        }

        private void ReceiveEnd(CloseReason reason,Exception exception)
        {
            this.state.RemoveState(STATE_RECEIVEING);
            this.Close(reason);
        }
        
        private void SAE_ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                this.ReceiveEnd(CloseReason.Exception,new WaxbillException("Receive Complete SocketError Is"+e.SocketError));
            }
            else if (e.BytesTransferred < 1)
            {
                this.ReceiveEnd(CloseReason.RemoteClose,null);
            }
            else
            {
                this.ReceiveCompleted(new ArraySegment<byte>(e.Buffer, e.Offset, e.BytesTransferred));
            }
        }
        #endregion
        
        #region Callback
        private void RaiseConnected()
        {
            try
            {
                this.OnConnected();
                //this.monitor.RaiseOnConnectedEvent(this);
            }
            catch (Exception exception)
            {
                Trace.Error(exception.Message, exception);
            }
        }

        private void RaiseDisconnected(CloseReason reason)
        {
            try
            {
                this.OnDisconnected(reason);
                //this.monitor.RaiseOnDisconnectedEvent(this, reason);
            }
            catch (Exception exception)
            {
                Trace.Error(exception.Message, exception);
            }
        }

        private void RaiseReceived(Packet packet)
        {
            try
            {
                this.OnReceived(packet);
                //this.monitor.RaiseOnReceivedEvent(this, packet);
            }
            catch (Exception exception)
            {
                Trace.Error(exception.Message, exception);
            }
        }

        private void RaiseSended(SendingQueue packet, bool result)
        {
            try
            {
                this.OnSended(packet, result);
                //this.monitor.RaiseOnSendedEvent(this, packet, result);
            }
            catch (Exception exception)
            {
                Trace.Error(exception.Message, exception);
            }
        }
        
        protected abstract void OnConnected();

        protected abstract void OnDisconnected(CloseReason reason);

        protected abstract void OnSended(SendingQueue packet, bool result);
        
        protected abstract void OnReceived(Packet packet);
        #endregion
    }
}

