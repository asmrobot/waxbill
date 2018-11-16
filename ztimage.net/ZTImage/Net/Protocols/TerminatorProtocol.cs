namespace ZTImage.Net.Protocols
{
    using System;

    public class TerminatorProtocol : BeginEndMarkProtocol
    {
        public TerminatorProtocol() : base(0, 10)
        {
        }
    }
}

