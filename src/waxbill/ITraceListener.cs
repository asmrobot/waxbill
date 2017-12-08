using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill
{
    public interface ITraceListener
    {
        /// <summary>
        /// debug
        /// </summary>
        /// <param name="message"></param>
        void Debug(string message);

        /// <summary>
        /// error
        /// </summary>
        /// <param name="message"></param>
        void Error(string message);

        /// <summary>
        /// error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        void Error(string message, Exception ex);
        /// <summary>
        /// info
        /// </summary>
        /// <param name="message"></param>
        void Info(string message);
    }

    public static class Trace
    {
        private static ITraceListener mTrace;
        public static void SetTrace(ITraceListener listener)
        {
            mTrace = listener;
        }
        /// <summary>
        /// debug
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            if (mTrace != null)
            {
                mTrace.Debug(message);
            }
            
        }

        /// <summary>
        /// error
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            if (mTrace != null)
            {
                mTrace.Error(message);
            }
        }

        /// <summary>
        /// error
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public static void Error(string message, Exception ex)
        {
            if (mTrace != null)
            {
                mTrace.Error(message,ex);
            }
        }
        /// <summary>
        /// info
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            if (mTrace != null)
            {
                mTrace.Info(message);
            }
        }
    }
}
