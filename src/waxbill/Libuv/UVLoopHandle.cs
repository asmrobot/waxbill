using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Libuv
{
    public class UVLoopHandle : UVHandle
    {
        public UVLoopHandle()
        {

        }

        public void Init()
        {
            CreateHandle(UVIntrop.loop_size());
            UVIntrop.loop_init(this);
        }

        public void Start()
        {
            UVIntrop.uv_run(this, (Int32)UVIntrop.UV_RUN_MODE.UV_RUN_DEFAULT);
        }


    }
}
