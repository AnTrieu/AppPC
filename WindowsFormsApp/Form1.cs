using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using System.IO;
using System.Security.Policy;
using System.Xml.Linq;
using WindowsFormsApp.Properties;

namespace WindowsFormsApp
{
    public partial class Form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        private String screen = "terminal";
        private bool isListening = true;
        private Thread udpListenerThread = null;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd,
                         int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender,
            System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void Form_SizeChanged(object sender, EventArgs e)
        {
            this.panel7.Width = (this.Width - 548) / 2;
            this.panel8.Width = (this.Width - 548) / 2;
            this.panel37.Width = (this.panel36.Width - 600) / 2;
            this.panel38.Width = (this.panel36.Width - 600) / 2;

            if (this.WindowState == FormWindowState.Maximized)
            {
                this.panel14.Width = 200;
                this.panel40.Width = 400;

                this.left_padding.Width = 200;
                this.right_padding.Width = 200;
                this.top_padding.Height = 100;
                this.bottom_padding.Height = 100;
            }
            else
            {
                this.panel14.Width = 150;
                this.panel40.Width = 300;

                this.left_padding.Width = 50;
                this.right_padding.Width = 50;
                this.top_padding.Height = 50;
                this.bottom_padding.Height = 50;
            }
            
        }


        protected override void WndProc(ref Message m)
        {
            const int RESIZE_HANDLE_SIZE = 10;

            switch (m.Msg)
            {
                case 0x0084/*NCHITTEST*/ :
                    base.WndProc(ref m);

                    if ((int)m.Result == 0x01/*HTCLIENT*/)
                    {
                        Point screenPoint = new Point(m.LParam.ToInt32());
                        Point clientPoint = this.PointToClient(screenPoint);
                        if (clientPoint.Y <= RESIZE_HANDLE_SIZE)
                        {
                            if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                                m.Result = (IntPtr)13/*HTTOPLEFT*/ ;
                            else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                                m.Result = (IntPtr)12/*HTTOP*/ ;
                            else
                                m.Result = (IntPtr)14/*HTTOPRIGHT*/ ;
                        }
                        else if (clientPoint.Y <= (Size.Height - RESIZE_HANDLE_SIZE))
                        {
                            if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                                m.Result = (IntPtr)10/*HTLEFT*/ ;
                            else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                                m.Result = (IntPtr)2/*HTCAPTION*/ ;
                            else
                                m.Result = (IntPtr)11/*HTRIGHT*/ ;
                        }
                        else
                        {
                            if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                                m.Result = (IntPtr)16/*HTBOTTOMLEFT*/ ;
                            else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                                m.Result = (IntPtr)15/*HTBOTTOM*/ ;
                            else
                                m.Result = (IntPtr)17/*HTBOTTOMRIGHT*/ ;
                        }
                    }
                    return;
            }
            base.WndProc(ref m);
        }

