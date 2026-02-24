using SharedCore; // Gọi class từ SharedCore để chuẩn bị dùng Packet
using System;
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

        public Form1()
        {
            InitializeComponent();
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (!_isRunning)
            {
                btnStart.Enabled = false; // Tắt nút sau khi đã bấm
                await StartServer();
            }
        }

        private async Task StartServer()
        {
            try
            {
                // Mở port 8888 để lắng nghe trên tất cả các địa chỉ IP của máy
                _server = new TcpListener(IPAddress.Any, 8888);
                _server.Start();
                _isRunning = true;

                Log("Server đã khởi động ở cổng 8888. Đang chờ Client kết nối...");

                // Vòng lặp vô tận để liên tục chờ nhiều Client kết nối
                while (_isRunning)
                {
                    // Chờ đến khi có 1 Client gọi tới
                    TcpClient client = await _server.AcceptTcpClientAsync();
                    Log($"[+] Có Client mới kết nối từ: {client.Client.RemoteEndPoint}");

                    // Tách Client này ra một luồng (Task) riêng để xử lý, 
                    // Server quay lại vòng lặp để chờ người tiếp theo
                    _ = Task.Run(() => HandleClient(client));
                }
            }
            catch (Exception ex)
            {
                Log($"Lỗi Server: {ex.Message}");
            }
        }

        // Hàm xử lý riêng cho từng Client
        private async void HandleClient(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();
                var reader = new StreamReader(stream);
                var writer = new StreamWriter(stream) { AutoFlush = true };

                // Đọc dữ liệu Client gửi lên
                string clientData = await reader.ReadLineAsync();
                if (clientData != null)
                {
                    Packet receivedPacket = JsonSerializer.Deserialize<Packet>(clientData);

                    if (receivedPacket.Type == PacketType.Login)
                    {
                        // Giải mã DTO
                        LoginDTO loginInfo = JsonSerializer.Deserialize<LoginDTO>(receivedPacket.Content);

                        if (loginInfo.Password == "123") // Tạm thời code cứng pass là 123
                        {
                            Log($"[+] User '{loginInfo.Username}' chứng thực thành công!");
                            await writer.WriteLineAsync("Chao mung ban den voi phong chat!");
                        }
                        else
                        {
                            Log($"[-] User '{loginInfo.Username}' sai mật khẩu.");
                            await writer.WriteLineAsync("Sai mat khau!");
                            client.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Lỗi xử lý Client: {ex.Message}");
            }
        }

        private async void btnStart_Click_1(object sender, EventArgs e)
        {
            if (!_isRunning)
            {
                btnStart.Enabled = false; // Tắt nút sau khi đã bấm
                await StartServer();      // Gọi hàm bật Server
            }
        }
        // Hàm phụ giúp in chữ lên màn hình (vì WinForms cần Invoke khi dùng đa luồng)
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
