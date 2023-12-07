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
    public partial class export_form : Form
    {
        private Panel panel1;
        private Button close_button;
        private Label label5;
        private ErrorProvider errorProvider1;
        private System.ComponentModel.IContainer components;
        public TextBox textBox1;
        private Label label1;
        private Label label2;
        private TextBox textBox2;
        private Button confirmButton;
        private Button button1;
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
        public event EventHandler<EventArgs> ConfirmClick;

        public export_form()
        {
            InitializeComponent();

            this.textBox1.Text = AppDomain.CurrentDomain.BaseDirectory;

        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(export_form));
            this.panel1 = new System.Windows.Forms.Panel();
            this.close_button = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.confirmButton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
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
            this.label5.Size = new System.Drawing.Size(95, 17);
            this.label5.TabIndex = 0;
            this.label5.Text = "Export project";
            // 
            // errorProvider1
            // 
            this.errorProvider1.ContainerControl = this;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 45);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(125, 22);
            this.label1.TabIndex = 12;
            this.label1.Text = "Export path: ";
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox1.ForeColor = System.Drawing.Color.White;
            this.textBox1.Location = new System.Drawing.Point(143, 45);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(304, 27);
            this.textBox1.TabIndex = 13;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 87);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 22);
            this.label2.TabIndex = 14;
            this.label2.Text = "Mode:";
            // 
            // textBox2
            // 
            this.textBox2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox2.ForeColor = System.Drawing.Color.White;
            this.textBox2.Location = new System.Drawing.Point(144, 89);
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.textBox2.Size = new System.Drawing.Size(334, 27);
            this.textBox2.TabIndex = 15;
            this.textBox2.Text = "Copy to box for play";
            // 
            // confirmButton
            // 
            this.confirmButton.BackColor = System.Drawing.Color.SteelBlue;
            this.confirmButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.confirmButton.Location = new System.Drawing.Point(189, 142);
            this.confirmButton.Name = "confirmButton";
            this.confirmButton.Size = new System.Drawing.Size(111, 34);
            this.confirmButton.TabIndex = 16;
            this.confirmButton.Text = "Confirm";
            this.confirmButton.UseVisualStyleBackColor = false;
            this.confirmButton.Click += new System.EventHandler(this.confirm_button_Click);
            // 
            // button1
            // 
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Image = ((System.Drawing.Image)(resources.GetObject("button1.Image")));
            this.button1.Location = new System.Drawing.Point(454, 45);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(30, 27);
            this.button1.TabIndex = 17;
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // export_form
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
            this.ClientSize = new System.Drawing.Size(490, 188);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.confirmButton);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "export_form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void close_button_Click(object sender, EventArgs e)
        {
            // Khi nút được nhấn, gọi sự kiện và truyền thông tin về control cha
            CloseClick?.Invoke(this, e);

            this.Close();
        }

        private void confirm_button_Click(object sender, EventArgs e)
        {
            this.Close();

            // Khi nút được nhấn, gọi sự kiện và truyền thông tin về control cha
            ConfirmClick?.Invoke(this, e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                // Set the initial selected path (optional)
                folderBrowserDialog.SelectedPath = @"C:\";

                // Show the dialog and get the result
                DialogResult result = folderBrowserDialog.ShowDialog();

                // Check if the user clicked OK
                if (result == DialogResult.OK)
                {
                    // Get the selected folder path
                    this.textBox1.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }
    }
}
