using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Libuv;
using waxbill.Utils;

namespace waxbill
{
    public abstract class SocketSession
    {
        protected TCPMonitor Monitor;
        protected UVTCPHandle TcpHandle;//客户端socket
        protected ServerOption Option;//服务器配置

        private Int32 m_state = 0;//会话状态
        private Packet mPacket;//本包
        public long ConnectionID { get; private set; }//连接ID
        private SendingQueue mSendingQueue;

        internal void Init(UVTCPHandle handle, TCPMonitor monitor, ServerOption option)
        {
            this.TcpHandle = handle;
            this.Monitor = monitor;
            this.Option = option;
            this.ConnectionID = monitor.GetNextConnectionID();
        }
        

        #region state
        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool GetState(int state)
        {
            return (m_state & state) == state;
        }

        public void RemoveState(int state)
        {
            while (true)
            {
                Int32 oldStatus = m_state;
                Int32 newStatus = oldStatus & (~state);
                if (Interlocked.CompareExchange(ref m_state, newStatus, oldStatus) == oldStatus)
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
                Int32 oldState = m_state;
                if (noClose)
                {
                    if (oldState >= SessionState.Closeing)
                    {
                        return false;
                    }
                }
                Int32 newState = m_state | state;
                if (Interlocked.CompareExchange(ref m_state, newState, oldState) == oldState)
                {
                    return true;
                }
            }
        }

        public bool TrySetState(int state)
        {
            while (true)
            {
                Int32 oldState = m_state;
                Int32 newState = oldState | state;
                if (newState == m_state)
                {
                    return false;
                }
                if (Interlocked.CompareExchange(ref m_state, newState, oldState) == oldState)
                {
                    return true;
                }
            }
        }


        public bool IsClosingOrClosed
        {
            get { return m_state >= SessionState.Closeing; }
        }

        public bool IsClosed
        {
            get { return m_state >= SessionState.Closed; }
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
            DateTime dt = DateTime.Now.AddMilliseconds(this.Option.SendTimeout);
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
            if (datas.Count > this.Option.SendQueueSize)
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
            DateTime dt = DateTime.Now.AddMilliseconds(this.Option.SendTimeout);
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
            SendingQueue oldQueue = this.mSendingQueue;
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
            SendingQueue oldQueue = this.mSendingQueue;
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
            SendingQueue oldQueue = this.mSendingQueue;
            if (oldQueue.Count <= 0)
            {
                return true;
            }
            if (!TrySetState(SessionState.Sending))
            {
                return true;
            }

            SendingQueue newQueue;
            if (!this.Monitor.SendingPool.TryGet(out newQueue))
            {
                SendEnd(null, CloseReason.InernalError);
                Trace.Error("没有分配到发送queue", null);
                return false;
            }

            newQueue.StartQueue();
            mSendingQueue = newQueue;
            oldQueue.StopQueue();

            return InternalSend(oldQueue);
        }

