using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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

        // Define a class to hold any data you want to pass
        public class ConfirmEventArgs : EventArgs
        {
            public string name { get; set; } // Example: Data to be passed from child to parent
            public string width_resolution { get; set; } // Example: Data to be passed from child to parent
            public string height_resolution { get; set; } // Example: Data to be passed from child to parent
            public string width_real { get; set; } // Example: Data to be passed from child to parent
            public string height_real { get; set; } // Example: Data to be passed from child to parent
            public string bittrate_select { get; set; } // Example: Data to be passed from child to parent

        }

        // Định nghĩa một delegate để đại diện cho sự kiện
        public delegate void ButtonClickEventHandler(object sender, EventArgs e);

        // Định nghĩa sự kiện bằng delegate ở trên
        public event EventHandler<ConfirmEventArgs> ConfirmClick;

        private void textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Kiểm tra nếu ký tự không phải là số hoặc không phải là ký tự điều khiển (ví dụ: backspace)
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                // Không cho phép nhập ký tự không phải số
                e.Handled = true;
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

            this.width_resolution.KeyPress += textBox_KeyPress;
            this.height_resolution.KeyPress += textBox_KeyPress;
            this.width_real.KeyPress += textBox_KeyPress;
            this.height_real.KeyPress += textBox_KeyPress;

            this.width_resolution.TextChanged += (sender, e) =>
            {
                this.width_real.Text = (sender as System.Windows.Forms.TextBox).Text;
            };
            this.height_resolution.TextChanged += (sender, e) =>
            {
                this.height_real.Text = (sender as System.Windows.Forms.TextBox).Text;
            };
        }

        public void set_name_program(string name)
        {
            this.name_program.Text = name;
        }

        public void set_resolution(string width, string height)
        {
            this.width_resolution.Text = width;
            this.height_resolution.Text = height;
        }

        public void set_resolution_real(string width, string height)
        {
            this.width_real.Text = width;
            this.height_real.Text = height;
        }

        public void set_bittrate(string bittrate)
        {
            this.bittrate_select.SelectedIndex = this.bittrate_select.FindString(bittrate); 
        }

        private void Confirm_Click(object sender, EventArgs e)
        {
            // Create an instance of ConfirmEventArgs and set any data you want to pass
            ConfirmEventArgs eventArgs = new ConfirmEventArgs
            {
                name = this.name_program.Text,
                width_resolution = this.width_resolution.Text,
                height_resolution = this.height_resolution.Text,
                width_real = this.width_real.Text,
                height_real = this.height_real.Text,
                bittrate_select = this.bittrate_select.Text
            };

            // Khi nút được nhấn, gọi sự kiện và truyền thông tin về control cha
            ConfirmClick?.Invoke(this, eventArgs);

            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
