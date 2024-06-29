using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static WindowsFormsApp.setting_form;

namespace WindowsFormsApp
{
    public partial class notify_form : Form
    {
        private Panel panel1;
        private Label label5;
        private System.ComponentModel.IContainer components;
        private Label label1;
        private Button ok_button;
        private Panel panel2;
        private Button cancel_button;
        private Panel panel3;
        private Panel panel5;
        private Panel panel4;
        private Panel panel6;
        public int ProgressValue = 0;
        private bool confirmMode = false;

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("User32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);


        // Định nghĩa một delegate để đại diện cho sự kiện
        public delegate void ButtonClickEventHandler(object sender, EventArgs e);

        // Định nghĩa sự kiện bằng delegate ở trên
        public event EventHandler<EventArgs> ConfirmClick;

        public notify_form(bool confirmMode)
        {
            this.confirmMode = confirmMode;
            InitializeComponent();

            if (confirmMode)
                this.cancel_button.Visible = true;
        }

        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.ok_button = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.cancel_button = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel6 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.SteelBlue;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.label5);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(490, 30);
            this.panel1.TabIndex = 11;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(204, 7);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(74, 17);
            this.label5.TabIndex = 0;
            this.label5.Text = "Warning !!!";
            // 
            // ok_button
            // 
            this.ok_button.BackColor = System.Drawing.Color.SteelBlue;
            this.ok_button.Dock = System.Windows.Forms.DockStyle.Right;
            this.ok_button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ok_button.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ok_button.Location = new System.Drawing.Point(244, 0);
            this.ok_button.Name = "ok_button";
            this.ok_button.Size = new System.Drawing.Size(111, 32);
            this.ok_button.TabIndex = 17;
            this.ok_button.Text = "OK";
            this.ok_button.UseVisualStyleBackColor = false;
            this.ok_button.Click += new System.EventHandler(this.confirm_button_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(136, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 22);
            this.label1.TabIndex = 12;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 30);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(490, 67);
            this.panel2.TabIndex = 18;
            // 
            // cancel_button
            // 
            this.cancel_button.BackColor = System.Drawing.Color.Gray;
            this.cancel_button.Dock = System.Windows.Forms.DockStyle.Right;
            this.cancel_button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancel_button.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancel_button.Location = new System.Drawing.Point(367, 0);
            this.cancel_button.Name = "cancel_button";
            this.cancel_button.Size = new System.Drawing.Size(111, 32);
            this.cancel_button.TabIndex = 19;
            this.cancel_button.Text = "CANCEL";
            this.cancel_button.UseVisualStyleBackColor = false;
            this.cancel_button.Visible = false;
            this.cancel_button.Click += new System.EventHandler(this.cancel_button_Click);
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.ok_button);
            this.panel3.Controls.Add(this.panel5);
            this.panel3.Controls.Add(this.cancel_button);
            this.panel3.Controls.Add(this.panel4);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 97);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(490, 32);
            this.panel3.TabIndex = 20;
            // 
            // panel5
            // 
            this.panel5.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel5.Location = new System.Drawing.Point(355, 0);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(12, 32);
            this.panel5.TabIndex = 21;
            // 
            // panel4
            // 
            this.panel4.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel4.Location = new System.Drawing.Point(478, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(12, 32);
            this.panel4.TabIndex = 20;
            // 
            // panel6
            // 
            this.panel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel6.Location = new System.Drawing.Point(0, 129);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(490, 12);
            this.panel6.TabIndex = 22;
            // 
            // notify_form
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
            this.ClientSize = new System.Drawing.Size(490, 141);
            this.Controls.Add(this.panel6);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "notify_form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private void CenterLabelInPanel()
        {
            // Tính toán vị trí X và Y để căn giữa Label trong Panel
            int x = (panel2.Width - label1.Width) / 2;
            int y = (panel2.Height - label1.Height) / 2;

            // Đặt vị trí cho Label
            label1.Location = new System.Drawing.Point(x, y);
        }

        public void set_message(String message)
        {
            this.label1.Text = message;

            CenterLabelInPanel();
        }

        private void confirm_button_Click(object sender, EventArgs e)
        {
            if (this.confirmMode)
            {
                // Khi nút được nhấn, gọi sự kiện và truyền thông tin về control cha
                ConfirmClick?.Invoke(this, e);             
            }

            this.Close();
        }
        private void cancel_button_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
