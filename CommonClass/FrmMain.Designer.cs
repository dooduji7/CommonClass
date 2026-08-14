namespace CommonClass
{
    partial class FrmMain
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnTest = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.btnLog = new System.Windows.Forms.Button();
            this.btnSocketTest = new System.Windows.Forms.Button();
            this.btnSocket2 = new System.Windows.Forms.Button();
            this.btnSockset3 = new System.Windows.Forms.Button();
            this.btnSockset4 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnTest
            // 
            this.btnTest.Location = new System.Drawing.Point(14, 15);
            this.btnTest.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(86, 29);
            this.btnTest.TabIndex = 0;
            this.btnTest.Text = "Test";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(14, 136);
            this.button1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(86, 29);
            this.button1.TabIndex = 0;
            this.button1.Text = "Test";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.btnTest_Click);
            // 
            // btnLog
            // 
            this.btnLog.Location = new System.Drawing.Point(14, 254);
            this.btnLog.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnLog.Name = "btnLog";
            this.btnLog.Size = new System.Drawing.Size(86, 29);
            this.btnLog.TabIndex = 0;
            this.btnLog.Text = "LogTest";
            this.btnLog.UseVisualStyleBackColor = true;
            this.btnLog.Click += new System.EventHandler(this.btnLog_Click);
            // 
            // btnSocketTest
            // 
            this.btnSocketTest.Location = new System.Drawing.Point(589, 15);
            this.btnSocketTest.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnSocketTest.Name = "btnSocketTest";
            this.btnSocketTest.Size = new System.Drawing.Size(242, 29);
            this.btnSocketTest.TabIndex = 0;
            this.btnSocketTest.Text = "SocketTest";
            this.btnSocketTest.UseVisualStyleBackColor = true;
            this.btnSocketTest.Click += new System.EventHandler(this.btnSocketTest_Click);
            // 
            // btnSocket2
            // 
            this.btnSocket2.Location = new System.Drawing.Point(589, 52);
            this.btnSocket2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnSocket2.Name = "btnSocket2";
            this.btnSocket2.Size = new System.Drawing.Size(242, 29);
            this.btnSocket2.TabIndex = 0;
            this.btnSocket2.Text = "SocketTest(Dsipose)";
            this.btnSocket2.UseVisualStyleBackColor = true;
            this.btnSocket2.Click += new System.EventHandler(this.btnSocket2_Click);
            // 
            // btnSockset3
            // 
            this.btnSockset3.Location = new System.Drawing.Point(589, 89);
            this.btnSockset3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnSockset3.Name = "btnSockset3";
            this.btnSockset3.Size = new System.Drawing.Size(242, 29);
            this.btnSockset3.TabIndex = 0;
            this.btnSockset3.Text = "SocketTest(연결확인)";
            this.btnSockset3.UseVisualStyleBackColor = true;
            this.btnSockset3.Click += new System.EventHandler(this.btnSockset3_Click);
            // 
            // btnSockset4
            // 
            this.btnSockset4.Location = new System.Drawing.Point(589, 126);
            this.btnSockset4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnSockset4.Name = "btnSockset4";
            this.btnSockset4.Size = new System.Drawing.Size(242, 29);
            this.btnSockset4.TabIndex = 0;
            this.btnSockset4.Text = "SocketTest(ReceiveLength)";
            this.btnSockset4.UseVisualStyleBackColor = true;
            this.btnSockset4.Click += new System.EventHandler(this.btnSockset4_Click);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(914, 562);
            this.Controls.Add(this.btnLog);
            this.Controls.Add(this.btnSockset4);
            this.Controls.Add(this.btnSockset3);
            this.Controls.Add(this.btnSocket2);
            this.Controls.Add(this.btnSocketTest);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnTest);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "FrmMain";
            this.Text = "TestForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnLog;
        private System.Windows.Forms.Button btnSocketTest;
        private System.Windows.Forms.Button btnSocket2;
        private System.Windows.Forms.Button btnSockset3;
        private System.Windows.Forms.Button btnSockset4;
    }
}

