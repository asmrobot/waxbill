using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill
{
    public class SessionState
    {
        public const Int32 CLOSED = 0x80;
        public const Int32 CLOSING = 0x40;
        public const Int32 RECEIVEING = 0x02;
        public const Int32 SENDING = 0x01;
    }
}
