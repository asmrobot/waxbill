using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Utils;

namespace waxbill.Protocols
{
    /// <summary>
    /// 固定长度协议
    /// </summary>
    public class FixedLengthProtocol:ProtocolBase
    {
        private int m_Length;

        public int Length
        {
            get
            {
                return m_Length;
            }
        }

        public FixedLengthProtocol(int length):base(0)
        {
            this.m_Length = length;
        }

        /// <summary>
        /// 是否成功解析开始
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="datas"></param>
        /// <param name="readlen"></param>
        /// <returns></returns>
        public override bool ParseStart(Packet packet, IntPtr datas, int count, out bool reset)
        {
            reset = false;
            packet.ForecastSize = this.m_Length;
            return true;
        }


        /// <summary>
        /// 解析结束
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="datas"></param>
        /// <param name="readlen"></param>
        /// <returns></returns>
        public override int IndexOfProtocolEnd(Packet packet, IntPtr datas, int count, out bool reset)
        {
            reset = false;
            if (packet.Count + count >= this.m_Length)
            {
                return this.m_Length - (Int32)packet.Count;
            }
            return -1;
        }
    }
}
