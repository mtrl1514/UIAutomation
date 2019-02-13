namespace UIAutomation
{
    partial class Win7UIAutomationClient
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
            this.listViewLinks = new System.Windows.Forms.ListView();
            this.columnHeaderLinkName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.buttonClose = new System.Windows.Forms.Button();
            this.labelMsgCount = new System.Windows.Forms.Label();
            this.labelMsgCountValue = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // listViewLinks
            // 
            this.listViewLinks.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.listViewLinks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderLinkName});
            this.listViewLinks.Location = new System.Drawing.Point(12, 12);
            this.listViewLinks.Name = "listViewLinks";
            this.listViewLinks.Size = new System.Drawing.Size(431, 286);
            this.listViewLinks.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listViewLinks.TabIndex = 1;
            this.listViewLinks.UseCompatibleStateImageBehavior = false;
            this.listViewLinks.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderLinkName
            // 
            this.columnHeaderLinkName.Text = "Hyperlink name";
            this.columnHeaderLinkName.Width = 260;
            // 
            // buttonRefresh
            // 
            this.buttonRefresh.Location = new System.Drawing.Point(12, 356);
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.Size = new System.Drawing.Size(70, 21);
            this.buttonRefresh.TabIndex = 7;
            this.buttonRefresh.Text = "&Refresh";
            this.buttonRefresh.UseVisualStyleBackColor = true;
            this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
            // 
            // buttonClose
            // 
            this.buttonClose.Location = new System.Drawing.Point(373, 356);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(70, 21);
            this.buttonClose.TabIndex = 10;
            this.buttonClose.Text = "&Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // labelMsgCount
            // 
            this.labelMsgCount.AutoSize = true;
            this.labelMsgCount.Location = new System.Drawing.Point(12, 310);
            this.labelMsgCount.Name = "labelMsgCount";
            this.labelMsgCount.Size = new System.Drawing.Size(174, 12);
            this.labelMsgCount.TabIndex = 11;
            this.labelMsgCount.Text = "Numbers of Messeges found:";
            // 
            // labelMsgCountValue
            // 
            this.labelMsgCountValue.AutoSize = true;
            this.labelMsgCountValue.Location = new System.Drawing.Point(189, 310);
            this.labelMsgCountValue.Name = "labelMsgCountValue";
            this.labelMsgCountValue.Size = new System.Drawing.Size(11, 12);
            this.labelMsgCountValue.TabIndex = 12;
            this.labelMsgCountValue.Text = "0";
            // 
            // Win7UIAutomationClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(456, 392);
            this.Controls.Add(this.labelMsgCountValue);
            this.Controls.Add(this.labelMsgCount);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.buttonRefresh);
            this.Controls.Add(this.listViewLinks);
            this.Name = "Win7UIAutomationClient";
            this.Text = "HookMessanger";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Win7UIAutomationClient_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView listViewLinks;
        private System.Windows.Forms.ColumnHeader columnHeaderLinkName;
        private System.Windows.Forms.Button buttonRefresh;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.Label labelMsgCount;
        private System.Windows.Forms.Label labelMsgCountValue;
    }
}

