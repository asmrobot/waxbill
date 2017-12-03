using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill
{
    public enum CloseReason
    {
        /// <summary>
        /// 其它线程正在关闭
        /// </summary>
        Closeing = 0,

        /// <summary>
        /// 内部错误
        /// </summary>
        InernalError = 1,

        /// <summary>
        /// 远程连接关闭
        /// </summary>
        RemoteClose = 2,

        /// <summary>
        /// 主动关闭
        /// </summary>
        Shutdown = 3,

        /// <summary>
        /// 未知原因
        /// </summary>
        Unknow = 4,

        /// <summary>
        /// 异常
        /// </summary>
        Exception = 5,

        /// <summary>
        /// 协议错误
        /// </summary>
        ProtocolError = 6
    }
}
