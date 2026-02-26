using SharedCore;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using NAudio.Wave;

namespace Client
{
    public partial class Form1 : Form
    {
        private TcpClient _client;
        private StreamWriter _writer;
        private StreamReader _reader;
        private CryptoService _crypto;

        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private bool isCalling = false;
        private bool isSendingFrame = false;

        private WaveInEvent _waveIn;
        private WaveOutEvent _waveOut;
        private BufferedWaveProvider _waveProvider;
        private bool _isMicOn = false;

        private DateTime callStartTime;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        // ---------------- THÊM HÀM LƯU/TẢI LỊCH SỬ CHAT ----------------
        private void SaveChatHistory(string sender, string message)
        {
            try
            {
                string filePath = Path.Combine(Application.StartupPath, "ChatHistory_Client.txt");
                string logLine = $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {sender}: {message}\r\n";
                File.AppendAllText(filePath, logLine);
            }
            catch { }
        }

        private void LoadChatHistory()
        {
            try
            {
                string filePath = Path.Combine(Application.StartupPath, "ChatHistory_Client.txt");
                if (File.Exists(filePath))
                {
                    string history = File.ReadAllText(filePath);
                    Invoke(new Action(() => {
                        rtbClientLogs.AppendText("--- LỊCH SỬ CHAT CŨ ---\n");
                        rtbClientLogs.AppendText(history);
                        rtbClientLogs.AppendText("-----------------------\n");
                    }));
                }
            }
            catch { }
        }
        // -----------------------------------------------------------------

        private void Form1_Load(object sender, EventArgs e)
        {
            // Tải lịch sử chat khi mở app
            LoadChatHistory();

            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count > 0)
            {
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
            }

            try
            {
                _waveIn = new WaveInEvent();
                _waveIn.WaveFormat = new WaveFormat(8000, 16, 1);
                _waveIn.DataAvailable += WaveIn_DataAvailable;

                _waveOut = new WaveOutEvent();
                _waveProvider = new BufferedWaveProvider(_waveIn.WaveFormat);
                _waveProvider.DiscardOnBufferOverflow = true;
                _waveOut.Init(_waveProvider);
                _waveOut.Play();
            }
            catch { Log("Không tìm thấy thiết bị Âm thanh trên Client này."); }
        }

        private async void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (!_isMicOn || _crypto == null || _writer == null) return;
            try
            {
                byte[] audioData = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, audioData, e.BytesRecorded);
                string base64 = Convert.ToBase64String(audioData);
                byte[] encrypted = _crypto.EncryptAES(base64);

