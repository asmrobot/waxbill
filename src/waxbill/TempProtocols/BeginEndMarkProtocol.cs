using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Utils;

namespace waxbill.Protocols
{
    public class BeginEndMarkProtocol : IProtocol
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
        

        protected unsafe bool ParseStart(Packet packet, IntPtr datas, int count,out Int32 giveupCount)
        {
            giveupCount = 0;
            if (this.mBeginChars <= 0)
            {
                return true;
            }
            byte* memory = (byte*)datas;
            if (memory[0] == mBeginChars)
            {
                return true;
            }
            giveupCount = count;
            return false;
        }


        protected unsafe bool ParseEnd(Packet packet, IntPtr datas, int count, out Int32 giveupCount)
        {
            giveupCount = 0;
            byte* memory = (byte*)datas;
            while (*memory != this.mEndChars&&giveupCount<count)
            {
                giveupCount++;
            }

            giveupCount++;
            if (giveupCount <= count)
            {
                packet.Write(datas, giveupCount);
                return true;
            }
            return false;
        }

        public bool TryToPacket(Packet packet, IntPtr datas, int count, out int giveupCount)
        {
            giveupCount = 0;
            if (!packet.IsStart)
            {
                if (!ParseStart(packet, datas, count, out giveupCount))
                {
                    return false;
                }
                if (giveupCount > count)
                {
                    giveupCount = count;
                    return false;
                }
                if (giveupCount > 0)
                {
                    datas = IntPtr.Add(datas,giveupCount);
                }
                packet.IsStart = true;
            }




        }

        public Packet CreatePacket(BufferManager buffer)
        {
            return new Packet(buffer);
        }
    }
}
