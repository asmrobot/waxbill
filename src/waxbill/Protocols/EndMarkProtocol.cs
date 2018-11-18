using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Utils;

namespace waxbill.Protocols
{
    public class EndMarkProtocol : ProtocolBase
    {
        private byte mEndChars = 0;

        public EndMarkProtocol(byte end):base(0)
        {
            if (end <=0)
            {
                throw new ArgumentNullException("end");
            }
            this.mEndChars = end;
        }
        
        /// <summary>
        /// 解析开始标记
        /// 注：如果开始标记不匹配，所有数据将会丢弃
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="datas"></param>
        /// <param name="count"></param>
        /// <param name="giveupCount"></param>
        /// <returns></returns>
        protected unsafe override bool ParseHeader(Packet packet,ArraySegment<byte> datas)
        {
            return true;
        }


        protected unsafe override bool ParseBody(Packet packet, ArraySegment<byte> datas, out int giveupCount)
        {
            giveupCount = 0;
            if (datas.Count <= 0)
            {
                return false;
            }
            
            while (giveupCount < datas.Count&& datas.Array[datas.Offset+giveupCount] != this.mEndChars)
            {
                giveupCount++;
            }

            bool result = false;
            if (giveupCount < datas.Count)
            {
                giveupCount++;
                result = true;                
            }
            packet.Write(new ArraySegment<byte>(datas.Array,datas.Offset, giveupCount));
            return result;
        }
        
    }
}