        private Bitmap normal_button()
        {
            // Create backgroud color button
            Bitmap bmp = new Bitmap(this.program_button.Width, this.program_button.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Rectangle r = new Rectangle(0, 0, bmp.Width, bmp.Height);
                using (LinearGradientBrush br = new LinearGradientBrush(
                                                    r,
                                                    Color.MidnightBlue,
                                                    Color.SteelBlue,
                                                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(br, r);
                }
            }

            return bmp;
        }

        private Bitmap hover_button()
        {
            // Create backgroud color button
            Bitmap bmp = new Bitmap(this.program_button.Width, this.program_button.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Rectangle r = new Rectangle(0, 0, bmp.Width, bmp.Height);
                using (LinearGradientBrush br = new LinearGradientBrush(
                                                    r,
                                                    Color.DarkBlue,
                                                    Color.SteelBlue,
                                                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(br, r);
                }
            }

            return bmp;
        }

        private Bitmap select_button()
        {
            // Create backgroud color button
            Bitmap bmp = new Bitmap(this.program_button.Width, this.program_button.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Rectangle r = new Rectangle(0, 0, bmp.Width, bmp.Height);
                using (LinearGradientBrush br = new LinearGradientBrush(
                                                    r,
                                                    Color.CornflowerBlue,
                                                    Color.LightBlue,
                                                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(br, r);
                }
            }

            return bmp;
        }

        private void MainButton_MouseHover(object sender, EventArgs e)
        {
            if (sender is Button)
            {
                Button button = (Button)sender;

                if (!screen.Equals(button.Name))
                    button.BackgroundImage = hover_button();
            }
        }

        private void MainButton_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Button)
            {
                Button button = (Button)sender;

                if (screen.Equals(button.Name))
                    button.BackgroundImage = select_button();
                else
                    button.BackgroundImage = normal_button();
            }
        }

        private void row_MouseEnter(object sender, EventArgs e)
        {
            // Thay đổi màu nền khi con trỏ chuột vào
            if (sender is Label)
            {
                Label label = (Label)sender;

                foreach (Control control in this.panel35.Controls)
                {
                    if (control is Panel panel_chill)
                    {
                        // Now, check if there is a TableLayoutPanel within panel_chill
                        TableLayoutPanel tableLayoutPanel = panel_chill.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                        if (tableLayoutPanel != null && tableLayoutPanel.Name.Equals(label.Name))
                        {
                            tableLayoutPanel.BackColor = System.Drawing.Color.SteelBlue;
                        }
                    }
                }
            }
            else if (sender is TableLayoutPanel)
            {
                TableLayoutPanel tablePanel = (TableLayoutPanel)sender;
                tablePanel.BackColor = System.Drawing.Color.SteelBlue;
            }
        }

        private void row_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Label)
            {
                Label label = (Label)sender;

                foreach (Control control in this.panel35.Controls)
                {
                    if (control is Panel panel_chill)
                    {
                        // Now, check if there is a TableLayoutPanel within panel_chill
                        TableLayoutPanel tableLayoutPanel = panel_chill.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                        if (tableLayoutPanel != null && tableLayoutPanel.Name.Equals(label.Name))
                        {
                            tableLayoutPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                        }
                    }
                }
            }
            else if (sender is TableLayoutPanel)
            {
                TableLayoutPanel tablePanel = (TableLayoutPanel)sender;
                tablePanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
            }
        }

        private void MainButton_MouseClick(object sender, EventArgs e)
        {
            if (sender is Button)
            {
                Button button = (Button)sender;

                // Set backgroud button -> normal
                this.program_button.BackgroundImage = normal_button();
                this.release_button.BackgroundImage = normal_button();
                this.terminal_button.BackgroundImage = normal_button();
                button.BackgroundImage = select_button();

                // Button opption default
                this.button_option_area.Visible = false;

                if (!screen.Equals(button.Name))
                {
                    // Select layout
                    if (button.Name.Equals("program_button"))
                    {
                        this.main_program.Visible = true;
                        this.main_release.Visible = false;
                        this.main_terminal.Visible = false;
                        this.button_option_area.Visible = true;
                    }
                    else if (button.Name.Equals("release_button"))
                    {
                        this.main_program.Visible = false;
                        this.main_release.Visible = true;
                        this.main_terminal.Visible = false;
                    }
                    else if (button.Name.Equals("terminal_button"))
                    {
                        this.main_program.Visible = false;
                        this.main_release.Visible = false;
                        this.main_terminal.Visible = true;
                    }
                }

                screen = button.Name;
            }
        }

        private void DashedBorderButton_Paint(object sender, PaintEventArgs e)
        {
            Button button = (Button)sender;
            Pen dashedPen = new Pen(Color.DarkGray);
            dashedPen.DashStyle = System.Drawing.Drawing2D.DashStyle.DashDot;

            // Draw the button's border with a dashed style
            e.Graphics.DrawRectangle(dashedPen, 0, 0, button.Width - 1, button.Height - 1);

            // Draw the button's text
            TextRenderer.DrawText(e.Graphics, button.Text, button.Font, new Rectangle(0, 0, button.Width, button.Height), button.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        public Form1()
        {
            InitializeComponent();

            this.panel4.MouseDown += new MouseEventHandler(Form1_MouseDown);
            this.panel5.MouseDown += new MouseEventHandler(Form1_MouseDown);
            
            this.program_button.BackgroundImage = normal_button();
            this.program_button.MouseHover += MainButton_MouseHover;
            this.program_button.MouseLeave += MainButton_MouseLeave;
            this.program_button.MouseClick += MainButton_MouseClick;

            this.release_button.BackgroundImage = normal_button();
            this.release_button.MouseHover += MainButton_MouseHover;
            this.release_button.MouseLeave += MainButton_MouseLeave;
            this.release_button.MouseClick += MainButton_MouseClick;

            this.terminal_button.BackgroundImage = select_button();
            this.terminal_button.MouseHover += MainButton_MouseHover;
            this.terminal_button.MouseLeave += MainButton_MouseLeave;
            this.terminal_button.MouseClick += MainButton_MouseClick;

            this.SizeChanged += new EventHandler(Form_SizeChanged);

            this.new_program_button.Paint += new PaintEventHandler(DashedBorderButton_Paint);
            screen = this.terminal_button.Name;


            // Tạo một luồng riêng cho việc lắng nghe UDP
            udpListenerThread = new Thread(() => UdpListener(45454));
            udpListenerThread.Start();

            Console.WriteLine("Ứng dụng chính đang chạy.");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Do nothing
        }

        private void min_button_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void max_button_Click(object sender, EventArgs e)
        {
            if(this.WindowState == FormWindowState.Normal)
            {
                this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
                this.WindowState = FormWindowState.Maximized;
            }
            else
                this.WindowState = FormWindowState.Normal;
        }

        private void close_button_Click(object sender, EventArgs e)
        {
            // Close thread catch UDP packet
            isListening = false;
            
            Application.Exit();
        }

        private void new_program_button_Click(object sender, EventArgs e)
        {
            setting_form popup = new setting_form();
        }

        private void refresh_Click(object sender, EventArgs e)
        {
            while (this.panel35.Controls.Count > 0)
            {
                Control control = this.panel35.Controls[0];
                this.panel35.Controls.Remove(control);
                control.Dispose(); // Optional, to release resources associated with the control
            }

            // Counter device
            this.total_pc.Text = "Total 0";
            this.online_pc.Text = "Total 0";
        }

        private void UdpListener(int udpPort)
        {
            UdpClient udpListener = new UdpClient(udpPort);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, udpPort);

            while (isListening)
            {
                try
                {
                    byte[] receivedBytes = udpListener.Receive(ref endPoint);
                    string receivedMessage = Encoding.ASCII.GetString(receivedBytes);
                    Boolean have_obj = false;
                    int counter_device = 1;

                    adv_packet data = JsonConvert.DeserializeObject<adv_packet>(receivedMessage);
   
                    // Check list device
                    foreach (Control control in this.panel35.Controls)
                    {
                        if (control is Panel panel_chill)
                        {
                            // Now, check if there is a TableLayoutPanel within panel_chill
                            TableLayoutPanel tableLayoutPanel = panel_chill.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                            if (tableLayoutPanel != null)
                            {
                                // Get the control in the first cell of the first column (assuming it's a Label)
                                Control controlInFirstColumn = tableLayoutPanel.GetControlFromPosition(0, 0);

                                if (controlInFirstColumn != null && controlInFirstColumn is Label device_name_label_in_list)
                                {
                                    // Device have added
                                    if (device_name_label_in_list.Text.Length > 0)
                                    {
                                        counter_device++;

                                        if (device_name_label_in_list.Text.Equals(data.deviceName))
                                        {
                                            have_obj = true;
                                        }
                                    }
                                }
                            }
                        }                  
                    }

                    if(!have_obj)
                    {
                        if (panel35.InvokeRequired)
                        {
                            // Add new device
                            panel35.Invoke((MethodInvoker)delegate
                            {
                                bool flag_lock = false;

                                // Get data from device
                                getScreenParams_packet data_getScreenParams = null;
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://192.168.1.4:18080/getScreenParams");
                                request.Method = "GET";

                                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                                {
                                    
                                    if (response.StatusCode == HttpStatusCode.OK)
                                    {
                                        using (Stream responseStream = response.GetResponseStream())
                                        {
                                            using (StreamReader reader = new StreamReader(responseStream))
                                            {                                                
                                                // Deserialize the JSON data into a C# object
                                                data_getScreenParams = JsonConvert.DeserializeObject<getScreenParams_packet>(reader.ReadToEnd());
                                                if(data_getScreenParams.code != 200)
                                                {
                                                    if (data_getScreenParams.code == 401)
                                                    {
                                                        // Show clock icon
                                                        flag_lock = true;
                                                    }
                                                    data_getScreenParams = null;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("HTTP request failed with status code: " + response.StatusCode);
                                    }
                                }

                                // Create the add panel
                                Panel addPanel = new Panel();
                                addPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                                addPanel.Dock = System.Windows.Forms.DockStyle.Top;
                                addPanel.Location = new System.Drawing.Point(0, 0);
                                addPanel.Size = new System.Drawing.Size(946, 60);
                                addPanel.Padding = new System.Windows.Forms.Padding(8, 0, 8, 8);

                                // Create the add table panel
                                TableLayoutPanel addTablePanel = new TableLayoutPanel();
                                addTablePanel.BorderStyle = BorderStyle.None;
                                addTablePanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                                addTablePanel.ColumnCount = 7;
                                addTablePanel.Name = data.deviceName;
                                addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
                                addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
                                addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
                                addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
                                addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
                                addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
                                addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
                                addTablePanel.MouseEnter += row_MouseEnter;
                                addTablePanel.MouseLeave += row_MouseLeave;

                                Label device_name_label = new Label();
                                device_name_label.AutoSize = true;
                                device_name_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                device_name_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                device_name_label.Location = new System.Drawing.Point(0, 0);
                                device_name_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                device_name_label.Size = new System.Drawing.Size(283, 30);
                                if (flag_lock)
                                {
                                    device_name_label.Image = global::WindowsFormsApp.Properties.Resources.lock_icon;
                                    device_name_label.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
                                }
                                device_name_label.TabIndex = 0;
                                device_name_label.Name = data.deviceName;
                                device_name_label.Text = data.deviceName;
                                device_name_label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                                device_name_label.MouseEnter += row_MouseEnter;
                                device_name_label.MouseLeave += row_MouseLeave;
                                addTablePanel.Controls.Add(device_name_label, 0, 0);


                                Label method_label = new Label();
                                method_label.AutoSize = true;
                                method_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                method_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                method_label.Location = new System.Drawing.Point(0, 0);
                                method_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                method_label.Size = new System.Drawing.Size(283, 30);
                                method_label.TabIndex = 0;
                                method_label.Name = data.deviceName;
                                method_label.Text = "LAN";
                                method_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                method_label.MouseEnter += row_MouseEnter;
                                method_label.MouseLeave += row_MouseLeave;
                                addTablePanel.Controls.Add(method_label, 1, 0);

                                Label address_label = new Label();
                                address_label.AutoSize = true;
                                address_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                address_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                address_label.Location = new System.Drawing.Point(0, 0);
                                address_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                address_label.Size = new System.Drawing.Size(283, 30);
                                address_label.TabIndex = 0;
                                address_label.Name = data.deviceName;
                                address_label.Text = endPoint.Address.ToString();
                                address_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                address_label.MouseEnter += row_MouseEnter;
                                address_label.MouseLeave += row_MouseLeave;
                                addTablePanel.Controls.Add(address_label, 2, 0);

                                Label resolution_label = new Label();
                                resolution_label.AutoSize = true;
                                resolution_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                resolution_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                resolution_label.Location = new System.Drawing.Point(0, 0);
                                resolution_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                resolution_label.Size = new System.Drawing.Size(283, 30);
                                resolution_label.TabIndex = 0;
                                resolution_label.Name = data.deviceName;
                                resolution_label.Text = data.screenWidth.ToString() + "*" + data.screenHeight.ToString();
                                resolution_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                resolution_label.MouseEnter += row_MouseEnter;
                                resolution_label.MouseLeave += row_MouseLeave;
                                addTablePanel.Controls.Add(resolution_label, 3, 0);

                                Label brightness_label = new Label();
                                brightness_label.AutoSize = true;
                                brightness_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                brightness_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                brightness_label.Location = new System.Drawing.Point(0, 0);
                                brightness_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                brightness_label.Size = new System.Drawing.Size(283, 30);
                                brightness_label.TabIndex = 0;
                                brightness_label.Name = data.deviceName;
                                brightness_label.Text = data_getScreenParams != null? data_getScreenParams.bright.ToString(): "--";
                                brightness_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                brightness_label.MouseEnter += row_MouseEnter;
                                brightness_label.MouseLeave += row_MouseLeave;
                                addTablePanel.Controls.Add(brightness_label, 4, 0);

                                Label voice_label = new Label();
                                voice_label.AutoSize = true;
                                voice_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                voice_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                voice_label.Location = new System.Drawing.Point(0, 0);
                                voice_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                voice_label.Size = new System.Drawing.Size(283, 30);
                                voice_label.TabIndex = 0;
                                voice_label.Name = data.deviceName;
                                voice_label.Text = data_getScreenParams != null ? (data_getScreenParams.voice.Equals("1.0")?"100" : data_getScreenParams.voice.TrimStart('0').Replace(".", "")) : "--"; ;
                                voice_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                voice_label.MouseEnter += row_MouseEnter;
                                voice_label.MouseLeave += row_MouseLeave;
                                addTablePanel.Controls.Add(voice_label, 5, 0);

                                Label version_label = new Label();
                                version_label.AutoSize = true;
                                version_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                version_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                version_label.Location = new System.Drawing.Point(0, 0);
                                version_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                version_label.Size = new System.Drawing.Size(283, 30);
                                version_label.TabIndex = 0;
                                version_label.Name = data.deviceName;
                                version_label.Text = "v" + data.systemVersion + "-" + data.appVersion;
                                version_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                version_label.MouseEnter += row_MouseEnter;
                                version_label.MouseLeave += row_MouseLeave;
                                addTablePanel.Controls.Add(version_label, 6, 0);

                                addTablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
                                addTablePanel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                                addTablePanel.ForeColor = System.Drawing.Color.White;
                                addTablePanel.Location = new System.Drawing.Point(0, 0);
                                addTablePanel.RowCount = 1;
                                addTablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
                                addTablePanel.Size = new System.Drawing.Size(946, 60);
                                addTablePanel.TabIndex = 0;

                                addTablePanel.Paint += (sender1, e1) =>
                                {
                                    int boTronKichThuoc = 20; // Điều chỉnh độ bo tròn ở đây
                                    GraphicsPath graphicsPath = new GraphicsPath();

                                    Rectangle rect = new Rectangle(0, 0, addTablePanel.Width, addTablePanel.Height);
                                    graphicsPath.AddArc(rect.X, rect.Y, boTronKichThuoc, boTronKichThuoc, 180, 90);
                                    graphicsPath.AddArc(rect.Right - boTronKichThuoc, rect.Y, boTronKichThuoc, boTronKichThuoc, 270, 90);
                                    graphicsPath.AddArc(rect.Right - boTronKichThuoc, rect.Bottom - boTronKichThuoc, boTronKichThuoc, boTronKichThuoc, 0, 90);
                                    graphicsPath.AddArc(rect.X, rect.Bottom - boTronKichThuoc, boTronKichThuoc, boTronKichThuoc, 90, 90);
                                    graphicsPath.CloseAllFigures();

                                    addTablePanel.Region = new Region(graphicsPath);
                                };

                                addPanel.Controls.Add(addTablePanel);
                                this.panel35.Controls.Add(addPanel);

                                // Counter device
                                this.total_pc.Text = "Total " + counter_device.ToString();
                                this.online_pc.Text = "Total " + counter_device.ToString();
                            });
                        }

                    }
                }
                catch (Exception e)
                {
                    // Xử lý lỗi
                    Console.WriteLine($"Lỗi: {e}");
                    Thread.Sleep(2000);
                }
            }
        }
    }
    public class getScreenParams_packet
    {
        public int bright { get; set; }
        public int contrast { get; set; }
        public bool screenOn { get; set; }
        public int screenType { get; set; }
        public string voice { get; set; }
        public int code { get; set; }
        public string message { get; set; }
        // Add more properties to match the structure of your JSON data
    }

    public class adv_packet
    {
        public string absolutePath { get; set; }
        public string appVersion { get; set; }
        public int broadcastType { get; set; }
        public string deviceDetail { get; set; }
        public string deviceId { get; set; }
        public string deviceName { get; set; }
        public string deviceVersion { get; set; }
        public string groupId { get; set; }
        public string groupName { get; set; }
        public bool isCaptain { get; set; }
        public string mdnsHost { get; set; }
        public string model { get; set; }
        public int screenHeight { get; set; }
        public int screenWidth { get; set; }
        public int showCloudSetting { get; set; }
        public string systemVersion { get; set; }
        public int totalSpace { get; set; }
        public int usableSpace { get; set; }
        public string usbState { get; set; }
        public int usbTotalSpace { get; set; }
        public int usbUsableSpace { get; set; }
        public string voice { get; set; }
    }
}
