using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Libuv
{
    public class UVRequest : UVMemory
    {
        private static UVIntrop.uv_write_cb mWritecb = WriteCallback;
        private const Int32 BUFFER_COUNT = 4;
        private IntPtr mBufs;
        private object mState;
        private Action<UVRequest, Int32, UVException, object> mCallback;
        private List<GCHandle> _pins = new List<GCHandle>(BUFFER_COUNT + 1);

        public UVRequest() : base(GCHandleType.Normal)
        { }




        public void Init()
        {
            Int32 requestSize = UVIntrop.req_size(UVIntrop.RequestType.WRITE);
            var bufferSize = Marshal.SizeOf(typeof(UVIntrop.uv_buf_t)) * BUFFER_COUNT;
            CreateMemory(requestSize + bufferSize);
            this.mBufs = this.handle + requestSize;
        }

        protected override bool ReleaseHandle()
        {
            DestroyMemory(handle);
            handle = IntPtr.Zero;
            return true;
        }


        public void Write(UVStreamHandle handle, ArraySegment<ArraySegment<byte>> bufs, Action<UVRequest, Int32, UVException, object> callback, object state)
        {
            WriteArraySegmentInternal(handle, bufs, null, callback, state);
        }

        unsafe public void WriteArraySegmentInternal(
            UVStreamHandle handle,
            ArraySegment<ArraySegment<byte>> bufs,
            UVStreamHandle sendHandle,
            Action<UVRequest,Int32,UVException,object> callback,
            object state)
        {
            try
            {
                var pBuffers = (UVIntrop.uv_buf_t*)mBufs;
                Int32 nBuffers = bufs.Count;
                if (nBuffers > BUFFER_COUNT)
                {
                    // create and pin buffer array when it's larger than the pre-allocated one
                    var bufArray = new UVIntrop.uv_buf_t[nBuffers];
                    var gcHandle = GCHandle.Alloc(bufArray, GCHandleType.Pinned);
                    _pins.Add(gcHandle);
                    pBuffers = (UVIntrop.uv_buf_t*)gcHandle.AddrOfPinnedObject();
                }

                for (var index = 0; index < nBuffers; index++)
                {
                    // create and pin each segment being written
                    var buf = bufs.Array[bufs.Offset + index];

                    var gcHandle = GCHandle.Alloc(buf.Array, GCHandleType.Pinned);
                    _pins.Add(gcHandle);
                    pBuffers[index] = UVIntrop.buf_init(
                        gcHandle.AddrOfPinnedObject() + buf.Offset,
                        buf.Count);
                }

                this.mCallback = callback;
                this.mState = state;

                if (sendHandle == null)
                {
                    UVIntrop.write(this, handle, pBuffers, nBuffers, mWritecb);
                }
                else
                {
                    UVIntrop.write2(this, handle, pBuffers, nBuffers, sendHandle, mWritecb);
                }
            }
            catch
            {
                this.mCallback = null;
                this.mState = null;
                UnpinGcHandles();
                throw;
            }
        }



        // Safe handle has instance method called Unpin
        // so using UnpinGcHandles to avoid conflict
        private void UnpinGcHandles()
        {
            var pinList = _pins;
            var count = pinList.Count;
            for (var i = 0; i < count; i++)
            {
                pinList[i].Free();
            }
            pinList.Clear();
        }





        private static void WriteCallback(IntPtr reqHandle, Int32 status)
        {
            var req = FromIntPtr<UVRequest>(reqHandle);
            req.UnpinGcHandles();

            var callback = req.mCallback;
            req.mCallback = null;

            var state = req.mState;
            req.mState = null;

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
    }
}
