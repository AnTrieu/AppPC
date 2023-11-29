using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static WindowsFormsApp.setting_form;

namespace WindowsFormsApp
{
    public partial class process_form : Form
    {
        private Panel panel1;
        private Button close_button;
        private Label label5;
        private ErrorProvider errorProvider1;
        private System.ComponentModel.IContainer components;
        public PictureBox progressBar1;
        public int ProgressValue = 0;

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

        // Định nghĩa một delegate để đại diện cho sự kiện
        public delegate void ButtonClickEventHandler(object sender, EventArgs e);

        // Định nghĩa sự kiện bằng delegate ở trên
        public event EventHandler<EventArgs> CloseClick;

        public process_form()
        {
            InitializeComponent();
        }
        private void picProgress_Paint(object sender, PaintEventArgs e)
        {
            PictureBox obj = sender as PictureBox;
            
            // Clear the background.
            e.Graphics.Clear(obj.BackColor);
            
            // Draw the progress bar.
            float fraction = (float)(ProgressValue - 0) / (100 - 0);
            int wid = (int)(fraction * obj.ClientSize.Width);
            e.Graphics.FillRectangle(Brushes.Green, 0, 0, wid, obj.ClientSize.Height);
            
            // Draw the text.
            e.Graphics.TextRenderingHint =
                TextRenderingHint.AntiAliasGridFit;
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                int percent = (int)(fraction * 100);
                if (percent == 200)
                {
                    e.Graphics.DrawString("Succeed", this.Font, Brushes.Black, obj.ClientRectangle, sf);
                }
                else if (percent == -1)
                {
                    e.Graphics.DrawString("Failed", this.Font, Brushes.Black, obj.ClientRectangle, sf);
                }
                else
                {
                    e.Graphics.DrawString(percent.ToString() + " %", this.Font, Brushes.Black, obj.ClientRectangle, sf);
                }
                  
            }
        }
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panel1 = new System.Windows.Forms.Panel();
            this.close_button = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
            this.progressBar1 = new System.Windows.Forms.PictureBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.progressBar1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.SteelBlue;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.close_button);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(490, 30);
            this.panel1.TabIndex = 11;
            // 
            // close_button
            // 
            this.close_button.BackgroundImage = global::WindowsFormsApp.Properties.Resources.close_white_icon;
            this.close_button.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.close_button.Dock = System.Windows.Forms.DockStyle.Right;
            this.close_button.FlatAppearance.BorderSize = 0;
            this.close_button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.close_button.Location = new System.Drawing.Point(458, 0);
            this.close_button.Name = "close_button";
            this.close_button.Size = new System.Drawing.Size(30, 28);
            this.close_button.TabIndex = 1;
            this.close_button.UseVisualStyleBackColor = true;
            this.close_button.Click += new System.EventHandler(this.close_button_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(204, 7);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(78, 17);
            this.label5.TabIndex = 0;
            this.label5.Text = "Processing";
            // 
            // errorProvider1
            // 
            this.errorProvider1.ContainerControl = this;
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.BackColor = System.Drawing.Color.White;
            this.progressBar1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.progressBar1.Location = new System.Drawing.Point(12, 61);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(466, 24);
            this.progressBar1.TabIndex = 3;
            this.progressBar1.TabStop = false;
            this.progressBar1.Paint += new System.Windows.Forms.PaintEventHandler(this.picProgress_Paint);
            // 
            // process_form
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
            this.ClientSize = new System.Drawing.Size(490, 103);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.progressBar1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "process_form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.progressBar1)).EndInit();
            this.ResumeLayout(false);

        }

        private void close_button_Click(object sender, EventArgs e)
        {
            // Khi nút được nhấn, gọi sự kiện và truyền thông tin về control cha
            CloseClick?.Invoke(this, e);

            this.Close();
        }
    }
}
