using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json; // Dùng để chuyển DTO thành chuỗi JSON
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedCore;

namespace Client
{
    public partial class Form1 : Form
    {
        private TcpClient _client;
        private StreamWriter _writer;
        private StreamReader _reader;

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

                // Kết nối tới IP cục bộ (localhost) ở cổng 8888
                _client = new TcpClient();
                await _client.ConnectAsync("127.0.0.1", 8888);

                var stream = _client.GetStream();
                _writer = new StreamWriter(stream) { AutoFlush = true };
                _reader = new StreamReader(stream);

                Log("Kết nối mạng thành công! Đang gửi thông tin chứng thực...");

                // 1. Tạo LoginDTO chứa thông tin
                var loginData = new LoginDTO
                {
                    Username = txtUsername.Text,
                    Password = "123" // Tạm thời hardcode pass, sau này có thể thêm ô nhập Pass
                };

                // 2. Đóng gói vào Packet
                var packet = new Packet
                {
                    Type = PacketType.Login,
                    Sender = txtUsername.Text,
                    Content = JsonSerializer.Serialize(loginData) // Biến DTO thành JSON
                };

                // 3. Gửi Packet (cũng dạng JSON) qua Server
                string jsonPacket = JsonSerializer.Serialize(packet);
                await _writer.WriteLineAsync(jsonPacket);

                // Mở một luồng chạy ngầm để liên tục lắng nghe phản hồi từ Server
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
                    // Chờ đọc tin nhắn từ Server
                    string responseLine = await _reader.ReadLineAsync();
                    if (responseLine == null) break; // Server ngắt kết nối

                    Log($"Server nói: {responseLine}");
                }
            }
            catch
            {
                Log("Đã mất kết nối với Server.");
            }
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

        private void rtbClientLogs_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtUsername_TextChanged(object sender, EventArgs e)
        {

        }
    }
}