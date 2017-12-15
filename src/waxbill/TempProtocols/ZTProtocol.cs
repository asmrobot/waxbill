using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Utils;

namespace waxbill.Protocols
{
    public class ZTProtocol:FixedHeaderProtocol
    {
        public ZTProtocol():base(6)
        {}

        /// <summary>
        /// 是否协议开始
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public override bool IsStart(byte[] datas)
        {
            if (datas[0] != 0x0d || datas[1] != 0x0a)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 通信长度，不包含协议头的长度
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public override int GetSize(byte[] datas)
        {
            return NetworkBitConverter.ToInt32(datas, 2);
        }
    }
}
