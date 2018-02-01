using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Exceptions;
using waxbill.Libuv;
using waxbill.Packets;
using waxbill.Utils;
using ZTImage.Log;

namespace waxbill.Sessions
{
    public abstract class SessionBase
    {
        protected MonitorBase Monitor;
        internal UVTCPHandle TcpHandle;//客户端socket
        
        private Int32 mState = 0;//会话状态
        private Packet mPacket;//本包
        public long ConnectionID { get; private set; }//连接ID

        private UVRequest mSendQueue;//发送队列
        private IPEndPoint mRemoteEndPoint;

        /// <summary>
        /// 远程地址
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return mRemoteEndPoint;
            }
        }

        internal void Init(Int64 connectionID, UVTCPHandle handle, MonitorBase monitor)
        {
            this.ConnectionID = connectionID;
            this.TcpHandle = handle;
            this.Monitor = monitor;
            this.mRemoteEndPoint = handle.RemoteEndPoint;
            
            if (!this.Monitor.TryGetSendQueue(out mSendQueue))
            {
                this.Close(CloseReason.Exception);
                Trace.Error("没有分配到发送queue", null);
            }

            this.mPacket = monitor.CreatePacket();
        }

        #region state
        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool GetState(int state)
        {
            return (mState & state) == state;
        }

        public void RemoveState(int state)
        {
            while (true)
            {
                Int32 oldStatus = mState;
                Int32 newStatus = oldStatus & (~state);
                if (Interlocked.CompareExchange(ref mState, newStatus, oldStatus) == oldStatus)
                {
                    return;
                }
            }
        }

        public void SetState(int state)
        {
            SetState(state, false);
        }

        public bool SetState(int state, bool noClose)
        {
            while (true)
            {
                Int32 oldState = mState;
                if (noClose)
                {
                    if (oldState >= SessionState.Closeing)
                    {
                        return false;
                    }
                }
                Int32 newState = mState | state;
                if (Interlocked.CompareExchange(ref mState, newState, oldState) == oldState)
                {
                    return true;
                }
            }
        }

        public bool TrySetState(int state)
        {
            while (true)
            {
                Int32 oldState = mState;
                Int32 newState = oldState | state;
                if (newState == mState)
                {
                    return false;
                }
                if (Interlocked.CompareExchange(ref mState, newState, oldState) == oldState)
                {
                    return true;
                }
            }
        }


        public bool IsClosingOrClosed
        {
            get { return mState >= SessionState.Closeing; }
        }

        public bool IsClosed
        {
            get { return mState >= SessionState.Closed; }
        }
        #endregion

        #region send
        /// <summary>
        /// 加入到发送列表中
        /// </summary>
        /// <param name="datas"></param>
        public void Send(byte[] datas)
        {
            Send(new ArraySegment<byte>(datas, 0, datas.Length));
        }

        /// <summary>
        /// 加入到发送列表中
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public void Send(byte[] datas, int offset, int size)
        {
            Send(new ArraySegment<byte>(datas, offset, size));
        }

        public void Send(ArraySegment<byte> data)
        {
            bool retry = false;
            if (TrySend(data, out retry))
            {
                return;
            }

            if (!retry)
            {
                return;
            }

            SpinWait wait = new SpinWait();
            DateTime dt = DateTime.Now.AddMilliseconds(this.Monitor.Option.SendTimeout);
            while (true)
            {
                wait.SpinOnce();
                if (TrySend(data, out retry))
                {
                    break;
                }

                if (DateTime.Now >= dt)
                {
                    break;
                }

                if (!retry)
                {
                    continue;
                }
            }
        }

        public void Send(IList<ArraySegment<byte>> datas)
        {
            if (datas.Count > this.Monitor.Option.SendQueueSize)
            {
                throw new ArgumentOutOfRangeException("发送内容大于缓存池");
            }

            bool retry = false;
            if (TrySend(datas, out retry))
            {
                return;
            }
            if (!retry)
            {
                return;
            }

            SpinWait wait = new SpinWait();
            DateTime dt = DateTime.Now.AddMilliseconds(this.Monitor.Option.SendTimeout);
            while (true)
            {
                wait.SpinOnce();
                if (TrySend(datas, out retry))
                {
                    break;
                }

                if (DateTime.Now >= dt)
                {
                    break;
                }

                if (!retry)
                {
                    continue;
                }
            }
        }

        private bool TrySend(ArraySegment<byte> data, out bool reTry)
        {
            reTry = false;
            UVRequest oldQueue = this.mSendQueue;
            if (oldQueue == null)
            {
                return false;
            }

            if (!oldQueue.EnQueue(data))
            {
                reTry = true;
                return false;
            }

            return PreSend();
        }

        private bool TrySend(IList<ArraySegment<byte>> datas, out bool reTry)
        {
            reTry = false;
            UVRequest oldQueue = this.mSendQueue;
            if (oldQueue == null)
            {
                //todo:是否关闭连接？
                return false;
            }

            if (!oldQueue.EnQueue(datas))
            {
                reTry = true;
                return false;
            }
            return PreSend();
        }

        private bool PreSend()
        {
            UVRequest oldQueue = this.mSendQueue;
            if (oldQueue.Count <= 0)
            {
                return true;
            }
            if (!TrySetState(SessionState.Sending))
            {
                return true;
            }

            UVRequest newQueue;

            if (!this.Monitor.TryGetSendQueue(out newQueue))
            {
                SendEnd(oldQueue, CloseReason.InernalError);
                Trace.Error("没有分配到发送queue", null);
                return false;
            }

            newQueue.StartQueue();
            this.mSendQueue = newQueue;
            oldQueue.StopQueue();

            return InternalSend(oldQueue);
        }

        private bool InternalSend(UVRequest queue)
        {
            if (IsClosingOrClosed)
            {
                SendEnd(queue, CloseReason.Closeing);
                return false;
            }

            try
            {
                queue.Send(this.TcpHandle, SendCompleted, null);
            }
            catch (Exception ex)
            {
                Trace.Error("发送出现错误", ex);
                SendEnd(queue, CloseReason.Exception);
                return false;
            }

            return true;
        }

        /// <summary>
        /// SAE发送完成回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendCompleted(UVRequest req, Int32 statue, UVException ex, object state)
        {
            if (ex != null)
            {
                this.OnSended(req, false);
                SendEnd(req, CloseReason.Exception);
                return;
            }

            //todo:发送下一包
            this.OnSended(req, true);
            req.Clear();
            this.Monitor.ReleaseSendQueue(req);

            RemoveState(SessionState.Sending);
            PreSend();
        }

        /// <summary>
        /// 发送中止
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="reason"></param>
        private void SendEnd(UVRequest queue, CloseReason reason)
        {
            if (queue != null)
            {
                queue.Clear();
                this.Monitor.ReleaseSendQueue(queue);
            }
            RemoveState(SessionState.Sending);
            this.Close(reason);
        }
        #endregion

        #region receive
        /// <summary>
        /// 消息接收完成
        /// </summary>
        /// <param name="connector"></param>
        /// <param name="datas"></param>
        /// <param name="callback"></param>
        private void ReceiveCompleted(IntPtr memory, Int32 nread, out Int32 giveupCount)
        {
            bool result = false;
            giveupCount = 0;
            try
            {
                result = this.Monitor.Protocol.TryToPacket(this.mPacket, memory, nread, out giveupCount);
            }
            catch (Exception ex)
            {
                Trace.Error("解析信息时发生错误", ex);
                ReceiveEnd(CloseReason.InernalError);
            }

            if (!result)
            {
                return;
            }

            if (giveupCount < 0 || giveupCount > nread)
            {
                throw new ArgumentOutOfRangeException("readlen", "readlen < 0 or > payload.Count.");
            }

            memory = IntPtr.Add(memory, giveupCount);
            int readCount = 0;

            //Packet oldPacket = this.mPacket;
            //this.mPacket = new Packet(this.Monitor.BufferManager);
            try
            {
                this.OnReceived(this.mPacket);
                this.mPacket.Reset();

            }
            catch (Exception ex)
            {
                this.mPacket.Dispose();
                Trace.Error("处理信息时出现错误", ex);
                ReceiveEnd(CloseReason.Exception);
                return;
            }

            if (giveupCount == nread)
            {
                return;
            }
            readCount = 0;
            ReceiveCompleted(memory, nread - giveupCount, out readCount);
            giveupCount += readCount;
        }

        /// <summary>
        /// 消息接收中止
        /// </summary>
        private void ReceiveEnd(CloseReason reason, Exception exception = null)
        {
            RemoveState(SessionState.Receiveing);
            this.Close(reason, exception);
        }
        #endregion

        #region control

        public void Close(CloseReason reason, Exception exception = null)
        {
            lock (this)
            {
                if (IsClosed)
                {
                    return;
                }

                if (!TrySetState(SessionState.Closeing))
                {
                    return;
                }

                this.OnDisconnected(reason);

                if (this.mReadDatas != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(this.mReadDatas);
                }

                this.TcpHandle.Dispose();
                this.FreeResource(reason);
            }
        }


        private void FreeResource(CloseReason reason)
        {
            //清空接收缓存
            if (this.mPacket != null)
            {
                this.mPacket.Dispose();
            }

            //清空发送缓存
            if (this.mSendQueue != null)
            {
                if (this.mSendQueue.Count > 0)
                {
                    this.OnSended(this.mSendQueue, false);
                }
                this.mSendQueue.Clear();
                this.Monitor.ReleaseSendQueue(this.mSendQueue);
            }
            SetState(SessionState.Closed);
        }
        #endregion

        #region handle
        private IntPtr mReadDatas = IntPtr.Zero;
        private Int32 mReadOffset = 0;

        /// <summary>
        /// 分配内存
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="suggsize"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        internal UVIntrop.uv_buf_t AllocMemoryCallback(UVStreamHandle handle, Int32 suggsize, object state)
        {
            if (mReadDatas == IntPtr.Zero)
            {
                if (!this.Monitor.TryGetReceiveMemory(out mReadDatas))
                {
                    throw new ArgumentOutOfRangeException("allow bytes");
                }
            }

            return new UVIntrop.uv_buf_t(mReadDatas + mReadOffset, this.Monitor.Option.ReceiveBufferSize - this.mReadOffset, UVIntrop.IsWindows);
        }

        /// <summary>
        /// 读取回调
        /// </summary>
        /// <param name="client"></param>
        /// <param name="nread"></param>
        /// <param name="exception"></param>
        /// <param name="buf"></param>
        /// <param name="state"></param>
        internal void ReadCallback(UVStreamHandle client, Int32 nread, UVException exception, ref UVIntrop.uv_buf_t buf, object state)
        {
            if (exception != null)
            {
                if (nread == UVIntrop.UV_EOF)
                {
                    ReceiveEnd(CloseReason.RemoteClose);
                }
                else
                {
                    ReceiveEnd(CloseReason.Exception, exception);
                }
                return;
            }

            Int32 giveupCount = 0;
            this.mReadOffset += nread;

            this.ReceiveCompleted(this.mReadDatas, this.mReadOffset, out giveupCount);
            if (giveupCount > 0)
            {
                if (giveupCount < this.mReadOffset)
                {
                    //没读完
                    this.mReadOffset = this.mReadOffset - giveupCount;
                    if (this.mReadOffset >= this.Monitor.Option.ReceiveBufferSize)
                    {
                        throw new ArgumentOutOfRangeException("数据没有读取,数据缓冲区已满");
                    }
                    UVIntrop.memorymove(this.mReadDatas + giveupCount, this.mReadDatas, this.mReadOffset, UVIntrop.IsWindows);
                }
                else
                {
                    //读完
                    this.mReadOffset = 0;
                }
            }
        }

        #endregion

        #region Events Raise

        internal void RaiseOnConnected()
        {
            OnConnected();
            this.TcpHandle.ReadStart(this.AllocMemoryCallback, this.ReadCallback, this, this);
        }
        /// <summary>
        /// 连接
        /// </summary>
        protected abstract void OnConnected();

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="connector"></param>
        protected abstract void OnDisconnected(CloseReason reason);

        /// <summary>
        /// 发送成功
        /// </summary>
        /// <param name="connector"></param>
        /// <param name="packet"></param>
        /// <param name="result"></param>
        protected abstract void OnSended(IList<UVIntrop.uv_buf_t> packet, bool result);

        protected abstract void OnReceived(Packet packet);
        #endregion

        //#region Callback
        //protected abstract void ConnectedCallback();

        //protected abstract void DisconnectedCallback(CloseReason reason);

        //protected abstract void SendedCallback(IList<UVIntrop.uv_buf_t> packet, bool result);

        //protected abstract void ReceiveCallback(Packet packet);
        //#endregion
    }
}
