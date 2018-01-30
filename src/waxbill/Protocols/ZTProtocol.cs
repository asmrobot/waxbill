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
    public class ZTProtocol:ProtocolBase<ZTProtocolPacket>
    {
        public ZTProtocol():base(6)
        {}

        /// <summary>
        /// 是否协议开始
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public bool IsStart(byte[] datas)
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
        public virtual int GetSize(byte[] datas)
        {
            return NetworkBitConverter.ToInt32(datas, 2);
        }
        

        /// <summary>
        /// 解析头部
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="datas"></param>
        /// <param name="count"></param>
        /// <param name="giveupCount"></param>
        /// <returns></returns>
        protected unsafe override bool ParseHeader(ZTProtocolPacket packet, IntPtr datas)
        {
            byte* memory = (byte*)datas;
            if (*memory != 0x0d || *(memory + 1) != 0x0a)
            {
                return false;
            }
            
            packet.ContentLength= NetworkBitConverter.ToInt32(*(memory+2),*(memory+3),*(memory+4),*(memory+5));
            return true;
        }


        protected unsafe override bool ParseBody(ZTProtocolPacket packet, IntPtr datas, int count, out Int32 giveupCount)
        {
            giveupCount = 0;
            bool result = false;
            if ((count + packet.Count) >= packet.ContentLength)
            {
                giveupCount = packet.ContentLength - (Int32)packet.Count;
                result = true;
            }
            else
            {
                giveupCount = count;
                result = false;
            }
            //保存数据 
            packet.Write(datas, giveupCount);
            return result;
        }
        
        public override Packet CreatePacket(BufferManager buffer)
        {
            return new ZTProtocolPacket(buffer);
        }

        protected int IndexOfProtocolEnd(Packet packet, IntPtr datas, int count, out bool reset)
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
