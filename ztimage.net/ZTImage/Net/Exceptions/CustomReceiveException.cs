namespace ZTImage.Net.Exceptions
{
    using System;

    public class CustomReceiveException : ZTNetException
    {
        public CustomReceiveException() : base("自定义接收信息错误")
        {
        }
    }
}

