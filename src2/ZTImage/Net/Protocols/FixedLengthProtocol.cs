﻿namespace ZTImage.Net.Protocols
{
    using System;
    using System.Runtime.InteropServices;
    using ZTImage.Net.Utils;

    public class FixedLengthProtocol : ProtocolBase
    {
        private int m_Length;

        public FixedLengthProtocol(int length) : base(0)
        {
            this.m_Length = length;
        }

        public override int IndexOfProtocolEnd(Packet packet, ArraySegment<byte> datas, out bool reset)
        {
            reset = false;
            if ((packet.Count + datas.Count) >= this.m_Length)
            {
                return (this.m_Length - packet.Count);
            }
            return -1;
        }

        public override bool ParseStart(Packet packet, ArraySegment<byte> datas, out bool reset)
        {
            reset = false;
            packet.ForecastSize = this.m_Length;
            return true;
        }

        public int Length
        {
            get
            {
                return this.m_Length;
            }
        }
    }
}

