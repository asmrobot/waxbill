using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Pools;
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
        protected override bool ParseHeader(Packet _packet, ArraySegment<byte> datas)
        {
            ZTProtocolPacket packet = _packet as ZTProtocolPacket;
            if (packet == null)
            {
                return false;
            }

            
            if (datas.Array[datas.Offset] != 0x0d || datas.Array[datas.Offset+1] != 0x0a)
            {
                return false;
            }
            
            packet.ContentLength= NetworkBitConverter.ToInt32(
                datas.Array[datas.Offset + 2],
                datas.Array[datas.Offset + 3],
                datas.Array[datas.Offset + 4],
                datas.Array[datas.Offset + 5]);
            return true;
        }


        protected override bool ParseBody(Packet _packet, ArraySegment<byte> datas, out Int32 giveupCount)
        {
            ZTProtocolPacket packet = _packet as ZTProtocolPacket;
            if (packet == null)
            {
                giveupCount = datas.Count;
                return false;
            }
            giveupCount = 0;
            bool result = false;
            if ((datas.Count + packet.Count) >= packet.ContentLength)
            {
                giveupCount = packet.ContentLength - (Int32)packet.Count;
                result = true;
            }
            else
            {
                giveupCount = datas.Count;
                result = false;
            }
            //保存数据 
            packet.Write(new ArraySegment<byte>(datas.Array,datas.Offset, giveupCount));
            return result;
        }
        
        public override Packet CreatePacket(PacketBufferPool buffer)
        {
            return new ZTProtocolPacket(buffer);
        }
    }
}
