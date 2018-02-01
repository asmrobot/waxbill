using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using waxbill.Exceptions;
using waxbill.Libuv.Collections;

namespace waxbill.Libuv
{
    public class UVTCPHandle : UVStreamHandle
    {
        private readonly static UVIntrop.uv_connection_cb mOnConnection = UVConnectionCb;
        private Action<UVTCPHandle, Int32, UVException, Object> mConnectionCallback;
        private object mConnectionCallbackState;
        
        public UVTCPHandle(UVLoopHandle loop)
        {
            CreateHandle(UVHandleType.TCP);
            UVIntrop.tcp_init(loop, this);
        }

        public void Bind(string ip, Int32 port)
        {
            UVException exception;
            SockAddr addr;
            UVIntrop.ip4_addr(ip, port, out addr, out exception);
            if (exception != null)
            {
                throw exception;
            }

            UVIntrop.tcp_bind(this, ref addr, 0);
        }

        public void Listen(int backlog, Action<UVTCPHandle, Int32, UVException, Object> connectionCallback, object state)
        {
            mConnectionCallbackState = state;
            mConnectionCallback = connectionCallback;
            UVIntrop.listen(this, backlog, mOnConnection);
        }


        public void Accept(UVTCPHandle handle)
        {
            UVIntrop.accept(this, handle);
        }

        public IPEndPoint LocalIPEndPoint
        {
            get
            {
                SockAddr addr = default(SockAddr);
                Int32 namelen = Marshal.SizeOf(addr);
                try
                {
                    UVIntrop.tcp_getsockname(this, out addr, ref namelen);
                }
                catch (UVException ex)
                {
                    throw ex;
                }
                return addr.GetIPEndPoint();
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                SockAddr addr = default(SockAddr);
                Int32 namelen = Marshal.SizeOf(addr);
                try
                {
                    UVIntrop.tcp_getpeername(this, out addr, ref namelen);
                }
                catch (UVException ex)
                {
                    throw ex;
                }
                return addr.GetIPEndPoint();
            }
        }


        public void NoDelay(bool enable)
        {
            UVIntrop.tcp_nodelay(this, enable);
        }

        protected override bool ReleaseHandle()
        {
            this.mConnectionCallbackState = null;
            this.mConnectionCallback = null;
            return base.ReleaseHandle();
        }

        //todo:重载各种write

        #region  Callback
        private static void UVConnectionCb(IntPtr server, Int32 status)
        {
            UVException error;
            UVIntrop.Check(status, out error);
            UVTCPHandle handle = UVMemory.FromIntPtr<UVTCPHandle>(server);
            try
            {
                handle.mConnectionCallback(handle, status, error, handle.mConnectionCallbackState);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

    }
}
