using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Utils;
namespace waxbill.Protocols
{
    public abstract class ProtocolBase<TPacket>:IProtocol where TPacket:Packet
    {
        private Int32 mHeaderSize;
        public Int32 HeaderSize
        {
            get
            {
                return mHeaderSize;
            }
        }
        public ProtocolBase(int headerSize)
        {
            this.mHeaderSize = headerSize;
        }

        /// <summary>
        /// 解析头部
        /// 注：实现函数负责把头部信息保存下来
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="datas"></param>
        /// <param name="count"></param>
        /// <param name="giveupCount"></param>
        /// <returns></returns>
        protected unsafe abstract bool ParseHeader(TPacket packet, IntPtr datas);

        /// <summary>
        /// 解析消息体
        /// 注：调用数据不包含头部数据
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="datas"></param>
        /// <param name="count"></param>
        /// <param name="giveupCount"></param>
        /// <returns></returns>
        protected unsafe abstract bool ParseBody(TPacket packet, IntPtr datas, int count, out Int32 giveupCount);

        public unsafe bool TryToPacket(Packet fpacket, IntPtr datas, int count, out int giveupCount)
        {
            giveupCount = 0;
            if (count <= 0)
            {
                return false;
            }
            TPacket packet = fpacket as TPacket;
            if (packet == null)
            {
                giveupCount = count;
                return false;
            }

            byte* memory = (byte*)datas;
            if (!packet.IsStart)
            {
                if (mHeaderSize >0)
                {
                    if (count < this.mHeaderSize)
                    {
                        return false;
                    }

                    if (!ParseHeader(packet, datas))
                    {
                        giveupCount = count;
                        return false;
                    }
                    giveupCount=this.mHeaderSize;
                    datas = IntPtr.Add(datas, this.mHeaderSize);
                    count -= this.mHeaderSize;
                }
                packet.IsStart = true;
            }

            Int32 giveup = 0;
            bool endResult = ParseBody(packet, datas, count, out giveup);
            if (giveup > count)
            {
                giveupCount += count;
                return false;
            }
            giveupCount += giveup;
            return endResult;
        }

        public abstract Packet CreatePacket(BufferManager buffer);
    }
    
}
