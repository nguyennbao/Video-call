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
                btnStart.Enabled = false;
                await StartServer();
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
                    Log($"[+] Có Client kết nối từ: {client.Client.RemoteEndPoint}");
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

                while (true)
                {
                    string clientData = await reader.ReadLineAsync();
                    if (clientData == null) break;

                    Packet receivedPacket = JsonSerializer.Deserialize<Packet>(clientData);

                    if (receivedPacket.Type == PacketType.Login)
                    {
                        LoginDTO loginInfo = JsonSerializer.Deserialize<LoginDTO>(receivedPacket.Content);
                        if (loginInfo.Password == "123")
                        {
                            username = loginInfo.Username;
                            Log($"[+] User '{username}' chứng thực thành công!");
                            _connectedClients[username] = writer;

                            var crypto = new CryptoService();
                            _clientKeys[username] = crypto;

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
                    else if (receivedPacket.Type == PacketType.KeyExchange)
                    {
                        _clientKeys[username].DeriveSharedSecret(receivedPacket.Payload);
                        Log($"[+] Đã thiết lập khóa bảo mật an toàn với '{username}'.");
                    }
                    // THÊM MỚI: Xử lý trung chuyển tin nhắn
                    else if (receivedPacket.Type == PacketType.Message)
                    {
                        // 1. Server mở khóa tin nhắn của người gửi
                        string decryptedMessage = _clientKeys[username].DecryptAES(receivedPacket.Payload);
                        Log($"[Chat] {username} gửi: {decryptedMessage}");

                        // 2. Chuyển tiếp cho tất cả các Client khác
                        foreach (var targetClient in _connectedClients)
                        {
                            if (targetClient.Key != username)
                            {
                                // Khóa lại tin nhắn bằng chìa khóa riêng của người nhận
                                byte[] reEncryptedMsg = _clientKeys[targetClient.Key].EncryptAES(decryptedMessage);

                                var forwardPacket = new Packet
                                {
                                    Type = PacketType.Message,
                                    Sender = username,
                                    Payload = reEncryptedMsg
                                };

                                await targetClient.Value.WriteLineAsync(JsonSerializer.Serialize(forwardPacket));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Lỗi xử lý Client {username}: {ex.Message}");
            }
            finally
            {
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

        private async void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtMessage.Text;

            // Kiểm tra xem có gõ chữ chưa và có ai đang kết nối không
            if (string.IsNullOrWhiteSpace(message) || _connectedClients.Count == 0) return;

            try
            {
                // 1. In tin nhắn lên màn hình của chính Server
                Log($"[Server]: {message}");

                // 2. Gửi cho TẤT CẢ các Client đang online
                foreach (var clientInfo in _connectedClients)
                {
                    string username = clientInfo.Key;
                    StreamWriter writer = clientInfo.Value;

                    // Kiểm tra xem Client này đã có khóa bảo mật chưa
                    if (_clientKeys.ContainsKey(username) && _clientKeys[username].SharedSecret != null)
                    {
                        // Dùng chìa khóa của riêng Client đó để mã hóa tin nhắn
                        byte[] encryptedMessage = _clientKeys[username].EncryptAES(message);

                        // Đóng gói tin nhắn với tên người gửi là "Server"
                        var packet = new Packet
                        {
                            Type = PacketType.Message,
                            Sender = "Server",
                            Payload = encryptedMessage
                        };

                        // Gửi đi
                        await writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                    }
                }

                // 3. Xóa chữ trong ô nhập đi sau khi gửi xong
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi Server gửi tin: " + ex.Message);
            }
        }
    }
}