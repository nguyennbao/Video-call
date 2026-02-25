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
using NAudio.Wave; // Thư viện âm thanh

namespace Client
{
    public partial class Form1 : Form
    {
        private TcpClient _client;
        private StreamWriter _writer;
        private StreamReader _reader;
        private CryptoService _crypto;

        // Biến Hình ảnh
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private bool isCalling = false;
        private bool isSendingFrame = false;

        // Biến Âm thanh
        private WaveInEvent _waveIn;
        private WaveOutEvent _waveOut;
        private BufferedWaveProvider _waveProvider;
        private bool _isMicOn = false;

        public Form1()
        {
            InitializeComponent();
            // Tự động liên kết sự kiện
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Cài đặt Camera
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count > 0)
            {
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
            }

            // Cài đặt Mic và Loa
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

        // --- XỬ LÝ ÂM THANH ---
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

        private void btnMic_Click(object sender, EventArgs e)
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

        // --- XỬ LÝ HÌNH ẢNH ---
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
                if (picLocal.Image != null) { picLocal.Image.Dispose(); picLocal.Image = null; }
                Log("Đã tắt camera.");
            }
        }

        private async void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
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

        // --- XỬ LÝ KẾT NỐI VÀ DATA ---
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
                    }
                    else if (packet.Type == PacketType.VideoFrame)
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
                    // NHẬN ÂM THANH TỪ SERVER VÀ PHÁT RA LOA
                    else if (packet.Type == PacketType.AudioFrame)
                    {
                        string base64 = _crypto.DecryptAES(packet.Payload);
                        byte[] audioBytes = Convert.FromBase64String(base64);
                        if (_waveProvider != null)
                        {
                            _waveProvider.AddSamples(audioBytes, 0, audioBytes.Length);
                        }
                    }
                }
            }
            catch { Log("Mất kết nối."); }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning) { videoSource.SignalToStop(); videoSource.WaitForStop(); }
            _waveIn?.Dispose();
            _waveOut?.Dispose();
        }

        private void Log(string m) => Invoke(new Action(() => rtbClientLogs.AppendText($"{m}\n")));
        private async void btnSend_Click(object sender, EventArgs e) { /* Giữ code cũ để tránh quá dài */ }
        private async void btnSendFile_Click(object sender, EventArgs e) { /* Giữ code cũ để tránh quá dài */ }

        private void btnMic_Click_1(object sender, EventArgs e)
        {

        }
    }
}