namespace SKYNET.Controls
{
    partial class SKYNET_CloseBox
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
            this.Icon = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.Icon)).BeginInit();
            this.SuspendLayout();
            // 
            // Icon
            // 
            this.Icon.BackColor = System.Drawing.Color.Transparent;
            this.Icon.Image = global::SKYNET.Properties.Resources.close;
            this.Icon.Location = new System.Drawing.Point(10, 7);
            this.Icon.Name = "Icon";
            this.Icon.Size = new System.Drawing.Size(13, 12);
            this.Icon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.Icon.TabIndex = 5;
            this.Icon.TabStop = false;
            this.Icon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnClicked);
            this.Icon.MouseLeave += new System.EventHandler(this.OnMouseLeave);
            this.Icon.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnMouseMove);
            // 
            // SKYNET_CloseBox
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(54)))), ((int)(((byte)(68)))));
            this.Controls.Add(this.Icon);
            this.Name = "SKYNET_CloseBox";
            this.Size = new System.Drawing.Size(34, 26);
            this.SizeChanged += new System.EventHandler(this.CloseBox_SizeChanged);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.OnClicked);
            this.MouseLeave += new System.EventHandler(this.OnMouseLeave);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnMouseMove);
            ((System.ComponentModel.ISupportInitialize)(this.Icon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox Icon;
    }
}
