using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Exceptions
{
    public class ConfigException:WaxbillException
    {
        public ConfigException(String msg):base(msg)
        {

        }
    }
}
