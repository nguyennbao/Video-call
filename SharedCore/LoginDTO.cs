using System;

namespace SharedCore
{
    [Serializable]
    public class LoginDTO
    {
        public string Username { get; set; }
        public string Password { get; set; } // Bạn có thể băm (hash) password trước khi gửi sau này
    }
}