using System;
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Timer = System.Windows.Forms.Timer;
//using Accord.Video.FFMPEG;
using MediaInfo.DotNetWrapper.Enumerations;
using System.Diagnostics;
using FFmpeg.AutoGen;
using System.Text.RegularExpressions;

namespace WindowsFormsApp
{
    public partial class Form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        private String screen = "terminal";
        private Thread udpListenerThread = null;
        private Boolean flagTermianlUDPThread = false;

        private FormWindowState windowStateK1 = FormWindowState.Normal;

        List<Control> controlsList = new List<Control>();

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
            if(this.WindowState == FormWindowState.Minimized)
            {
                return;
            }

            this.panel7.Width = (this.Width - 548) / 2;
            this.panel8.Width = (this.Width - 548) / 2;
            this.panel37.Width = (this.panel36.Width - 600) / 2;
            this.panel38.Width = (this.panel36.Width - 600) / 2;

            if (this.WindowState == FormWindowState.Maximized)
            {
                this.panel6.Width = 350;
                this.panel14.Width = 150;
                this.panel40.Width = 500;

                this.show_file.Height = 400;

                this.bk_program.Width = 130;

                this.panel77.Width = 1000;
                this.label45.Top = 400;
                this.label45.Left = 350;

                this.General.Top = this.label45.Top - 50;
                this.Advanced.Top = this.label45.Top - 50;
            }
            else
            {
                this.panel6.Width = 250;
                this.panel14.Width = 100;
                this.panel40.Width = 350;

                this.show_file.Height = 250;

                this.bk_program.Width = 70;

                this.panel77.Width = 400;
                this.label45.Top = 330;
                this.label45.Left = 100;

                this.General.Top = this.label45.Top - 30;
                this.Advanced.Top = this.label45.Top - 30;
                
            }

            this.Advanced.Left = this.panel71.Width + this.panel78.Width - 50;

            // Refresh design area
            refresh_program_design();

            windowStateK1 = this.WindowState;
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
                {
                    button.BackgroundImage = hover_button();
                }
                   
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

        private void row_device_MouseEnter(object sender, EventArgs e)
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

        private void row_device_MouseLeave(object sender, EventArgs e)
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

        private void row_device_release_MouseEnter(object sender, EventArgs e)
        {
            // Thay đổi màu nền khi con trỏ chuột vào
            if (sender is Label)
            {
                Label label = (Label)sender;

                foreach (Control control in this.panel84.Controls)
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

        private void row_device_release_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Label)
            {
                Label label = (Label)sender;

                foreach (Control control in this.panel84.Controls)
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

        private void row_file_MouseEnter(object sender, EventArgs e)
        {
            // Thay đổi màu nền khi con trỏ chuột vào
            if (sender is Label)
            {
                Label label = (Label)sender;

                foreach (Control control in this.panel46.Controls)
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

        private void row_file_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Label)
            {
                Label label = (Label)sender;

                foreach (Control control in this.panel46.Controls)
                {
                    if (control is Panel panel_chill)
                    {
                        // Now, check if there is a TableLayoutPanel within panel_chill
                        TableLayoutPanel tableLayoutPanel = panel_chill.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                        if (tableLayoutPanel != null && tableLayoutPanel.Name.Equals(label.Name))
                        {
                            tableLayoutPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                        }
                    }
                }
            }
            else if (sender is TableLayoutPanel)
            {
                TableLayoutPanel tablePanel = (TableLayoutPanel)sender;
                tablePanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
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
        }

        public Form1()
        {
            InitializeComponent();

            this.WindowState = windowStateK1;

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
            this.new_resource.Paint += new PaintEventHandler(DashedBorderButton_Paint);

            this.General.BackgroundImageLayout = ImageLayout.Stretch;
            this.General.BackgroundImage = normal_button();
            this.General.MouseDown += (sender, e) =>
            {
                (sender as Button).BackgroundImage = select_button();
            };
            this.General.MouseUp += (sender, e) =>
            {
                (sender as Button).BackgroundImage = normal_button();

                if(!this.panel72.Visible)
                {
                    MessageBox.Show("Please select the program to upload.", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    String IP_client = "";

                    foreach (Control control in this.panel84.Controls)
                    {
                        if (control is Panel panel)
                        {
                            foreach (Control innerControl in panel.Controls)
                            {
                                if (innerControl is TableLayoutPanel tableLayoutPanel)
                                {
                                    RadioButton radioObj = (RadioButton) tableLayoutPanel.GetControlFromPosition(0, 0);
                                    Label labelObj = (Label)tableLayoutPanel.GetControlFromPosition(2, 0);
                                    if (radioObj.Checked)
                                    {
                                        IP_client = labelObj.Text;
                                    }
                                }
                            }
                        }
                    }

                    if (IP_client.Length == 0)
                    {
                        MessageBox.Show("Please select the device to upload.", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        process_form popup = new process_form();
                        popup.Name = IP_client;

                        // Start a new thread for the dialog with parameters
                        Thread dialogThread = new Thread(new ParameterizedThreadStart(SendFileThread));
                        dialogThread.Start(popup);

                        // Show the dialog asynchronously without blocking the UI thread
                        popup.ShowDialog();

                    }                       
                }
            };
            this.General.Paint += (sender, e) =>
            {
                Button obj = sender as Button;

                GraphicsPath path = new GraphicsPath();
                path.AddEllipse(0, 0, obj.Width, obj.Height);
                obj.Region = new Region(path);

                // Vẽ đường viền
                int borderWidth = 2; // Độ dày của đường viền
                using (Pen pen = new Pen(Color.LightBlue, borderWidth))
                {
                    e.Graphics.DrawEllipse(pen, borderWidth / 2, borderWidth / 2, obj.Width - borderWidth, obj.Height - borderWidth);
                }
            };

            this.Advanced.BackgroundImageLayout = ImageLayout.Stretch;
            this.Advanced.BackgroundImage = normal_button();
            this.Advanced.Paint += (sender, e) =>
            {
                Button obj = sender as Button;

                GraphicsPath path = new GraphicsPath();
                path.AddEllipse(0, 0, obj.Width, obj.Height);
                obj.Region = new Region(path);

                // Vẽ đường viền
                int borderWidth = 2; // Độ dày của đường viền
                using (Pen pen = new Pen(Color.LightBlue, borderWidth))
                {
                    e.Graphics.DrawEllipse(pen, borderWidth / 2, borderWidth / 2, obj.Width - borderWidth, obj.Height - borderWidth);
                }
            };

            screen = this.terminal_button.Name;
 
            // Tạo một luồng riêng cho việc lắng nghe UDP
            udpListenerThread = new Thread(() => UdpListener(45454));
            udpListenerThread.Start();

            // Đăng ký sự kiện DragDrop và DragEnter cho Panel
            this.main_program.AllowDrop = true;     
            this.main_program.DragOver += Target_DragOver;

            this.panel43.MouseClick += (sender, e) =>
            {
                unselect_object();                        
            };

            for (int panelIndex = this.panel46.Controls.Count - 1; panelIndex > 0; panelIndex--)
            {
                this.panel46.Controls.RemoveAt(panelIndex);
            }

            if (File.Exists("material.data"))
            {
                // Đọc thông tin từ tệp tin khi ứng dụng được khởi động
                using (StreamReader sr = new StreamReader("material.data"))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        add_detail_file(line, false);
                    }
                }
            }

            // Create the "Output" directory if it doesn't exist
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output"));
                Console.WriteLine("The 'Output' directory has been created.");
            }

            // Create the "Log" directory if it doesn't exist
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log"));
                Console.WriteLine("The 'Log' directory has been created.");
            }
        }

        private void SendFileThread(object parameter)
        {
            long total_size = 0;
            bool flag_cancel = false;

            process_form dialog = (process_form) parameter;
            string IP_client = (string)dialog.Name;
            dialog.Name = "File sent unsuccessfully";

            // Buffer size for receiving video data
            byte[] buffer = new byte[10240 + 256];
            byte[] responseBuffer = new byte[256]; // Adjust the size according to your needs

            // Set up client socket
            TcpClient clientSocket = null;
            NetworkStream networkStream = null;

            dialog.CloseClick += (sender, e) =>
            {
                flag_cancel = true;
            };

            try
            {
                // Convert video
                if (controlsList.Count > 1)
                {
                    long longestDuration = 0;

                    // Step 1: get all path video in program list ###
                    foreach (Control control in controlsList)
                    {
                        if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                        {
                            // Deserialize JSON data from the Name property
                            Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);
                            long longestDurationWindown = 0;

                            for (int idx = 0; idx < infoWindow.list.Count; idx++)
                            {
                                string extension = System.IO.Path.GetExtension(infoWindow.list[idx]).ToLower();

                                // Is a video
                                if (extension == ".mp4"  || extension == ".avi" ||
                                    extension == ".wmv"  || extension == ".mpg" ||
                                    extension == ".rmvp" || extension == ".mov" ||
                                    extension == ".dat"  || extension == ".flv")
                                {
                                    using (Process process = new Process())
                                    {
                                        process.StartInfo.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg.exe"; // Assuming "ffmpeg" is in the PATH
                                        process.StartInfo.Arguments = $"-i \"{infoWindow.list[idx]}\"";
                                        process.StartInfo.UseShellExecute = false;
                                        process.StartInfo.RedirectStandardOutput = true;
                                        process.StartInfo.RedirectStandardError = true;
                                        process.StartInfo.CreateNoWindow = true;

                                        process.OutputDataReceived += (sender, e) =>
                                        {
                                            Console.WriteLine($"Process: {e.Data}");
                                        };
                                        process.ErrorDataReceived += (sender, e) =>
                                        {
                                            if (!string.IsNullOrEmpty(e.Data))
                                            {
                                                // Variables for capturing duration
                                                string durationPattern = @"Duration: (\d+:\d+:\d+\.\d+)";
                                                Regex regex = new Regex(durationPattern);

                                                // Search for duration pattern in the output
                                                Match match = regex.Match(e.Data);

                                                if (match.Success)
                                                {
                                                    // Extract the matched duration
                                                    string durationString = match.Groups[1].Value;

                                                    longestDurationWindown += (long)TimeSpan.Parse(durationString).TotalMilliseconds;
                                                }
                                            }

                                        };
                                        process.Start();
                                        process.BeginOutputReadLine();
                                        process.BeginErrorReadLine();

                                        process.WaitForExit();
                                    }


                                }
                                else if (extension == ".jpg" || extension == ".bmp" ||
                                         extension == ".png" || extension == ".gif")
                                {
                                    longestDurationWindown += 1000;
                                }                             
                            }

                            if (longestDuration < longestDurationWindown)
                            {
                                longestDuration = longestDurationWindown;
                            }
                        }
                    }

                    Console.WriteLine("Convert finish " + longestDuration);
                }

                // Carculator total size                          
                //if (controlsList[0] is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                //{
                //    // Deserialize JSON data from the Name property
                //    Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);
                //
                //    for (int idx = 0; idx < infoWindow.list.Count; idx++)
                //    {
                //        var mediaInfo = new MediaInfo.DotNetWrapper.MediaInfo();
                //        mediaInfo.Open(infoWindow.list[idx]);
                //
                //        total_size += long.Parse(mediaInfo.Get(StreamKind.General, 0, "FileSize"));
                //    }
                //
                //}
                //
                //if (total_size == 0)
                //{
                //    MessageBox.Show("File error, please try again", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //}
                //else
                //{
                //    clientSocket = new TcpClient();
                //    clientSocket.Connect(IP_client, 12345); // Replace with the server's IP and port
                //
                //    // Get the network stream for receiving data
                //    networkStream = clientSocket.GetStream();
                //
                //    Boolean send_plan = false;
                //    long sended_size = 0;
                //    int percent = 0, percentK1 = -1;
                //
                //    // Send file                        
                //    if (controlsList[0] is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                //    {
                //        // Deserialize JSON data from the Name property
                //        Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);
                //
                //        for (int idx = 0; idx < infoWindow.list.Count; idx++)
                //        {
                //            // Active send plan
                //            send_plan = true;
                //
                //            using (FileStream receivedVideoFile = new FileStream(infoWindow.list[idx], FileMode.Open, FileAccess.Read))
                //            {
                //                int bytesRead = 0;
                //                int idxChuck = 0;
                //
                //                long length_file = receivedVideoFile.Length;
                //   
                //                // Receive video data in chunks
                //                while ((bytesRead = receivedVideoFile.Read(buffer, 256, buffer.Length - 256)) > 0)
                //                {
                //                    if (flag_cancel)
                //                    {
                //                        var detailPacket = new
                //                        {
                //                            command = "SEND_CANCEL",
                //                            plan = ""
                //                        };
                //
                //                        byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(detailPacket));
                //                        Array.Copy(jsonBytes, buffer, jsonBytes.Length);
                //
                //                        networkStream.Write(buffer, 0, buffer.Length);
                //                        networkStream.Flush();
                //
                //                        // Clean (reset) the buffer
                //                        Array.Clear(buffer, 0, buffer.Length);
                //                        Array.Clear(responseBuffer, 0, responseBuffer.Length);
                //
                //                        // Release memory
                //                        jsonBytes = null;
                //                        detailPacket = null;
                //
                //                        break;
                //                    }
                //                    else
                //                    {
                //                        var detailPacket = new
                //                        {
                //                            command = "SEND_FILE",
                //                            chuck = idxChuck++,
                //                            path = infoWindow.list[idx],
                //                            sended = bytesRead,
                //                            length = length_file
                //                        };
                //
                //                        byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(detailPacket));
                //                        Array.Copy(jsonBytes, buffer, Math.Min(jsonBytes.Length, 256));
                //
                //                        networkStream.Write(buffer, 0, Math.Max(bytesRead + 256, buffer.Length));
                //                        networkStream.Flush();
                //
                //                        sended_size += bytesRead;
                //                        percent = ((int)Math.Round((double)sended_size * 100 / (double)total_size, 0));
                //                        if (percentK1 != percent)
                //                        {
                //                            ManualResetEvent resetEvent = new ManualResetEvent(false);
                //
                //                            // set process bar
                //                            dialog.Invoke((MethodInvoker)delegate
                //                            {
                //                                try
                //                                {
                //                                    // Your UI update code
                //                                    dialog.ProgressValue = percent;
                //                                    dialog.progressBar1.Refresh();
                //                                }
                //                                finally
                //                                {
                //                                    // Signal that the UI update is completed
                //                                    resetEvent.Set();
                //                                }
                //                            });
                //
                //                            // Block until the UI update is completed
                //                            resetEvent.WaitOne();
                //                            resetEvent = null;
                //
                //                            percentK1 = percent;
                //                        }
                //
                //                        // Wait for the response in first time
                //                        if (idxChuck == 1)
                //                        {
                //                            int bytesReadResponse = networkStream.Read(responseBuffer, 0, responseBuffer.Length);
                //                            string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesReadResponse);
                //                            Console.WriteLine("Server Response: " + response);
                //
                //                            if (response.Equals("Exist file"))
                //                            {
                //                                sended_size = sended_size - bytesRead + length_file;
                //                                break;
                //                            }
                //                        }
                //
                //                        // Release memory
                //                        jsonBytes = null;
                //                        detailPacket = null;
                //
                //                        // Clean (reset) the buffer
                //                        Array.Clear(buffer, 0, buffer.Length);
                //                        Array.Clear(responseBuffer, 0, responseBuffer.Length);
                //                    }
                //                }
                //            }
                //        }
                //
                //        // Send plan first windown
                //        if (send_plan && controlsList.Count > 0)
                //        {
                //            send_plan = false;
                //
                //            // Resize child panels in program list (panel43)
                //            var visiblePanels = this.panel43.Controls
                //                .OfType<Panel>();
                //
                //            foreach (var panel_chill in visiblePanels)
                //            {
                //                var detailPacket = new
                //                {
                //                    command = "SEND_PLAN",
                //                    info_program = JsonConvert.DeserializeObject<Info_Program>(panel_chill.Name),
                //                    info_windown = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name)
                //                };
                //
                //                byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(detailPacket));
                //                Array.Copy(jsonBytes, buffer, jsonBytes.Length);
                //
                //                networkStream.Write(buffer, 0, buffer.Length);
                //                networkStream.Flush();
                //
                //                // Clean (reset) the buffer
                //                Array.Clear(buffer, 0, buffer.Length);
                //                Array.Clear(responseBuffer, 0, responseBuffer.Length);
                //
                //                // Release memory
                //                jsonBytes = null;
                //                detailPacket = null;
                //            }
                //        }
                //    }
                //}

                dialog.Name = "Send file successfully";

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi: {ex}");
            }
            finally
            {
                buffer = null;

                // Release memory
                if (networkStream != null)
                {
                    networkStream.Close();
                    networkStream = null;
                }

                if (clientSocket != null && clientSocket.Connected)
                {
                    clientSocket.Close();
                    clientSocket = null;
                }

                if (dialog.Name.Equals("Send file successfully"))
                {
                    dialog.ProgressValue = 200;
                    
                }
                else
                {
                    dialog.ProgressValue = -1;
                }

                // Close the dialog on the dialog's thread
                if (!flag_cancel)
                {
                    dialog.Invoke((MethodInvoker)delegate
                    {
                        dialog.progressBar1.Refresh();

                        //dialog.Close();
                    });
                }

            }
        }

