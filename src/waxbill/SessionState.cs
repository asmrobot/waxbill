using System;

namespace waxbill
{
    

    public class SessionState
    {
        public const int Closed = 0x80;
        public const int Closeing = 0x40;
        public const int Receiveing = 2;
        public const int Sending = 1;
    }
}

