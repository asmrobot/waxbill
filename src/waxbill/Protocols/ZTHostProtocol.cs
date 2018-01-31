using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Utils;

namespace waxbill.Protocols
{
    public class ZTHostProtocol :ZTProtocol
    {
        /// <summary>
        /// 通信长度，不包含协议头的长度
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public override int GetSize(byte[] datas)
        {
            return BitConverter.ToInt32(datas, 2);
        }
        
    }
}
