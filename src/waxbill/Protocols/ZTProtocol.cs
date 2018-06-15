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
    /// 解析出来的包不包含协议头
    /// </summary>
    public class ZTProtocol:ProtocolBase
    {
        public static ZTProtocol Define = new ZTProtocol();
        public ZTProtocol():base(6)
        {}

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
        protected unsafe override bool ParseHeader(Packet _packet, IntPtr datas)
        {
            ZTProtocolPacket packet = _packet as ZTProtocolPacket;
            if (packet == null)
            {
                return false;
            }

            byte* memory = (byte*)datas;
            if (*memory != 0x0d || *(memory + 1) != 0x0a)
            {
                return false;
            }
            
            packet.ContentLength= NetworkBitConverter.ToInt32(*(memory+2),*(memory+3),*(memory+4),*(memory+5));
            return true;
        }


        protected unsafe override bool ParseBody(Packet _packet, IntPtr datas, int count, out Int32 giveupCount)
        {
            ZTProtocolPacket packet = _packet as ZTProtocolPacket;
            if (packet == null)
            {
                giveupCount = count;
                return false;
            }
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
    }
}
