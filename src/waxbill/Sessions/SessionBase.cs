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
        private Socket _Connector;
        private SocketMonitor monitor;
        private Packet mPacket;
        private SocketAsyncEventArgs mReceiveSAE;
        private SendingQueue mSendingQueue;
        private SocketAsyncEventArgs mSendSAE;
        private int mState;

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
                    this._Connector.Shutdown(SocketShutdown.Both);
                    this._Connector.Close();
                    this._Connector = null;
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
            if (this.mPacket != null)
            {
                this.mPacket.Reset();
            }
            if (this.mSendingQueue != null)
            {
                if (this.mSendingQueue.Count > 0)
                {
                    this.RaiseSended(this.mSendingQueue, false);
                }
                this.mSendingQueue.Clear();
                this.monitor.SendingPool.Release(this.mSendingQueue);
            }
            this.mSendSAE.Completed -= new EventHandler<SocketAsyncEventArgs>(this.SAE_SendCompleted);
            this.mSendSAE.UserToken = null;
            this.mSendSAE.SetBuffer(null, 0, 0);
            this.mSendSAE = null;
            this.mReceiveSAE.Completed -= new EventHandler<SocketAsyncEventArgs>(this.SAE_ReceiveCompleted);
            this.mReceiveSAE.UserToken = null;
            this.monitor.SocketEventArgsPool.Release(this.mReceiveSAE);
            this.mReceiveSAE = null;
            this._Connector = null;
            this.SetState(0x80);
        }

        public bool GetState(int state)
        {
            return ((this.mState & state) == state);
        }

        internal void Initialize(Socket client, SocketMonitor monitor)
        {
            if (client == null)
            {
                throw new ArgumentNullException("connector");
            }
            if (monitor == null)
            {
                throw new ArgumentNullException("monitor");
            }
            this.monitor = monitor;
            this._Connector = client;
            this.ConnectionID = monitor.GetNextConnectionID();
            this.mSendSAE = new SocketAsyncEventArgs();
            this.mSendSAE.Completed += new EventHandler<SocketAsyncEventArgs>(this.SAE_SendCompleted);
            if (!this.monitor.SocketEventArgsPool.TryGet(out this.mReceiveSAE))
            {
                throw new Exception("无法获取");
            }

            this.mReceiveSAE.Completed += new EventHandler<SocketAsyncEventArgs>(this.SAE_ReceiveCompleted);
            this.mPacket = new Packet(this.monitor.BufferManager);
        }

        private void InternalReceive()
        {
            if (!this.IsClosingOrClosed)
            {
                bool flag = true;
                try
                {
                    flag = this._Connector.ReceiveAsync(this.mReceiveSAE);
                }
                catch (Exception exception)
                {
                    Trace.Error("接收消息时出现错误", exception);
                    this.ReceiveEnd(CloseReason.InernalError);
                }
                if (!flag)
                {
                    this.SAE_ReceiveCompleted(this, this.mReceiveSAE);
                }
            }
        }

        private bool InternalSend(SendingQueue queue)
        {
            if (this.IsClosingOrClosed)
            {
                this.SendEnd(queue, CloseReason.Closeing);
                return false;
            }
            bool flag = true;
            try
            {
                if (queue.Count > 1)
                {
                    this.mSendSAE.BufferList = queue;
                }
                else
                {
                    ArraySegment<byte> segment = queue[0];
                    this.mSendSAE.SetBuffer(segment.Array, segment.Offset, segment.Count);
                }
                this.mSendSAE.UserToken = queue;
                flag = this._Connector.SendAsync(this.mSendSAE);
            }
            catch (Exception exception)
            {
                Trace.Error("发送出现错误", exception);
                this.SendEnd(queue, CloseReason.Exception);
                return false;
            }
            if (!flag)
            {
                this.SAE_SendCompleted(this, this.mSendSAE);
            }
            return true;
        }

        private bool PreSend()
        {
            SendingQueue queue2;
            SendingQueue mSendingQueue = this.mSendingQueue;
            if (mSendingQueue.Count <= 0)
            {
                return true;
            }
            if (!this.TrySetState(1))
            {
                return true;
            }
            if (!this.monitor.SendingPool.TryGet(out queue2))
            {
                this.SendEnd(null, CloseReason.InernalError);
                Trace.Error("没有分配到发送queue", null);
                return false;
            }
            queue2.StartQueue();
            this.mSendingQueue = queue2;
            mSendingQueue.StopQueue();
            return this.InternalSend(mSendingQueue);
        }

        
        private void ReceiveCompleted(ArraySegment<byte> datas)
        {
            bool flag = false;
            int readlen = 0;
            try
            {
                flag = this.monitor._Protocol.TryToPacket(this.mPacket, datas, out readlen);
            }
            catch (Exception exception)
            {
                Trace.Error("解析信息时发生错误", exception);
                this.ReceiveEnd(CloseReason.InernalError);
            }
            if (flag)
            {
                Packet oldPacket = this.mPacket;
                this.mPacket = new Packet(this.monitor.BufferManager);
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

        private void ReceiveEnd(CloseReason reason)
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
                mState = this.mState;
                num2 = mState & ~state;
            }
            while (Interlocked.CompareExchange(ref this.mState, num2, mState) != mState);
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
            SendingQueue userToken = e.UserToken as SendingQueue;
            if (userToken == null)
            {
                Trace.Error("未知错误help!~");
            }
            else if (e.SocketError != SocketError.Success)
            {
                this.RaiseSended(userToken, false);
                this.SendEnd(userToken, CloseReason.Exception);
            }
            else if (userToken.Count <= e.BytesTransferred)
            {
               
                e.SetBuffer(null, 0, 0);
                e.BufferList = null;
                this.RaiseSended(userToken, true);
                userToken.Clear();
                this.monitor.SendingPool.Release(userToken);
                this.RemoveState(1);
                this.PreSend();
            }
            else
            {
                userToken.TrimByte(e.BytesTransferred);
                this.InternalSend(userToken);
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
            DateTime time = DateTime.Now.AddMilliseconds((double) this.monitor.Config.SendTimeout);
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
            if (datas.Count > this.monitor.SendingQueueSize)
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
            DateTime time = DateTime.Now.AddMilliseconds((double) this.monitor.Config.SendTimeout);
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
                this.monitor.SendingPool.Release(queue);
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
                mState = this.mState;
                if (noClose && (mState >= 0x40))
                {
                    return false;
                }
                num2 = this.mState | state;
            }
            while (Interlocked.CompareExchange(ref this.mState, num2, mState) != mState);
            return true;
        }

        public void Start()
        {
            this.RaiseConnected();
            if (!this.monitor.SendingPool.TryGet(out this.mSendingQueue))
            {
                Trace.Error("无法获取可用的发送池");
            }
            else
            {
                this.mSendingQueue.StartQueue();
                if (this.TrySetState(2))
                {
                    this.InternalReceive();
                }
            }
        }

        private bool TrySend(ArraySegment<byte> data, out bool reTry)
        {
            reTry = false;
            SendingQueue mSendingQueue = this.mSendingQueue;
            if (mSendingQueue == null)
            {
                return false;
            }
            if (!mSendingQueue.EnQueue(data))
            {
                reTry = true;
                return false;
            }
            return this.PreSend();
        }

        private bool TrySend(IList<ArraySegment<byte>> datas, out bool reTry)
        {
            reTry = false;
            SendingQueue mSendingQueue = this.mSendingQueue;
            if (mSendingQueue == null)
            {
                return false;
            }
            if (!mSendingQueue.EnQueue(datas))
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
                mState = this.mState;
                num2 = mState | state;
                if (num2 == this.mState)
                {
                    return false;
                }
            }
            while (Interlocked.CompareExchange(ref this.mState, num2, mState) != mState);
            return true;
        }

        public long ConnectionID { get; private set; }

        public bool IsClosed
        {
            get
            {
                return (this.mState >= 0x80);
            }
        }

        public bool IsClosingOrClosed
        {
            get
            {
                return (this.mState >= 0x40);
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                try
                {
                    return (this._Connector.RemoteEndPoint as IPEndPoint);
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
                this.monitor.RaiseOnConnectedEvent(this);
            }
            catch
            {
            }
        }

        private void RaiseDisconnected(CloseReason reason)
        {
            try
            {
                this.OnDisconnected(reason);
                this.monitor.RaiseOnDisconnectedEvent(this, reason);
            }
            catch
            {
            }
        }

        private void RaiseReceived(Packet packet)
        {
            try
            {
                this.OnReceived(packet);
                this.monitor.RaiseOnReceivedEvent(this, packet);
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
                this.monitor.RaiseOnSendedEvent(this, packet, result);
            }
            catch
            {
            }
        }



        protected abstract void OnConnected();

        protected abstract void OnDisconnected(CloseReason reason);

        protected abstract void OnSended(SendingQueue packet, bool result);


        protected abstract void OnReceived(Packet packet);
        #endregion
    }
}