        private void unselect_object()
        {
            // Check list item exist
            foreach (Control control in this.panel43.Controls)
            {
                if (control is Panel panel_chill && panel_chill.Visible)
                {
                    this.panel70.Visible = false;

                    for (int panelIndex = control.Controls.Count - 1; panelIndex >= 0; panelIndex--)
                    {
                        Control panel = control.Controls[panelIndex];

                        // Check if the control is a Panel
                        if (panel is Panel)
                        {
                            // Loop through controls in the panel in reverse order
                            for (int i = panel.Controls.Count - 1; i >= 0; i--)
                            {
                                Control control1 = panel.Controls[i];

                                // Check if the control is a Label
                                if (control1 is Label)
                                {
                                    // Remove the Label from the panel
                                    panel.Controls.RemoveAt(i);
                                }
                            }
                        }
                    }
                }
            }

            // Unselect all
            foreach (Control control1 in controlsList)
            {
                if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                {
                    // Deserialize JSON data from the Name property
                    Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);

                    for (int i1 = 0; i1 < infoWindow1.selected.Count; i1++)
                    {
                        infoWindow1.selected[i1] = false;
                    }

                    resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                }
            }

            foreach (Control control1 in this.list_windowns.Controls)
            {
                control1.Refresh();
            }

            this.panel70.Visible = false;
        }

        public static double Normalize(double x, double minA, double maxA, double minB, double maxB)
        {
            double value = (x - minA) * (maxB - minB) / (maxA - minA) + minB;
            return value > maxB ? maxB : value < 0 ? 0 : value;
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
            if (this.WindowState == FormWindowState.Normal)
            {
                this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
                this.WindowState = FormWindowState.Maximized;
            }
            else
                this.WindowState = FormWindowState.Normal;
        }

        private void close_button_Click(object sender, EventArgs e)
        {
            // Cancel the UDP listener thread
            flagTermianlUDPThread = true;
            udpListenerThread.Join();

            Application.Exit();
        }

        private void new_program_button_Click(object sender, EventArgs e)
        {
            if(!this.panel47.Visible && !this.panel72.Visible)
            {
                setting_form popup = new setting_form();

                popup.ConfirmClick += (sender1, e1) =>
                {
                    if (int.TryParse(e1.width_real, out int width) && int.TryParse(e1.height_real, out int height))
                    {
                        var infoProgram = new
                        {
                            name = e1.name,
                            width_resolution = e1.width_resolution,
                            height_resolution = e1.height_resolution,
                            width_real = e1.width_real,
                            height_real = e1.height_real,
                            bittrate_select = e1.bittrate_select
                        };

                        this.panel43.Controls.Clear();

                        // Get the maximum allowable width and height based on the mainPanel's size
                        int width_contain = this.panel43.Width;
                        int height_contain = this.panel43.Height;
                        int width_select = width;
                        int height_select = height;
                        float delta = (float)width_select / (float)height_select;
                        float width_config = 0;
                        float height_config = 0;
                        do
                        {
                            height_config += 1;
                            width_config += delta;
                        }
                        while ((width_config < (width_contain - 50)) && (height_config < (height_contain - 70)));

                        // Create the inner panel based on the adjusted width and height
                        Panel innerPanel = new Panel
                        {
                            Width = (int)width_config,
                            Height = (int)height_config,
                            BackColor = Color.Black,
                            Name = JsonConvert.SerializeObject(infoProgram),
                            AllowDrop = true
                        };
 
                        // Calculate the position to center the inner panel within the main panel
                        int x = (width_contain - (int)width_config) / 2;
                        int y = (height_contain - (int)height_config) / 2;


                        // Set the location of the inner panel
                        innerPanel.Location = new Point(x, y);

                        // Đăng ký sự kiện DragDrop và DragEnter cho Panel
                        innerPanel.DragDrop += TargetPanel_DragDrop;
                        innerPanel.DragEnter += TargetPanel_DragEnter;
                        innerPanel.DragOver += Target_DragOver;
                        innerPanel.MouseDown += (sender2, e2) =>
                        {
                            unselect_object();
                        };

                        // Add the inner panel to the main panel
                        this.panel43.Controls.Add(innerPanel);

                        // Create list program
                        this.panel47.Visible = true;
                        label36.Text = width + "*" + height;
                        label35.Text = e1.name;

                        // Create list program
                        this.panel72.Visible = true;
                        label43.Text = width + "*" + height;
                        label44.Text = e1.name;
                    }
                };

                popup.ShowDialog();
            }
            else
            {
                MessageBox.Show("Exceeded number of allowed programs.", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

   
        private void TargetPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("PictureBoxImage") && e.Data.GetDataPresent("PictureBoxName") && (e.AllowedEffect & DragDropEffects.Move) != 0)
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void TargetPanel_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("PictureBoxImage") && e.Data.GetDataPresent("PictureBoxName"))
            {
                // Unselect all
                foreach (Control control in controlsList)
                {
                    if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                    {
                        // Deserialize JSON data from the Name property
                        Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                        for(int i = 0; i< infoWindow.selected.Count;i++)
                        {
                            infoWindow.selected[i] = false;
                        }

                        resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                    }
                }

                // Get the object name from the data
                int lenght_list = controlsList.Count + 1;
                string objectName = e.Data.GetData("PictureBoxName") as string;
                string[] list_object = {objectName};
                bool[] list_selected = {true};

                Panel destinationPanel = sender as Panel;
                ResizablePanel windown = null;
                var info_program = JsonConvert.DeserializeObject<Info_Program>(destinationPanel.Name);

                int max_app_width = destinationPanel.Width - 2;
                int max_app_height = destinationPanel.Height - 2;
                if (controlsList.Count == 0)
                {
                    var info_windown = new
                    {
                        name = "Windown " + lenght_list,
                        path_windown = "",
                        windown_height = int.Parse(info_program.height_real),
                        windown_width = int.Parse(info_program.width_real),
                        windown_top = 0,
                        windown_left = 0,
                        list = list_object,
                        selected = list_selected
                    };

                    windown = new ResizablePanel(destinationPanel)
                    {
                        Location = new Point(0, 0),
                        Size = new Size(max_app_width, max_app_height),
                        BackColor = Color.Transparent,
                        Name = JsonConvert.SerializeObject(info_windown),
                        AllowDrop = true
                    };
                    
                    this.textBox1.Text = "0";
                    this.textBox2.Text = "0";
                    this.textBox4.Text = Math.Ceiling(Normalize(max_app_width, 0, max_app_width, 0, int.Parse(info_program.width_real))).ToString();
                    this.textBox3.Text = Math.Ceiling(Normalize(max_app_height, 0, max_app_height, 0, int.Parse(info_program.height_real))).ToString();
                    this.panel70.Visible = true;
                }
                else
                {
                    int X = (sender as Control).PointToClient(new Point(e.X, e.Y)).X;
                    int Y = (sender as Control).PointToClient(new Point(e.X, e.Y)).Y;
                    var info_windown = new
                    {
                        name = "Windown " + lenght_list,
                        path_windown = "",
                        windown_height = (int) Math.Ceiling(Normalize(50, 0, max_app_height, 0, int.Parse(info_program.height_real))),
                        windown_width = (int) Math.Ceiling(Normalize(100, 0, max_app_width, 0, int.Parse(info_program.width_real))),
                        windown_top = (int) Math.Ceiling(Normalize(Y, 0, max_app_height, 0, int.Parse(info_program.height_real))),
                        windown_left = (int) Math.Ceiling(Normalize(X, 0, max_app_width, 0, int.Parse(info_program.width_real))),
                        list = list_object,
                        selected = list_selected
                    };

                    windown = new ResizablePanel(destinationPanel)
                    {
                        Location = new Point(X , Y),
                        Size = new Size(100, 50),
                        BackColor = Color.Transparent,
                        Name = JsonConvert.SerializeObject(info_windown),
                        AllowDrop = true
                    };
                }

                windown.CustomEventMouseDown += (sender1, e1, X, Y, app_width, app_height, active_select) =>
                {           
                    this.panel70.Visible = true;

                    this.textBox1.Text = Math.Ceiling(Normalize(X, 0, max_app_width, 0, int.Parse(info_program.width_real))).ToString();
                    this.textBox2.Text = Math.Ceiling(Normalize(Y, 0, max_app_height, 0, int.Parse(info_program.height_real))).ToString();
                    this.textBox4.Text = Math.Ceiling(Normalize(app_width, 0, max_app_width, 0, int.Parse(info_program.width_real))).ToString();
                    this.textBox3.Text = Math.Ceiling(Normalize(app_height, 0, max_app_height, 0, int.Parse(info_program.height_real))).ToString();

                    // Deserialize JSON data from the Name property
                    Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(windown.Name);

                    // Select first item
                    foreach (Control control1 in controlsList)
                    {
                        if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                        {
                            // Deserialize JSON data from the Name property
                            Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);


                            if (infoWindow1.Name.Equals(infoWindow.Name))
                            {
                                // update detail location
                                infoWindow1.windown_width = int.Parse(this.textBox4.Text);
                                infoWindow1.windown_height = int.Parse(this.textBox3.Text);
                                infoWindow1.windown_top = int.Parse(this.textBox2.Text);
                                infoWindow1.windown_left = int.Parse(this.textBox1.Text);
                            }

                            resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                        }
                    }

                    if (!active_select)
                    {
                        return;
                    }

                    // Check again
                    if ((int.Parse(this.textBox1.Text) + int.Parse(this.textBox4.Text)) > int.Parse(info_program.width_real))
                    {
                        this.textBox4.Text = (int.Parse(info_program.width_real) - int.Parse(this.textBox1.Text)).ToString();
                    }

                    if ((int.Parse(this.textBox2.Text) + int.Parse(this.textBox3.Text)) > int.Parse(info_program.height_real))
                    {
                        this.textBox3.Text = (int.Parse(info_program.height_real) - int.Parse(this.textBox2.Text)).ToString();
                    }

                    // Select first item
                    foreach (Control control1 in controlsList)
                    {
                        if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                        {
                            // Deserialize JSON data from the Name property
                            Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);

                            for (int i = 0; i < infoWindow1.selected.Count; i++)
                            {
                                infoWindow1.selected[i] = false;
                            }

                            if (infoWindow1.Name.Equals(infoWindow.Name))
                            {
                                infoWindow1.selected[0] = true;

                                // update detail location
                                infoWindow1.windown_width = int.Parse(this.textBox4.Text);
                                infoWindow1.windown_height = int.Parse(this.textBox3.Text);
                                infoWindow1.windown_top = int.Parse(this.textBox2.Text);
                                infoWindow1.windown_left = int.Parse(this.textBox1.Text);
                            }

                            resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                        }
                    }
           
                    foreach (Control control1 in this.list_windowns.Controls)
                    {
                        if(control1.Name != null)
                            control1.Refresh();
                    }

                    windown.InitializeResizeHandles();
                };

                windown.CustomEventDragDrop += (sender1, e1) =>
                {
                    // Unselect all
                    foreach (Control control in controlsList)
                    {
                        if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                        {
                            // Deserialize JSON data from the Name property
                            Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                            for (int i = 0; i < infoWindow.selected.Count; i++)
                            {
                                infoWindow.selected[i] = false;
                            }

                            resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                            
                        }
                    }

                    // Update controlsList
                    foreach (Control control in controlsList)
                    {
                        if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                        {
                            // Deserialize JSON data from the Name property
                            Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                            if (infoWindow.Name.Equals("Windown " + lenght_list.ToString()))
                            {
                                infoWindow.list.Add(e1.Data.GetData("PictureBoxName") as string);
                                infoWindow.selected.Add(true);
                                resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                            }
                        }
                    }
                    
                    // Draw windown list
                    draw_list_windown(controlsList);
                };

                windown.CustomEventDragOver += (sender1, e1) =>
                {
                    if (drappPictureBox.Visible)
                    {
                        drappPictureBox.Location = new Point(Cursor.Position.X - this.Location.X, Cursor.Position.Y - this.Location.Y - 80);
                    }
                };

                // Load the video file
                windown.videoFileReader = new Accord.Video.FFMPEG.VideoFileReader();

                int interval = 20;
                long total_frame = 0;

                // Create PictureBox for the image
                PictureBox pictureBox = new PictureBox();
                pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
                pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox.Padding = new System.Windows.Forms.Padding(1, 1, 1, 1);
                pictureBox.Name = "0";
                pictureBox.MouseDown += (sender1, e1) =>
                {
                    windown.maunalActiveMouseDown(sender1, e1);
                };
                pictureBox.MouseUp += (sender1, e1) =>
                {
                    windown.maunalActiveMouseUp(sender1, e1);
                };
                pictureBox.MouseMove += (sender1, e1) =>
                {
                    windown.maunalActiveMouseMove(sender1, e1);
                };
                windown.Controls.Add(pictureBox);

                windown.updateTimer = new Timer();
                windown.updateTimer.Interval = interval;
                windown.updateTimer.Tick += (sender1, e1) =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(new MethodInvoker(delegate { /* Công việc giao diện người dùng */ }));
                    }
                    else
                    {
                        if (total_frame != windown.videoFileReader.FrameCount)
                        {
                            total_frame = windown.videoFileReader.FrameCount;
                            pictureBox.Name = "0";
                        }
                        else if (int.Parse(pictureBox.Name) > total_frame)
                        {
                            pictureBox.Name = "0";
                        }

                        // Get the first frame
                        // Giải phóng hình ảnh cũ trước khi gán hình ảnh mới
                        if (pictureBox.Image != null)
                        {
                            pictureBox.Image.Dispose();
                        }
                        pictureBox.Image = windown.videoFileReader.ReadVideoFrame(int.Parse(pictureBox.Name));
                        pictureBox.Name = (int.Parse(pictureBox.Name) + 1).ToString();

                    }
                };

                controlsList.Insert(0, windown);             
                destinationPanel.Controls.AddRange(controlsList.ToArray());

                // Draw windown list
                draw_list_windown(controlsList);
            }
        }

        private void Target_DragOver(object sender, DragEventArgs e)
        {
            if (drappPictureBox.Visible)
            {
                drappPictureBox.Location = new Point(Cursor.Position.X - this.Location.X, Cursor.Position.Y - this.Location.Y - 80);
            }
        }

        private void draw_list_windown(List<Control> lists)
        {
            // Xóa tất cả các điều khiển con trong panel14
            this.list_windowns.Controls.Clear();

            foreach (Control control in lists)
            {
                if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                {
                    // Deserialize JSON data from the Name property
                    Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);
                    
                    Panel delta_Panel1 = new Panel();
                    delta_Panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                    delta_Panel1.Dock = System.Windows.Forms.DockStyle.Top;
                    delta_Panel1.Location = new System.Drawing.Point(0, 0);
                    delta_Panel1.Size = new System.Drawing.Size(946, 3);
                    delta_Panel1.Padding = new System.Windows.Forms.Padding(8, 0, 8, 8);

                    Panel title_Panel = new Panel();
                    title_Panel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                    title_Panel.Dock = System.Windows.Forms.DockStyle.Top;
                    title_Panel.Location = new System.Drawing.Point(0, 0);
                    title_Panel.Size = new System.Drawing.Size(946, 30);
                    title_Panel.Padding = new System.Windows.Forms.Padding(8, 0, 8, 8);

                    Panel delta_Panel2 = new Panel();
                    delta_Panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                    delta_Panel2.Dock = System.Windows.Forms.DockStyle.Top;
                    delta_Panel2.Location = new System.Drawing.Point(0, 0);
                    delta_Panel2.Size = new System.Drawing.Size(946, 2);
                    delta_Panel2.Padding = new System.Windows.Forms.Padding(8, 0, 8, 8);

                    Label windown_name_label = new Label();
                    windown_name_label.AutoSize = true;
                    windown_name_label.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
                    windown_name_label.Dock = System.Windows.Forms.DockStyle.Fill;
                    windown_name_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                    windown_name_label.Location = new System.Drawing.Point(0, 0);
                    windown_name_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    windown_name_label.ForeColor = System.Drawing.Color.White;
                    windown_name_label.Text = infoWindow.Name;
                    title_Panel.Controls.Add(windown_name_label);

                    for (int i = infoWindow.list.Count - 1; i >= 0; i--)
                    {
                        String selectfilePath = infoWindow.list[i];
                        if (!File.Exists(selectfilePath))
                            continue;

                        String typeFile = "Video";
                        string extension = System.IO.Path.GetExtension(selectfilePath).ToLower();
                        Image videoFrame = null;

                        Panel item_Panel = new Panel();
                        item_Panel.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
                        item_Panel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                        item_Panel.Dock = System.Windows.Forms.DockStyle.Top;
                        item_Panel.Location = new System.Drawing.Point(0, 0);
                        item_Panel.Size = new System.Drawing.Size(946, 80);
                        item_Panel.Name = i.ToString();
                        item_Panel.Paint += (sender, e) =>
                        {
                            // Unselect all
                            foreach (Control control1 in lists)
                            {
                                if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                {
                                    // Deserialize JSON data from the Name property
                                    Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);
                                    if (infoWindow1.Name.Equals(infoWindow.Name))
                                    {
                                        Color initialBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64))))); // Set to your initial color
                                        if (infoWindow1.selected[int.Parse((sender as Control).Name)])
                                        {
                                            initialBorderColor = Color.LightBlue;

                                            this.panel70.Visible = true;

                                            int max_app_width = control1.Parent.Width - 2;
                                            int max_app_height = control1.Parent.Height - 2;
                                            var info_program = JsonConvert.DeserializeObject<Info_Program>(control1.Parent.Name);

                                            this.textBox1.Text = Math.Ceiling(Normalize(control1.Left, 0, max_app_width, 0, int.Parse(info_program.width_real))).ToString();
                                            this.textBox2.Text = Math.Ceiling(Normalize(control1.Top, 0, max_app_height, 0, int.Parse(info_program.height_real))).ToString();
                                            this.textBox4.Text = Math.Ceiling(Normalize(control1.Width, 0, max_app_width, 0, int.Parse(info_program.width_real))).ToString();
                                            this.textBox3.Text = Math.Ceiling(Normalize(control1.Height, 0, max_app_height, 0, int.Parse(info_program.height_real))).ToString();

                                            // Check again
                                            if ((int.Parse(this.textBox1.Text) + int.Parse(this.textBox4.Text)) > int.Parse(info_program.width_real))
                                            {
                                                this.textBox4.Text = (int.Parse(info_program.width_real) - int.Parse(this.textBox1.Text)).ToString();
                                            }

                                            if ((int.Parse(this.textBox2.Text) + int.Parse(this.textBox3.Text)) > int.Parse(info_program.height_real))
                                            {
                                                this.textBox3.Text = (int.Parse(info_program.height_real) - int.Parse(this.textBox2.Text)).ToString();
                                            }

                                            ResizablePanel panel_windown = control1 as ResizablePanel;
                                            panel_windown.InitializeResizeHandles();
                                            

                                            String path_file = infoWindow1.list[int.Parse((sender as Control).Name)];
                                            string extension_1 = System.IO.Path.GetExtension(path_file).ToLower();

                                            // Is a video
                                            if (extension_1 == ".mp4" || extension_1 == ".avi" ||
                                                extension_1 == ".wmv" || extension_1 == ".mpg" ||
                                                extension_1 == ".rmvp" || extension_1 == ".mov" ||
                                                extension_1 == ".dat" || extension_1 == ".flv")
                                            {
                                                if (!infoWindow1.path_windown.Equals(path_file) || !panel_windown.updateTimer.Enabled)
                                                {
                                                    // update data
                                                    infoWindow1.path_windown = path_file;
                                                    resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);

                                                    panel_windown.updateTimer.Stop();
                                                    panel_windown.videoFileReader.Close();
                                                    
                                                    panel_windown.videoFileReader.Open(path_file);
                                                    panel_windown.updateTimer.Interval = 1000 / (int)panel_windown.videoFileReader.FrameRate.Value;
                                                    panel_windown.updateTimer.Start();
                                                }

                                            }
                                            else if (extension_1 == ".jpg" || extension_1 == ".bmp" ||
                                                extension_1 == ".png" || extension_1 == ".gif")
                                            {
                                                panel_windown.updateTimer.Stop();

                                                // Iterate through each control in the panel
                                                foreach (Control control_1 in panel_windown.Controls)
                                                {
                                                    // Check if the control is a PictureBox
                                                    if (control_1 is PictureBox)
                                                    {
                                                        // You've found the PictureBox inside the Panel
                                                        PictureBox pictureBoxInPanel = (PictureBox)control_1;

                                                        // Now you can work with pictureBoxInPanel as needed
                                                        if (pictureBoxInPanel.Image != null)
                                                        {
                                                            pictureBoxInPanel.Image.Dispose();
                                                        }
                                                        pictureBoxInPanel.Image = System.Drawing.Image.FromFile(path_file); // Set a new image

                                                        // Break out of the loop if you only need the first PictureBox
                                                        break;
                                                    }
                                                }
                                            }
                                            // Draw a border with a different color and thickness
                                            using (Pen pen = new Pen(initialBorderColor, 2)) // You can change Color.Red to your desired color
                                            {
                                                e.Graphics.DrawRectangle(pen, item_Panel.Padding.Left, 0, item_Panel.Width - 1 - item_Panel.Padding.Left, item_Panel.Height - 1);
                                            }
                                        }
                                    }
                                }
                            }
                        };

                        Panel picture_Panel = new Panel();
                        picture_Panel.BackColor = Color.Transparent;
                        picture_Panel.Padding = new System.Windows.Forms.Padding(2, 2, 0, 2);
                        picture_Panel.Dock = System.Windows.Forms.DockStyle.Left;
                        picture_Panel.Location = new System.Drawing.Point(0, 0);
                        picture_Panel.Size = new System.Drawing.Size(100, 80);

                        // Is a video
                        if (extension == ".mp4" || extension == ".avi" ||
                            extension == ".wmv" || extension == ".mpg" ||
                            extension == ".rmvp" || extension == ".mov" ||
                            extension == ".dat" || extension == ".flv")
                        {

                            // Load the video file
                            Accord.Video.FFMPEG.VideoFileReader videoFileReader = new Accord.Video.FFMPEG.VideoFileReader();
                            videoFileReader.Open(selectfilePath);

                            // Get the first frame
                            videoFrame = videoFileReader.ReadVideoFrame();

                            // Close the video file reader
                            videoFileReader.Close();
                        }
                        else if (extension == ".jpg" || extension == ".bmp" ||
                            extension == ".png" || extension == ".gif")
                        {
                            videoFrame = System.Drawing.Image.FromFile(selectfilePath);
                            typeFile = "Image";
                        }

                        // Create PictureBox for the image
                        PictureBox pictureBox = new PictureBox();
                        pictureBox.Image = videoFrame;
                        pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                        pictureBox.Name = selectfilePath;
                        pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;

                        pictureBox.MouseDown += (sender, e) =>
                        {
                            // Unselect all
                            foreach (Control control1 in lists)
                            {
                                if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                {
                                    // Deserialize JSON data from the Name property
                                    Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);

                                    for (int i1 = 0; i1 < infoWindow1.selected.Count; i1++)
                                    {
                                        infoWindow1.selected[i1] = false;
                                    }

                                    if (infoWindow1.Name.Equals(infoWindow.Name))
                                    {
                                        Control parentControl = (sender as Control).Parent.Parent;
                                        infoWindow1.selected[int.Parse(parentControl.Name)] = true;
                                    }

                                    resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                                }
                            }

                            foreach (Control control1 in this.list_windowns.Controls)
                            {
                                if (control1.Name != null)
                                    control1.Refresh();
                            }
                        };
                        picture_Panel.Controls.Add(pictureBox);

                        Panel label_Panel = new Panel();
                        label_Panel.BackColor = Color.Transparent;
                        label_Panel.Padding = new System.Windows.Forms.Padding(0, 2, 2, 2);
                        label_Panel.Dock = System.Windows.Forms.DockStyle.Fill;

                        Label name_label = new Label();
                        name_label.Padding = new System.Windows.Forms.Padding(5, 25, 0, 0);
                        name_label.Dock = System.Windows.Forms.DockStyle.Fill;
                        name_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                        name_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                        name_label.ForeColor = System.Drawing.Color.White;
                        name_label.Text = typeFile;
                        name_label.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                        name_label.MouseClick += (sender, e) =>
                        {
                            // Unselect all
                            foreach (Control control1 in lists)
                            {
                                if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                {
                                    // Deserialize JSON data from the Name property
                                    Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);

                                    for (int i1 = 0; i1 < infoWindow1.selected.Count; i1++)
                                    {
                                        infoWindow1.selected[i1] = false;
                                    }

                                    if (infoWindow1.Name.Equals(infoWindow.Name))
                                    {
                                        Control parentControl = (sender as Control).Parent.Parent;
                                        infoWindow1.selected[int.Parse(parentControl.Name)] = true;
                                    }

                                    resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                                }
                            }

                            foreach (Control control1 in this.list_windowns.Controls)
                            {
                                if (control1.Name != null)
                                    control1.Refresh();
                            }
                        };

                        label_Panel.Controls.Add(name_label);

                        item_Panel.Controls.Add(label_Panel);
                        item_Panel.Controls.Add(picture_Panel);
                        this.list_windowns.Controls.Add(item_Panel);
                    }
                    this.list_windowns.Controls.Add(delta_Panel2);
                    this.list_windowns.Controls.Add(title_Panel);
                    this.list_windowns.Controls.Add(delta_Panel1);

                    // Update background windown
                    if (infoWindow.list.Count == 0)
                    {
                        if (resizablePanel.updateTimer != null)
                        {
                            resizablePanel.updateTimer.Stop();
                        }

                        if (resizablePanel.videoFileReader != null)
                        {
                            resizablePanel.videoFileReader.Close();
                        }

                        // Iterate through each PictureBox control in the panel_windown
                        foreach (PictureBox pictureBoxInPanel in resizablePanel.Controls.OfType<PictureBox>())
                        {
                            if (pictureBoxInPanel.Image != null)
                            {
                                pictureBoxInPanel.Image = null;
                            }
                        }
                    }
                }
            }
        }

        private void refresh_Click(object sender, EventArgs e)
        {
            while (this.panel35.Controls.Count > 0)
            {
                Control control = this.panel35.Controls[0];
                this.panel35.Controls.Remove(control);
                control.Dispose();
            }

            while (this.panel84.Controls.Count > 0)
            {
                Control control = this.panel84.Controls[0];
                this.panel84.Controls.Remove(control);
                control.Dispose();
            }

            // Counter device
            this.total_pc.Text = "Total 0";
            this.online_pc.Text = "Total 0";

            // Tạo một luồng riêng cho việc lắng nghe UDP
            if(udpListenerThread != null)
            {
                flagTermianlUDPThread = true;
                udpListenerThread.Join();
                udpListenerThread = null;
            }
            

            udpListenerThread = new Thread(() => UdpListener(45454));
            udpListenerThread.Start();
        }

        private void UdpListener(int udpPort)
        {
            try
            {
                using (UdpClient udpListener = new UdpClient(udpPort))
                {
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, udpPort);

                    // Reinit
                    flagTermianlUDPThread = false;
                    int counter_break = 0;
                    String first_device = "";

                    while (!flagTermianlUDPThread)
                    {
                        try
                        {
                            // Use Task.Factory.StartNew to run the asynchronous operation with a cancellation token
                            byte[] receivedBytes = udpListener.Receive(ref endPoint);

                            if (receivedBytes.Length == 0)
                            {
                                continue;
                            }
                            
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

                                                    if (first_device.Length == 0)
                                                    {
                                                        first_device = data.deviceName;
                                                    }                                                        
                                                    else if (first_device.Equals(data.deviceName))
                                                    {
                                                        counter_break--;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (!have_obj)
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
                                                        if (data_getScreenParams.code != 200)
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
                                        addTablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
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
                                        addTablePanel.MouseEnter += row_device_MouseEnter;
                                        addTablePanel.MouseLeave += row_device_MouseLeave;

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
                                        device_name_label.MouseEnter += row_device_MouseEnter;
                                        device_name_label.MouseLeave += row_device_MouseLeave;
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
                                        method_label.MouseEnter += row_device_MouseEnter;
                                        method_label.MouseLeave += row_device_MouseLeave;
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
                                        address_label.MouseEnter += row_device_MouseEnter;
                                        address_label.MouseLeave += row_device_MouseLeave;
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
                                        resolution_label.MouseEnter += row_device_MouseEnter;
                                        resolution_label.MouseLeave += row_device_MouseLeave;
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
                                        brightness_label.Text = data_getScreenParams != null ? data_getScreenParams.bright.ToString() : "--";
                                        brightness_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                        brightness_label.MouseEnter += row_device_MouseEnter;
                                        brightness_label.MouseLeave += row_device_MouseLeave;
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
                                        voice_label.Text = data_getScreenParams != null ? (data_getScreenParams.voice.Equals("1.0") ? "100" : data_getScreenParams.voice.TrimStart('0').Replace(".", "")) : "--"; ;
                                        voice_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                        voice_label.MouseEnter += row_device_MouseEnter;
                                        voice_label.MouseLeave += row_device_MouseLeave;
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
                                        version_label.MouseEnter += row_device_MouseEnter;
                                        version_label.MouseLeave += row_device_MouseLeave;
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


                                        // Create the add panel
                                        Panel addPanelRelease = new Panel();
                                        addPanelRelease.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                                        addPanelRelease.Dock = System.Windows.Forms.DockStyle.Top;
                                        addPanelRelease.Location = new System.Drawing.Point(0, 0);
                                        addPanelRelease.Size = new System.Drawing.Size(946, 60);
                                        addPanelRelease.Padding = new System.Windows.Forms.Padding(8, 0, 8, 8);

                                        // Create the add table panel
                                        TableLayoutPanel addTablePanelRelease = new TableLayoutPanel();
                                        addTablePanelRelease.BorderStyle = BorderStyle.None;
                                        addTablePanelRelease.Dock = System.Windows.Forms.DockStyle.Fill;
                                        addTablePanelRelease.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                                        addTablePanelRelease.ColumnCount = 4;
                                        addTablePanelRelease.Name = data.deviceName;
                                        addTablePanelRelease.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 5F));
                                        addTablePanelRelease.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                                        addTablePanelRelease.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
                                        addTablePanelRelease.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
                                        addTablePanelRelease.MouseEnter += row_device_release_MouseEnter;
                                        addTablePanelRelease.MouseLeave += row_device_release_MouseLeave;

                                        RadioButton radioButton1 = new RadioButton();
                                        //radioButton1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                                        radioButton1.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                        radioButton1.Dock = System.Windows.Forms.DockStyle.Fill;
                                        radioButton1.FlatAppearance.BorderSize = 0;
                                        radioButton1.Font = new System.Drawing.Font("Microsoft Sans Serif", 50F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                                        //radioButton1.ForeColor = System.Drawing.Color.White;
                                        radioButton1.Location = new System.Drawing.Point(0, 0);
                                        radioButton1.Margin = new System.Windows.Forms.Padding(0);
                                        radioButton1.Name = data.deviceName;
                                        radioButton1.Size = new System.Drawing.Size(100, 70);
                                        radioButton1.TabIndex = 0;
                                        radioButton1.TabStop = true;
                                        //radioButton1.UseVisualStyleBackColor = true;
                                        radioButton1.MouseEnter += row_device_release_MouseEnter;
                                        radioButton1.MouseLeave += row_device_release_MouseLeave;
                                        radioButton1.MouseClick += (sender, e) =>
                                        {
                                            foreach (Control control in this.panel84.Controls)
                                            {
                                                if (control is Panel panel)
                                                {
                                                    foreach (Control innerControl in panel.Controls)
                                                    {
                                                        if (innerControl is TableLayoutPanel tableLayoutPanel)
                                                        {
                                                            RadioButton radioObj = (RadioButton)tableLayoutPanel.GetControlFromPosition(0, 0);
                                                            radioObj.Checked = false;
                                                        }
                                                    }
                                                }
                                            }
                                            radioButton1.Checked = true;
                                        };
                                        addTablePanelRelease.Controls.Add(radioButton1, 0, 0);

                                        Label device_name_release_label = new Label();
                                        device_name_release_label.AutoSize = true;
                                        device_name_release_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                        device_name_release_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                        device_name_release_label.Location = new System.Drawing.Point(0, 0);
                                        device_name_release_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                        device_name_release_label.Size = new System.Drawing.Size(283, 30);
                                        device_name_release_label.TabIndex = 0;
                                        device_name_release_label.Name = data.deviceName;
                                        device_name_release_label.Text = data.deviceName;
                                        device_name_release_label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                                        device_name_release_label.MouseEnter += row_device_release_MouseEnter;
                                        device_name_release_label.MouseLeave += row_device_release_MouseLeave;
                                        addTablePanelRelease.Controls.Add(device_name_release_label, 1, 0);

                                        Label address_release_label = new Label();
                                        address_release_label.AutoSize = true;
                                        address_release_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                        address_release_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                        address_release_label.Location = new System.Drawing.Point(0, 0);
                                        address_release_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                        address_release_label.Size = new System.Drawing.Size(283, 30);
                                        address_release_label.TabIndex = 0;
                                        address_release_label.Name = data.deviceName;
                                        address_release_label.Text = endPoint.Address.ToString();
                                        address_release_label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                                        address_release_label.MouseEnter += row_device_release_MouseEnter;
                                        address_release_label.MouseLeave += row_device_release_MouseLeave;
                                        addTablePanelRelease.Controls.Add(address_release_label, 2, 0);

                                        Label remain_release_label = new Label();
                                        remain_release_label.AutoSize = true;
                                        remain_release_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                        remain_release_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                        remain_release_label.Location = new System.Drawing.Point(0, 0);
                                        remain_release_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                        remain_release_label.Size = new System.Drawing.Size(283, 30);
                                        remain_release_label.TabIndex = 0;
                                        remain_release_label.Name = data.deviceName;
                                        remain_release_label.Text = Math.Round(((float)data.usableSpace / 1024), 2).ToString();
                                        remain_release_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                        remain_release_label.MouseEnter += row_device_release_MouseEnter;
                                        remain_release_label.MouseLeave += row_device_release_MouseLeave;
                                        addTablePanelRelease.Controls.Add(remain_release_label, 3, 0);

                                        addTablePanelRelease.Dock = System.Windows.Forms.DockStyle.Fill;
                                        addTablePanelRelease.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                                        addTablePanelRelease.ForeColor = System.Drawing.Color.White;
                                        addTablePanelRelease.Location = new System.Drawing.Point(0, 0);
                                        addTablePanelRelease.RowCount = 1;
                                        addTablePanelRelease.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
                                        addTablePanelRelease.Size = new System.Drawing.Size(946, 60);
                                        addTablePanelRelease.TabIndex = 0;

                                        addTablePanelRelease.Paint += (sender1, e1) =>
                                        {
                                            int boTronKichThuoc = 20; // Điều chỉnh độ bo tròn ở đây
                                            GraphicsPath graphicsPath = new GraphicsPath();

                                            Rectangle rect = new Rectangle(0, 0, addTablePanelRelease.Width, addTablePanelRelease.Height);
                                            graphicsPath.AddArc(rect.X, rect.Y, boTronKichThuoc, boTronKichThuoc, 180, 90);
                                            graphicsPath.AddArc(rect.Right - boTronKichThuoc, rect.Y, boTronKichThuoc, boTronKichThuoc, 270, 90);
                                            graphicsPath.AddArc(rect.Right - boTronKichThuoc, rect.Bottom - boTronKichThuoc, boTronKichThuoc, boTronKichThuoc, 0, 90);
                                            graphicsPath.AddArc(rect.X, rect.Bottom - boTronKichThuoc, boTronKichThuoc, boTronKichThuoc, 90, 90);
                                            graphicsPath.CloseAllFigures();

                                            addTablePanelRelease.Region = new Region(graphicsPath);
                                        };

                                        addPanelRelease.Controls.Add(addTablePanelRelease);
                                        this.panel84.Controls.Add(addPanelRelease);

                                        // Counter device
                                        this.total_pc.Text = "Total " + counter_device.ToString();
                                        this.online_pc.Text = "Total " + counter_device.ToString();
                                    });
                                }

                            }
                            else if(counter_break < 0)
                            {
                                Console.WriteLine("Scan device finished");
                                break;
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
            catch (Exception e)
            {
                Console.WriteLine($"Lỗi: {e}");
            }
        }

        private void new_resource_Click(object sender, EventArgs e)
        {
            // Lấy đường dẫn đến thư mục cuối cùng trong UserProfile của người dùng
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string lastDirectory = Path.Combine(userProfile, "Documents");

            // Create an instance of the OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog();
            
            // Set the title of the dialog
            openFileDialog.Title = "Select a File";

            // Set the initial directory (optional)
            openFileDialog.InitialDirectory = lastDirectory;

            // Allow the user to select multiple files
            openFileDialog.Multiselect = true;

            // Set the file types allowed to be selected
            openFileDialog.Filter = "Video Files (*.mp4;*.avi;*.wmv;*.mpg;*.rmvp;*.mov;*.dat;*.flv)|*.mp4;*.avi;*.wmv;*.mpg;*.rmvp;*.mov;*.dat;*.flv|Image Files (*.jpg;*.bmp;*.png;*.gif)|*.jpg;*.bmp;*.png;*.gif|All Files (*.*)|*.*";

            // Show the dialog and check if the user selected a file
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Get the selected file paths (for multiple selections)
                string[] selectedFilePaths = openFileDialog.FileNames;

                foreach (string selectfilePath in selectedFilePaths)
                {
                    add_detail_file(selectfilePath, true);                    
                }                
            }
        }

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            // Bắt đầu quá trình kéo khi nhấn chuột trái
            if (e.Button == MouseButtons.Left)
            {
                Control parentControl = (sender as Control).Parent;
                if (parentControl is TableLayoutPanel tableLayoutPanel)
                {
                    Control controlInFirstColumn = tableLayoutPanel.GetControlFromPosition(0, 0);
                    
                    // Kiểm tra xem controlInFirstColumn có phải là PictureBox không
                    if (controlInFirstColumn is PictureBox pictureBox)
                    {
                        // Check list item exist
                        foreach (Control control in this.panel43.Controls)
                        {
                            if (control is Panel panel_chill && panel_chill.Visible)
                            {
                                // Show image
                                drappPictureBox.Image = pictureBox.Image;
                                drappPictureBox.Visible = true;
                                drappPictureBox.Location = new Point(Cursor.Position.X - this.Location.X, Cursor.Position.Y - this.Location.Y - 80);

                                // Tạo một DataObject để chứa thông tin cần truyền đi
                                DataObject data = new DataObject();
                                data.SetData("PictureBoxImage", pictureBox.Image);  // Chuyển hình ảnh
                                data.SetData("PictureBoxName", pictureBox.Name);    // Chuyển tên

                                pictureBox.DoDragDrop(data, DragDropEffects.Move);

                                // Hide image
                                drappPictureBox.Visible = false;
                            }
                        }
                    }
                }
            }
        }

        private void add_detail_file(string selectfilePath, bool allow_write)
        {
            if (!File.Exists(selectfilePath))
                return;

            string extension = System.IO.Path.GetExtension(selectfilePath).ToLower();

            Image videoFrame = null;
            var mediaInfo = new MediaInfo.DotNetWrapper.MediaInfo();
            mediaInfo.Open(selectfilePath);

            string fileName = System.IO.Path.GetFileName(selectfilePath);
            String typeFile = "Video";
            String resolution = mediaInfo.Get(StreamKind.Video, 0, "Width") + "*" + mediaInfo.Get(StreamKind.Video, 0, "Height");

            String duration = "00:00:00";
            try
            {
                if (double.Parse(mediaInfo.Get(StreamKind.General, 0, "Duration")) > 0)
                {
                    // Convert duration to TimeSpan
                    TimeSpan durationTimeSpan = TimeSpan.FromMilliseconds(double.Parse(mediaInfo.Get(StreamKind.General, 0, "Duration")));
                    duration = $"{(int)durationTimeSpan.TotalHours:D2}:{durationTimeSpan.Minutes:D2}:{durationTimeSpan.Seconds:D2}";
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"{e}");
            }


            String Path = selectfilePath;

            // Check list item exist
            foreach (Control control in this.panel46.Controls)
            {
                if (control is Panel panel_chill)
                {
                    // Now, check if there is a TableLayoutPanel within panel_chill
                    TableLayoutPanel tableLayoutPanel = panel_chill.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                    if (tableLayoutPanel != null)
                    {
                        // Get the control in the first cell of the first column (assuming it's a Label)
                        Control controlInFirstColumn = tableLayoutPanel.GetControlFromPosition(1, 0);
                        if (controlInFirstColumn != null && controlInFirstColumn is Label device_name_label_in_list)
                        {
                            // File have added
                            if (device_name_label_in_list.Text.Length > 0 && device_name_label_in_list.Text.Equals(fileName))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            if(allow_write)
            {
                // Lưu thông tin vào tệp tin
                using (StreamWriter sw = new StreamWriter("material.data", true))
                {
                    sw.WriteLine(selectfilePath);
                }
            }

            // Is a video
            if (extension == ".mp4" || extension == ".avi" ||
                extension == ".wmv" || extension == ".mpg" ||
                extension == ".rmvp" || extension == ".mov" ||
                extension == ".dat" || extension == ".flv")
            {

                // Load the video file
                Accord.Video.FFMPEG.VideoFileReader videoFileReader = new Accord.Video.FFMPEG.VideoFileReader();
                videoFileReader.Open(selectfilePath);

                // Get the first frame
                videoFrame = videoFileReader.ReadVideoFrame();

                // Close the video file reader
                videoFileReader.Close();
            }
            else if (extension == ".jpg" || extension == ".bmp" ||
                extension == ".png" || extension == ".gif")
            {
                videoFrame = System.Drawing.Image.FromFile(selectfilePath);

                typeFile = "Image";
                resolution = System.Drawing.Image.FromFile(selectfilePath).Width.ToString() + "*" + System.Drawing.Image.FromFile(selectfilePath).Height.ToString();
            }

            Panel row_file = new Panel();
            row_file.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            row_file.Dock = System.Windows.Forms.DockStyle.Top;
            row_file.Location = new System.Drawing.Point(0, 62);
            row_file.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            row_file.Size = new System.Drawing.Size(696, 55);
            row_file.TabIndex = 0;
            row_file.ForeColor = System.Drawing.Color.White;
            row_file.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));


            // Create the add table panel
            TableLayoutPanel addTablePanel = new TableLayoutPanel();
            addTablePanel.BorderStyle = BorderStyle.None;
            addTablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            addTablePanel.Name = fileName;
            addTablePanel.AutoSize = false;
            addTablePanel.ColumnCount = 6;
            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 17F));
            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 17F));
            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 31F));
            addTablePanel.MouseEnter += row_file_MouseEnter;
            addTablePanel.MouseLeave += row_file_MouseLeave;
            
            // Create PictureBox for the image
            PictureBox pictureBox = new PictureBox();
            pictureBox.Image = videoFrame;
            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox.Name = selectfilePath;
            pictureBox.AllowDrop = true;
            addTablePanel.Controls.Add(pictureBox, 0, 0);
      
            Label device_name_label = new Label();
            device_name_label.AutoSize = false;
            device_name_label.AutoEllipsis = true;
            device_name_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            device_name_label.Location = new System.Drawing.Point(0, 0);
            device_name_label.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            device_name_label.Size = new System.Drawing.Size(283, 30);
            device_name_label.TabIndex = 0;
            device_name_label.Name = fileName;        
            device_name_label.Text = fileName;
            device_name_label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            device_name_label.Width = 283; // Set your desired width
            device_name_label.MouseEnter += row_file_MouseEnter;
            device_name_label.MouseLeave += row_file_MouseLeave;
            device_name_label.MouseDown += PictureBox_MouseDown;
            addTablePanel.Controls.Add(device_name_label, 1, 0);

            Label type_label = new Label();
            type_label.AutoSize = false;
            type_label.AutoEllipsis = true;
            type_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            type_label.Location = new System.Drawing.Point(0, 0);
            type_label.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            type_label.Size = new System.Drawing.Size(283, 30);
            type_label.TabIndex = 0;
            type_label.Name = fileName;
            type_label.Text = typeFile;
            type_label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            type_label.MouseEnter += row_file_MouseEnter;
            type_label.MouseLeave += row_file_MouseLeave;
            type_label.MouseDown += PictureBox_MouseDown;
            addTablePanel.Controls.Add(type_label, 2, 0);

            Label resolution_label = new Label();
            resolution_label.AutoSize = false;
            resolution_label.AutoEllipsis = true;
            resolution_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            resolution_label.Location = new System.Drawing.Point(0, 0);
            resolution_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
            resolution_label.Size = new System.Drawing.Size(283, 30);
            resolution_label.TabIndex = 0;
            resolution_label.Name = fileName;
            resolution_label.Text = resolution;
            resolution_label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            resolution_label.MouseEnter += row_file_MouseEnter;
            resolution_label.MouseLeave += row_file_MouseLeave;
            resolution_label.MouseDown += PictureBox_MouseDown;
            addTablePanel.Controls.Add(resolution_label, 3, 0);

            Label duration_label = new Label();
            duration_label.AutoSize = false;
            duration_label.AutoEllipsis = true;
            duration_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            duration_label.Location = new System.Drawing.Point(0, 0);
            duration_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
            duration_label.Size = new System.Drawing.Size(283, 30);
            duration_label.TabIndex = 0;
            duration_label.Name = fileName;
            duration_label.Text = duration;
            duration_label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            duration_label.MouseEnter += row_file_MouseEnter;
            duration_label.MouseLeave += row_file_MouseLeave;
            duration_label.MouseDown += PictureBox_MouseDown;
            addTablePanel.Controls.Add(duration_label, 4, 0);

            Label path_label = new Label();
            path_label.AutoSize = false;
            path_label.AutoEllipsis = true;
            path_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            path_label.Location = new System.Drawing.Point(0, 0);
            path_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
            path_label.Size = new System.Drawing.Size(283, 30);
            path_label.TabIndex = 0;
            path_label.Name = fileName;
            path_label.Text = Path;
            path_label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            path_label.MouseEnter += row_file_MouseEnter;
            path_label.MouseLeave += row_file_MouseLeave;
            path_label.MouseDown += PictureBox_MouseDown;
            addTablePanel.Controls.Add(path_label, 5, 0);

            row_file.Controls.Add(addTablePanel);
            this.panel46.Controls.Add(row_file);
        }

        private void textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Kiểm tra xem ký tự được nhập vào có phải là số không
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; // Loại bỏ ký tự không phải số
            }
        }

        private void setting_program(object sender, EventArgs e)
        {
            foreach (Control control in this.panel43.Controls)
            {
                if (control is Panel panel_chill && panel_chill.Visible)
                {
                    var info_program = JsonConvert.DeserializeObject<Info_Program>(control.Name);

                    setting_form popup = new setting_form();
                    popup.ConfirmClick += (sender1, e1) =>
                    {
                        if (int.TryParse(e1.width_real, out int width) && int.TryParse(e1.height_real, out int height))
                        {
                            reinit();

                            var infoProgram = new
                            {
                                name = e1.name,
                                width_resolution = e1.width_resolution,
                                height_resolution = e1.height_resolution,
                                width_real = e1.width_real,
                                height_real = e1.height_real,
                                bittrate_select = e1.bittrate_select
                            };
                            
                            this.panel43.Controls.Clear();
                            
                            // Get the maximum allowable width and height based on the mainPanel's size
                            int width_contain = this.panel43.Width;
                            int height_contain = this.panel43.Height;
                            int width_select = width;
                            int height_select = height;
                            float delta = (float)width_select / (float)height_select;
                            float width_config = 0;
                            float height_config = 0;
                            do
                            {
                                height_config += 1;
                                width_config += delta;
                            }
                            while ((width_config < (width_contain - 50)) && (height_config < (height_contain - 70)));
                            
                            // Create the inner panel based on the adjusted width and height
                            Panel innerPanel = new Panel
                            {
                                Width = (int)width_config,
                                Height = (int)height_config,
                                BackColor = Color.Black,
                                Name = JsonConvert.SerializeObject(infoProgram),
                                AllowDrop = true
                            };
                            
                            // Calculate the position to center the inner panel within the main panel
                            int x = (width_contain - (int)width_config) / 2;
                            int y = (height_contain - (int)height_config) / 2;
                            
                            
                            // Set the location of the inner panel
                            innerPanel.Location = new Point(x, y);
                            
                            // Đăng ký sự kiện DragDrop và DragEnter cho Panel
                            innerPanel.DragDrop += TargetPanel_DragDrop;
                            innerPanel.DragEnter += TargetPanel_DragEnter;
                            innerPanel.DragOver += Target_DragOver;
                            innerPanel.MouseDown += (sender2, e2) =>
                            {
                                unselect_object();
                            };
                            
                            // Add the inner panel to the main panel
                            this.panel43.Controls.Add(innerPanel);
                            
                            // Create list program
                            this.panel47.Visible = true;
                            label36.Text = width + "*" + height;
                            label35.Text = e1.name;

                            // Create list program
                            this.panel72.Visible = true;
                            label43.Text = width + "*" + height;
                            label44.Text = e1.name;
                        }
                    };

                    popup.set_name_program(info_program.Name);
                    popup.set_resolution(info_program.width_resolution, info_program.height_resolution);
                    popup.set_resolution_real(info_program.width_real, info_program.height_real);
                    popup.set_bittrate(info_program.bittrate_select);

                    popup.ShowDialog();
                    break;
                }
            }         
        }

        private void delete_program(object sender, EventArgs e)
        {            
            reinit();
        }

        private void reinit()
        {
            foreach (Control control1 in controlsList)
            {
                ResizablePanel panel_windown = control1 as ResizablePanel;

                if(panel_windown.updateTimer != null)
                {
                    panel_windown.updateTimer.Stop();
                    panel_windown.updateTimer = null;
                }

                if (panel_windown.videoFileReader != null)
                {
                    panel_windown.videoFileReader.Close();
                    panel_windown.videoFileReader = null;
                }
            }

            controlsList.Clear();

            this.list_windowns.Controls.Clear();
            this.panel43.Controls.Clear();
            this.panel47.Visible = false;
            this.panel72.Visible = false;

        }

        private void refresh_program_design()
        {
            // Resize child panels in program list (panel43)
            var visiblePanels = this.panel43.Controls
                .OfType<Panel>()
                .Where(panel => panel.Visible);
            
            foreach (var panel_chill in visiblePanels)
            {
                int widthK1 = panel_chill.Width;
                int heightK1 = panel_chill.Height;
                var info_program = JsonConvert.DeserializeObject<Info_Program>(panel_chill.Name);
            
                // Get the maximum allowable width and height based on the mainPanel's size
                int width_contain = this.panel43.Width;
                int height_contain = this.panel43.Height;
                int width_select = int.Parse(info_program.width_real);
                int height_select = int.Parse(info_program.height_real);
                float delta = (float)width_select / (float)height_select;
                float width_config = 0;
                float height_config = 0;
                do
                {
                    height_config += 1;
                    width_config += delta;
                }
                while ((width_config < (width_contain - 50)) && (height_config < (height_contain - 70)));
            
                // Calculate the position to center the inner panel within the main panel
                int x = (width_contain - (int)width_config) / 2;
                int y = (height_contain - (int)height_config) / 2;
            
                panel_chill.Width = (int)width_config;
                panel_chill.Height = (int)height_config;
                panel_chill.Location = new Point(x, y);
            
                foreach (Control control1 in controlsList)
                {
                    control1.Left = (int)Math.Ceiling(Normalize(control1.Location.X, 0, widthK1, 0, panel_chill.Width));
                    control1.Top = (int)Math.Ceiling(Normalize(control1.Location.Y, 0, heightK1, 0, panel_chill.Height));
                    control1.Width = (int) Math.Ceiling(Normalize(control1.Width, 0, widthK1, 0, panel_chill.Width));
                    control1.Height = (int)Math.Ceiling(Normalize(control1.Height, 0, heightK1, 0, panel_chill.Height));
                }
            }

            unselect_object();
        }

        private void button_function(object sender, EventArgs e)
        {
            if ((sender as Button).Name.Equals("Save"))
            {
                // Resize child panels in program list (panel43)
                var visiblePanels = this.panel43.Controls
                    .OfType<Panel>()
                    .Where(panel => panel.Visible);

                foreach (var panel_show in visiblePanels)
                {
                    var info_program = JsonConvert.DeserializeObject<Info_Program>(panel_show.Name);
                    List<Info_Window> info_windown = new List<Info_Window>();

                    foreach (Control control1 in controlsList)
                    {
                        ResizablePanel panel_windown = control1 as ResizablePanel;

                        info_windown.Add(JsonConvert.DeserializeObject<Info_Window>(panel_windown.Name));
                    }
                    
                    // Open a file dialog to select the output file path
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "PROGRAM files (*.data)|*.data|All files (*.*)|*.*";
                    saveFileDialog.Title = "Save PROGRAM File";
                    saveFileDialog.FileName = info_program.Name;

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        var save_info = new
                        {
                            info_program = info_program,
                            info_windown = info_windown
                        };

                        // Save the program string to the selected file path
                        string filePath = saveFileDialog.FileName;
                        File.WriteAllText(filePath, JsonConvert.SerializeObject(save_info, Formatting.None));
                    }
                }
            }
            else if ((sender as Button).Name.Equals("Open"))
            {
                
                // Open a file dialog to select a file
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "PROGRAM files (*.data)|*.data|All files (*.*)|*.*";
                openFileDialog.Title = "Open PROGRAM File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (Path.GetExtension(openFileDialog.FileName).ToLower() == ".data")
                    {
                        // Read the selected file
                        string filePath = openFileDialog.FileName;

                        // Read the content of the file
                        var info_stored = JsonConvert.DeserializeObject<Info_stored>(File.ReadAllText(filePath));

                        // Set up show area
                        reinit();
                        
                        var infoProgram = new
                        {
                            name = info_stored.info_program.Name,
                            width_resolution = info_stored.info_program.width_resolution,
                            height_resolution = info_stored.info_program.height_resolution,
                            width_real = info_stored.info_program.width_real,
                            height_real = info_stored.info_program.height_real,
                            bittrate_select = info_stored.info_program.bittrate_select
                        };
                        
                        this.panel43.Controls.Clear();
                        
                        // Get the maximum allowable width and height based on the mainPanel's size
                        int width = int.Parse(info_stored.info_program.width_real);
                        int height = int.Parse(info_stored.info_program.height_real);
                        int width_contain = this.panel43.Width;
                        int height_contain = this.panel43.Height;
                        int width_select = width;
                        int height_select = height;
                        float delta = (float)width_select / (float)height_select;
                        float width_config = 0;
                        float height_config = 0;
                        do
                        {
                            height_config += 1;
                            width_config += delta;
                        }
                        while ((width_config < (width_contain - 50)) && (height_config < (height_contain - 70)));
                        
                        // Create the inner panel based on the adjusted width and height
                        Panel innerPanel = new Panel
                        {
                            Width = (int)width_config,
                            Height = (int)height_config,
                            BackColor = Color.Black,
                            Name = JsonConvert.SerializeObject(infoProgram),
                            AllowDrop = true
                        };
                        
                        // Calculate the position to center the inner panel within the main panel
                        int x = (width_contain - (int)width_config) / 2;
                        int y = (height_contain - (int)height_config) / 2;
                        
                        
                        // Set the location of the inner panel
                        innerPanel.Location = new Point(x, y);
                        
                        // Đăng ký sự kiện DragDrop và DragEnter cho Panel
                        innerPanel.DragDrop += TargetPanel_DragDrop;
                        innerPanel.DragEnter += TargetPanel_DragEnter;
                        innerPanel.DragOver += Target_DragOver;
                        innerPanel.MouseDown += (sender2, e2) =>
                        {
                            unselect_object();
                        };
                        
                        // Add the inner panel to the main panel
                        this.panel43.Controls.Add(innerPanel);

                        // Create list program
                        this.panel47.Visible = true;
                        this.panel72.Visible = true;
                        label36.Text = int.Parse(info_stored.info_program.width_real) + "*" + height;
                        label35.Text = info_stored.info_program.Name;

                        var visiblePanels = this.panel43.Controls
                            .OfType<Panel>()
                            .Where(panel => panel.Visible);

                        foreach (var destinationPanel in visiblePanels)
                        {
                            // Draw windows
                            for (int idx = 0; idx < info_stored.info_windown.Count; idx++)
                            {
                                int idx_window = info_stored.info_windown.Count - idx - 1;
                                for (int idx_item = 0; idx_item < info_stored.info_windown[idx_window].list.Count; idx_item++)
                                {
                                    // Get the object name from the data
                                    int lenght_list = idx_window + 1;
                                    string objectName = info_stored.info_windown[idx_window].list[idx_item];
                                    string[] list_object = {objectName};
                                    bool[] list_selected = {false};

                                    int max_app_width = destinationPanel.Width - 2;
                                    int max_app_height = destinationPanel.Height - 2;
                                    int X = (int)Math.Ceiling(Normalize(info_stored.info_windown[idx_window].windown_left, 0, int.Parse(info_stored.info_program.width_real), 0, max_app_width));
                                    int Y = (int)Math.Ceiling(Normalize(info_stored.info_windown[idx_window].windown_top, 0, int.Parse(info_stored.info_program.height_real), 0, max_app_height));
                                    int width_windown = (int)Math.Ceiling(Normalize(info_stored.info_windown[idx_window].windown_width, 0, int.Parse(info_stored.info_program.width_real), 0, max_app_width));
                                    int height_windown = (int)Math.Ceiling(Normalize(info_stored.info_windown[idx_window].windown_height, 0, int.Parse(info_stored.info_program.height_real), 0, max_app_height));
                                    
                                    var info_windown = new
                                    {
                                        name = "Windown " + lenght_list,
                                        path_windown = "",
                                        windown_height = info_stored.info_windown[idx_window].windown_height,
                                        windown_width = info_stored.info_windown[idx_window].windown_width,
                                        windown_top = info_stored.info_windown[idx_window].windown_top,
                                        windown_left = info_stored.info_windown[idx_window].windown_left,
                                        list = list_object,
                                        selected = list_selected
                                    };

                                    ResizablePanel windown_load = null;
                                    if (idx_item == 0)
                                    {
                                        windown_load = new ResizablePanel(destinationPanel)
                                        {
                                            Location = new Point(X, Y),
                                            Size = new Size(width_windown, height_windown),
                                            BackColor = Color.Transparent,
                                            Name = JsonConvert.SerializeObject(info_windown),
                                            AllowDrop = true
                                        };

                                        windown_load.CustomEventMouseDown += (sender1, e1, X1, Y1, app_width, app_height, active_select) =>
                                        {
                                            this.panel70.Visible = true;

                                            this.textBox1.Text = Math.Ceiling(Normalize(X1, 0, max_app_width, 0, int.Parse(info_stored.info_program.width_real))).ToString();
                                            this.textBox2.Text = Math.Ceiling(Normalize(Y1, 0, max_app_height, 0, int.Parse(info_stored.info_program.height_real))).ToString();
                                            this.textBox4.Text = Math.Ceiling(Normalize(app_width, 0, max_app_width, 0, int.Parse(info_stored.info_program.width_real))).ToString();
                                            this.textBox3.Text = Math.Ceiling(Normalize(app_height, 0, max_app_height, 0, int.Parse(info_stored.info_program.height_real))).ToString();

                                            // Deserialize JSON data from the Name property
                                            Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(windown_load.Name);

                                            // Select first item
                                            foreach (Control control1 in controlsList)
                                            {
                                                if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                                {
                                                    // Deserialize JSON data from the Name property
                                                    Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);


                                                    if (infoWindow1.Name.Equals(infoWindow.Name))
                                                    {
                                                        // update detail location
                                                        infoWindow1.windown_width = int.Parse(this.textBox4.Text);
                                                        infoWindow1.windown_height = int.Parse(this.textBox3.Text);
                                                        infoWindow1.windown_top = int.Parse(this.textBox2.Text);
                                                        infoWindow1.windown_left = int.Parse(this.textBox1.Text);

                                                    }

                                                    resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                                                }
                                            }

                                            // Select first item
                                            foreach (Control control1 in controlsList)
                                            {
                                                if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                                {
                                                    Console.WriteLine(resizablePanel1.Name);
                                                }
                                            }
                                            if (!active_select)
                                            {
                                                return;
                                            }
                                  
                                            // Check again
                                            if ((int.Parse(this.textBox1.Text) + int.Parse(this.textBox4.Text)) > int.Parse(info_stored.info_program.width_real))
                                            {
                                                this.textBox4.Text = (int.Parse(info_stored.info_program.width_real) - int.Parse(this.textBox1.Text)).ToString();
                                            }

                                            if ((int.Parse(this.textBox2.Text) + int.Parse(this.textBox3.Text)) > int.Parse(info_stored.info_program.height_real))
                                            {
                                                this.textBox3.Text = (int.Parse(info_stored.info_program.height_real) - int.Parse(this.textBox2.Text)).ToString();
                                            }



                                            // Select first item
                                            foreach (Control control1 in controlsList)
                                            {
                                                if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                                {
                                                    // Deserialize JSON data from the Name property
                                                    Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);

                                                    for (int i = 0; i < infoWindow1.selected.Count; i++)
                                                    {
                                                        infoWindow1.selected[i] = false;
                                                    }

                                                    if (infoWindow1.Name.Equals(infoWindow.Name))
                                                    {
                                                        infoWindow1.selected[0] = true;

                                                        // update detai location
                                                        infoWindow1.windown_width = int.Parse(this.textBox4.Text);
                                                        infoWindow1.windown_height = int.Parse(this.textBox3.Text);
                                                        infoWindow1.windown_top = int.Parse(this.textBox2.Text);
                                                        infoWindow1.windown_left = int.Parse(this.textBox1.Text);
                                                    }

                                                    resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                                                }
                                            }

                                            foreach (Control control1 in this.list_windowns.Controls)
                                            {
                                                if (control1.Name != null)
                                                    control1.Refresh();
                                            }

                                            windown_load.InitializeResizeHandles();
                                        };

                                        windown_load.CustomEventDragDrop += (sender1, e1) =>
                                        {
                                            // Unselect all
                                            foreach (Control control in controlsList)
                                            {
                                                if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                                {
                                                    // Deserialize JSON data from the Name property
                                                    Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                                                    for (int i = 0; i < infoWindow.selected.Count; i++)
                                                    {
                                                        infoWindow.selected[i] = false;
                                                    }

                                                    resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);

                                                }
                                            }

                                            // Update controlsList
                                            foreach (Control control in controlsList)
                                            {
                                                if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                                {
                                                    // Deserialize JSON data from the Name property
                                                    Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                                                    if (infoWindow.Name.Equals("Windown " + lenght_list.ToString()))
                                                    {
                                                        infoWindow.list.Add(e1.Data.GetData("PictureBoxName") as string);
                                                        infoWindow.selected.Add(true);
                                                        resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                                                    }
                                                }
                                            }

                                            foreach (Control control in controlsList)
                                            {
                                                if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                                {
                                                    Console.WriteLine(resizablePanel.Name);
                                                }
                                            }

                                            // Draw windown list
                                            draw_list_windown(controlsList);
                                        };

                                        windown_load.CustomEventDragOver += (sender1, e1) =>
                                        {
                                            if (drappPictureBox.Visible)
                                            {
                                                drappPictureBox.Location = new Point(Cursor.Position.X - this.Location.X, Cursor.Position.Y - this.Location.Y - 80);
                                            }
                                        };

                                        // Load the video file
                                        windown_load.videoFileReader = new Accord.Video.FFMPEG.VideoFileReader();
                                        windown_load.videoFileReader.Open(objectName);

                                        long total_frame = 0;

                                        // Create PictureBox for the image
                                        PictureBox pictureBox = new PictureBox();
                                        pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
                                        pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                                        pictureBox.Padding = new System.Windows.Forms.Padding(1, 1, 1, 1);
                                        pictureBox.Name = "0";
                                        pictureBox.MouseDown += (sender1, e1) =>
                                        {
                                            windown_load.maunalActiveMouseDown(sender1, e1);
                                        };
                                        pictureBox.MouseUp += (sender1, e1) =>
                                        {
                                            windown_load.maunalActiveMouseUp(sender1, e1);
                                        };
                                        pictureBox.MouseMove += (sender1, e1) =>
                                        {
                                            windown_load.maunalActiveMouseMove(sender1, e1);
                                        };
                                        windown_load.Controls.Add(pictureBox);



                                        windown_load.updateTimer = new Timer();
                                        windown_load.updateTimer.Interval = 1000 / (int)windown_load.videoFileReader.FrameRate.Value;
                                        windown_load.updateTimer.Tick += (sender1, e1) =>
                                        {
                                            if (InvokeRequired)
                                            {
                                                Invoke(new MethodInvoker(delegate { /* Công việc giao diện người dùng */ }));
                                            }
                                            else
                                            {
                                                if (total_frame != windown_load.videoFileReader.FrameCount)
                                                {
                                                    total_frame = windown_load.videoFileReader.FrameCount;
                                                    pictureBox.Name = "0";
                                                }
                                                else if (int.Parse(pictureBox.Name) > total_frame)
                                                {
                                                    pictureBox.Name = "0";
                                                }

                                                // Get the first frame
                                                // Giải phóng hình ảnh cũ trước khi gán hình ảnh mới
                                                if (pictureBox.Image != null)
                                                {
                                                    pictureBox.Image.Dispose();
                                                }
                                                pictureBox.Image = windown_load.videoFileReader.ReadVideoFrame(int.Parse(pictureBox.Name));
                                                pictureBox.Name = (int.Parse(pictureBox.Name) + 1).ToString();

                                            }
                                        };
                                        windown_load.updateTimer.Start();

                                        controlsList.Insert(0, windown_load);
                                        destinationPanel.Controls.AddRange(controlsList.ToArray());
                                    }
                                    else
                                    {
                                        // Update controlsList
                                        foreach (Control control in controlsList)
                                        {
                                            if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                            {
                                                // Deserialize JSON data from the Name property
                                                Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                                                if (infoWindow.Name.Equals("Windown " + lenght_list.ToString()))
                                                {
                                                    infoWindow.list.Add(objectName);
                                                    infoWindow.selected.Add(false);
                                                    resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Draw windown list
                            draw_list_windown(controlsList);

                            unselect_object();
                        }
                    }
                    else
                    {
                        MessageBox.Show("The file is not in the correct format.", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                }
            }
            else
            {
                bool flag_draw = false;
                
                foreach (Control control1 in controlsList)
                {
                    if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                    {
                        // Deserialize JSON data from the Name property
                        Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);
                        int idx_select = -1;

                        for (int i1 = 0; i1 < infoWindow1.selected.Count; i1++)
                        {
                            if (infoWindow1.selected[i1])
                            {
                                idx_select = i1;
                            }
                        }
                        
                        // None object select
                        if (idx_select < 0)
                            continue;

                        if ((sender as Button).Name.Equals("Up"))
                        {
                            
                            if (idx_select > 0)
                            {
                                infoWindow1.path_windown = "";
                                infoWindow1.selected[idx_select] = false;
                                infoWindow1.selected[idx_select - 1] = true;
                                resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);

                                flag_draw = true;
                            }
                        }
                        else if ((sender as Button).Name.Equals("Down"))
                        {
                            if (idx_select < (infoWindow1.selected.Count - 1))
                            {
                                infoWindow1.path_windown = "";
                                infoWindow1.selected[idx_select] = false;
                                infoWindow1.selected[idx_select + 1] = true;
                                resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);

                                flag_draw = true;
                            }
                        }
                        else if ((sender as Button).Name.Equals("Delete"))
                        {
                            if (infoWindow1.selected[idx_select])
                            {
                                infoWindow1.list.RemoveAt(idx_select);
                                infoWindow1.selected.RemoveAt(idx_select);

                                infoWindow1.path_windown = "";
                                if (infoWindow1.selected.Count > 0)
                                {
                                    idx_select = idx_select - 1;
                                    if (idx_select < 0)
                                        idx_select = 0;

                                    infoWindow1.selected[idx_select] = true;
                                }

                                resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);

                                flag_draw = true;
                            }
                        }
                    }
                }

                if (flag_draw)
                {
                    flag_draw = false;

                    // Draw windown list
                    draw_list_windown(controlsList);
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

    public class Info_Program
    {
        public string Name { get; set; }
        public string width_resolution { get; set; }
        public string height_resolution { get; set; }
        public string width_real { get; set; }
        public string height_real { get; set; }
        public string bittrate_select { get; set; }
    }

    public class Info_Window
    {
        public string Name { get; set; }
        public string path_windown { get; set; }
        public int windown_height { get; set; }
        public int windown_width { get; set; }
        public int windown_top { get; set; }
        public int windown_left { get; set; }
        public List<string> list { get; set; }
        public List<bool> selected { get; set; }
    }

    public class Info_stored
    {
        public Info_Program info_program { get; set; }
        public List<Info_Window> info_windown { get; set; }
    }

    public class program_info_android
    {
        public string name { get; set; }
        public string date { get; set; }
        public string edit { get; set; }
        public string max_width { get; set; }
        public string max_height { get; set; }
        public string bitrate { get; set; }
        public int showArea_height { get; set; }
        public int showArea_width { get; set; }
        public bool adaptive_size { get; set; }
    }

}
