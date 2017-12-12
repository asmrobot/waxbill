using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Utils;

namespace waxbill
{
    public interface IProtocol
    {
        /// <summary>
        /// 数据转化协议数据包
        /// </summary>
        /// <param name="packet">之前的数据</param>
        /// <param name="datas">本次数据</param>
        /// <param name="nread">本次可以最长读取的长度</param>
        /// <param name="giveupCount">本次读取长度,</param>
        /// <returns>是否读完一条信息</returns>
        bool TryToPacket(ref Packet packet,IntPtr datas,Int32 count, out int giveupCount);
    }
}
