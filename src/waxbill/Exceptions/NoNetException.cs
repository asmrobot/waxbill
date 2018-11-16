namespace waxbill.Exceptions
{
    using System;

    public class NoNetException : WaxbillException
    {
        public NoNetException() : base("没有网络")
        {
        }

        public NoNetException(string msg) : base(msg)
        {
        }
    }
}