        private bool InternalSend(SendingQueue queue)
        {
            if (IsClosingOrClosed)
            {
                SendEnd(queue, CloseReason.Closeing);
                return false;
            }

            //var isAsync = true;
            try
            {
                UVRequest request = new UVRequest();
                request.Init();
                request.Write(this.TcpHandle, new ArraySegment<ArraySegment<byte>>(queue.ToArray(), 0, queue.Count), SendCompleted, null);
                request.Close();
                //if (queue.Count > 1)
                //{
                    
                //    //todo:发送queueu this._SendSAE.BufferList = queue;
                //    //this.TcpHandle.TryWrite()
                //}
                //else
                //{
                //    ArraySegment<byte> buffer = queue[0];
                //    //todo:发送buffer this._SendSAE.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                //    //client.TryWrite(t);
                //}
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
        private void SendCompleted(UVRequest req, Int32  statue, UVException ex, object state)
        {
            //if (queue == null)
            //{
            //    Trace.Error("未知错误help!~");
            //    return;
            //}

            ////if (e.SocketError != SocketError.Success)
            ////{
            ////    this.RaiseSended(queue, false);
            ////    SendEnd(queue, CloseReason.Exception);
            ////    return;
            ////}

            //int sum = queue.Sum(b => b.Count);
            //if (sum <= transCount)
            //{
            //    //发送下一包
            //    //e.SetBuffer(null, 0, 0);
            //    //e.BufferList = null;
            //    this.RaiseSended(queue, true);
            //    queue.Clear();
            //    this.Monitor.SendingPool.Push(queue);

            //    RemoveState(SessionState.Sending);
            //    PreSend();
            //}
            //else
            //{
            //    //发送剩余
            //    queue.TrimByte(transCount);
            //    InternalSend(queue);
            //}

        }

        /// <summary>
        /// 发送中止
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="reason"></param>
        private void SendEnd(SendingQueue queue, CloseReason reason)
        {
            if (queue != null)
            {
                queue.Clear();
                this.Monitor.SendingPool.Push(queue);
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
        private void ReceiveCompleted(IntPtr memory,Int32 nread,out Int32 giveupCount)
        {
            bool result = false;
            giveupCount = 0;
            try
            {
                result = this.Monitor.Protocol.TryToPacket(ref this.mPacket, memory,nread, out giveupCount);
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

            

            Packet oldPacket = this.mPacket;
            this.mPacket = new Packet(this.Monitor.BufferManager);
            try
            {
                this.RaiseReceive(oldPacket);
            }
            catch (Exception ex)
            {
                Trace.Error("处理信息时出现错误", ex);
                ReceiveEnd(CloseReason.Exception);
                return;
            }
            finally
            {
                oldPacket.Clear();
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
        private void ReceiveEnd(CloseReason reason,Exception exception=null)
        {
            RemoveState(SessionState.Receiveing);
            this.Close(reason, exception);
        }
        #endregion

        #region control

        public void Close(CloseReason reason,Exception exception=null)
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

                this.RaiseDisconnect(reason);

                if (this.mReadDatas != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(this.mReadDatas);
                }
                //todo:关闭句柄
                this.TcpHandle.Dispose();

                this.FreeResource(reason);
            }
        }


        private void FreeResource(CloseReason reason)
        {
            //清空接收缓存
            if (this.mPacket != null)
            {
                this.mPacket.Clear();
            }

            //清空发送缓存
            if (this.mSendingQueue != null)
            {
                if (this.mSendingQueue.Count > 0)
                {
                    this.RaiseSended(this.mSendingQueue, false);
                }
                this.mSendingQueue.Clear();
                this.Monitor.SendingPool.Push(this.mSendingQueue);
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
                mReadDatas = Marshal.AllocHGlobal(this.Option.ReceiveBufferSize);
            }
            
            return new UVIntrop.uv_buf_t(mReadDatas+mReadOffset, this.Option.ReceiveBufferSize-this.mReadOffset, UVIntrop.IsWindows);
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

            if (nread == 0)
            {
                //todo:研究
                //Close(CloseReason.RemoteClose, null);
                Console.WriteLine("据说可以忽略");
                return;
            }
            else
            {
                if (mPacket == null)
                {
                    mPacket = new Packet(this.Monitor.BufferManager);
                }

                Int32 giveupCount = 0;
                this.mReadOffset += nread;

                this.ReceiveCompleted(this.mReadDatas,this.mReadOffset,out giveupCount);
                if (giveupCount > 0)
                {
                    if (giveupCount < this.mReadOffset)
                    {
                        //没读完
                        //todo:优化，以后尽量不移动数据
                        this.mReadOffset = this.mReadOffset - giveupCount;
                        UVIntrop.memorymove(this.mReadDatas + giveupCount, this.mReadDatas, this.mReadOffset, UVIntrop.IsWindows);
                    }
                    else
                    {
                        //读完
                        this.mReadOffset = 0;
                    }
                }
            }
        }


        


        #endregion

        #region Events Raise
        /// <summary>
        /// 连接
        /// </summary>
        internal void RaiseAccept()
        {
            try
            {
                ConnectedCallback();
                Monitor.RaiseOnConnectionEvent(this);
            }
            catch
            { }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="connector"></param>
        private void RaiseDisconnect(CloseReason reason)
        {
            try
            {
                DisconnectedCallback(reason);
                Monitor.RaiseOnDisconnectedEvent(this, reason);
            }
            catch
            { }

        }

        /// <summary>
        /// 发送成功
        /// </summary>
        /// <param name="connector"></param>
        /// <param name="packet"></param>
        /// <param name="result"></param>
        private void RaiseSended(SendingQueue packet, bool result)
        {
            try
            {
                SendedCallback(packet, result);
                Monitor.RaiseOnSendedEvent(this, packet, result);
            }
            catch
            { }

        }

        private void RaiseReceive(Packet packet)
        {
            try
            {
                ReceiveCallback(packet);
                Monitor.RaiseOnReceiveEvent(this, packet);
            }
            catch (Exception ex)
            {
                Trace.Error(ex.Message, ex);
            }
        }
        #endregion

        #region Callback
        protected abstract void ConnectedCallback();

        protected abstract void DisconnectedCallback(CloseReason reason);

        protected abstract void SendedCallback(SendingQueue packet, bool result);

        protected abstract void ReceiveCallback(Packet packet);
        #endregion
    }
}
