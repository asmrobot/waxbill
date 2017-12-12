using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Packets
{
    public interface IPacket:IDisposable
    {
        Int64 Count { get; }

        byte[] Read();

        Int32 Read(int sourceOffset, byte[] targetDatas, int offset, int count);

        ///// <summary>
        ///// 添加字节段
        ///// </summary>
        ///// <param name="bytes"></param>
        //void Write(IntPtr bytes, Int32 count);

        ///// <summary>
        ///// 添加字节段
        ///// </summary>
        ///// <param name="bytes"></param>
        //void Write(ArraySegment<byte> bytes);

    }
}
