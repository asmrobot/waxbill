namespace ZTImage.Net.Protocols
{
    using System;
    using System.Runtime.InteropServices;
    using ZTImage.Net.Utils;

    public class BeginEndMarkProtocol : ProtocolBase
    {
        private byte _Begin;
        private byte _End;

        public BeginEndMarkProtocol(byte begin, byte end) : base(1)
        {
            if (end <= 0)
            {
                throw new ArgumentNullException("end");
            }
            this._Begin = begin;
            this._End = end;
        }

        public override int IndexOfProtocolEnd(Packet packet, ArraySegment<byte> datas, out bool reset)
        {
            reset = false;
            for (int i = 0; i < datas.Count; i++)
            {
                if (datas.Array[datas.Offset + i] == this._End)
                {
                    return (i + 1);
                }
            }
            return -1;
        }

        public override bool ParseStart(Packet packet, ArraySegment<byte> datas, out bool reset)
        {
            reset = false;
            if (this._Begin <= 0)
            {
                return true;
            }
            if (datas.Array[datas.Offset] == this._Begin)
            {
                return true;
            }
            reset = true;
            return false;
        }
    }
}

