using System;

namespace SharedCore
{
    public enum PacketType
    {
        Login,
        LoginResponse,
        Message,
        File,
        KeyExchange,
        VideoFrame,
        AudioFrame,
        UserList // Thêm loại này
    }
}