using FFmpeg.AutoGen;
using Newtonsoft.Json;
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
    public partial class upload_form : Form
    {
        private Panel panel1;
        private Button close_button;
        private Label label5;
        private System.ComponentModel.IContainer components;
        private Button confirmButton;
        private Button cancleButton;
        private Panel panel3;
        private Panel panel2;
        private Label device_counter;
        private Label program_counter;
        private CheckBox sync_mode;
        private Panel panel4;
        private Panel panel5;
        private TableLayoutPanel tableLayoutPanel1;
        private Label label3;
        private Label label1;
        private Label label2;
        private TableLayoutPanel tableLayoutPanel2;
        private Label label4;
        private Label label6;
        private Label label7;
        private Panel panel6;
        private List<string> program_list;
        private List<string> device_list;

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("User32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);


        // Định nghĩa một delegate để đại diện cho sự kiện
        public delegate void ButtonClickEventHandler(object sender, EventArgs e);

        // Define a class to hold any data you want to pass
        public class ConfirmEventArgs : EventArgs
        {
            public List<string> program_list { get; set; }
            public List<string> device_list { get; set; }
            public bool sync_mode { get; set; }
        }

        // Định nghĩa sự kiện bằng delegate ở trên
        public event EventHandler<EventArgs> CloseClick;
        public event EventHandler<ConfirmEventArgs> ConfirmClick;

        public upload_form(List<string> program_list, List<string> device_list)
        {
            this.program_list = program_list;
            this.device_list = device_list;

            InitializeComponent();

            program_list.Reverse();
            device_list.Reverse();

            // Add layout
            foreach (string program in program_list)
            {
                var info_program = JsonConvert.DeserializeObject<Info_Program>(program);

                Label label1 = new Label(); 
                label1.AutoSize = true;
                label1.Dock = System.Windows.Forms.DockStyle.Fill;
                label1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                label1.Location = new System.Drawing.Point(0, 0);
                label1.Margin = new System.Windows.Forms.Padding(0);
                label1.Size = new System.Drawing.Size(186, 29);
                label1.TabIndex = 1;
                label1.Text = info_program.Name;
                label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

                Label label2 = new Label();
                label2.AutoSize = true;
                label2.Dock = System.Windows.Forms.DockStyle.Fill;
                label2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                label2.Location = new System.Drawing.Point(325, 0);
                label2.Margin = new System.Windows.Forms.Padding(0);
                label2.Size = new System.Drawing.Size(140, 29);
                label2.TabIndex = 3;
                label2.Text = $"{info_program.width_real} x {info_program.height_real}";
                label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

                Label label3 = new Label();
                label3.AutoSize = true;
                label3.Dock = System.Windows.Forms.DockStyle.Fill;
                label3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                label3.Location = new System.Drawing.Point(186, 0);
                label3.Margin = new System.Windows.Forms.Padding(0);
                label3.Size = new System.Drawing.Size(139, 29);
                label3.TabIndex = 2;
                label3.Text = $"{info_program.width_resolution} x {info_program.height_resolution}";
                label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

                TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
                tableLayoutPanel.ColumnCount = 3;
                tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
                tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
                tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
                tableLayoutPanel.Controls.Add(label1, 0, 0);
                tableLayoutPanel.Controls.Add(label2, 2, 0);
                tableLayoutPanel.Controls.Add(label3, 1, 0);
                tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
                tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
                tableLayoutPanel.RowCount = 1;
                tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
                tableLayoutPanel.Size = new System.Drawing.Size(465, 29);
                tableLayoutPanel.TabIndex = 0;

                Panel panel = new Panel();
                panel.Controls.Add(tableLayoutPanel);
                panel.Dock = System.Windows.Forms.DockStyle.Top;
                panel.Location = new System.Drawing.Point(0, 29);
                panel.Size = new System.Drawing.Size(465, 29);
                panel.TabIndex = 1;

                this.panel2.Controls.Add(panel);

                // Set the panel to index 0 in panel2
                this.panel2.Controls.SetChildIndex(panel, 0);
            }

            foreach (string device in device_list)
            {
                var info_device = JsonConvert.DeserializeObject<Info_send_device>(device);

                Label label1 = new Label();
                label1.AutoSize = true;
                label1.Dock = System.Windows.Forms.DockStyle.Fill;
                label1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                label1.Location = new System.Drawing.Point(0, 0);
                label1.Margin = new System.Windows.Forms.Padding(0);
                label1.Size = new System.Drawing.Size(186, 29);
                label1.TabIndex = 1;
                label1.Text = info_device.UUID;
                label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

                Label label2 = new Label();
                label2.AutoSize = true;
                label2.Dock = System.Windows.Forms.DockStyle.Fill;
                label2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                label2.Location = new System.Drawing.Point(325, 0);
                label2.Margin = new System.Windows.Forms.Padding(0);
                label2.Size = new System.Drawing.Size(140, 29);
                label2.TabIndex = 3;
                label2.Text = info_device.Storage;
                label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

                Label label3 = new Label();
                label3.AutoSize = true;
                label3.Dock = System.Windows.Forms.DockStyle.Fill;
                label3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                label3.Location = new System.Drawing.Point(186, 0);
                label3.Margin = new System.Windows.Forms.Padding(0);
                label3.Size = new System.Drawing.Size(139, 29);
                label3.TabIndex = 2;
                label3.Text = info_device.Resolution;
                label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

                TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
                tableLayoutPanel.ColumnCount = 3;
                tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
                tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
                tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
                tableLayoutPanel.Controls.Add(label1, 0, 0);
                tableLayoutPanel.Controls.Add(label2, 2, 0);
                tableLayoutPanel.Controls.Add(label3, 1, 0);
                tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
                tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
                tableLayoutPanel.RowCount = 1;
                tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
                tableLayoutPanel.Size = new System.Drawing.Size(465, 29);
                tableLayoutPanel.TabIndex = 0;

                Panel panel = new Panel();
                panel.Controls.Add(tableLayoutPanel);
                panel.Dock = System.Windows.Forms.DockStyle.Top;
                panel.Location = new System.Drawing.Point(0, 29);
                panel.Size = new System.Drawing.Size(465, 29);
                panel.TabIndex = 1;

                this.panel3.Controls.Add(panel);

                // Set the panel to index 0 in panel2
                this.panel3.Controls.SetChildIndex(panel, 0);
            }

            this.program_counter.Text = $"Show select ({program_list.Count})";
            this.device_counter.Text = $"Show select ({device_list.Count})";
        }

        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.close_button = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.confirmButton = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.cancleButton = new System.Windows.Forms.Button();
            this.program_counter = new System.Windows.Forms.Label();
            this.device_counter = new System.Windows.Forms.Label();
            this.sync_mode = new System.Windows.Forms.CheckBox();
            this.panel6 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel4.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel5.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.panel6.SuspendLayout();
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
            this.panel1.Size = new System.Drawing.Size(992, 30);
            this.panel1.TabIndex = 11;
            // 
            // close_button
            // 
            this.close_button.BackgroundImage = global::WindowsFormsApp.Properties.Resources.close_white_icon;
            this.close_button.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.close_button.Dock = System.Windows.Forms.DockStyle.Right;
            this.close_button.FlatAppearance.BorderSize = 0;
            this.close_button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.close_button.Location = new System.Drawing.Point(960, 0);
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
            this.label5.Location = new System.Drawing.Point(417, 7);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(100, 17);
            this.label5.TabIndex = 0;
            this.label5.Text = "Upload project";
            // 
            // confirmButton
            // 
            this.confirmButton.BackColor = System.Drawing.Color.SteelBlue;
            this.confirmButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.confirmButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.confirmButton.Location = new System.Drawing.Point(368, 545);
            this.confirmButton.Name = "confirmButton";
            this.confirmButton.Size = new System.Drawing.Size(111, 26);
            this.confirmButton.TabIndex = 17;
            this.confirmButton.Text = "Upload";
            this.confirmButton.UseVisualStyleBackColor = false;
            this.confirmButton.Click += new System.EventHandler(this.confirm_button_Click);
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.panel4);
            this.panel2.Location = new System.Drawing.Point(12, 89);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(467, 400);
            this.panel2.TabIndex = 18;
            // 
            // panel4
            // 
            this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel4.Controls.Add(this.tableLayoutPanel1);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel4.Location = new System.Drawing.Point(0, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(465, 29);
            this.panel4.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(463, 27);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(0, 0);
            this.label3.Margin = new System.Windows.Forms.Padding(0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(185, 27);
            this.label3.TabIndex = 1;
            this.label3.Text = "Thumbnail - name";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(323, 0);
            this.label2.Margin = new System.Windows.Forms.Padding(0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(140, 27);
            this.label2.TabIndex = 3;
            this.label2.Text = "Real";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(185, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(138, 27);
            this.label1.TabIndex = 2;
            this.label1.Text = "Resolution";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.panel5);
            this.panel3.Location = new System.Drawing.Point(498, 89);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(467, 400);
            this.panel3.TabIndex = 19;
            // 
            // panel5
            // 
            this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel5.Controls.Add(this.tableLayoutPanel2);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel5.Location = new System.Drawing.Point(0, 0);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(465, 29);
            this.panel5.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel2.Controls.Add(this.label4, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label6, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.label7, 1, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(463, 27);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(0, 0);
            this.label4.Margin = new System.Windows.Forms.Padding(0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(185, 27);
            this.label4.TabIndex = 1;
            this.label4.Text = "Device name";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(323, 0);
            this.label6.Margin = new System.Windows.Forms.Padding(0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(140, 27);
            this.label6.TabIndex = 3;
            this.label6.Text = "Storage";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label7.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(185, 0);
            this.label7.Margin = new System.Windows.Forms.Padding(0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(138, 27);
            this.label7.TabIndex = 2;
            this.label7.Text = "Resolution";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cancleButton
            // 
            this.cancleButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
            this.cancleButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancleButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancleButton.Location = new System.Drawing.Point(499, 545);
            this.cancleButton.Name = "cancleButton";
            this.cancleButton.Size = new System.Drawing.Size(111, 26);
            this.cancleButton.TabIndex = 20;
            this.cancleButton.Text = "Cancel";
            this.cancleButton.UseVisualStyleBackColor = false;
            this.cancleButton.Click += new System.EventHandler(this.close_button_Click);
            // 
            // program_counter
            // 
            this.program_counter.AutoSize = true;
            this.program_counter.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.program_counter.Location = new System.Drawing.Point(9, 74);
            this.program_counter.Name = "program_counter";
            this.program_counter.Size = new System.Drawing.Size(95, 13);
            this.program_counter.TabIndex = 21;
            this.program_counter.Text = "Show select (0)";
            // 
            // device_counter
            // 
            this.device_counter.AutoSize = true;
            this.device_counter.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.device_counter.Location = new System.Drawing.Point(496, 73);
            this.device_counter.Name = "device_counter";
            this.device_counter.Size = new System.Drawing.Size(95, 13);
            this.device_counter.TabIndex = 22;
            this.device_counter.Text = "Show select (0)";
            // 
            // sync_mode
            // 
            this.sync_mode.AutoSize = true;
            this.sync_mode.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sync_mode.Location = new System.Drawing.Point(12, 495);
            this.sync_mode.Name = "sync_mode";
            this.sync_mode.Size = new System.Drawing.Size(106, 21);
            this.sync_mode.TabIndex = 24;
            this.sync_mode.Text = "Sync mode";
            this.sync_mode.UseVisualStyleBackColor = true;
            // 
            // panel6
            // 
            this.panel6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel6.Controls.Add(this.confirmButton);
            this.panel6.Controls.Add(this.panel2);
            this.panel6.Controls.Add(this.panel3);
            this.panel6.Controls.Add(this.cancleButton);
            this.panel6.Controls.Add(this.program_counter);
            this.panel6.Controls.Add(this.device_counter);
            this.panel6.Controls.Add(this.sync_mode);
            this.panel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel6.Location = new System.Drawing.Point(0, 30);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(992, 583);
            this.panel6.TabIndex = 25;
            // 
            // upload_form
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(992, 613);
            this.Controls.Add(this.panel6);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "upload_form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.panel6.ResumeLayout(false);
            this.panel6.PerformLayout();
            this.ResumeLayout(false);

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

            // Create an instance of ConfirmEventArgs and set any data you want to pass
            ConfirmEventArgs eventArgs = new ConfirmEventArgs
            {
                program_list = this.program_list,
                device_list  = this.device_list,
                sync_mode    = sync_mode.Checked
            };

            // Khi nút được nhấn, gọi sự kiện và truyền thông tin về control cha
            ConfirmClick?.Invoke(this, eventArgs);
        }
    }
}
