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
        private CryptoService _crypto;

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

                var loginData = new LoginDTO { Username = txtUsername.Text, Password = "123" };
                var packet = new Packet
                {
                    Type = PacketType.Login,
                    Sender = txtUsername.Text,
                    Content = JsonSerializer.Serialize(loginData)
                };

                await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));

                _ = Task.Run(() => ListenToServer());
            }
            catch (Exception ex)
            {
                Log($"Lỗi kết nối: {ex.Message}");
                btnConnect.Enabled = true;
            }
        }

        // HÀM MỚI: Xử lý khi bấm nút Gửi tin nhắn
        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text) || _crypto == null) return;

            try
            {
                string message = txtMessage.Text;

                // 1. Mã hóa tin nhắn bằng khóa AES
                byte[] encryptedMessage = _crypto.EncryptAES(message);

                // 2. Đóng gói vào Packet
                var packet = new Packet
                {
                    Type = PacketType.Message,
                    Sender = txtUsername.Text,
                    Payload = encryptedMessage
                };

                // 3. Gửi đi
                await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));

                Log($"[Tôi]: {message}");
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi gửi tin: " + ex.Message);
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

                        if (receivedPacket.Type == PacketType.KeyExchange)
                        {
                            Log("Đang thiết lập khóa bảo mật Diffie-Hellman...");
                            _crypto = new CryptoService();
                            _crypto.DeriveSharedSecret(receivedPacket.Payload);

                            var keyPacket = new Packet
                            {
                                Type = PacketType.KeyExchange,
                                Sender = txtUsername.Text,
                                Payload = _crypto.PublicKey
                            };
                            await _writer.WriteLineAsync(JsonSerializer.Serialize(keyPacket));
                            Log("Thiết lập bảo mật thành công! Kênh truyền đã được mã hóa.");
                        }
                        // XỬ LÝ KHI NHẬN ĐƯỢC TIN NHẮN TỪ NGƯỜI KHÁC
                        else if (receivedPacket.Type == PacketType.Message)
                        {
                            // Giải mã mảng byte lộn xộn trở lại thành chữ
                            string decryptedMessage = _crypto.DecryptAES(receivedPacket.Payload);
                            Log($"[{receivedPacket.Sender}]: {decryptedMessage}");
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

        // Giữ lại các hàm trống này để tránh lỗi giao diện
        private void rtbClientLogs_TextChanged(object sender, EventArgs e) { }
        private void txtUsername_TextChanged(object sender, EventArgs e) { }
    }
}