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

        public TCPServer(string ip,Int32 port,ServerOption option)
        {
            Validate.ThrowIfNullOrWhite(ip, "ip地址不正确");
            Validate.ThrowIfZeroOrMinus(port, "端口号不正确");
            Validate.ThrowIfNull(option, "服务配置参数不正确");

            //转化ip
            

        }

        
    }
}
