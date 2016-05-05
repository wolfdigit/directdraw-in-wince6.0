using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SmartDeviceProject2
{
    public partial class Form1 : Form
    {
        DirectDraw ddraw=null;
        public Form1()
        {
            InitializeComponent();
            ddraw = new DirectDraw(this, false);
        }

        int clkCnt=0;
        private void Form1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                //const int x1 = 10, x2 = 800, y1 = 10, y2 = 600;
                Rectangle rect = this.RectangleToScreen(this.ClientRectangle);
                int x1 = rect.X, x2 = rect.X + rect.Width, y1 = rect.Y, y2 = rect.Y + rect.Height;
                //MessageBox.Show(x1 + "," + y1);
                switch (clkCnt % 4)
                {
                    case 0:
                        ddraw.drawPxByPx(x1, y1, x2, y2, 0xF800);  // red
                        break;
                    case 1:
                        ddraw.drawPxByPx(x1, y1, x2, y2, 0xFFC0);  // yellow
                        break;
                    case 2:
                        ddraw.drawPxByPx(x1, y1, x2, y2, 0x07C0);  // green
                        break;
                    case 3:
                        ddraw.drawPxByPx(x1, y1, x2, y2, 0x003E);  // blue
                        break;
                }
                clkCnt++;
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (ddraw!=null) ddraw.resize();
        }
    }
}