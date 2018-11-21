using System;
using System.Net;
using System.Net.Sockets;
using waxbill.Sessions;
using waxbill.Utils;

namespace waxbill
{
    internal class SocketListener<TSession> where TSession: SessionBase, new()
    {
        private IPEndPoint mineEnndPoint;
        private SocketAsyncEventArgs connectSAE;
        private Socket socket;
        private TCPServer<TSession> tcpServer;

        internal SocketListener(IPEndPoint endpoint, TCPServer<TSession> server)
        {
            Preconditions.ThrowIfNull(endpoint, "endpoint");
            this.mineEnndPoint = endpoint;
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.connectSAE = new SocketAsyncEventArgs();
            this.connectSAE.Completed += new EventHandler<SocketAsyncEventArgs>(this.AcceptComplete);
            this.tcpServer = server;
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
                session.Initialize(acceptSocket, this.tcpServer);
                this.tcpServer.Accept(session);
            }
            this.InternalAccept();
        }

        private void InternalAccept()
        {
            if (this.socket != null)
            {
                bool flag = true;
                try
                {
                    flag = this.socket.AcceptAsync(this.connectSAE);
                }
                catch (Exception exception)
                {
                    Trace.Error("接收出错", exception);
                    this.Stop();
                }
                if (!flag)
                {
                    this.AcceptComplete(this, this.connectSAE);
                }
            }
        }

        public void Start()
        {
            this.socket.Bind(this.mineEnndPoint);
            this.socket.Listen(this.tcpServer.Option.MaxBlockSize);
            this.InternalAccept();
        }

        public void Stop()
        {
            if (this.socket != null)
            {
                this.socket.Close();
                this.socket = null;
            }
        }
    }
}

