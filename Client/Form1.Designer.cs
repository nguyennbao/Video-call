namespace Client
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtUsername = new TextBox();
            btnConnect = new Button();
            rtbClientLogs = new RichTextBox();
            txtMessage = new TextBox();
            btnSend = new Button();
            btnSendFile = new Button();
            btnCall = new Button();
            picLocal = new PictureBox();
            picRemote = new PictureBox();
            btnMic = new Button();
            txtIP = new TextBox();
            txtPassword = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            ((System.ComponentModel.ISupportInitialize)picLocal).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picRemote).BeginInit();
            SuspendLayout();
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(137, 26);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(152, 27);
            txtUsername.TabIndex = 0;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(294, 360);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(184, 60);
            btnConnect.TabIndex = 1;
            btnConnect.Text = "Kết nối Đăng nhập";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // rtbClientLogs
            // 
            rtbClientLogs.Location = new Point(12, 166);
            rtbClientLogs.Name = "rtbClientLogs";
            rtbClientLogs.ReadOnly = true;
            rtbClientLogs.Size = new Size(277, 166);
            rtbClientLogs.TabIndex = 2;
            rtbClientLogs.Text = "";
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(12, 338);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(199, 27);
            txtMessage.TabIndex = 3;
            // 
            // btnSend
            // 
            btnSend.Location = new Point(151, 371);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(65, 36);
            btnSend.TabIndex = 4;
            btnSend.Text = "Gửi";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // btnSendFile
            // 
            btnSendFile.Location = new Point(12, 373);
            btnSendFile.Name = "btnSendFile";
            btnSendFile.Size = new Size(103, 34);
            btnSendFile.TabIndex = 5;
            btnSendFile.Text = "Gửi File Và Ảnh";
            btnSendFile.UseVisualStyleBackColor = true;
            btnSendFile.Click += btnSendFile_Click;
            // 
            // btnCall
            // 
            btnCall.Location = new Point(345, 283);
            btnCall.Name = "btnCall";
            btnCall.Size = new Size(94, 29);
            btnCall.TabIndex = 6;
            btnCall.Text = "Bật Video";
            btnCall.UseVisualStyleBackColor = true;
            btnCall.Click += btnCall_Click;
            // 
            // picLocal
            // 
            picLocal.Location = new Point(637, 304);
            picLocal.Name = "picLocal";
            picLocal.Size = new Size(151, 134);
            picLocal.SizeMode = PictureBoxSizeMode.Zoom;
            picLocal.TabIndex = 7;
            picLocal.TabStop = false;
            picLocal.Click += picLocal_Click;
            // 
            // picRemote
            // 
            picRemote.Location = new Point(345, 87);
            picRemote.Name = "picRemote";
            picRemote.Size = new Size(344, 190);
            picRemote.SizeMode = PictureBoxSizeMode.Zoom;
            picRemote.TabIndex = 8;
            picRemote.TabStop = false;
            // 
            // btnMic
            // 
            btnMic.Location = new Point(481, 284);
            btnMic.Name = "btnMic";
            btnMic.Size = new Size(111, 29);
            btnMic.TabIndex = 9;
            btnMic.Text = "Bật Mic";
            btnMic.UseVisualStyleBackColor = true;
            btnMic.Click += btnMic_Click_1;
            // 
            // txtIP
            // 
            txtIP.Location = new Point(137, 59);
            txtIP.Name = "txtIP";
            txtIP.Size = new Size(152, 27);
            txtIP.TabIndex = 10;
            txtIP.Text = "127.0.0.1";
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(137, 101);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.Size = new Size(152, 27);
            txtPassword.TabIndex = 11;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(19, 33);
            label1.Name = "label1";
            label1.Size = new Size(112, 20);
            label1.TabIndex = 12;
            label1.Text = "Tên Đăng Nhập";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(65, 66);
            label2.Name = "label2";
            label2.Size = new Size(66, 20);
            label2.TabIndex = 13;
            label2.Text = "IP Server";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(59, 108);
            label3.Name = "label3";
            label3.Size = new Size(72, 20);
            label3.TabIndex = 14;
            label3.Text = "Mật Khẩu";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtPassword);
            Controls.Add(txtIP);
            Controls.Add(btnMic);
            Controls.Add(picRemote);
            Controls.Add(picLocal);
            Controls.Add(btnCall);
            Controls.Add(btnSendFile);
            Controls.Add(btnSend);
            Controls.Add(txtMessage);
            Controls.Add(rtbClientLogs);
            Controls.Add(btnConnect);
            Controls.Add(txtUsername);
            Name = "Form1";
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)picLocal).EndInit();
            ((System.ComponentModel.ISupportInitialize)picRemote).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtUsername;
        private Button btnConnect;
        private RichTextBox rtbClientLogs;
        private TextBox txtMessage;
        private Button btnSend;
        private Button btnSendFile;
        private Button btnCall;
        private PictureBox picLocal;
        private PictureBox picRemote;
        private Button btnMic;
        private TextBox txtIP;
        private TextBox txtPassword;
        private Label label1;
        private Label label2;
        private Label label3;
    }
}
