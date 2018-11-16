namespace ZTImage.Net.WebSockets
{
    using System;

    public enum Opcode : byte
    {
        BinaryFrame = 2,
        ConnectionCloseFrame = 8,
        ContinuationFrame = 0,
        PingFrame = 9,
        PongFrame = 10,
        TextFrame = 1
    }
}

