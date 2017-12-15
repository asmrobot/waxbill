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
        private byte mBeginChars = 0;
        private byte mEndChars = 0;

        public BeginEndMarkProtocol(byte end):this(0,end)
        {}

        public BeginEndMarkProtocol(byte begin,byte end)
        {
            if (end <=0)
            {
                throw new ArgumentNullException("end");
            }
            this.mBeginChars = begin;
            this.mEndChars = end;
        }
        

        protected unsafe override bool ParseStart(Packet packet, IntPtr datas, int count, out bool reset)
        {
            reset = false;
            if (this.mBeginChars <= 0)
            {
                return true;
            }
            byte* memory = (byte*)datas;
            if (memory[0] == mBeginChars)
            {
                return true;
            }
            reset = true;
            return false;
        }


        protected unsafe override int IndexOfProtocolEnd(Packet packet, IntPtr datas, int count, out bool reset)
        {
            reset = false;
            byte* memory = (byte*)datas;

            for (int i = 0; i < count; i++)
            {
                if (memory[i] == this.mEndChars)
                {
                    return i + 1;
                }
            }
            return -1;
        }
    }
}
