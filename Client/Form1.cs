using SharedCore;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Form1 : Form
    {
        private TcpClient _client;
        private StreamWriter _writer;
        private StreamReader _reader;
        private CryptoService _crypto; // Bộ xử lý mã hóa của Client

        public Form1()
        {
            InitializeComponent();
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Vui lòng nhập Username!");
                return;
            }

            try
            {
                btnConnect.Enabled = false;
                Log("Đang kết nối đến Server...");

                _client = new TcpClient();
                await _client.ConnectAsync("127.0.0.1", 8888);

                var stream = _client.GetStream();
                _writer = new StreamWriter(stream) { AutoFlush = true };
                _reader = new StreamReader(stream);

                Log("Kết nối mạng thành công! Đang gửi thông tin chứng thực...");

                var loginData = new LoginDTO
                {
                    Username = txtUsername.Text,
                    Password = "123"
                };

                var packet = new Packet
                {
                    Type = PacketType.Login,
                    Sender = txtUsername.Text,
                    Content = JsonSerializer.Serialize(loginData)
                };

                string jsonPacket = JsonSerializer.Serialize(packet);
                await _writer.WriteLineAsync(jsonPacket);

                _ = Task.Run(() => ListenToServer());
            }
            catch (Exception ex)
            {
                Log($"Lỗi kết nối: {ex.Message}");
                btnConnect.Enabled = true;
            }
        }

        private async Task ListenToServer()
        {
            try
            {
                while (true)
                {
                    string responseData = await _reader.ReadLineAsync();
                    if (responseData == null) break;

                    try
                    {
                        Packet receivedPacket = JsonSerializer.Deserialize<Packet>(responseData);

                        // Xử lý khi Server gửi Public Key xuống
                        if (receivedPacket.Type == PacketType.KeyExchange)
                        {
                            Log("Đang thiết lập khóa bảo mật Diffie-Hellman với Server...");

                            // 1. Tạo bộ mã hóa riêng của Client
                            _crypto = new CryptoService();

                            // 2. Lấy Public Key của Server để sinh ra Chìa khóa bí mật chung
                            _crypto.DeriveSharedSecret(receivedPacket.Payload);

                            // 3. Gửi Public Key của mình lại cho Server
                            var keyPacket = new Packet
                            {
                                Type = PacketType.KeyExchange,
                                Sender = txtUsername.Text,
                                Payload = _crypto.PublicKey
                            };
                            await _writer.WriteLineAsync(JsonSerializer.Serialize(keyPacket));

                            Log("Thiết lập bảo mật thành công! Kênh truyền đã được mã hóa.");
                        }
                    }
                    catch
                    {
                        Log($"Server: {responseData}");
                    }
                }
            }
            catch { Log("Đã mất kết nối với Server."); }
        }

        private void Log(string message)
        {
            if (rtbClientLogs.InvokeRequired)
            {
                rtbClientLogs.Invoke(new Action(() => Log(message)));
            }
            else
            {
                rtbClientLogs.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\r\n");
            }
        }

        // Tui giữ lại 2 hàm trống này để giao diện (Design) của bạn không bị báo lỗi thiếu hàm nhé
        private void rtbClientLogs_TextChanged(object sender, EventArgs e) { }
        private void txtUsername_TextChanged(object sender, EventArgs e) { }
    }
}