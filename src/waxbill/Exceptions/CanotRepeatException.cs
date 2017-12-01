using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Exceptions
{
    public class CanotRepeatException:WaxbillException
    {
        public CanotRepeatException(string message):base(message)
        {

        }
    }
}