                var packet = new Packet { Type = PacketType.AudioFrame, Sender = txtUsername.Text, Payload = encrypted };
                await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
            }
            catch { }
        }

        private void btnMic_Click_1(object sender, EventArgs e)
        {
            if (_waveIn == null) return;
            if (!_isMicOn)
            {
                _waveIn.StartRecording();
                _isMicOn = true;
                btnMic.Text = "Tắt Mic";
                Log("Đã bật Mic.");
            }
            else
            {
                _waveIn.StopRecording();
                _isMicOn = false;
                btnMic.Text = "Bật Mic";
                Log("Đã tắt Mic.");
            }
        }

        private async void btnCall_Click(object sender, EventArgs e)
        {
            if (videoSource == null) return;
            if (!isCalling)
            {
                isCalling = true;
                videoSource.Start();
                btnCall.Text = "Tắt Gọi Video";
                callStartTime = DateTime.Now;
                Log("Đã bật camera.");
            }
            else
            {
                isCalling = false;
                videoSource.SignalToStop();

                btnCall.Text = "Bật Gọi Video";
                if (picLocal.Image != null) { picLocal.Image.Dispose(); picLocal.Image = null; }
                picLocal.Invalidate();
                Log("Đã tắt camera.");

                await Task.Delay(300);

                if (_writer != null && _crypto != null)
                {
                    byte[] encryptedStop = _crypto.EncryptAES("STOP");
                    var packet = new Packet { Type = PacketType.VideoFrame, Sender = txtUsername.Text, Content = "STOP_VIDEO", Payload = encryptedStop };
                    await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                }

                try
                {
                    TimeSpan duration = DateTime.Now - callStartTime;
                    string callLog = $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {txtUsername.Text} đã thực hiện cuộc gọi video kéo dài {duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}\r\n";
                    File.AppendAllText("CallHistory.txt", callLog);
                }
                catch { }
            }
        }

        private async void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!isCalling) return;
            try
            {
                Bitmap frame = (Bitmap)eventArgs.Frame.Clone();
                picLocal.Invoke(new Action(() =>
                {
                    if (picLocal.Image != null) picLocal.Image.Dispose();
                    picLocal.Image = (Bitmap)frame.Clone();
                }));

                if (_crypto != null && _writer != null && !isSendingFrame)
                {
                    isSendingFrame = true;
                    try
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (Bitmap resized = new Bitmap(frame, new Size(320, 240)))
                                resized.Save(ms, ImageFormat.Jpeg);

                            byte[] imageBytes = ms.ToArray();
                            string base64 = Convert.ToBase64String(imageBytes);
                            byte[] encrypted = _crypto.EncryptAES(base64);

                            var packet = new Packet { Type = PacketType.VideoFrame, Sender = txtUsername.Text, Payload = encrypted };
                            await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                        }
                    }
                    finally { isSendingFrame = false; }
                }
                frame.Dispose();
            }
            catch { isSendingFrame = false; }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text)) return;
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
                var packet = new Packet { Type = PacketType.Login, Sender = txtUsername.Text, Content = JsonSerializer.Serialize(loginData) };
                await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                _ = Task.Run(() => ListenToServer());
            }
            catch (Exception ex) { Log($"Lỗi: {ex.Message}"); btnConnect.Enabled = true; }
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
                        // LƯU LỊCH SỬ TIN NHẮN NHẬN ĐƯỢC
                        SaveChatHistory(packet.Sender, msg);
                    }
                    else if (packet.Type == PacketType.VideoFrame)
                    {
                        if (packet.Content == "STOP_VIDEO")
                        {
                            picRemote.Invoke(new Action(() =>
                            {
                                if (picRemote.Image != null) { picRemote.Image.Dispose(); picRemote.Image = null; }
                                picRemote.Invalidate();
                            }));
                        }
                        else
                        {
                            string base64 = _crypto.DecryptAES(packet.Payload);
                            byte[] imageBytes = Convert.FromBase64String(base64);
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            using (Image img = Image.FromStream(ms))
                            {
                                Bitmap bitmapToDisplay = new Bitmap(img);
                                picRemote.Invoke(new Action(() =>
                                {
                                    if (picRemote.Image != null) picRemote.Image.Dispose();
                                    picRemote.Image = bitmapToDisplay;
                                }));
                            }
                        }
                    }
                    else if (packet.Type == PacketType.AudioFrame)
                    {
                        string base64 = _crypto.DecryptAES(packet.Payload);
                        byte[] audioBytes = Convert.FromBase64String(base64);
                        if (_waveProvider != null)
                        {
                            _waveProvider.AddSamples(audioBytes, 0, audioBytes.Length);
                        }
                    }
                    else if (packet.Type == PacketType.File)
                    {
                        try
                        {
                            string base64File = _crypto.DecryptAES(packet.Payload);
                            byte[] fileBytes = Convert.FromBase64String(base64File);
                            string fileName = packet.Content;

                            string savePath = Path.Combine(Application.StartupPath, "ReceivedFiles");
                            if (!Directory.Exists(savePath))
                                Directory.CreateDirectory(savePath);

                            string fullPath = Path.Combine(savePath, $"{packet.Sender}_{fileName}");
                            File.WriteAllBytes(fullPath, fileBytes);

                            Log($"[{packet.Sender}] đã gửi một file: {fileName}");
                        }
                        catch { }
                    }
                }
            }
            catch { Log("Mất kết nối."); }
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text) || _writer == null || _crypto == null) return;
            try
            {
                string message = txtMessage.Text;
                Log($"[Tôi]: {message}");

                // LƯU LỊCH SỬ TIN NHẮN GỬI ĐI
                SaveChatHistory("Tôi", message);

                byte[] encryptedMessage = _crypto.EncryptAES(message);
                var packet = new Packet { Type = PacketType.Message, Sender = txtUsername.Text, Payload = encryptedMessage };
                await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                txtMessage.Clear();
            }
            catch { }
        }

        private async void btnSendFile_Click(object sender, EventArgs e)
        {
            if (_writer == null || _crypto == null)
            {
                MessageBox.Show("Vui lòng kết nối tới Server trước khi gửi file!");
                return;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Tất cả các file|*.*|Hình ảnh|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = ofd.FileName;
                        string fileName = Path.GetFileName(filePath);
                        byte[] fileBytes = File.ReadAllBytes(filePath);
                        string base64File = Convert.ToBase64String(fileBytes);
                        byte[] encryptedFile = _crypto.EncryptAES(base64File);

                        var packet = new Packet
                        {
                            Type = PacketType.File,
                            Sender = txtUsername.Text,
                            Content = fileName,
                            Payload = encryptedFile
                        };

                        await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                        Log($"[Tôi]: Đã gửi file {fileName}");
                    }
                    catch (Exception ex) { Log($"Lỗi khi gửi file: {ex.Message}"); }
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning) { videoSource.SignalToStop(); videoSource.WaitForStop(); }
            _waveIn?.Dispose();
            _waveOut?.Dispose();
        }

        private void Log(string m) => Invoke(new Action(() => rtbClientLogs.AppendText($"{m}\n")));

        private void picLocal_Click(object sender, EventArgs e) { }
    }
}