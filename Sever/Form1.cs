using SharedCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sever
{
    public partial class Form1 : Form
    {
        private TcpListener _server;
        private bool _isRunning = false;

        // Hai danh sách dùng để quản lý Client và Khóa bảo mật của từng Client
        private Dictionary<string, StreamWriter> _connectedClients = new Dictionary<string, StreamWriter>();
        private Dictionary<string, CryptoService> _clientKeys = new Dictionary<string, CryptoService>();

        public Form1()
        {
            InitializeComponent();
        }

        private async void btnStart_Click_1(object sender, EventArgs e)
        {
            if (!_isRunning)
            {
                btnStart.Enabled = false; // Tắt nút sau khi đã bấm
                await StartServer();      // Gọi hàm bật Server
            }
        }

        private async Task StartServer()
        {
            try
            {
                _server = new TcpListener(IPAddress.Any, 8888);
                _server.Start();
                _isRunning = true;

                Log("Server đã khởi động ở cổng 8888. Đang chờ Client kết nối...");

                while (_isRunning)
                {
                    TcpClient client = await _server.AcceptTcpClientAsync();
                    Log($"[+] Có Client mới kết nối từ: {client.Client.RemoteEndPoint}");

                    // Tách luồng riêng cho mỗi Client
                    _ = Task.Run(() => HandleClient(client));
                }
            }
            catch (Exception ex)
            {
                Log($"Lỗi Server: {ex.Message}");
            }
        }

        private async void HandleClient(TcpClient client)
        {
            string username = "";
            try
            {
                var stream = client.GetStream();
                var reader = new StreamReader(stream);
                var writer = new StreamWriter(stream) { AutoFlush = true };

                // Vòng lặp liên tục chờ tin nhắn từ Client này
                while (true)
                {
                    string clientData = await reader.ReadLineAsync();
                    if (clientData == null) break; // Bị ngắt kết nối

                    Packet receivedPacket = JsonSerializer.Deserialize<Packet>(clientData);

                    // Xử lý khi Client gửi yêu cầu Login
                    if (receivedPacket.Type == PacketType.Login)
                    {
                        LoginDTO loginInfo = JsonSerializer.Deserialize<LoginDTO>(receivedPacket.Content);
                        if (loginInfo.Password == "123")
                        {
                            username = loginInfo.Username;
                            Log($"[+] User '{username}' chứng thực thành công!");

                            // Lưu Client vào danh sách để quản lý
                            _connectedClients[username] = writer;

                            // 1. Khởi tạo thuật toán mã hóa cho Client này
                            var crypto = new CryptoService();
                            _clientKeys[username] = crypto;

                            // 2. Gửi Public Key của Server sang cho Client
                            var keyPacket = new Packet
                            {
                                Type = PacketType.KeyExchange,
                                Sender = "Server",
                                Payload = crypto.PublicKey
                            };
                            await writer.WriteLineAsync(JsonSerializer.Serialize(keyPacket));
                        }
                        else
                        {
                            Log($"[-] User '{loginInfo.Username}' sai mật khẩu.");
                            client.Close();
                            return;
                        }
                    }
                    // Xử lý khi Client gửi Public Key lên
                    else if (receivedPacket.Type == PacketType.KeyExchange)
                    {
                        // Nhận Public Key từ Client và chốt "Chìa khóa bí mật chung"
                        _clientKeys[username].DeriveSharedSecret(receivedPacket.Payload);
                        Log($"[+] Đã thiết lập khóa bảo mật an toàn với '{username}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Lỗi xử lý Client {username}: {ex.Message}");
            }
            finally
            {
                // Dọn dẹp khi Client thoát
                if (!string.IsNullOrEmpty(username) && _connectedClients.ContainsKey(username))
                {
                    _connectedClients.Remove(username);
                    _clientKeys.Remove(username);
                    Log($"[-] Client '{username}' đã ngắt kết nối.");
                }
                client.Close();
            }
        }

        private void Log(string message)
        {
            if (rtbLogs.InvokeRequired)
            {
                rtbLogs.Invoke(new Action(() => Log(message)));
            }
            else
            {
                rtbLogs.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\r\n");
            }
        }
    }
}