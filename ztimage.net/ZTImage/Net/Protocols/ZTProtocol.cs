namespace ZTImage.Net.Protocols
{
    using System;
    using ZTImage.Net.Utils;

    public class ZTProtocol : FixedHeaderProtocol
    {
        public ZTProtocol() : base(6)
        {
        }

        public override int GetSize(byte[] datas)
        {
            return NetworkBitConverter.ToInt32(datas, 2);
        }

        public override bool IsStart(byte[] datas)
        {
            return ((datas[0] == 13) && (datas[1] == 10));
        }
    }
}

