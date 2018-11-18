using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Utils;

namespace waxbill.Protocols
{
    public class BeginEndMarkProtocol : ProtocolBase
    {
        private byte mBeginChars = 0;
        private byte mEndChars = 0;

        public BeginEndMarkProtocol(byte begin,byte end):base(1)
        {
            if (begin<=0||end <=0)
            {
                throw new ArgumentNullException("begin or end");
            }
            this.mBeginChars = begin;
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
        protected override bool ParseHeader(Packet packet,ArraySegment<byte> datas)
        {
            if (datas.Array[datas.Offset] == mBeginChars)
            {
                packet.Write(new ArraySegment<byte>(datas.Array,datas.Offset, 1));
                return true;
            }
            return false;
        }


        protected override bool ParseBody(Packet packet, ArraySegment<byte> datas, out int giveupCount)
        {
            giveupCount = 0;
            if (datas.Count <= 0)
            {
                return false;
            }
            
            while (giveupCount < datas.Count&& datas.Array[datas.Offset+ giveupCount] != this.mEndChars)
            {
                giveupCount++;
            }

            bool result = false;
            if (giveupCount < datas.Count)
            {
                giveupCount++;
                result = true;                
            }
            packet.Write(new ArraySegment<byte>(datas.Array,datas.Offset,giveupCount));
            return result;
        }
        

    }
}
