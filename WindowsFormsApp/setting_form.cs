using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApp
{
    public partial class setting_form : Form
    {
        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("User32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            const int WM_NCPAINT = 0x85;
            base.WndProc(ref m);

            if (m.Msg == WM_NCPAINT)
            {

                IntPtr hdc = GetWindowDC(m.HWnd);
                if ((int)hdc != 0)
                {
                    Graphics g = Graphics.FromHdc(hdc);
                    g.FillRectangle(Brushes.Green, 10, 0, 4800, 23);
                    g.Flush();
                    ReleaseDC(m.HWnd, hdc);
                }

            }
        }

        public setting_form()
        {
            InitializeComponent();

            // Set program name default
            DateTime currentTime = DateTime.UtcNow;
            long epochTime = (long)(currentTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            this.name_program.Text = "Program_" + epochTime;

            this.width_resolution.Text = "1920";
            this.height_resolution.Text = "1080";

            this.width_real.Text = "1920";
            this.height_real.Text = "1080";

            this.bittrate_select.SelectedIndex = 5;
            this.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
