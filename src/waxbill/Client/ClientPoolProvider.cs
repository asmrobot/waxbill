using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Pools;

namespace waxbill.Client
{
    /// <summary>
    /// TCPClient缓存池提供
    /// </summary>
    public class ClientPoolProvider:PoolProvider
    {
        private ClientPoolProvider():base(TCPOptions.CLIENT_DEFAULT)
        {}


        private static ClientPoolProvider instance;
        private static Object lockHelper = new object();

        public static ClientPoolProvider Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockHelper)
                    {
                        if (instance == null)
                        {
                            instance = new ClientPoolProvider();
                        }
                    }
                }
                return instance;
            }
        }

    }
}
