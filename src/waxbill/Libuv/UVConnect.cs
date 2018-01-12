using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Libuv.Collections;
using waxbill.Utils;
using ZTImage.Log;

namespace waxbill.Libuv
{
    public unsafe class UVConnect : UVMemory
    {
        private static UVIntrop.uv_connect_cb mConnectcb = ConnectCallback;
        private object mState;
        private Action<UVConnect, Int32, UVException, object> mCallback;
        

        public UVConnect() : base(GCHandleType.Normal)
        {
            Int32 requestSize = UVIntrop.req_size(UVIntrop.RequestType.CONNECT);
            CreateMemory(requestSize);
        }

        public void Connect(UVTCPHandle tcp,string ip,Int32 port, Action<UVConnect, Int32, UVException, object> callback,object state)
        {
            this.mCallback = callback;
            this.mState = state;
            UVException ex;
            SockAddr addr;
            UVIntrop.ip4_addr(ip, port, out addr, out ex);
            if (ex != null)
            {
                throw ex;
            }

            UVIntrop.tcp_connect(this, tcp, ref addr, mConnectcb);
        }

        
        
        protected override bool ReleaseHandle()
        {
            DestroyMemory(handle);
            handle = IntPtr.Zero;
            return true;
        }
        
        private static void ConnectCallback(IntPtr reqHandle, Int32 status)
        {
            var req = FromIntPtr<UVConnect>(reqHandle);
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
                Trace.Error("UvConnectCb", ex);
                throw;
            }
        }
    }
}
