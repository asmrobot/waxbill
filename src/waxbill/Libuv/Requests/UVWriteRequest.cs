using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Utils;
using ZTImage.Log;

namespace waxbill.Libuv
{
    public unsafe class UVWriteRequest : UVRequest, IList<UVIntrop.uv_buf_t>
    {
        public static UVIntrop.uv_write_cb mWritecb = WriteCallback;
        public Int32 mMaxQueueSize;
        public UVIntrop.uv_buf_t* mBufs;
        public object mState;
        public Action<UVRequest, Int32, UVException, object> mCallback;
        public GCHandle[] mPins;

        private bool m_IsReadOnly = false;
        private int m_Updateing = 0;//进入队列数
        public int mCurrentQueueSize;
        

        public UVWriteRequest(Int32 queueSize) 
        {
            waxbill.Utils.Validate.ThrowIfZeroOrMinus(queueSize, "发送队列要大于0");
            this.mMaxQueueSize = queueSize;
            Int32 requestSize = UVIntrop.req_size(UVRequestType.WRITE);
            var bufferSize = Marshal.SizeOf(typeof(UVIntrop.uv_buf_t)) * this.mMaxQueueSize;
            this.mPins = new GCHandle[this.mMaxQueueSize];
            CreateMemory(requestSize + bufferSize);
            this.mBufs = (UVIntrop.uv_buf_t*)(this.handle + requestSize);
        }
        
        public void StartQueue()
        {
            m_IsReadOnly = false;
        }

        public void StopQueue()
        {
            if (m_IsReadOnly)
            {
                return;
            }

            m_IsReadOnly = true;
            if (m_Updateing < 0)
            {
                return;
            }

            SpinWait wait = new SpinWait();
            wait.SpinOnce();
            while (m_Updateing > 0)
            {
                wait.SpinOnce();
            }
        }
        
        public bool EnQueue(ArraySegment<byte> item)
        {
            if (m_IsReadOnly)
            {
                return false;
            }

            Interlocked.Increment(ref m_Updateing);
            while (!m_IsReadOnly)
            {
                bool conflict = false;
                if (TryEnQueue(item, out conflict))
                {
                    Interlocked.Decrement(ref m_Updateing);
                    return true;
                }

                if (!conflict)
                {
                    break;
                }
            }
            Interlocked.Decrement(ref m_Updateing);
            return false;
        }

        unsafe private bool TryEnQueue(ArraySegment<byte> item, out bool conflict)
        {
            conflict = false;
            int currentCount = mCurrentQueueSize;
            if (currentCount >= this.mMaxQueueSize)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref mCurrentQueueSize, currentCount + 1, currentCount) != currentCount)
            {
                conflict = true;
                return false;
            }

            //添加
            var gcHandle = GCHandle.Alloc(item.Array, GCHandleType.Pinned);
            mPins[currentCount]=gcHandle;
            this.mBufs[currentCount] = UVIntrop.buf_init(gcHandle.AddrOfPinnedObject() + item.Offset, item.Count);

            return true;
        }

        public bool EnQueue(IList<ArraySegment<byte>> items)
        {
            if (m_IsReadOnly)
            {
                return false;
            }

            Interlocked.Increment(ref m_Updateing);
            while (!m_IsReadOnly)
            {
                bool conflict = false;
                if (TryEnQueue(items, out conflict))
                {
                    Interlocked.Decrement(ref m_Updateing);
                    return true;
                }

                if (!conflict)
                {
                    break;
                }
            }
            Interlocked.Decrement(ref m_Updateing);
            return false;
        }

        private bool TryEnQueue(IList<ArraySegment<byte>> items, out bool conflict)
        {
            conflict = false;
            int oldCurrent = mCurrentQueueSize;
            if (oldCurrent + items.Count >= this.mMaxQueueSize)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref mCurrentQueueSize, oldCurrent + items.Count, oldCurrent) != oldCurrent)
            {
                conflict = true;
                return false;
            }

            for (int i = 0; i < items.Count; i++)
            {
                ArraySegment<byte> item = items[i];
                var gcHandle = GCHandle.Alloc(item.Array, GCHandleType.Pinned);
                mPins[oldCurrent+i] = gcHandle;
                this.mBufs[oldCurrent+i] = UVIntrop.buf_init(gcHandle.AddrOfPinnedObject() + item.Offset, item.Count);
            }
            return true;
        }
        
        public unsafe void Send(UVStreamHandle stream,Action<UVRequest,Int32,UVException,object> callback,object state)
        {
            try
            {
                this.mCallback = callback;
                this.mState = state;
                UVIntrop.write(this, stream, this.mBufs, this.mCurrentQueueSize, mWritecb);
            }
            catch
            {
                this.mCallback = null;
                this.mState = null;
                UnpinGCHandles();
                throw;
            }
        }
        
        // Safe handle has instance method called Unpin
        // so using UnpinGcHandles to avoid conflict
        internal void UnpinGCHandles()
        {            
            for (var i = 0; i < this.mCurrentQueueSize; i++)
            {
                this.mPins[i].Free();
                this.mPins[i] = default(GCHandle);
            }
        }

        private static void WriteCallback(IntPtr reqHandle, Int32 status)
        {
            var req = FromIntPtr<UVRequest>(reqHandle);
            var callback = req.mCallback;
            var state = req.mState;


            UVException error = null;
            if (status < 0)
            {
                UVIntrop.Check(status, out error);
            }

            try
            {
                callback(req, status, error, state);
            }
            catch (Exception ex)
            {
                Trace.Error("UvWriteCb", ex);
                throw;
            }
        }

        #region IList

        public UVIntrop.uv_buf_t this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index 小于0");
                }
                
                var value = this.mBufs[index];
                return value;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public int IndexOf(UVIntrop.uv_buf_t item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, UVIntrop.uv_buf_t item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                return this.mCurrentQueueSize;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return this.m_IsReadOnly;
            }
        }


        public void Add(UVIntrop.uv_buf_t item)
        {
            throw new NotSupportedException();
        }



        public void Clear()
        {
            this.UnpinGCHandles();
            this.mCallback = null;
            this.mState = null;

            this.mCurrentQueueSize = 0;

        }

        public bool Contains(UVIntrop.uv_buf_t item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(UVIntrop.uv_buf_t[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public bool Remove(UVIntrop.uv_buf_t item)
        {
            throw new NotImplementedException();
        }


        public unsafe IEnumerator<UVIntrop.uv_buf_t> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }



        #endregion


    }
}
