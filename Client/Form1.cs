using SharedCore;
using System;
using System.Collections.Generic;
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

        // --- CÁC BIẾN CHO CHAT RIÊNG ---
        private string _currentTarget = "All";
        private ComboBox cmbUsers;
        private Label lblTarget;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
        }

        private void SetupPrivateChatUI()
        {
            // Tự động tạo Label và ComboBox chọn người nhận mà không cần kéo thả
            lblTarget = new Label() { Text = "Gửi tới:", Location = new Point(345, 338), AutoSize = true, Visible = false };
            cmbUsers = new ComboBox() { Location = new Point(410, 335), Size = new Size(130, 28), DropDownStyle = ComboBoxStyle.DropDownList, Visible = false };
            cmbUsers.Items.Add("All");
            cmbUsers.SelectedIndex = 0;

            cmbUsers.SelectedIndexChanged += (s, e) => {
                _currentTarget = cmbUsers.SelectedItem?.ToString() ?? "All";
                Log($"=> Đã chuyển chế độ gửi sang: {_currentTarget}");
            };

            this.Controls.Add(lblTarget);
            this.Controls.Add(cmbUsers);
        }

        private void ToggleUI(bool isLoggedIn)
        {
            rtbClientLogs.Visible = isLoggedIn;
            txtMessage.Visible = isLoggedIn;
            btnSend.Visible = isLoggedIn;
            btnSendFile.Visible = isLoggedIn;
            btnCall.Visible = isLoggedIn;
            btnMic.Visible = isLoggedIn;
            picLocal.Visible = isLoggedIn;
            picRemote.Visible = isLoggedIn;

            // Hiện chức năng chat riêng
            if (lblTarget != null) lblTarget.Visible = isLoggedIn;
            if (cmbUsers != null) cmbUsers.Visible = isLoggedIn;

            txtUsername.Enabled = !isLoggedIn;
            txtPassword.Enabled = !isLoggedIn;
            txtIP.Enabled = !isLoggedIn;
            btnConnect.Visible = !isLoggedIn;
        }

        private void SaveChatHistory(string sender, string message)
        {
            try
            {
                string filePath = Path.Combine(Application.StartupPath, "ChatHistory_Client.txt");
                string logLine = $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {sender} -> {_currentTarget}: {message}\r\n";
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

        private void Form1_Load(object sender, EventArgs e)
        {
            SetupPrivateChatUI(); // Tạo UI chat riêng
            ToggleUI(false);

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

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtIP.Text) || string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Vui lòng nhập IP Server, Tên đăng nhập và Mật khẩu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                btnConnect.Enabled = false;
                _client = new TcpClient();
                await _client.ConnectAsync(txtIP.Text, 8888);

                var stream = _client.GetStream();
                _writer = new StreamWriter(stream) { AutoFlush = true };
                _reader = new StreamReader(stream);

                var loginData = new LoginDTO { Username = txtUsername.Text, Password = txtPassword.Text };
                var packet = new Packet { Type = PacketType.Login, Sender = txtUsername.Text, Content = JsonSerializer.Serialize(loginData) };
                await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));

                _ = Task.Run(() => ListenToServer());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối tới Server: {ex.Message}", "Lỗi");
                btnConnect.Enabled = true;
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

                    if (packet.Type == PacketType.LoginResponse)
                    {
                        if (packet.Content == "SUCCESS")
                        {
                            Invoke(new Action(() => {
                                ToggleUI(true);
                                LoadChatHistory();
                                Log("Đăng nhập thành công! Bắt đầu kết nối bảo mật...");
                            }));
                        }
                        else
                        {
                            Invoke(new Action(() => {
                                MessageBox.Show("Đăng nhập thất bại: Sai tài khoản hoặc mật khẩu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                btnConnect.Enabled = true;
                            }));
                            _client.Close();
                            return;
                        }
                    }
                    else if (packet.Type == PacketType.KeyExchange)
                    {
                        _crypto = new CryptoService();
                        _crypto.DeriveSharedSecret(packet.Payload);
                        var response = new Packet { Type = PacketType.KeyExchange, Sender = txtUsername.Text, Payload = _crypto.PublicKey };
                        await _writer.WriteLineAsync(JsonSerializer.Serialize(response));
                        Log("Bảo mật AES đã sẵn sàng!");
                    }
                    // --- NHẬN DANH SÁCH USER TỪ SERVER ĐỂ CẬP NHẬT COMBOBOX ---
                    else if (packet.Type == PacketType.UserList)
                    {
                        var users = JsonSerializer.Deserialize<List<string>>(packet.Content);
                        Invoke(new Action(() => {
                            if (cmbUsers != null)
                            {
                                string currentSelection = cmbUsers.SelectedItem?.ToString();
                                cmbUsers.Items.Clear();
                                cmbUsers.Items.Add("All");
                                foreach (var u in users)
                                {
                                    if (u != txtUsername.Text) cmbUsers.Items.Add(u);
                                }

                                if (currentSelection != null && cmbUsers.Items.Contains(currentSelection))
                                    cmbUsers.SelectedItem = currentSelection;
                                else
                                    cmbUsers.SelectedIndex = 0;
                            }
                        }));
                    }
                    else if (packet.Type == PacketType.Message)
                    {
                        string msg = _crypto.DecryptAES(packet.Payload);
                        string privateTag = (packet.Target == txtUsername.Text) ? " [Riêng tư]" : "";
                        Log($"[{packet.Sender}]{privateTag}: {msg}");
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

                            string privateTag = (packet.Target == txtUsername.Text) ? " [File Riêng Tư]" : "";
                            Log($"[{packet.Sender}]{privateTag} đã gửi một file: {fileName}");
                        }
                        catch { }
                    }
                }
            }
            catch { Log("Mất kết nối với Server."); }
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

                var packet = new Packet { Type = PacketType.AudioFrame, Sender = txtUsername.Text, Target = _currentTarget, Payload = encrypted };
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
                Log($"Đã bật Mic (Đang phát cho: {_currentTarget}).");
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
                Log($"Đã bật camera (Đang phát cho: {_currentTarget}).");
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
                    var packet = new Packet { Type = PacketType.VideoFrame, Sender = txtUsername.Text, Target = _currentTarget, Content = "STOP_VIDEO", Payload = encryptedStop };
                    await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                }
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

                            var packet = new Packet { Type = PacketType.VideoFrame, Sender = txtUsername.Text, Target = _currentTarget, Payload = encrypted };
                            await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                        }
                    }
                    finally { isSendingFrame = false; }
                }
                frame.Dispose();
            }
            catch { isSendingFrame = false; }
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text) || _writer == null || _crypto == null) return;
            try
            {
                string message = txtMessage.Text;
                string privateTag = (_currentTarget != "All") ? $" [Tới {_currentTarget}]" : "";
                Log($"[Tôi]{privateTag}: {message}");

                SaveChatHistory("Tôi", message);

                byte[] encryptedMessage = _crypto.EncryptAES(message);
                var packet = new Packet { Type = PacketType.Message, Sender = txtUsername.Text, Target = _currentTarget, Payload = encryptedMessage };
                await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                txtMessage.Clear();
            }
            catch { }
        }

        private async void btnSendFile_Click(object sender, EventArgs e)
        {
            if (_writer == null || _crypto == null) return;

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
                            Target = _currentTarget,
                            Content = fileName,
                            Payload = encryptedFile
                        };

                        await _writer.WriteLineAsync(JsonSerializer.Serialize(packet));
                        string privateTag = (_currentTarget != "All") ? $" [Tới {_currentTarget}]" : "";
                        Log($"[Tôi]{privateTag}: Đã gửi file {fileName}");
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