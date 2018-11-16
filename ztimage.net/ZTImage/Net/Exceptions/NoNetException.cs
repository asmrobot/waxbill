namespace ZTImage.Net.Exceptions
{
    using System;

    public class NoNetException : ZTNetException
    {
        public NoNetException() : base("没有网络")
        {
        }

        public NoNetException(string msg) : base(msg)
        {
        }
    }
}

