namespace SKYNET.GUI.Controls
{
    partial class ProcessControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.LB_Name = new System.Windows.Forms.Label();
            this.LB_Usage = new System.Windows.Forms.Label();
            this.PN_Container = new System.Windows.Forms.Panel();
            this.PB_Icon = new System.Windows.Forms.PictureBox();
            this.PB_Kill = new System.Windows.Forms.PictureBox();
            this.PN_Container.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PB_Icon)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PB_Kill)).BeginInit();
            this.SuspendLayout();
            // 
            // LB_Name
            // 
            this.LB_Name.Font = new System.Drawing.Font("Segoe UI Emoji", 9.75F);
            this.LB_Name.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.LB_Name.Location = new System.Drawing.Point(64, 10);
            this.LB_Name.Name = "LB_Name";
            this.LB_Name.Size = new System.Drawing.Size(177, 35);
            this.LB_Name.TabIndex = 21;
            this.LB_Name.Text = "[SKYNET] RAM Optimizer";
            this.LB_Name.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.LB_Name.MouseLeave += new System.EventHandler(this.Control_MouseLeave);
            this.LB_Name.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Control_MouseMove);
            // 
            // LB_Usage
            // 
            this.LB_Usage.AutoSize = true;
            this.LB_Usage.Font = new System.Drawing.Font("Segoe UI Emoji", 9.75F);
            this.LB_Usage.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.LB_Usage.Location = new System.Drawing.Point(260, 20);
            this.LB_Usage.Name = "LB_Usage";
            this.LB_Usage.Size = new System.Drawing.Size(48, 17);
            this.LB_Usage.TabIndex = 24;
            this.LB_Usage.Text = "100 KB";
            this.LB_Usage.MouseLeave += new System.EventHandler(this.Control_MouseLeave);
            this.LB_Usage.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Control_MouseMove);
            // 
            // PN_Container
            // 
            this.PN_Container.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.PN_Container.Controls.Add(this.PB_Icon);
            this.PN_Container.Controls.Add(this.LB_Usage);
            this.PN_Container.Controls.Add(this.LB_Name);
            this.PN_Container.Controls.Add(this.PB_Kill);
            this.PN_Container.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PN_Container.Location = new System.Drawing.Point(1, 1);
            this.PN_Container.Name = "PN_Container";
            this.PN_Container.Size = new System.Drawing.Size(368, 54);
            this.PN_Container.TabIndex = 25;
            this.PN_Container.MouseLeave += new System.EventHandler(this.Control_MouseLeave);
            this.PN_Container.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Control_MouseMove);
            // 
            // PB_Icon
            // 
            this.PB_Icon.Image = global::SKYNET.Properties.Resources.clipart1330334;
            this.PB_Icon.Location = new System.Drawing.Point(11, 10);
            this.PB_Icon.Name = "PB_Icon";
            this.PB_Icon.Size = new System.Drawing.Size(35, 35);
            this.PB_Icon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.PB_Icon.TabIndex = 0;
            this.PB_Icon.TabStop = false;
            this.PB_Icon.MouseLeave += new System.EventHandler(this.Control_MouseLeave);
            this.PB_Icon.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Control_MouseMove);
            // 
            // PB_Kill
            // 
            this.PB_Kill.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PB_Kill.Image = global::SKYNET.Properties.Resources.remove_48px;
            this.PB_Kill.Location = new System.Drawing.Point(335, 17);
            this.PB_Kill.Name = "PB_Kill";
            this.PB_Kill.Size = new System.Drawing.Size(20, 20);
            this.PB_Kill.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.PB_Kill.TabIndex = 23;
            this.PB_Kill.TabStop = false;
            this.PB_Kill.Click += new System.EventHandler(this.PB_Kill_Click);
            this.PB_Kill.MouseLeave += new System.EventHandler(this.Control_MouseLeave);
            this.PB_Kill.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Control_MouseMove);
            // 
            // ProcessControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(58)))), ((int)(((byte)(58)))), ((int)(((byte)(58)))));
            this.Controls.Add(this.PN_Container);
            this.Name = "ProcessControl";
            this.Padding = new System.Windows.Forms.Padding(1);
            this.Size = new System.Drawing.Size(370, 56);
            this.PN_Container.ResumeLayout(false);
            this.PN_Container.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PB_Icon)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PB_Kill)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox PB_Icon;
        private System.Windows.Forms.Label LB_Name;
        private System.Windows.Forms.PictureBox PB_Kill;
        private System.Windows.Forms.Label LB_Usage;
        private System.Windows.Forms.Panel PN_Container;
    }
}
