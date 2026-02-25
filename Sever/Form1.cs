using SharedCore;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave; // Thư viện âm thanh

namespace Sever
{
    public partial class Form1 : Form
    {
        private TcpListener _server;
        private bool _isRunning = false;

        private ConcurrentDictionary<string, StreamWriter> _connectedClients = new ConcurrentDictionary<string, StreamWriter>();
        private ConcurrentDictionary<string, CryptoService> _clientKeys = new ConcurrentDictionary<string, CryptoService>();

        // Biến âm thanh của Server
        private WaveInEvent _waveIn;
        private WaveOutEvent _waveOut;
        private BufferedWaveProvider _waveProvider;
        private bool _isMicOn = false;

        public Form1()
        {
            InitializeComponent();
            // Tự động liên kết sự kiện Form Load và Close
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // Khởi tạo Mic và Loa cho Server
                _waveIn = new WaveInEvent();
                _waveIn.WaveFormat = new WaveFormat(8000, 16, 1);
                _waveIn.DataAvailable += WaveIn_DataAvailable;

                _waveOut = new WaveOutEvent();
                _waveProvider = new BufferedWaveProvider(_waveIn.WaveFormat);
                _waveProvider.DiscardOnBufferOverflow = true; // Chống tràn RAM loa
                _waveOut.Init(_waveProvider);
                _waveOut.Play();
            }
            catch { Log("Không tìm thấy thiết bị Âm thanh trên Server."); }
        }

        private async void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (!_isMicOn || _connectedClients.Count == 0) return;

            try
            {
                byte[] audioData = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, audioData, e.BytesRecorded);
                string base64 = Convert.ToBase64String(audioData);

                // Server nói, mã hóa riêng cho từng Client và gửi đi
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
            catch { /* Bỏ qua lỗi mạng khi đang nói */ }
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
                            var keyPacket = new Packet { Type = PacketType.KeyExchange, Sender = "Server", Payload = crypto.PublicKey };
                            await writer.WriteLineAsync(JsonSerializer.Serialize(keyPacket));
                        }
                        else
                        {
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
                        BroadcastData(username, PacketType.Message, receivedPacket.Payload); // Gửi cho mọi người
                    }
                    else if (receivedPacket.Type == PacketType.File)
                    {
                        BroadcastData(username, PacketType.File, receivedPacket.Payload, receivedPacket.Content);
                    }
                    else if (receivedPacket.Type == PacketType.VideoFrame)
                    {
                        string decryptedBase64 = _clientKeys[username].DecryptAES(receivedPacket.Payload);
                        try
                        {
                            byte[] imageBytes = Convert.FromBase64String(decryptedBase64);
                            using (MemoryStream ms = new MemoryStream(imageBytes))
                            using (Image img = Image.FromStream(ms))
                            {
                                Bitmap bitmapToDisplay = new Bitmap(img);
                                this.Invoke(new Action(() => {
                                    if (picClientVideo.Image != null) picClientVideo.Image.Dispose();
                                    picClientVideo.Image = bitmapToDisplay;
                                }));
                            }
                        }
                        catch { }
                        BroadcastData(username, PacketType.VideoFrame, receivedPacket.Payload);
                    }
                    // SERVER NHẬN ÂM THANH: PHÁT RA LOA VÀ CHUYỂN TIẾP CHO NGƯỜI KHÁC
                    else if (receivedPacket.Type == PacketType.AudioFrame)
                    {
                        string decryptedBase64 = _clientKeys[username].DecryptAES(receivedPacket.Payload);
                        byte[] audioBytes = Convert.FromBase64String(decryptedBase64);

                        // 1. Phát ra loa của Server
                        if (_waveProvider != null)
                        {
                            _waveProvider.AddSamples(audioBytes, 0, audioBytes.Length);
                        }

                        // 2. Chuyển tiếp cho các Client khác
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

        // Hàm hỗ trợ trung chuyển dữ liệu gọn gàng hơn
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _waveIn?.Dispose();
            _waveOut?.Dispose();
        }

        private void Log(string message)
        {
            if (rtbLogs.InvokeRequired) rtbLogs.Invoke(new Action(() => Log(message)));
            else rtbLogs.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\r\n");
        }
    }
}