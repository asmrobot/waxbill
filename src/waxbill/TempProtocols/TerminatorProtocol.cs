using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Protocols
{
    /// <summary>
    /// 回车分隔
    /// </summary>
    public class TerminatorProtocol : BeginEndMarkProtocol
    {
        public TerminatorProtocol():base(0,(byte)'\n')
        {}

    }
}
