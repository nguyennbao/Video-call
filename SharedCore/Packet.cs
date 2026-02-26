using System;

namespace SharedCore
{
    [Serializable]
    public class Packet
    {
        public PacketType Type { get; set; }
        public string Sender { get; set; }
        public string Target { get; set; } // Quyết định người nhận (All hoặc tên User cụ thể)
        public string Content { get; set; }
        public byte[] Payload { get; set; }
    }
}