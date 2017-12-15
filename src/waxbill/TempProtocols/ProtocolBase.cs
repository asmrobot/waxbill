using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Utils;
namespace waxbill.Protocols
{
    public abstract class ProtocolBase:IProtocol
    {
        /// <summary>
        /// 解析开始
        /// </summary>
        /// <param name="packet">数据包</param>
        /// <param name="datas">数据</param>
        /// <param name="count">数据长度</param>
        /// <param name="giveupCount">丢弃长度</param>
        /// <returns></returns>
        protected abstract bool ParseStart(Packet packet, IntPtr datas,Int32 count,out Int32 giveupCount);
        
        /// <summary>
        /// 解析结束
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="datas"></param>
        /// <param name="reset"></param>
        /// <returns>没有查找到结束，则返回-1</returns>
        protected abstract Int32 IndexOfProtocolEnd(Packet packet, IntPtr datas,Int32 count, out bool reset);



        #region implements
        public bool TryToPacket(Packet packet, IntPtr memory, int len, out int giveupCount)
        {
            bool reset = false;
            giveupCount = 0;

            //处理开始
            if (!packet.IsStart)
            {
                if (!ParseStart(packet, memory, len, out giveupCount))
                {
                    if (giveupCount > len)
                    {
                        giveupCount = len;
                    }
                    return false;
                }
                packet.IsStart = true;
            }


            giveupCount = IndexOfProtocolEnd(packet, memory, len, out reset);
            if (giveupCount < 0)
            {
                giveupCount = len;
                if (reset)
                {
                    packet.Reset();
                }
                else
                {
                    packet.Write(memory, len);
                }
                return false;
            }

            packet.Write(memory, giveupCount);
            return true;
        }
        
        public Packet CreatePacket(BufferManager buffer)
        {
            return new Packet(buffer);
        }
        #endregion
    }
}
