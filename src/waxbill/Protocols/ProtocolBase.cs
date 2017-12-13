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
        private Int32 m_HeaderSize;
        public Int32 HeaderSize
        {
            get
            {
                return m_HeaderSize;
            }
            protected set
            {
                m_HeaderSize = value;
            }
        }

        public ProtocolBase(Int32 headerSize)
        {
            this.m_HeaderSize = headerSize;
        }
        
        public bool TryToPacket(ref Packet packet, IntPtr memory, int len, out int readlen)
        {
            bool reset = false;
            readlen = 0;

            //处理开始
            if (!packet.IsStart)
            {
                if (!ParseStart(packet, memory,len, out reset))
                {
                    readlen = len;
                    if (reset)
                    {
                        packet.Clear();
                    }
                    else
                    {
                        packet.Write(memory,len);
                    }
                    return false;
                }
                packet.IsStart = true;
            }


            readlen = IndexOfProtocolEnd(packet, memory,len, out reset);
            if (readlen < 0)
            {
                readlen = len;
                if (reset)
                {
                    packet.Clear();
                }
                else
                {
                    packet.Write(memory,len);
                }
                return false;
            }

            packet.Write(memory, readlen);
            return true;
        }

        /// <summary>
        /// 是否成功解析开始
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="datas"></param>
        /// <param name="readlen"></param>
        /// <returns></returns>
        public abstract bool ParseStart(Packet packet, IntPtr datas,Int32 count, out bool reset);
        
        /// <summary>
        /// 解析结束
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="datas"></param>
        /// <param name="reset"></param>
        /// <returns>没有查找到结束，则返回-1</returns>
        public abstract Int32 IndexOfProtocolEnd(Packet packet, IntPtr datas,Int32 count, out bool reset);
        
    }
}
