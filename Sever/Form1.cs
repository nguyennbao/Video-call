using SharedCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using AForge.Video;
using AForge.Video.DirectShow;

namespace Sever
{
    public partial class Form1 : Form
    {
        private TcpListener _server;
        private bool _isRunning = false;

        private ConcurrentDictionary<string, StreamWriter> _connectedClients = new ConcurrentDictionary<string, StreamWriter>();
        private ConcurrentDictionary<string, CryptoService> _clientKeys = new ConcurrentDictionary<string, CryptoService>();

        // TẠO DANH SÁCH TÀI KHOẢN HỢP LỆ CHO SERVER (Bạn có thể thêm bớt ở đây)
        private Dictionary<string, string> _validAccounts = new Dictionary<string, string>()
        {
            { "admin", "admin123" },
            { "bao", "123456" },
            { "test", "1111" }
        };

        // Âm thanh
        private WaveInEvent _waveIn;
        private WaveOutEvent _waveOut;
        private BufferedWaveProvider _waveProvider;
        private bool _isMicOn = false;

        // Camera Server
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private bool isServerVideoOn = false;
        private bool isSendingFrame = false;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private void SaveChatHistory(string sender, string message)
        {
            try
            {
                string filePath = Path.Combine(Application.StartupPath, "ChatHistory_Server.txt");
                string logLine = $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {sender}: {message}\r\n";
                File.AppendAllText(filePath, logLine);
            }
            catch { }
        }

        private void LoadChatHistory()
        {
            try
            {
                string filePath = Path.Combine(Application.StartupPath, "ChatHistory_Server.txt");
                if (File.Exists(filePath))
                {
                    string history = File.ReadAllText(filePath);
                    Invoke(new Action(() =>
                    {
                        rtbLogs.AppendText("--- LỊCH SỬ CHAT CŨ ---\n");
                        rtbLogs.AppendText(history);
                        rtbLogs.AppendText("-----------------------\n");
                    }));
                }
            }
            catch { }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadChatHistory();

            try
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count > 0)
                {
                    videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                    videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
                }
            }
            catch { Log("Không tìm thấy thiết bị Camera trên Server."); }

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
            catch { Log("Không tìm thấy thiết bị Âm thanh trên Server."); }
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
            catch (Exception ex) { Log($"Lỗi Server: {ex.Message}"); }
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

