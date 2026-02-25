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
            ((System.ComponentModel.ISupportInitialize)picLocal).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picRemote).BeginInit();
            SuspendLayout();
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(256, 12);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(237, 27);
            txtUsername.TabIndex = 0;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(12, 372);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(136, 45);
            btnConnect.TabIndex = 1;
            btnConnect.Text = "Kết nối Đăng nhập";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // rtbClientLogs
            // 
            rtbClientLogs.Location = new Point(29, 87);
            rtbClientLogs.Name = "rtbClientLogs";
            rtbClientLogs.ReadOnly = true;
            rtbClientLogs.Size = new Size(277, 166);
            rtbClientLogs.TabIndex = 2;
            rtbClientLogs.Text = "";
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(36, 301);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(199, 27);
            txtMessage.TabIndex = 3;
            // 
            // btnSend
            // 
            btnSend.Location = new Point(189, 379);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(87, 45);
            btnSend.TabIndex = 4;
            btnSend.Text = "Gửi";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // btnSendFile
            // 
            btnSendFile.Location = new Point(321, 372);
            btnSendFile.Name = "btnSendFile";
            btnSendFile.Size = new Size(113, 59);
            btnSendFile.TabIndex = 5;
            btnSendFile.Text = "Gửi File Và Ảnh";
            btnSendFile.UseVisualStyleBackColor = true;
            btnSendFile.Click += btnSendFile_Click;
            // 
            // btnCall
            // 
            btnCall.Location = new Point(645, 380);
            btnCall.Name = "btnCall";
            btnCall.Size = new Size(94, 29);
            btnCall.TabIndex = 6;
            btnCall.Text = "Gọi Video";
            btnCall.UseVisualStyleBackColor = true;
            btnCall.Click += btnCall_Click;
            // 
            // picLocal
            // 
            picLocal.Location = new Point(409, 79);
            picLocal.Name = "picLocal";
            picLocal.Size = new Size(248, 199);
            picLocal.SizeMode = PictureBoxSizeMode.Zoom;
            picLocal.TabIndex = 7;
            picLocal.TabStop = false;
            // 
            // picRemote
            // 
            picRemote.Location = new Point(663, 241);
            picRemote.Name = "picRemote";
            picRemote.Size = new Size(125, 133);
            picRemote.SizeMode = PictureBoxSizeMode.Zoom;
            picRemote.TabIndex = 8;
            picRemote.TabStop = false;
            // 
            // btnMic
            // 
            btnMic.Location = new Point(490, 379);
            btnMic.Name = "btnMic";
            btnMic.Size = new Size(111, 29);
            btnMic.TabIndex = 9;
            btnMic.Text = "Bật Mic";
            btnMic.UseVisualStyleBackColor = true;
            btnMic.Click += btnMic_Click_1;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
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
    }
}
