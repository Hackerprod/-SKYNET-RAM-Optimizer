using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SKYNET.Controls
{
    public partial class SKYNET_MinimizeBox : UserControl
    {
        private Color color;
        private Color focusedColor;
        private int iconSize;

        [Category("SKYNET")]
        public event EventHandler Clicked;

        [Category("SKYNET")]
        public Color Color
        {
            get
            {
                return color;
            }
            set
            {
                color = value;
                BackColor = value;

                int R = value.R < 245 ? value.R + 10 : 255;
                int G = value.G < 245 ? value.G + 10 : 255;
                int B = value.B < 245 ? value.B + 10 : 255;

                FocusedColor = Color.FromArgb(R, G, B);
            }
        }
        
        [Category("SKYNET")]
        public Color FocusedColor
        {
            get
            {
                return focusedColor;
            }
            set
            {
                focusedColor = value;
            }
        }

        [Category("SKYNET")]
        public int IconSize
        {
            get
            {
                return iconSize;
            }
            set
            {
                iconSize = value;
                Icon.Size = new Size(iconSize, iconSize);
                CenterIcon();
            }
        }

        public SKYNET_MinimizeBox()
        {
            InitializeComponent();
            Size = new Size(34, 26);
            iconSize = Icon.Width;
        }

        private void OnClicked(object sender, MouseEventArgs e)
        {
            Clicked?.Invoke(this, new EventArgs());
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            BackColor = FocusedColor;
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            BackColor = Color;
        }

        private void MinimizeBox_SizeChanged(object sender, EventArgs e)
        {
            CenterIcon();
        }

        private void CenterIcon()
        {
            int X = (this.Width - Icon.Width) / 2;
            int Y = (this.Height - Icon.Height) / 2;

            Icon.Location = new Point(X, Y);
        }
    }
}
