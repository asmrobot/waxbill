using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill
{
    public class Trace
    {
        private static Action<string, Exception> mErrorWriter;
        private static Action<string> mInfoWriter;


        public static void SetMessageWriter(Action<string,Exception> errorWriter, Action<string> infoWriter)
        {
            mErrorWriter = errorWriter;
            mInfoWriter = infoWriter;
        }


        public static void Error(string msg,Exception ex)
        {
            if (mErrorWriter != null)
            {
                mErrorWriter(msg, ex);
            }
        }

        public static void Error(string msg)
        {
            if (mErrorWriter != null)
            {
                mErrorWriter(msg, null);
            }
        }


        public static void Info(string msg)
        {
            if (mInfoWriter != null)
            {
                mInfoWriter(msg);
            }
        }
    }
}
