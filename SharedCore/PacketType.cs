using System;
using System.Collections.Generic;
using System.Text;

namespace SharedCore
{
    public enum PacketType
    {
        Login,          // Yêu cầu đăng nhập
        LoginResponse,  // Phản hồi đăng nhập (thành công/thất bại)
        Message,        // Tin nhắn văn bản
        File,           // Gửi file/hình ảnh
        KeyExchange,    // Trao đổi khóa Diffie-Hellman
        VideoFrame      // Khung hình video camera
    }
}