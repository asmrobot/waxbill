﻿using System;
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
using ZTImage.Log;

namespace waxbill
{

    internal delegate void ListenerConnect(Int64 connectionID, UVTCPHandle client);
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
        public event ListenerConnect OnListenerConnect;

        private void RaiseOnlistenerConnect(Int64 connectionID,UVTCPHandle client)
        {
            if (OnListenerConnect != null)
            {
                OnListenerConnect(connectionID,client);
            }
        }
        
        public TCPListener()
        {
            this.mLoopHandle = new UVLoopHandle();
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
                RaiseOnlistenerConnect(connectionID,client);
            }
            catch(Exception wex)
            {
                Trace.Error("accept error",wex);
                client.Dispose();
            }
        }
        



    }
}
