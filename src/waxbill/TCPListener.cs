using System;
using System.Collections.Concurrent;
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
        private UVTCPHandle mServerHandle = null;
        private UVLoopHandle mLoopHandle = null;

        private long mConnectionIncremer = 0;
        
        public delegate void NewSession(Int64 connectionID,UVTCPHandle client);

        /// <summary>
        /// new session event
        /// </summary>
        public event NewSession OnStartSession;
        private void RaiseStartSession(Int64 connectionID,UVTCPHandle client)
        {
            if (OnStartSession != null)
            {
                OnStartSession(connectionID,client);
            }
        }
        
        public TCPListener(string localIP,Int32 localPort)
        {
            Validate.ThrowIfNull(localIP, "endpoint不能为空");
            Validate.ThrowIfZeroOrMinus(localPort, "端口号不正确");


            this.mIP = localIP;
            this.mPort = localPort;


            this.mLoopHandle = new UVLoopHandle();
            this.mServerHandle = new UVTCPHandle(this.mLoopHandle);
        }


        public void Start(Int32 backlog)
        {
            try
            {
                this.mServerHandle.Bind(this.mIP, this.mPort);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
            this.mServerHandle.Listen(backlog, OnConnection, this);
            this.mLoopHandle.AsyncStart();
        }

        public void Stop()
        {
            this.mServerHandle.Close();                
            this.mLoopHandle.Stop();
            this.mLoopHandle.Close();
        }

        private void OnConnection(UVStreamHandle stream, Int32 status, UVException ex, object state)
        {
            if (ex != null)
            {
                Trace.Error("connection error", ex);
                return;
            }
            TCPListener listener = state as TCPListener;
            if (listener == null)
            {
                return;
            }

            UVTCPHandle client = new UVTCPHandle(listener.mLoopHandle);

            try
            {
                stream.Accept(client);

                Int64 connectionID= Interlocked.Increment(ref this.mConnectionIncremer);
                RaiseStartSession(connectionID,client);
            }
            catch(Exception wex)
            {
                Trace.Error("accept error",wex);
                client.Dispose();
            }
        }
        



    }
}
