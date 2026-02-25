using SharedCore;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using AForge.Video;
using AForge.Video.DirectShow;

namespace Client
{
    public partial class Form1 : Form
    {
        private TcpClient _client;
        private StreamWriter _writer;
        private StreamReader _reader;
        private CryptoService _crypto;

        // Các biến dùng cho tính năng Gọi Video
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private bool isCalling = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Lấy danh sách thiết bị camera khi form vừa load lên
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count > 0)
            {
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
            }
            else
            {
                MessageBox.Show("Không tìm thấy Webcam trên máy tính này!");
            }
        }

        // Sự kiện khi bấm nút Gọi Video (nhớ gán sự kiện Click cho btnCall)
        private void btnCall_Click(object sender, EventArgs e)
        {
            if (videoSource == null) return;

            if (!isCalling)
            {
                videoSource.Start();
                isCalling = true;
                btnCall.Text = "Tắt Gọi Video";
                Log("Đã bật camera.");
            }
            else
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
                isCalling = false;
                btnCall.Text = "Bật Gọi Video";
                picLocal.Image = null;
                Log("Đã tắt camera.");
            }
        }

        // Sự kiện xảy ra liên tục khi Webcam bắt được khung hình mới
        private async void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                Bitmap frame = (Bitmap)eventArgs.Frame.Clone();

                // Cập nhật lên PictureBox của mình (phải dùng Invoke vì nó chạy trên Thread khác)
                picLocal.Invoke(new Action(() => {
                    picLocal.Image = (Bitmap)frame.Clone();
                }));

                // Nếu đã kết nối và mã hóa thành công thì gửi khung hình đi
                if (_crypto != null && _writer != null)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Thu nhỏ ảnh lại để gửi không bị lag mạng (rất quan trọng)
                        Bitmap resized = new Bitmap(frame, new Size(320, 240));
                        resized.Save(ms, ImageFormat.Jpeg);
                        byte[] imageBytes = ms.ToArray();

                        string base64 = Convert.ToBase64String(imageBytes);
                        byte[] encrypted = _crypto.EncryptAES(base64);

                        var packet = new Packet
                        {
                            Type = PacketType.VideoFrame,
                            Sender = txtUsername.Text,
                            Payload = encrypted
                        };

                        await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                    }
                }
            }
            catch { /* Bỏ qua lỗi trong lúc nén/gửi hình liên tục */ }
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

                        string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                        string fullPath = Path.Combine(downloadPath, fileName);
                        File.WriteAllBytes(fullPath, fileBytes);

                        Log($"[Hệ thống]: Nhận file thành công! Lưu tại Downloads\\{fileName}");
                        Process.Start("explorer.exe", downloadPath);
                    }
                    // NHẬN KHUNG HÌNH VIDEO VÀ HIỂN THỊ
                    else if (packet.Type == PacketType.VideoFrame)
                    {
                        string base64 = _crypto.DecryptAES(packet.Payload);
                        byte[] imageBytes = Convert.FromBase64String(base64);

                        using (MemoryStream ms = new MemoryStream(imageBytes))
                        {
                            Image img = Image.FromStream(ms);

                            // Cập nhật giao diện từ thread lắng nghe mạng
                            picRemote.Invoke(new Action(() => {
                                if (picRemote.Image != null) picRemote.Image.Dispose();
                                picRemote.Image = (Image)img.Clone();
                            }));
                        }
                    }
                }
            }
            catch { Log("Mất kết nối."); }
        }

        // Tắt Camera khi tắt ứng dụng để không bị treo Webcam
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
        }

        private void Log(string m) => Invoke(new Action(() => rtbClientLogs.AppendText($"{m}\n")));
    }
}