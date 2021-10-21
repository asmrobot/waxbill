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
    

    public abstract class Session
    {
        private Socket connector;
        private Packet packet;
        private SocketAsyncEventArgs receiveSAE;
        private SendingQueue _readlySendingQueue;
        private SocketAsyncEventArgs sendSAE;
        private ConcurrentState state;//状态

        public SocketMonitor Monitor 
        { 
            get; 
            set; 
        }

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
                return (this.state >= SessionState.CLOSED);
            }
        }

        /// <summary>
        /// 是否关闭或正在关闭
        /// </summary>
        public bool IsClosingOrClosed
        {
            get
            {
                return (this.state >= SessionState.CLOSING);
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
            if (!this.Monitor.SendingPool.TryGet(out this._readlySendingQueue))
            {
                Trace.Error("无法获取可用的发送池");
            }
            else
            {
                this._readlySendingQueue.StartQueue();
                if (this.state.SetState(SessionState.RECEIVEING))
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
            if (!this.IsClosingOrClosed)
            {
                if (!this.state.SetState(SessionState.CLOSING))
                {
                    return;
                }

                SpinWait wait = new SpinWait();
                while (true)
                {
                    if (!this.state.GetState(SessionState.SENDING))
                    {
                        break;
                    }
                    wait.SpinOnce();
                }
                this.RaiseDisconnected(reason);
                this.FreeResource(reason);
            }
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
            SendingQueue newQueue;
            SendingQueue sendingQueue = this._readlySendingQueue;
            if (sendingQueue.Count <= 0)
            {
                return true;
            }
            if (!this.state.SetState(SessionState.SENDING))
            {
                return true;
            }
            if (!this.Monitor.SendingPool.TryGet(out newQueue))
            {
                this.SendEnd(SendingQueue.Null, CloseReason.Exception,new WaxbillException("没有分配到发送queue"));
                return false;
            }
            newQueue.StartQueue();
            this._readlySendingQueue = newQueue;
            sendingQueue.StopQueue();
            return this.InternalSend(sendingQueue);
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
                this.state.RemoveState(SessionState.SENDING);
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
            if (datas.Count > this._readlySendingQueue.Capacity)
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
            this.state.RemoveState(SessionState.SENDING);
            this.Close(reason);
        }
        
        private bool TrySend(ArraySegment<byte> data, out bool reTry)
        {
            reTry = false;
            SendingQueue mSendingQueue = this._readlySendingQueue;
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
            SendingQueue mSendingQueue = this._readlySendingQueue;
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
            this.state.RemoveState(SessionState.RECEIVEING);
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



        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="reason"></param>
        private void FreeResource(CloseReason reason)
        {
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

            if (this.packet != null)
            {
                this.packet.Reset();
            }
            if (this._readlySendingQueue != SendingQueue.Null)
            {
                if (this._readlySendingQueue.Count > 0)
                {
                    this.RaiseSended(this._readlySendingQueue, false);
                }
                this._readlySendingQueue.Clear();
                this.Monitor.SendingPool.Release(this._readlySendingQueue);
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
            this.state.SetState(SessionState.CLOSED);
        }

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

