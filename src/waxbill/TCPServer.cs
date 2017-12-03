using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using waxbill.Libuv;
using waxbill.Utils;

namespace waxbill
{
    public class TCPServer<TSession>:TCPMonitor where TSession:SocketSession,new()
    {
        public TCPServer(IProtocol protocol,string ip,Int32 port):this(protocol,ip,port,ServerOption.Define)
        {}

        public TCPServer(IProtocol protocol,string ip, Int32 port, ServerOption option):base(protocol,option)
        {
            Validate.ThrowIfZeroOrMinus(port, "端口号不正确");
            

            this.LocalIP = ip;
            if (string.IsNullOrEmpty(ip))
            {
                this.LocalIP = "0.0.0.0";
            }

            this.LocalPort = port;
            this.Listener = new TCPListener(this.LocalIP, this.LocalPort);
            this.Listener.OnStartSession += Listener_OnStartSession;
            this.LoopHandle = new UVLoopHandle();
            this.LoopHandle.Init();

            this.ServerHandle = new UVTCPHandle();
            this.ServerHandle.Init(this.LoopHandle);
        }



        public string LocalIP { get; private set; }

        public Int32 LocalPort { get; private set; }

        public TCPListener Listener { get; private set; }

        public UVTCPHandle ServerHandle { get; private set; }

        public UVLoopHandle LoopHandle { get; private set; }


        public void Start()
        {
            this.Listener.Start(this.Option.ListenBacklog,this.ServerHandle,this.LoopHandle);
            this.LoopHandle.Start();
        }

        public void Stop()
        {
            this.Listener.Stop();
        }
        
        private void Listener_OnStartSession(UVTCPHandle client)
        {
            SocketSession session = new TSession();
            session.Init(client,this,this.Option);
            session.RaiseAccept();
            client.ReadStart(session.AllocMemoryCallback, session.ReadCallback, session,session);
        }
    }
}
