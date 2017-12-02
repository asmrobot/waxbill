using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using waxbill.Tools;

namespace waxbill
{
    public class TCPServer
    {
        public TCPServer(string ip,Int32 port):this(ip,port,ServerOption.Define)
        {}

        public TCPServer(string ip, Int32 port, ServerOption option)
        {
            Validate.ThrowIfZeroOrMinus(port, "端口号不正确");
            Validate.ThrowIfNull(option, "服务配置参数不正确");

            
            //转化ip
            if (string.IsNullOrEmpty(ip))
            {
                this.Local = IPAddress.Any;
            }
            else
            {
                IPAddress address;
                if (IPAddress.TryParse(ip, out address))
                {
                    this.Local = address;
                }
                else
                {
                    throw new waxbill.Exceptions.FormatException("地址格式不正确");
                }
            }

            this.Listener = new TCPListener(new IPEndPoint(this.Local, this.Port));
            this.Listener.OnStartSession += Listener_OnStartSession;
        }

        

        public void Start()
        {
            this.Listener.Start();
        }

        public void Stop()
        {
            this.Listener.Stop();
        }
        


        public IPAddress Local { get; private set; }

        public Int32 Port { get; private set; }

        public TCPListener Listener { get; private set; }

        private void Listener_OnStartSession(int sessionid)
        {
            Console.WriteLine("connection ok");
        }



    }
}
