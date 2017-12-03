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
    public class SessionState
    {
        public const Int32 Sending = 1;
        public const Int32 Receiveing = 2;
        public const Int32 Normal = 4;


        public const Int32 Closeing = 64;
        public const Int32 Closed = 128;
    }

    public abstract class SocketSession
    {
        protected TCPMonitor Monitor;
        protected UVTCPHandle TcpHandle;//客户端socket
        protected ServerOption Option;//服务器配置

        private Int32 m_state = 0;//会话状态
        private Packet m_Packet;//本包
        public long ConnectionID { get; private set; }//连接ID
        private SendingQueue m_SendingQueue;

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
            SendingQueue oldQueue = this.m_SendingQueue;
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
            SendingQueue oldQueue = this.m_SendingQueue;
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
            SendingQueue oldQueue = this.m_SendingQueue;
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
                //todo:ZTImage.Log.Trace.Error("没有分配到发送queue", null);
                return false;
            }

            newQueue.StartQueue();
            m_SendingQueue = newQueue;
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

            var isAsync = true;
            try
            {
                if (queue.Count > 1)
                {
                    this._SendSAE.BufferList = queue;
                }
                else
                {
                    ArraySegment<byte> buffer = queue[0];
                    this._SendSAE.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                }
                this._SendSAE.UserToken = queue;
                isAsync = this._Connector.SendAsync(this._SendSAE);
            }
            catch (Exception ex)
            {
                //todo:ZTImage.Log.Trace.Error("发送出现错误", ex);
                SendEnd(queue, CloseReason.Exception);
                return false;
            }

            if (!isAsync) this.SAE_SendCompleted(this, this._SendSAE);
            return true;
        }

        /// <summary>
        /// SAE发送完成回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SAE_SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            var queue = e.UserToken as SendingQueue;
            if (queue == null)
            {
                ZTImage.Log.Trace.Error("未知错误help!~");
                return;
            }

            if (e.SocketError != SocketError.Success)
            {
                this.RaiseSended(queue, false);
                SendEnd(queue, CloseReason.Exception);
                return;
            }

            int sum = queue.Sum(b => b.Count);
            if (sum <= e.BytesTransferred)
            {
                //发送下一包
                e.SetBuffer(null, 0, 0);
                e.BufferList = null;
                this.RaiseSended(queue, true);
                queue.Clear();
                this.Monitor.SendingPool.Push(queue);

                RemoveState(SessionState.Sending);
                PreSend();
            }
            else
            {
                //发送剩余
                queue.TrimByte(e.BytesTransferred);
                InternalSend(queue);
            }

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
        private void InternalReceive(SocketAsyncEventArgs e)
        {
            if (GetState(SessionState.Closed) || GetState(SessionState.Closeing) || (e == null)) return;

            bool completedAsync = true;
            try
            {
                completedAsync = this._Connector.ReceiveAsync(e);
            }
            catch (Exception ex)
            {
                ZTImage.Log.Trace.Error("接收消息时出现错误", ex);
                ReceiveEnd(CloseReason.InernalError);
            }

            if (!completedAsync) this.SAE_ReceiveCompleted(this, e);
        }

        //todo:remove
        byte[] datas = new byte[40960];
        private void SAE_ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                ReceiveEnd(CloseReason.Exception);
                return;
            }
            if (e.BytesTransferred < 1)
            {
                ReceiveEnd(CloseReason.RemoteClose);
                return;
            }

            //todo:remove
            Buffer.BlockCopy(e.Buffer, e.Offset, datas, 0, e.BytesTransferred);
            this.InternalReceive(this._ReceiveSAE);
            return;
            if (m_Packet == null)
            {
                m_Packet = new Packet(this.Monitor.BufferManager);
            }
            this.ReceiveCompleted(new ArraySegment<byte>(e.Buffer, e.Offset, e.BytesTransferred));
        }

        /// <summary>
        /// 消息接收完成
        /// </summary>
        /// <param name="connector"></param>
        /// <param name="datas"></param>
        /// <param name="callback"></param>
        private void ReceiveCompleted(ArraySegment<byte> datas)
        {
            bool result = false;
            int readlen = 0;
            try
            {
                result = this.Monitor._Protocol.TryToMessage(ref this.m_Packet, datas, out readlen);
            }
            catch (Exception ex)
            {
                ZTImage.Log.Trace.Error("解析信息时发生错误", ex);
                ReceiveEnd(CloseReason.InernalError);
            }

            if (result)
            {
                Packet oldPacket = this.m_Packet;
                this.m_Packet = new Packet(this.Monitor.BufferManager);
                System.Threading.ThreadPool.QueueUserWorkItem((obj) =>
                {
                    try
                    {
                        this.RaiseReceive(oldPacket);
                        ReceiveCompletedLoop(datas, readlen);
                    }
                    catch (Exception ex)
                    {
                        ZTImage.Log.Trace.Error("处理信息时出现错误", ex);
                        ReceiveEnd(CloseReason.Exception);
                        return;
                    }
                    finally
                    {
                        oldPacket.Clear();
                    }
                });
                return;
            }
            else
            {
                ReceiveCompletedLoop(datas, readlen);
            }
        }

        /// <summary>
        /// message process callback
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="readlen"></param>
        /// <exception cref="ArgumentOutOfRangeException">readlength less than 0 or greater than payload.Count.</exception>
        private void ReceiveCompletedLoop(ArraySegment<byte> datas, int readlen)
        {
            if (readlen < 0 || readlen > datas.Count)
                throw new ArgumentOutOfRangeException("readlen", "readlen < 0 or > payload.Count.");

            if (readlen == 0 || readlen == datas.Count)
            {
                this.InternalReceive(this._ReceiveSAE);
                return;
            }

            //粘包处理
            this.ReceiveCompleted(new ArraySegment<byte>(datas.Array, datas.Offset + readlen, datas.Count - readlen));
        }

        /// <summary>
        /// 消息接收中止
        /// </summary>
        private void ReceiveEnd(CloseReason reason)
        {
            RemoveState(SessionState.Receiveing);
            this.Close(reason);
        }
        #endregion

        #region control

        public void Close(CloseReason reason)
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

                try
                {
                    this._Connector.Shutdown(SocketShutdown.Both);
                    this._Connector.Close();
                    this._Connector = null;
                }
                catch (Exception ex)
                {
                    Log.Trace.Error("关闭连接失败", ex);
                }

                this.FreeResource(reason);
            }

        }


        private void FreeResource(CloseReason reason)
        {

            //清空接收缓存
            if (this.m_Packet != null)
            {
                this.m_Packet.Clear();
            }

            //清空发送缓存
            if (this.m_SendingQueue != null)
            {
                if (this.m_SendingQueue.Count > 0)
                {
                    this.RaiseSended(this.m_SendingQueue, false);
                }
                this.m_SendingQueue.Clear();
                this.Monitor.SendingPool.Push(this.m_SendingQueue);

            }

            this._SendSAE.Completed -= new EventHandler<SocketAsyncEventArgs>(this.SAE_SendCompleted);
            this._SendSAE.UserToken = null;
            this._SendSAE.SetBuffer(null, 0, 0);
            this._SendSAE = null;


            this._ReceiveSAE.Completed -= new EventHandler<SocketAsyncEventArgs>(this.SAE_ReceiveCompleted);
            this._ReceiveSAE.UserToken = null;
            this.Monitor.SocketEventArgsPool.RealseSocketAsyncEventArgs(this._ReceiveSAE);
            this._ReceiveSAE = null;

            this._Connector = null;
            SetState(SessionState.Closed);

        }
        #endregion


        #region handle
        IntPtr content = IntPtr.Zero;
        /// <summary>
        /// 分配内存
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="suggsize"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        internal UVIntrop.uv_buf_t AllocMemoryCallback(UVStreamHandle handle, Int32 suggsize, object state)
        {
            if (content == IntPtr.Zero)
            {
                content = Marshal.AllocHGlobal(1024);
            }
            
            return new UVIntrop.uv_buf_t(content, 1024, UVIntrop.IsWindows);
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
                    this.RaiseDisconnect(CloseReason.RemoteClose);
                    return;
                }
                Console.WriteLine("有错误");
                return;
            }

            if (nread == 0)
            {
                Console.WriteLine("据说可以忽略");
                return;
            }
            else
            {
                //read

                byte[] t = new byte[nread];
                Marshal.Copy(content, t, 0, nread);
                Console.WriteLine("读取字节数:" + nread.ToString() + "," + System.Text.Encoding.UTF8.GetString(t));

                client.TryWrite(t);
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

        ///// <summary>
        ///// 发送成功
        ///// </summary>
        ///// <param name="connector"></param>
        ///// <param name="packet"></param>
        ///// <param name="result"></param>
        //private void RaiseSended(SendingQueue packet, bool result)
        //{
        //    try
        //    {
        //        SendedCallback(packet, result);
        //        Monitor.RaiseOnSendedEvent(this, packet, result);
        //    }
        //    catch
        //    { }

        //}

        //private void RaiseReceive(Packet packet)
        //{
        //    try
        //    {
        //        ReceiveCallback(packet);
        //        Monitor.RaiseOnReceiveEvent(this, packet);
        //    }
        //    catch (Exception ex)
        //    {
        //        ZTImage.Log.Trace.Error(ex.Message, ex);
        //    }
        //}
        #endregion

        #region Callback
        protected abstract void ConnectedCallback();

        protected abstract void DisconnectedCallback(CloseReason reason);

        protected abstract void SendedCallback(SendingQueue packet, bool result);

        protected abstract void ReceiveCallback(Packet packet);
        #endregion
    }
}
