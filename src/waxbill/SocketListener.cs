using System;
using System.Net;
using System.Net.Sockets;
using waxbill.Sessions;
using waxbill.Utils;

namespace waxbill
{
    internal class SocketListener<TSession> where TSession: SessionBase, new()
    {
        private IPEndPoint _EndPoint;
        private SocketAsyncEventArgs mSAE;
        private Socket mSocket;
        private TCPServer<TSession> mSocketServer;

        internal SocketListener(IPEndPoint endpoint, TCPServer<TSession> server)
        {
            Preconditions.ThrowIfNull(endpoint, "endpoint");
            this._EndPoint = endpoint;
            this.mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.mSAE = new SocketAsyncEventArgs();
            this.mSAE.Completed += new EventHandler<SocketAsyncEventArgs>(this.AcceptComplete);
            this.mSocketServer = server;
        }

        private void AcceptComplete(object sender, SocketAsyncEventArgs e)
        {
            Socket acceptSocket = e.AcceptSocket;
            if (e.SocketError != SocketError.Success)
            {
                acceptSocket = null;
            }
            e.AcceptSocket = null;
            if (acceptSocket != null)
            {
                TSession session = Activator.CreateInstance<TSession>();
                session.Initialize(acceptSocket, this.mSocketServer);
                this.mSocketServer.Accept(session);
            }
            this.InternalAccept();
        }

        private void InternalAccept()
        {
            if (this.mSocket != null)
            {
                bool flag = true;
                try
                {
                    flag = this.mSocket.AcceptAsync(this.mSAE);
                }
                catch (Exception exception)
                {
                    Trace.Error("接收出错", exception);
                    this.Stop();
                }
                if (!flag)
                {
                    this.AcceptComplete(this, this.mSAE);
                }
            }
        }

        public void Start()
        {
            this.mSocket.Bind(this._EndPoint);
            this.mSocket.Listen(this.mSocketServer.Config.MaxBlockSize);
            this.InternalAccept();
        }

        public void Stop()
        {
            if (this.mSocket != null)
            {
                this.mSocket.Close();
                this.mSocket = null;
            }
        }
    }
}