                    // XỬ LÝ ĐĂNG NHẬP
                    if (receivedPacket.Type == PacketType.Login)
                    {
                        LoginDTO loginInfo = JsonSerializer.Deserialize<LoginDTO>(receivedPacket.Content);

                        // Kiểm tra Username và Password có trong từ điển không
                        if (_validAccounts.ContainsKey(loginInfo.Username) && _validAccounts[loginInfo.Username] == loginInfo.Password)
                        {
                            username = loginInfo.Username;
                            Log($"[+] User '{username}' đăng nhập thành công!");

                            _connectedClients[username] = writer;

                            // 1. Phản hồi thành công để Client mở giao diện
                            var successPacket = new Packet { Type = PacketType.LoginResponse, Sender = "Server", Content = "SUCCESS" };
                            await writer.WriteLineAsync(JsonSerializer.Serialize(successPacket));

                            // 2. Gửi khóa bảo mật
                            var crypto = new CryptoService();
                            _clientKeys[username] = crypto;
                            var keyPacket = new Packet { Type = PacketType.KeyExchange, Sender = "Server", Payload = crypto.PublicKey };
                            await writer.WriteLineAsync(JsonSerializer.Serialize(keyPacket));
                        }
                        else
                        {
                            Log($"[-] User '{loginInfo.Username}' cố đăng nhập nhưng sai tài khoản/mật khẩu.");
                            // Phản hồi lỗi
                            var failPacket = new Packet { Type = PacketType.LoginResponse, Sender = "Server", Content = "FAIL" };
                            await writer.WriteLineAsync(JsonSerializer.Serialize(failPacket));
                            client.Close();
                            return;
                        }
                    }
                    else if (receivedPacket.Type == PacketType.KeyExchange)
                    {
                        _clientKeys[username].DeriveSharedSecret(receivedPacket.Payload);
                        Log($"[+] Đã thiết lập khóa bảo mật an toàn với '{username}'.");
                    }
                    else if (receivedPacket.Type == PacketType.Message)
                    {
                        string decryptedMessage = _clientKeys[username].DecryptAES(receivedPacket.Payload);
                        Log($"[Chat] {username} gửi: {decryptedMessage}");
                        SaveChatHistory(username, decryptedMessage);
                        BroadcastData(username, PacketType.Message, receivedPacket.Payload);
                    }
                    else if (receivedPacket.Type == PacketType.File)
                    {
                        try
                        {
                            string decryptedBase64 = _clientKeys[username].DecryptAES(receivedPacket.Payload);
                            byte[] fileBytes = Convert.FromBase64String(decryptedBase64);
                            string fileName = receivedPacket.Content;

                            string savePath = Path.Combine(Application.StartupPath, "ServerReceivedFiles");
                            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

                            string fullPath = Path.Combine(savePath, $"{username}_{fileName}");
                            File.WriteAllBytes(fullPath, fileBytes);
                            Log($"[Hệ thống] {username} vừa gửi file: {fileName}");

                            BroadcastData(username, PacketType.File, receivedPacket.Payload, receivedPacket.Content);
                        }
                        catch { }
                    }
                    else if (receivedPacket.Type == PacketType.VideoFrame)
                    {
                        if (receivedPacket.Content == "STOP_VIDEO")
                        {
                            this.Invoke(new Action(() =>
                            {
                                if (picClientVideo.Image != null) { picClientVideo.Image.Dispose(); picClientVideo.Image = null; }
                                picClientVideo.Invalidate();
                            }));
                            BroadcastData(username, PacketType.VideoFrame, receivedPacket.Payload, "STOP_VIDEO");
                        }
                        else
                        {
                            string decryptedBase64 = _clientKeys[username].DecryptAES(receivedPacket.Payload);
                            try
                            {
                                byte[] imageBytes = Convert.FromBase64String(decryptedBase64);
                                using (MemoryStream ms = new MemoryStream(imageBytes))
                                using (Image img = Image.FromStream(ms))
                                {
                                    Bitmap bitmapToDisplay = new Bitmap(img);
                                    this.Invoke(new Action(() =>
                                    {
                                        if (picClientVideo.Image != null) picClientVideo.Image.Dispose();
                                        picClientVideo.Image = bitmapToDisplay;
                                    }));
                                }
                            }
                            catch { }
                            BroadcastData(username, PacketType.VideoFrame, receivedPacket.Payload);
                        }
                    }
                    else if (receivedPacket.Type == PacketType.AudioFrame)
                    {
                        string decryptedBase64 = _clientKeys[username].DecryptAES(receivedPacket.Payload);
                        byte[] audioBytes = Convert.FromBase64String(decryptedBase64);

                        if (_waveProvider != null)
                        {
                            _waveProvider.AddSamples(audioBytes, 0, audioBytes.Length);
                        }

                        BroadcastData(username, PacketType.AudioFrame, receivedPacket.Payload);
                    }
                }
            }
            catch { }
            finally
            {
                if (!string.IsNullOrEmpty(username))
                {
                    _connectedClients.TryRemove(username, out _);
                    _clientKeys.TryRemove(username, out _);
                    Log($"[-] Client '{username}' đã ngắt kết nối.");
                }
                client.Close();
            }
        }

        private async void BroadcastData(string senderUsername, PacketType type, byte[] originalPayload, string content = null)
        {
            string decryptedBase64 = _clientKeys[senderUsername].DecryptAES(originalPayload);

            foreach (var targetClient in _connectedClients)
            {
                if (targetClient.Key != senderUsername)
                {
                    byte[] reEncrypted = _clientKeys[targetClient.Key].EncryptAES(decryptedBase64);
                    var forwardPacket = new Packet { Type = type, Sender = senderUsername, Payload = reEncrypted, Content = content };
                    await targetClient.Value.WriteLineAsync(JsonSerializer.Serialize(forwardPacket));
                }
            }
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            string message = txtMessage.Text;
            if (string.IsNullOrWhiteSpace(message) || _connectedClients.Count == 0) return;
            try
            {
                Log($"[Server]: {message}");
                SaveChatHistory("Server", message);

                foreach (var clientInfo in _connectedClients)
                {
                    if (_clientKeys.ContainsKey(clientInfo.Key))
                    {
                        byte[] encryptedMessage = _clientKeys[clientInfo.Key].EncryptAES(message);
                        var packet = new Packet { Type = PacketType.Message, Sender = "Server", Payload = encryptedMessage };
                        await clientInfo.Value.WriteLineAsync(JsonSerializer.Serialize(packet));
                    }
                }
                txtMessage.Clear();
            }
            catch { }
        }

        // --- CAMERA VÀ MIC SERVER (GIỮ NGUYÊN) ---
        private async void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!isServerVideoOn || isSendingFrame || _connectedClients.Count == 0) return;
            try
            {
                isSendingFrame = true;
                Bitmap frame = (Bitmap)eventArgs.Frame.Clone();

                picServerVideo.Invoke(new Action(() =>
                {
                    if (picServerVideo.Image != null) picServerVideo.Image.Dispose();
                    picServerVideo.Image = (Bitmap)frame.Clone();
                }));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (Bitmap resized = new Bitmap(frame, new Size(320, 240)))
                        resized.Save(ms, ImageFormat.Jpeg);

                    byte[] imageBytes = ms.ToArray();
                    string base64 = Convert.ToBase64String(imageBytes);

                    foreach (var clientInfo in _connectedClients)
                    {
                        string username = clientInfo.Key;
                        StreamWriter writer = clientInfo.Value;

                        if (_clientKeys.ContainsKey(username) && _clientKeys[username].SharedSecret != null)
                        {
                            byte[] encrypted = _clientKeys[username].EncryptAES(base64);
                            var packet = new Packet { Type = PacketType.VideoFrame, Sender = "Server", Payload = encrypted };
                            await writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                        }
                    }
                }
                frame.Dispose();
            }
            catch { }
            finally { isSendingFrame = false; }
        }

        private async void btnVideoServer_Click(object sender, EventArgs e)
        {
            if (videoSource == null) return;
            if (!isServerVideoOn)
            {
                isServerVideoOn = true;
                videoSource.Start();
                btnVideoServer.Text = "Tắt Video Server";
                Log("Đã bật camera Server.");
            }
            else
            {
                isServerVideoOn = false;
                videoSource.SignalToStop();

                btnVideoServer.Text = "Bật Video Server";
                if (picServerVideo.Image != null) { picServerVideo.Image.Dispose(); picServerVideo.Image = null; }
                picServerVideo.Invalidate();
                Log("Đã tắt camera Server.");

                await Task.Delay(300);

                foreach (var clientInfo in _connectedClients)
                {
                    if (_clientKeys.ContainsKey(clientInfo.Key))
                    {
                        byte[] encryptedStop = _clientKeys[clientInfo.Key].EncryptAES("STOP");
                        var packet = new Packet { Type = PacketType.VideoFrame, Sender = "Server", Content = "STOP_VIDEO", Payload = encryptedStop };
                        await clientInfo.Value.WriteLineAsync(JsonSerializer.Serialize(packet));
                    }
                }
            }
        }

        private async void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (!_isMicOn || _connectedClients.Count == 0) return;

            try
            {
                byte[] audioData = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, audioData, e.BytesRecorded);
                string base64 = Convert.ToBase64String(audioData);

                foreach (var clientInfo in _connectedClients)
                {
                    string username = clientInfo.Key;
                    StreamWriter writer = clientInfo.Value;

                    if (_clientKeys.ContainsKey(username) && _clientKeys[username].SharedSecret != null)
                    {
                        byte[] encrypted = _clientKeys[username].EncryptAES(base64);
                        var packet = new Packet { Type = PacketType.AudioFrame, Sender = "Server", Payload = encrypted };
                        await writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                    }
                }
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
                Log("Đã bật Mic Server.");
            }
            else
            {
                _waveIn.StopRecording();
                _isMicOn = false;
                btnMic.Text = "Bật Mic";
                Log("Đã tắt Mic Server.");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning) { videoSource.SignalToStop(); videoSource.WaitForStop(); }
            _waveIn?.Dispose();
            _waveOut?.Dispose();
        }

        private void Log(string message)
        {
            if (rtbLogs.InvokeRequired) rtbLogs.Invoke(new Action(() => Log(message)));
            else rtbLogs.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\r\n");
        }

        private void txtMessage_TextChanged(object sender, EventArgs e)
        {

        }
    }
}