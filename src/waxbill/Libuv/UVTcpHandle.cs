using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using waxbill.Libuv.Collections;

namespace waxbill.Libuv
{
    public class UVTCPHandle : UVStreamHandle
    {

        public UVTCPHandle()
        {

        }

        public void Init(UVLoopHandle loop)
        {
            CreateHandle(UVIntrop.HandleType.TCP);
            UVIntrop.tcp_init(loop, this);
        }

        public void Bind(string ip,Int32 port)
        {
            UVException exception;
            SockAddr addr;
            UVIntrop.ip4_addr("0.0.0.0", 12308, out addr, out exception);
            if (exception != null)
            {
                throw exception;
            }

            UVIntrop.tcp_bind(this, ref addr, 0);
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
                    throw;
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
                    throw;
                }
                return addr.GetIPEndPoint();
            }
        }

    }
}
