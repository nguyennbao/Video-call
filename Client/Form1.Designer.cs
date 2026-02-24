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
            SuspendLayout();
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(63, 12);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(237, 27);
            txtUsername.TabIndex = 0;
            txtUsername.TextChanged += txtUsername_TextChanged;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(54, 364);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(181, 74);
            btnConnect.TabIndex = 1;
            btnConnect.Text = "Kết nối Đăng nhập";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // rtbClientLogs
            // 
            rtbClientLogs.Location = new Point(63, 65);
            rtbClientLogs.Name = "rtbClientLogs";
            rtbClientLogs.ReadOnly = true;
            rtbClientLogs.Size = new Size(342, 208);
            rtbClientLogs.TabIndex = 2;
            rtbClientLogs.Text = "";
            rtbClientLogs.TextChanged += rtbClientLogs_TextChanged;
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(138, 300);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(199, 27);
            txtMessage.TabIndex = 3;
            // 
            // btnSend
            // 
            btnSend.Location = new Point(302, 364);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(130, 62);
            btnSend.TabIndex = 4;
            btnSend.Text = "Gửi";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnSend);
            Controls.Add(txtMessage);
            Controls.Add(rtbClientLogs);
            Controls.Add(btnConnect);
            Controls.Add(txtUsername);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtUsername;
        private Button btnConnect;
        private RichTextBox rtbClientLogs;
        private TextBox txtMessage;
        private Button btnSend;
    }
}
