using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Exceptions;
using ZTImage.Libuv;
using waxbill.Utils;

namespace waxbill
{
    internal class TCPListener
    {
        private string mIP;
        private Int32 mPort;
        private UVTCPHandle mServerHandle = null;
        private UVLoopHandle mLoopHandle = null;
        private long mConnectionIncremer = 0;
        
        /// <summary>
        /// new session event
        /// </summary>
        public Action<Int64,UVTCPHandle> OnNewConnected;

        
        public TCPListener()
        {
            //this.mLoopHandle = new UVLoopHandle();
            this.mLoopHandle = UVLoopHandle.Define;
            this.mServerHandle = new UVTCPHandle(this.mLoopHandle);
        }


        public void Start(string ip, Int32 port,Int32 backlog)
        {
            Validate.ThrowIfNull(ip, "endpoint不能为空");
            Validate.ThrowIfZeroOrMinus(port, "端口号不正确");
            
            this.mIP = ip;
            this.mPort = port;

            try
            {
                this.mServerHandle.Bind(this.mIP, this.mPort);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
            this.mServerHandle.Listen(backlog, OnConnection, this);
            this.mLoopHandle.AsyncStart((loop)=> {
                //todo:uv exits
            });
        }

        public void Stop()
        {
            this.mServerHandle.Close();                
            this.mLoopHandle.Stop();
            this.mLoopHandle.Close();
        }

        private void OnConnection(UVTCPHandle server, Int32 status, UVException ex, object state)
        {
            if (ex != null)
            {
                Trace.Error("connection error", ex);
                return;
            }
           
            UVTCPHandle client = new UVTCPHandle(this.mLoopHandle);

            try
            {
                server.Accept(client);
                Int64 connectionID= Interlocked.Increment(ref this.mConnectionIncremer);
                if (OnNewConnected != null)
                {
                    OnNewConnected(connectionID, client);
                }
            }
            catch(Exception wex)
            {
                Trace.Error("accept error",wex);
                client.Dispose();
            }
        }
        



    }
}
