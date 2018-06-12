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
        protected unsafe override bool ParseHeader(Packet packet,IntPtr datas)
        {
            return true;
        }


        protected unsafe override bool ParseBody(Packet packet, IntPtr datas, int count, out int giveupCount)
        {
            giveupCount = 0;
            if (count <= 0)
            {
                return false;
            }
            byte* memory = (byte*)datas;
            while (giveupCount < count&& *memory != this.mEndChars)
            {
                giveupCount++;
                memory++;
            }

            bool result = false;
            if (giveupCount < count)
            {
                giveupCount++;
                result = true;                
            }
            packet.Write(datas, giveupCount);
            return result;
        }
        
    }
}
