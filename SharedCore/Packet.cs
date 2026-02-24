using System;

namespace SharedCore
{
    [Serializable]
    public class Packet
    {
        public PacketType Type { get; set; }
        public string Sender { get; set; }

        // Dùng để chứa text hoặc dữ liệu JSON của các DTO khác
        public string Content { get; set; }

        // Dùng để chứa dữ liệu nhị phân (file, hình ảnh, khóa mã hóa)
        public byte[] Payload { get; set; }
    }
}