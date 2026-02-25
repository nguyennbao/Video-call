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
                    // Trung chuyển tin nhắn
                    else if (receivedPacket.Type == PacketType.Message)
                    {
                        string decryptedMessage = _clientKeys[username].DecryptAES(receivedPacket.Payload);
                        Log($"[Chat] {username} gửi: {decryptedMessage}");

                        foreach (var targetClient in _connectedClients)
                        {
                            if (targetClient.Key != username)
                            {
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
                    // Trung chuyển File
                    else if (receivedPacket.Type == PacketType.File)
                    {
                        string fileName = receivedPacket.Content;
                        Log($"[Server] Đang chuyển tiếp file '{fileName}' từ {username}");

                        // 1. Giải mã lấy nội dung gốc
                        string decryptedBase64 = _clientKeys[username].DecryptAES(receivedPacket.Payload);

                        // 2. Khóa lại và gửi cho những người khác
                        foreach (var targetClient in _connectedClients)
                        {
                            if (targetClient.Key != username)
                            {
                                byte[] reEncryptedFile = _clientKeys[targetClient.Key].EncryptAES(decryptedBase64);
                                var forwardPacket = new Packet
                                {
                                    Type = PacketType.File,
                                    Sender = username,
                                    Content = fileName,
                                    Payload = reEncryptedFile
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

        private async void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtMessage.Text;
            if (string.IsNullOrWhiteSpace(message) || _connectedClients.Count == 0) return;

            try
            {
                Log($"[Server]: {message}");

                foreach (var clientInfo in _connectedClients)
                {
                    string username = clientInfo.Key;
                    StreamWriter writer = clientInfo.Value;

                    if (_clientKeys.ContainsKey(username) && _clientKeys[username].SharedSecret != null)
                    {
                        byte[] encryptedMessage = _clientKeys[username].EncryptAES(message);
                        var packet = new Packet
                        {
                            Type = PacketType.Message,
                            Sender = "Server",
                            Payload = encryptedMessage
                        };
                        await writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                    }
                }
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi Server gửi tin: " + ex.Message);
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