using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Exceptions;
using waxbill.Tools;

namespace waxbill
{
    public class TCPListener
    {
        private IPEndPoint mEndPoint;
        private Int32 mState = 0;

        public delegate void NewSession(Int32 sessionid);
        

        /// <summary>
        /// new session event
        /// </summary>
        public event NewSession OnStartSession;
        

        public TCPListener(IPEndPoint endpoint)
        {
            Validate.ThrowIfNull(endpoint, "endpoint不能为空");
            this.mEndPoint = endpoint;
        }


        public void Start()
        {
            if (Interlocked.CompareExchange(ref mState, 1, 0) == 0)
            {
                //真正的开始
            }

            throw new CanotRepeatException("不可重复开始");
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref mState, 0, 1) == 1)
            {
                //真正的关闭
            }
        }


        
    }
}
