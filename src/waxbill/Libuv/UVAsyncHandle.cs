﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Libuv
{
    public class UVAsyncHandle : UVMemory
    {
        protected override bool ReleaseHandle()
        {
            throw new NotImplementedException();
        }
    }
}
