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
            picClientVideo = new PictureBox();
            btnMic = new Button();
            ((System.ComponentModel.ISupportInitialize)picClientVideo).BeginInit();
            SuspendLayout();
            // 
            // btnStart
            // 
            btnStart.Location = new Point(95, 312);
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
            btnSend.Location = new Point(409, 312);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(181, 68);
            btnSend.TabIndex = 2;
            btnSend.Text = "Gửi";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(245, 253);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(211, 27);
            txtMessage.TabIndex = 3;
            // 
            // picClientVideo
            // 
            picClientVideo.Location = new Point(431, 12);
            picClientVideo.Name = "picClientVideo";
            picClientVideo.Size = new Size(338, 204);
            picClientVideo.TabIndex = 4;
            picClientVideo.TabStop = false;
            // 
            // btnMic
            // 
            btnMic.Location = new Point(658, 332);
            btnMic.Name = "btnMic";
            btnMic.Size = new Size(111, 29);
            btnMic.TabIndex = 10;
            btnMic.Text = "Bật/Tắt Mic";
            btnMic.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnMic);
            Controls.Add(picClientVideo);
            Controls.Add(txtMessage);
            Controls.Add(btnSend);
            Controls.Add(rtbLogs);
            Controls.Add(btnStart);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)picClientVideo).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnStart;
        private RichTextBox rtbLogs;
        private Button btnSend;
        private TextBox txtMessage;
        private PictureBox picClientVideo;
        private Button btnMic;
    }
}
