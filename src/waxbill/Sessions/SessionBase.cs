using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
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
        private int state;

        public void Close(CloseReason reason)
        {
            if (this.TrySetState(0x40))
            {
                SpinWait wait = new SpinWait();
                while (true)
                {
                    if (!this.GetState(1))
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
            this.Monitor.ReceiveBufferPool.Release(new ArraySegment<byte>(this.receiveSAE.Buffer,this.receiveSAE.Offset,this.receiveSAE.Count));
            this.receiveSAE.SetBuffer(null, 0, 0);
            this.Monitor.SocketEventArgsPool.Release(this.receiveSAE);
            this.receiveSAE = null;


            this.connector = null;
            this.SetState(0x80);
        }

        public bool GetState(int state)
        {
            return ((this.state & state) == state);
        }

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

            this.packet = new Packet(this.Monitor.PacketBufferPool);
        }

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
                    Trace.Error("接收消息时出现错误", exception);
                    this.ReceiveEnd(CloseReason.Exception);
                }
                if (!flag)
                {
                    this.SAE_ReceiveCompleted(this, this.receiveSAE);
                }
            }
        }

        private bool InternalSend(SendingQueue queue)
        {
            if (this.IsClosingOrClosed)
            {
                this.SendEnd(queue, CloseReason.Default);
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
                Trace.Error("发送出现错误", exception);
                this.SendEnd(queue, CloseReason.Exception);
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
            if (!this.TrySetState(1))
            {
                return true;
            }
            if (!this.Monitor.SendingPool.TryGet(out queue2))
            {
                this.SendEnd(SendingQueue.Null, CloseReason.Exception);
                Trace.Error("没有分配到发送queue", null);
                return false;
            }
            queue2.StartQueue();
            this.sendingQueue = queue2;
            mSendingQueue.StopQueue();
            return this.InternalSend(mSendingQueue);
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
                Trace.Error("解析信息时发生错误", exception);
                this.ReceiveEnd(CloseReason.Exception);
            }
            if (flag)
            {
                Packet oldPacket = this.packet;
                this.packet = new Packet(this.Monitor.PacketBufferPool);
                ThreadPool.QueueUserWorkItem(delegate (object obj) {
                    try
                    {
                        this.RaiseReceived(oldPacket);
                        this.ReceiveCompletedLoop(datas, readlen);
                    }
                    catch (Exception exception)
                    {
                        Trace.Error("处理信息时出现错误", exception);
                        this.ReceiveEnd(CloseReason.Exception);
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

        private void ReceiveEnd(CloseReason reason,Exception exception=null)
        {
            this.RemoveState(2);
            this.Close(reason);
        }

        public void RemoveState(int state)
        {
            int mState;
            int num2;
            do
            {
                mState = this.state;
                num2 = mState & ~state;
            }
            while (Interlocked.CompareExchange(ref this.state, num2, mState) != mState);
        }

        private void SAE_ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                this.ReceiveEnd(CloseReason.Exception);
            }
            else if (e.BytesTransferred < 1)
            {
                this.ReceiveEnd(CloseReason.RemoteClose);
            }
            else
            {
                this.ReceiveCompleted(new ArraySegment<byte>(e.Buffer, e.Offset, e.BytesTransferred));
            }
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
                this.SendEnd(sendQueue, CloseReason.Exception);
            }
            else if (sendQueue.Count <= e.BytesTransferred)
            {
               
                e.SetBuffer(null, 0, 0);
                e.BufferList = null;
                this.RaiseSended(sendQueue, true);
                sendQueue.Clear();
                this.Monitor.SendingPool.Release(sendQueue);
                this.RemoveState(1);
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
            DateTime time = DateTime.Now.AddMilliseconds((double) this.Monitor.Option.SendTimeout);
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
            if (datas.Count >this.sendingQueue.Capacity)
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
            DateTime time = DateTime.Now.AddMilliseconds((double) this.Monitor.Option.SendTimeout);
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

        
        private void SendEnd(SendingQueue queue, CloseReason reason)
        {
            if (queue != null)
            {
                queue.Clear();
                this.Monitor.SendingPool.Release(queue);
            }
            this.RemoveState(1);
            this.Close(reason);
        }

        public void SetState(int state)
        {
            this.SetState(state, false);
        }

        public bool SetState(int state, bool noClose)
        {
            int mState;
            int num2;
            do
            {
                mState = this.state;
                if (noClose && (mState >= 0x40))
                {
                    return false;
                }
                num2 = this.state | state;
            }
            while (Interlocked.CompareExchange(ref this.state, num2, mState) != mState);
            return true;
        }

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
                if (this.TrySetState(2))
                {
                    this.InternalReceive();
                }
            }
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

        public bool TrySetState(int state)
        {
            int mState;
            int num2;
            do
            {
                mState = this.state;
                num2 = mState | state;
                if (num2 == this.state)
                {
                    return false;
                }
            }
            while (Interlocked.CompareExchange(ref this.state, num2, mState) != mState);
            return true;
        }

        public long ConnectionID { get; private set; }

        public bool IsClosed
        {
            get
            {
                return (this.state >= 0x80);
            }
        }

        public bool IsClosingOrClosed
        {
            get
            {
                return (this.state >= 0x40);
            }
        }

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

