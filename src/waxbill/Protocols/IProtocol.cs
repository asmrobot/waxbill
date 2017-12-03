using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Utils;

namespace waxbill
{
    public interface IProtocol
    {
        /// <summary>
        /// 读取数据转化信息
        /// </summary>
        /// <param name="packet">之前的数据</param>
        /// <param name="datas">本次数据</param>
        /// <param name="readlen">本次读取长度</param>
        /// <returns>是否读完一条信息</returns>
        bool TryToMessage(ref Packet packet,ArraySegment<byte> datas, out int readlen);
    }
}
