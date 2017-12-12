using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
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
        

        public unsafe override bool ParseStart(Packet packet, IntPtr datas, int count, out bool reset)
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
