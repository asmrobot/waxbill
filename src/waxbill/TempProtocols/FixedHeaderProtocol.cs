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
    /// <summary>
    /// 有头的二进制协议
    /// </summary>
    public abstract class FixedHeaderProtocol : ProtocolBase
    {

        private Int32 mHeaderSize;
        public Int32 HeaderSize
        {
            get
            {
                return mHeaderSize;
            }
        }
        public FixedHeaderProtocol(int headerSize)
        {
            if (headerSize < 1)
            {
                throw new ArgumentNullException("headersize");
            }
            this.mHeaderSize = headerSize;
        }

        protected unsafe override bool ParseStart(Packet packet, IntPtr datas, int count, out Int32 giveupCount)
        {
            giveupCount = 0;
            if (packet.Count + count < this.HeaderSize)
            {
                return false;
            }

            //todo:临时头部大小
            byte[] temp = new byte[this.HeaderSize];
            if (packet.Count > 0)
            {
                //合并
                int min = Math.Min(this.HeaderSize, (Int32)packet.Count);
                for (int i = 0; i < min; i++)
                {
                    temp[i] = packet[i];
                }

                if (packet.Count < this.HeaderSize)
                {
                    byte* tmpDatas = (byte*)datas;
                    for (int i = 0; i < this.HeaderSize - min; i++)
                    {
                        
                        temp[min + i] = tmpDatas[i];
                    }
                }
            }
            else
            {
                Marshal.Copy(datas, temp, 0, this.HeaderSize);
            }

            if (!IsStart(temp))
            {
                reset = true;
                return false;
            }

            packet.ForecastSize = GetSize(temp);
            if (packet.ForecastSize <= 0)
            {
                reset = true;
                return false;
            }
            packet.ForecastSize += this.HeaderSize;
            return true;
        }
        
        /// <summary>
        /// 判断是否协议头
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public abstract bool IsStart(byte[] datas);

        /// <summary>
        /// 得到协议长度,不包含头
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="reset"></param>
        /// <returns></returns>
        public abstract Int32 GetSize(byte[] datas);


        protected override int IndexOfProtocolEnd(Packet packet, IntPtr datas, int count, out bool reset)
        {
            reset = false;
            if (packet.Count + count >= packet.ForecastSize)
            {
                return packet.ForecastSize - (Int32)packet.Count;
            }
            return -1;

        }
    }
}
