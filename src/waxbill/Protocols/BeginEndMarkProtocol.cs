using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Utils;

namespace waxbill.Protocols
{
    public class BeginEndMarkProtocol : ProtocolBase
    {
        private byte _Begin = 0;
        private byte _End = 0;
        public BeginEndMarkProtocol(byte begin,byte end):base(1)
        {
            if (end <=0)
            {
                throw new ArgumentNullException("end");
            }
            this._Begin = begin;
            this._End = end;
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
        //    if (this._Begin <= 0)
        //    {
        //        return true;
        //    }
        //    if (datas.Array[datas.Offset] == _Begin)
        //    {
        //        return true;
        //    }
        //    reset = true;
        //    return false;
        //}


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
        //    //return datas.Count;
        //    for (int i = 0; i < datas.Count; i++)
        //    {
        //        if (datas.Array[datas.Offset + i] == this._End)
        //        {
        //            return i + 1;
        //        }
        //    }
        //    return -1;
        //}

        unsafe public override bool ParseStart(Packet packet, IntPtr datas, int count, out bool reset)
        {
            reset = false;
            if (this._Begin <= 0)
            {
                return true;
            }
            byte* memory = (byte*)datas;
            if (memory[0] == _Begin)
            {
                return true;
            }
            reset = true;
            return false;
        }

        unsafe public override int IndexOfProtocolEnd(Packet packet, IntPtr datas, int count, out bool reset)
        {
            reset = false;
            byte* memory = (byte*)datas;

            for (int i = 0; i < count; i++)
            {
                if (memory[i] == this._End)
                {
                    return i + 1;
                }
            }
            return -1;
        }
    }
}
