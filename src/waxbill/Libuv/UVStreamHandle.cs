using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using waxbill.Exceptions;

namespace waxbill.Libuv
{

    public class UVStreamHandle : UVHandle
    {

        private readonly static UVIntrop.uv_connection_cb mOnConnection = UVConnectionCb;
        private GCHandle mStreamGCHandle;
        private Action<UVStreamHandle, Int32, UVException, Object> mConnectionCallback;
        private object mCallbackState;

        public void Listen(int backlog,Action<UVStreamHandle,Int32 ,UVException,Object> callback,object state)
        {
            if (mStreamGCHandle.IsAllocated)
            {
                throw new CanotRepeatException("只能监听一次端口 ");
            }
            try
            {
                mCallbackState = state;
                mConnectionCallback = callback;
                mStreamGCHandle = GCHandle.Alloc(this, GCHandleType.Normal);
                UVIntrop.listen(this, backlog, mOnConnection);
            }
            catch
            {
                mConnectionCallback = null;
                mCallbackState = null;
                if (mStreamGCHandle.IsAllocated)
                {
                    mStreamGCHandle.Free();
                }
            }
            
        }

        public void Accept(UVStreamHandle handle)
        {
            UVIntrop.accept(this, handle);
        }

        public void ReadStart()
        {

        }

        public void ReadStop()
        {

        }


        public void WriteStart()
        {

        }



        protected override bool ReleaseHandle()
        {
            this.mCallbackState = null;
            this.mConnectionCallback = null;
            if (this.mStreamGCHandle.IsAllocated)
            {
                this.mStreamGCHandle.Free();
            }
            return base.ReleaseHandle();
        }

        #region cb
        private static void UVConnectionCb(IntPtr server, Int32 status)
        {
            UVIntrop.Check(status, out var error);

            UVStreamHandle stream = UVMemory.FromIntPtr<UVStreamHandle>(server);
            try
            {
                stream.mConnectionCallback(stream, status, error, stream.mCallbackState);
            }
            catch (Exception ex)
            {
                throw;
            }
            
        }


        private static void UVAllowCb(IntPtr handle, int suggestedSize, out UVIntrop.uv_buf_t buf)
        {
            buf = new UVIntrop.uv_buf_t();
        }

        private static void UVReadCb(IntPtr handle, int status, ref UVIntrop.uv_buf_t buf)
        {

        }
        #endregion
    }
}
