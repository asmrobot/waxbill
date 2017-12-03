using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Exceptions;
using waxbill.Libuv;
using waxbill.Utils;

namespace waxbill
{


    public class TCPListener
    {
        private string mIP;
        private Int32 mPort;
        private Int32 mState = 0;
        private UVTCPHandle mServerHandle = null;
        private UVLoopHandle mLoopHandle = null;

        public delegate void NewSession(UVTCPHandle client);

        /// <summary>
        /// new session event
        /// </summary>
        public event NewSession OnStartSession;
        private void RaiseStartSession(UVTCPHandle client)
        {
            if (OnStartSession != null)
            {
                OnStartSession(client);
            }

        }
        
        public TCPListener(string localIP,Int32 localPort)
        {
            Validate.ThrowIfNull(localIP, "endpoint不能为空");
            Validate.ThrowIfZeroOrMinus(localPort, "端口号不正确");


            this.mIP = localIP;
            this.mPort = localPort;
        }


        public void Start(Int32 backlog,UVTCPHandle serverHandle,UVLoopHandle loopHandle)
        {
            if (Interlocked.CompareExchange(ref mState, 1, 0) == 0)
            {
                this.mServerHandle = serverHandle;
                this.mLoopHandle = loopHandle;
                try
                {
                    this.mServerHandle.Bind(this.mIP, this.mPort);
                }
                catch (Exception ex)
                {
                    this.mState = 0;
                    throw ex;
                }
                //真正的开始
                this.mServerHandle.Listen(backlog, OnConnection, this);
                return;

            }

            throw new CanotRepeatException("不可重复开始");
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref mState, 0, 1) == 1)
            {
                //真正的关闭
                this.mServerHandle.Close();
            }
        }

        private void OnConnection(UVStreamHandle stream, Int32 status, UVException ex, object state)
        {
            TCPListener listener = state as TCPListener;
            if (listener == null)
            {
                return;
            }

            UVTCPHandle client = new UVTCPHandle();
            client.Init(listener.mLoopHandle);

            try
            {
                stream.Accept(client);
                RaiseStartSession(client);
            }
            catch
            {
                Console.WriteLine("accept error");
                client.Dispose();
            }
        }
    }
}
