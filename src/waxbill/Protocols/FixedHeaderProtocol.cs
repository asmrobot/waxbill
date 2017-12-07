using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using waxbill.Utils;

namespace waxbill.Protocols
{
    /// <summary>
    /// 有头的二进制协议
    /// </summary>
    public abstract class FixedHeaderProtocol : ProtocolBase
    {
        public FixedHeaderProtocol(int headerSize):base(headerSize)
        {
            if (headerSize < 1)
            {
                throw new ArgumentNullException("headersize");
            }
        }

        public unsafe override bool ParseStart(Packet packet, IntPtr datas, int count, out bool reset)
        {
            reset = false;
            if (packet.Count + count < this.HeaderSize)
            {
                return false;
            }

            //todo:临时头部大小
            byte[] temp = new byte[this.HeaderSize];
            if (packet.Count > 0)
            {
                //合并
                int min = Math.Min(this.HeaderSize, packet.Count);
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
                //Buffer.BlockCopy(datas.Array, datas.Offset, temp, 0, this.HeaderSize);
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


        ///// <summary>
        ///// 是否成功解析开始
        ///// </summary>
        ///// <param name="packet"></param>
        ///// <param name="datas"></param>
        ///// <param name="readlen"></param>
        ///// <returns></returns>
        //public override bool ParseStart(Packet packet, ArraySegment<byte> datas, out bool reset)
        //{
        //    reset = false;
        //    if (packet.Count+datas.Count < this.HeaderSize)
        //    {
        //        return false;
        //    }

        //    //todo:临时头部大小
        //    byte[] temp = new byte[this.HeaderSize];
        //    if (packet.Count > 0)
        //    {
        //        //合并
        //        int min = Math.Min(this.HeaderSize, packet.Count);
        //        for (int i = 0; i < min; i++)
        //        {
        //            temp[i] = packet[i];
        //        }

        //        if (packet.Count < this.HeaderSize)
        //        {
        //            for (int i = 0; i < this.HeaderSize - min; i++)
        //            {
        //                temp[min + i] = datas.Array[datas.Offset + i];
        //            }
        //        }
        //    }
        //    else
        //    {
        //        Buffer.BlockCopy(datas.Array, datas.Offset, temp, 0, this.HeaderSize);
        //    }

        //    if (!IsStart(temp))
        //    {
        //        reset = true;
        //        return false;
        //    }

        //    packet.ForecastSize = GetSize(temp);
        //    if (packet.ForecastSize <= 0)
        //    {
        //        reset = true;
        //        return false;
        //    }
        //    packet.ForecastSize += this.HeaderSize;
        //    return true;
        //}

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


        public override int IndexOfProtocolEnd(Packet packet, IntPtr datas, int count, out bool reset)
        {
            reset = false;
            if (packet.Count + count >= packet.ForecastSize)
            {
                return packet.ForecastSize - packet.Count;
            }
            return -1;

        }


        ///// <summary>
        ///// 解析结束
        ///// </summary>
        ///// <param name="packet"></param>
        ///// <param name="datas"></param>
        ///// <param name="readlen"></param>
        ///// <returns></returns>
        //public override Int32 IndexOfProtocolEnd(Packet packet, ArraySegment<byte> datas, out bool reset)
        //{
        //    reset = false;
        //    if (packet.Count + datas.Count >= packet.ForecastSize)
        //    {
        //        return packet.ForecastSize - packet.Count;
        //    }
        //    return -1;
        //}

    }
}
