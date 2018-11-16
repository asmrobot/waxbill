namespace ZTImage.Net.Exceptions
{
    using System;

    public class IPParseException : ZTNetException
    {
        public IPParseException() : base("ip地址错误")
        {
        }
    }
}

