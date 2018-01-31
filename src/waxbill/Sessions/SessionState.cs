using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill
{
    public class SessionState
    {
        public const Int32 Sending = 1;
        public const Int32 Receiveing = 2;
        public const Int32 Normal = 4;


        public const Int32 Closeing = 64;
        public const Int32 Closed = 128;
    }
}
