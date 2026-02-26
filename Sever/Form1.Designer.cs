namespace Sever
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
            btnStart = new Button();
            rtbLogs = new RichTextBox();
            btnSend = new Button();
            txtMessage = new TextBox();
            picServerVideo = new PictureBox();
            btnMic = new Button();
            btnVideoServer = new Button();
            picClientVideo = new PictureBox();
            label4 = new Label();
            ((System.ComponentModel.ISupportInitialize)picServerVideo).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picClientVideo).BeginInit();
            SuspendLayout();
            // 
            // btnStart
            // 
            btnStart.Location = new Point(21, 322);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(230, 68);
            btnStart.TabIndex = 0;
            btnStart.Text = "Khởi động Server";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click_1;
            // 
            // rtbLogs
            // 
            rtbLogs.Location = new Point(21, 12);
            rtbLogs.Name = "rtbLogs";
            rtbLogs.ReadOnly = true;
            rtbLogs.Size = new Size(334, 204);
            rtbLogs.TabIndex = 1;
            rtbLogs.Text = "";
            // 
            // btnSend
            // 
            btnSend.Location = new Point(269, 236);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(123, 44);
            btnSend.TabIndex = 2;
            btnSend.Text = "Gửi";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(52, 245);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(211, 27);
            txtMessage.TabIndex = 3;
            txtMessage.TextChanged += txtMessage_TextChanged;
            // 
            // picServerVideo
            // 
            picServerVideo.Location = new Point(558, 290);
            picServerVideo.Name = "picServerVideo";
            picServerVideo.Size = new Size(215, 128);
            picServerVideo.SizeMode = PictureBoxSizeMode.Zoom;
            picServerVideo.TabIndex = 4;
            picServerVideo.TabStop = false;
            // 
            // btnMic
            // 
            btnMic.Location = new Point(423, 332);
            btnMic.Name = "btnMic";
            btnMic.Size = new Size(111, 52);
            btnMic.TabIndex = 10;
            btnMic.Text = "Bật/Tắt Mic";
            btnMic.UseVisualStyleBackColor = true;
            btnMic.Click += btnMic_Click;
            // 
            // btnVideoServer
            // 
            btnVideoServer.Location = new Point(281, 332);
            btnVideoServer.Name = "btnVideoServer";
            btnVideoServer.Size = new Size(111, 48);
            btnVideoServer.TabIndex = 11;
            btnVideoServer.Text = "Bật Video";
            btnVideoServer.UseVisualStyleBackColor = true;
            btnVideoServer.Click += btnVideoServer_Click;
            // 
            // picClientVideo
            // 
            picClientVideo.Location = new Point(423, 23);
            picClientVideo.Name = "picClientVideo";
            picClientVideo.Size = new Size(312, 231);
            picClientVideo.SizeMode = PictureBoxSizeMode.Zoom;
            picClientVideo.TabIndex = 12;
            picClientVideo.TabStop = false;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(7, 248);
            label4.Name = "label4";
            label4.Size = new Size(39, 20);
            label4.TabIndex = 16;
            label4.Text = "Chat";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label4);
            Controls.Add(picClientVideo);
            Controls.Add(btnVideoServer);
            Controls.Add(btnMic);
            Controls.Add(picServerVideo);
            Controls.Add(txtMessage);
            Controls.Add(btnSend);
            Controls.Add(rtbLogs);
            Controls.Add(btnStart);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)picServerVideo).EndInit();
            ((System.ComponentModel.ISupportInitialize)picClientVideo).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnStart;
        private RichTextBox rtbLogs;
        private Button btnSend;
        private TextBox txtMessage;
        private PictureBox picServerVideo;
        private Button btnMic;
        private Button btnVideoServer;
        private PictureBox picClientVideo;
        private Label label4;
    }
}
