using SharedCore;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

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
                MessageBox.Show("Vui lòng nhập tên người dùng!");
                return;
            }

            try
            {
                btnConnect.Enabled = false;
                Log("Đang kết nối tới Server...");

                _client = new TcpClient();
                await _client.ConnectAsync("127.0.0.1", 8888);

                var stream = _client.GetStream();
                _writer = new StreamWriter(stream) { AutoFlush = true };
                _reader = new StreamReader(stream);

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
                Log($"Lỗi: {ex.Message}");
                btnConnect.Enabled = true;
            }
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text) || _crypto == null) return;
            try
            {
                string msg = txtMessage.Text;
                byte[] encrypted = _crypto.EncryptAES(msg);
                var packet = new Packet { Type = PacketType.Message, Sender = txtUsername.Text, Payload = encrypted };
                await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                Log($"[Tôi]: {msg}");
                txtMessage.Clear();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private async void btnSendFile_Click(object sender, EventArgs e)
        {
            if (_crypto == null) return;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] fileBytes = File.ReadAllBytes(ofd.FileName);
                        string base64 = Convert.ToBase64String(fileBytes);
                        byte[] encrypted = _crypto.EncryptAES(base64);

                        var packet = new Packet
                        {
                            Type = PacketType.File,
                            Sender = txtUsername.Text,
                            Content = Path.GetFileName(ofd.FileName),
                            Payload = encrypted
                        };
                        await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                        Log($"[Hệ thống]: Đã gửi file {packet.Content}");
                    }
                    catch (Exception ex) { MessageBox.Show("Lỗi gửi file: " + ex.Message); }
                }
            }
        }

        private async Task ListenToServer()
        {
            try
            {
                while (true)
                {
                    string data = await _reader.ReadLineAsync();
                    if (data == null) break;
                    var packet = JsonSerializer.Deserialize<Packet>(data);

                    if (packet.Type == PacketType.KeyExchange)
                    {
                        _crypto = new CryptoService();
                        _crypto.DeriveSharedSecret(packet.Payload);
                        var response = new Packet { Type = PacketType.KeyExchange, Sender = txtUsername.Text, Payload = _crypto.PublicKey };
                        await _writer.WriteLineAsync(JsonSerializer.Serialize(response));
                        Log("Bảo mật AES đã sẵn sàng!");
                    }
                    else if (packet.Type == PacketType.Message)
                    {
                        string msg = _crypto.DecryptAES(packet.Payload);
                        Log($"[{packet.Sender}]: {msg}");
                    }
                    else if (packet.Type == PacketType.File)
                    {
                        string fileName = packet.Content;
                        byte[] fileBytes = Convert.FromBase64String(_crypto.DecryptAES(packet.Payload));

                        // LƯU VÀO DOWNLOADS
                        string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                        string fullPath = Path.Combine(downloadPath, fileName);
                        File.WriteAllBytes(fullPath, fileBytes);

                        Log($"[Hệ thống]: Nhận file thành công! Lưu tại Downloads\\{fileName}");
                        Process.Start("explorer.exe", downloadPath); // Tự mở thư mục
                    }
                }
            }
            catch { Log("Mất kết nối."); }
        }

        private void Log(string m) => Invoke(new Action(() => rtbClientLogs.AppendText($"{m}\n")));
    }
}