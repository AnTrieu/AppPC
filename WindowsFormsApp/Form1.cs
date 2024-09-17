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
using MediaInfo.DotNetWrapper.Enumerations;
using System.Diagnostics;
using FFmpeg.AutoGen;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using Accord.Video.FFMPEG;
using System.Runtime.InteropServices.ComTypes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Windows.Media.Media3D;
using System.IO.Pipes;
using System.Timers;
using static System.Windows.Forms.AxHost;
using System.ComponentModel;
using System.Reflection;

namespace WindowsFormsApp
{    
    public partial class Form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        private String screen = "terminal_button";
        private Thread udpListenerThread = null;
        private Boolean flagTermianlUDPThread = false;
        private String outputPath = "";
        private FormWindowState windowStateK1 = FormWindowState.Normal;
        private bool force_edit = false;
        private float currentScale = 1.0f;
        private long current_time_box = 0;
        private long running_time_box = 0;
        private string device_select = "";
        private string file_select = "";
        private ContextMenuStrip contextMenu = null;
        private int countProgram = 0;
        private int currentIdxList = 0;
        private System.Threading.Timer timerSystem;
        private List<Control>[] controlsListSelect = new List<Control>[100];        

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd,
                         int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Đảm bảo dừng timer khi form đóng để tránh chạy không cần thiết
            timerSystem?.Dispose();
        }

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
            if (this.WindowState == FormWindowState.Minimized)
            {
                return;
            }

            // Show all
            this.main_program.Visible = true;
            this.main_release.Visible = true;
            this.main_terminal.Visible = true;

            this.panel7.Width = (this.Width - 548) / 2;
            this.panel8.Width = (this.Width - 548) / 2;
            this.panel37.Width = (this.panel36.Width - 500) / 2;
            this.panel38.Width = (this.panel36.Width - 500) / 2;

            if (this.WindowState == FormWindowState.Maximized)
            {
                this.panel6.Width = 450;
                this.panel14.Width = 300;
                this.panel40.Width = 300;

                this.show_file.Height = 250;

                this.panel77.Width = 1600;
            }
            else
            {
                this.panel6.Width = 350;
                this.panel14.Width = 200;
                this.panel40.Width = 260;

                this.show_file.Height = 250;

                this.panel77.Width = 600;
            }

            this.General.Top = this.panel77.Height / 2 - 30;
            this.General.Left = this.panel71.Width - 35;

            this.Advanced.Top = this.panel77.Height / 2 - 30;
            this.Advanced.Left = this.panel71.Width + this.panel78.Width - 40;

            this.panel90.Height = (this.panel77.Height - this.panel82.Height) / 3;
            this.panel76.Height = (this.panel77.Height - this.panel82.Height) / 3;
            this.panel48.Height = (this.panel77.Height - this.panel82.Height) / 3;

            // Refresh design area
            refresh_program_design(currentScale);

            // Select screen
            this.main_program.Visible = false;
            this.main_release.Visible = false;
            this.main_terminal.Visible = false;
            if (screen.Equals("terminal_button"))
            {
                this.main_terminal.Visible = true;
            }
            else if (screen.Equals("release_button"))
            {
                this.main_release.Visible = true;
            }
            else if (screen.Equals("program_button"))
            {
                this.main_program.Visible = true;
            }

            windowStateK1 = this.WindowState;
        }


        protected override void WndProc(ref Message m)
        {
            const int RESIZE_HANDLE_SIZE = 10;

            switch (m.Msg)
            {
                case 0x0021: // Move down

                    // Remove text box
                    foreach (Control control1 in this.main_terminal.Controls)
                    {
                        if (control1 is TextBox && control1.Name.Equals("edit"))
                        {
                            control1.Dispose();
                        }
                    }

                    break;
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
                        if (tableLayoutPanel != null)
                        {
                            Info_device infoDevice = JsonConvert.DeserializeObject<Info_device>(tableLayoutPanel.Name);
                            if (infoDevice.deviceName.Equals(label.Name))
                                tableLayoutPanel.BackColor = System.Drawing.Color.SteelBlue;
                        }
                    }
                }
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

                        if (tableLayoutPanel != null)
                        {
                            Info_device infoDevice = JsonConvert.DeserializeObject<Info_device>(tableLayoutPanel.Name);
                            if (infoDevice.deviceName.Equals(label.Name) && !infoDevice.selected)
                                tableLayoutPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                        }
                    }
                }
            }
        }

        private void row_device_MouseClick(object sender, EventArgs e)
        {
            // Unselect all
            foreach (Control control in this.panel35.Controls)
            {
                if (control is Panel panel_chill)
                {
                    // Now, check if there is a TableLayoutPanel within panel_chill
                    TableLayoutPanel tableLayoutPanel = panel_chill.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                    if (tableLayoutPanel != null)
                    {
                        Info_device infoDevice = JsonConvert.DeserializeObject<Info_device>(tableLayoutPanel.Name);
                        infoDevice.selected = false;
                        tableLayoutPanel.Name = JsonConvert.SerializeObject(infoDevice);

                        if (sender is Label)
                        {
                            Label label = (Label)sender;
                            if (!infoDevice.deviceName.Equals(label.Name))
                            {
                                tableLayoutPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                            }
                        }
                    }
                }
            }

            if (sender is Label)
            {
                Label label = (Label)sender;

                foreach (Control control in this.panel35.Controls)
                {
                    if (control is Panel panel_chill)
                    {
                        // Now, check if there is a TableLayoutPanel within panel_chill
                        TableLayoutPanel tableLayoutPanel = panel_chill.Controls.OfType<TableLayoutPanel>().FirstOrDefault();

                        if (tableLayoutPanel != null)
                        {
                            Info_device infoDevice = JsonConvert.DeserializeObject<Info_device>(tableLayoutPanel.Name);
                            if (infoDevice.deviceName.Equals(label.Name))
                            {
                                infoDevice.selected = true;
                                tableLayoutPanel.Name = JsonConvert.SerializeObject(infoDevice);

                                if (!device_select.Equals(infoDevice.deviceName))
                                {
                                    device_select = infoDevice.deviceName;

                                    // Clear data
                                    infoDevice.session_id = "";
                                    infoDevice.password = "";
                                    tableLayoutPanel.Name = JsonConvert.SerializeObject(infoDevice);

                                    this.screenshort_label.Visible = true;
                                    if (this.panel12.Controls.Count > 1)
                                    {
                                        this.panel12.Controls.RemoveAt(1);
                                    }

                                    // Get password
                                    var cmd_for_device = new
                                    {
                                        deviceName = infoDevice.deviceName,
                                        type = "SOCKET",
                                        command = "GET_INFO",
                                        ip_address = infoDevice.ip_address
                                    };

                                    // Start a new thread for the dialog with parameters
                                    Thread dialogThread = new Thread(new ParameterizedThreadStart(communicationThread));
                                    dialogThread.Start(JsonConvert.SerializeObject(cmd_for_device));
                                }
                                else if ((infoDevice.session_id != null) && (infoDevice.session_id.Length > 0))
                                {
                                    HttpWebRequest request_logo = (HttpWebRequest)WebRequest.Create($"http://{infoDevice.ip_address}:18080/logo");
                                    request_logo.Method = "POST";
                                    request_logo.Headers.Add("Cookie", $"{infoDevice.session_id}");

                                    try
                                    {
                                        // Set a timeout value in milliseconds (e.g., 10 seconds)
                                        request_logo.Timeout = 5000;

                                        using (HttpWebResponse response_logo = (HttpWebResponse)request_logo.GetResponse())
                                        {
                                            if (response_logo.StatusCode == HttpStatusCode.OK)
                                            {
                                                using (Stream responseStream = response_logo.GetResponseStream())
                                                {
                                                    // Use Invoke to update the UI control from the UI thread
                                                    this.panel12.Invoke((MethodInvoker)delegate
                                                    {
                                                        this.screenshort_label.Visible = false;

                                                        if (this.panel12.Controls.Count > 1)
                                                        {
                                                            PictureBox screen_shot = this.panel12.Controls[1] as PictureBox;
                                                            screen_shot.Image = new Bitmap(responseStream);
                                                        }
                                                    });
                                                }
                                            }
                                        }
                                    }
                                    catch (WebException ex)
                                    {
                                        // Handle timeout or other WebException here
                                        if (ex.Status == WebExceptionStatus.Timeout)
                                        {
                                            // Handle timeout
                                            Console.WriteLine("Request timed out.");
                                        }
                                        else
                                        {
                                            // Handle other WebException
                                            Console.WriteLine($"WebException: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void communicationThread(object parameter)
        {
            Command_device cmd_packet = JsonConvert.DeserializeObject<Command_device>((string)parameter);

            if (cmd_packet.type.Equals("SOCKET"))
            {
                byte[] data = new byte[10240 + 256];

                // Get info from device
                TcpClient client = new TcpClient();
                NetworkStream stream = null;
                try
                {
                    client.Connect(cmd_packet.ip_address, 12345);

                    // Convert the JSON string to bytes
                    byte[] byteArray = Encoding.UTF8.GetBytes((string)parameter);
                    Array.Copy(byteArray, data, byteArray.Length);

                    // Get the network stream from the TcpClient
                    stream = client.GetStream();

                    // Send the data
                    stream.Write(data, 0, data.Length);
                    stream.Flush();

                    for (int delay = 0; delay < 30; delay++)
                    {
                        Thread.Sleep(100);

                        if (stream.DataAvailable)
                        {
                            break;
                        }
                    }

                    if (stream.DataAvailable)
                    {
                        Array.Clear(data, 0, data.Length);
                        int read_bytes = stream.Read(data, 0, data.Length);

                        Command_response_device response_device = JsonConvert.DeserializeObject<Command_response_device>(Encoding.UTF8.GetString(data));

                        // Login
                        HttpWebRequest request_login = (HttpWebRequest)WebRequest.Create($"http://{cmd_packet.ip_address}:18080/login");
                        request_login.Method = "POST";

                        // Add parameters to the request body
                        string postData = $"password={response_device.password}";
                        request_login.ContentType = "application/x-www-form-urlencoded";
                        request_login.ContentLength = Encoding.UTF8.GetBytes(postData).Length;

                        using (Stream dataStream = request_login.GetRequestStream())
                        {
                            dataStream.Write(Encoding.UTF8.GetBytes(postData), 0, Encoding.UTF8.GetBytes(postData).Length);
                        }

                        using (HttpWebResponse response_login = (HttpWebResponse)request_login.GetResponse())
                        {
                            if (response_login.StatusCode == HttpStatusCode.OK)
                            {
                                string session_id = response_login.Headers["Set-Cookie"];

                                // Get current time
                                HttpWebRequest request_cur_time = (HttpWebRequest)WebRequest.Create($"http://{cmd_packet.ip_address}:18080/getCurTime");
                                request_cur_time.Method = "POST";
                                request_cur_time.Headers.Add("Cookie", $"{session_id}");
                                using (HttpWebResponse response_signnal_input = (HttpWebResponse)request_cur_time.GetResponse())
                                {
                                    if (response_signnal_input.StatusCode == HttpStatusCode.OK)
                                    {
                                        using (Stream responseStream = response_signnal_input.GetResponseStream())
                                        {
                                            using (StreamReader reader = new StreamReader(responseStream))
                                            {
                                                getCurTime data_parse = JsonConvert.DeserializeObject<getCurTime>(reader.ReadToEnd());
                                                current_time_box = data_parse.curTimeMills;
                                                //Console.WriteLine(reader.ReadToEnd());
                                            }
                                        }
                                    }
                                }

                                // Get screen status
                                HttpWebRequest request_screen = (HttpWebRequest)WebRequest.Create($"http://{cmd_packet.ip_address}:18080/getScreenParams");
                                request_screen.Method = "POST";
                                request_screen.Headers.Add("Cookie", $"{session_id}");
                                using (HttpWebResponse response = (HttpWebResponse)request_screen.GetResponse())
                                {
                                    if (response.StatusCode == HttpStatusCode.OK)
                                    {
                                        using (Stream responseStream = response.GetResponseStream())
                                        {
                                            using (StreamReader reader = new StreamReader(responseStream))
                                            {
                                                this.screen_status.Invoke((MethodInvoker)delegate
                                                {
                                                    getScreenParams data_parse = JsonConvert.DeserializeObject<getScreenParams>(reader.ReadToEnd());
                                                    if (data_parse.screenOn)
                                                    {
                                                        this.screen_status.Text = $"Open";
                                                        this.screen_status.ForeColor = Color.Green;
                                                    }
                                                    else
                                                    {
                                                        this.screen_status.Text = $"Close";
                                                        this.screen_status.ForeColor = Color.Red;
                                                    }

                                                    this.screen_status.Left = 276;
                                                });

                                            }
                                        }
                                    }
                                }

                                // Get screenshot
                                if (!this.screen_status.Text.Equals("Close"))
                                {
                                    HttpWebRequest request_logo = (HttpWebRequest)WebRequest.Create($"http://{cmd_packet.ip_address}:18080/logo");
                                    request_logo.Method = "POST";
                                    request_logo.Headers.Add("Cookie", $"{session_id}");

                                    using (HttpWebResponse response_logo = (HttpWebResponse)request_logo.GetResponse())
                                    {
                                        if (response_logo.StatusCode == HttpStatusCode.OK)
                                        {
                                            using (Stream responseStream = response_logo.GetResponseStream())
                                            {
                                                // Use Invoke to update the UI control from the UI thread
                                                this.panel12.Invoke((MethodInvoker)delegate
                                                {
                                                    this.screenshort_label.Visible = false;
                                                    PictureBox screen_shot = null;
                                                    Bitmap bm = new Bitmap(responseStream);

                                                    if (this.panel12.Controls.Count == 1)
                                                    {
                                                        screen_shot = new PictureBox();
                                                        screen_shot.Image = bm;
                                                        screen_shot.Dock = DockStyle.Fill;
                                                        screen_shot.SizeMode = PictureBoxSizeMode.StretchImage;

                                                        // Add the PictureBox to the panel's Controls collection
                                                        this.panel12.Controls.Add(screen_shot);
                                                    }
                                                    else
                                                    {
                                                        screen_shot = this.panel12.Controls[1] as PictureBox;
                                                        screen_shot.Image = bm;
                                                    }
                                                });
                                            }
                                        }
                                    }
                                }

                                // Save password and session ID
                                foreach (Control control in this.panel35.Controls)
                                {
                                    if (control is Panel panel_chill)
                                    {
                                        // Now, check if there is a TableLayoutPanel within panel_chill
                                        TableLayoutPanel tableLayoutPanel = panel_chill.Controls.OfType<TableLayoutPanel>().FirstOrDefault();

                                        if (tableLayoutPanel != null)
                                        {
                                            Info_device infoDevice = JsonConvert.DeserializeObject<Info_device>(tableLayoutPanel.Name);
                                            if (infoDevice.ip_address.Equals(cmd_packet.ip_address))
                                            {
                                                infoDevice.password = response_device.password;
                                                infoDevice.session_id = session_id;
                                                tableLayoutPanel.Name = JsonConvert.SerializeObject(infoDevice);
                                            }
                                        }
                                    }
                                }

                                this.resolution_device.Invoke((MethodInvoker)delegate
                                {
                                    this.resolution_device.Text = $"{response_device.width} x {response_device.height}";
                                });

                                this.name_device.Invoke((MethodInvoker)delegate
                                {
                                    this.name_device.Text = response_device.UUID;
                                    this.name_device.Left = 70;
                                });

                                this.ip_device.Invoke((MethodInvoker)delegate
                                {
                                    this.ip_device.Text = cmd_packet.ip_address;
                                    this.ip_device.Left = 246;
                                });
                            }
                        }
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    if (stream != null)
                        stream.Close();
                    if (client != null)
                        client.Close();
                    data = null;
                }
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
            // Clear select file
            file_select = null;

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
            if (file_select == null)
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
        }

        private void row_file_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Control parentControl = (sender as Control).Parent;
                if (parentControl is TableLayoutPanel tableLayoutPanel)
                {
                    Control controlInFiveColumn = tableLayoutPanel.GetControlFromPosition(5, 0);

                    // Kiểm tra xem controlInFirstColumn có phải là PictureBox không
                    if (controlInFiveColumn is Label label)
                    {
                        String pathFile = label.Text;

                        if (File.Exists(pathFile))
                        {
                            file_select = pathFile;
                        }
                    }
                }

                // Hiển thị context menu tại vị trí của con trỏ chuột
                contextMenu.Show(this, this.PointToClient(Cursor.Position));
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

        private void NumericTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow only numeric input (0-9) and control keys like Backspace
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; // Mark the event as handled, preventing the character from being entered
            }
            else
            {
                force_edit = true;
            }
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (!force_edit)
                return;

            force_edit = false;

            if (sender is TextBox objInput)
            {
                // Resize child panels in program list (panel43)
                var visiblePanels = this.panel43.Controls
                    .OfType<Panel>()
                    .Where(panel => panel.Visible);

                foreach (var panel_chill in visiblePanels)
                {
                    var info_program = JsonConvert.DeserializeObject<Info_Program>(panel_chill.Name);

                    // Left
                    int X = this.textBox1.Text.Length > 0 ? int.Parse(this.textBox1.Text) : 0;

                    // Top
                    int Y = this.textBox2.Text.Length > 0 ? int.Parse(this.textBox2.Text) : 0;

                    // width
                    int width = this.textBox4.Text.Length > 0 ? int.Parse(this.textBox4.Text) : 0;

                    // height
                    int height = this.textBox3.Text.Length > 0 ? int.Parse(this.textBox3.Text) : 0;

                    foreach (Control control in controlsListSelect[currentIdxList])
                    {
                        if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                        {
                            Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                            if (infoWindow.Name.Equals(this.panel70.Name))
                            {
                                int left_expect     = (int)Math.Round(Normalize(X, 0, int.Parse(info_program.width_real), 0, info_program.width_area));
                                int top_expect      = (int)Math.Round(Normalize(Y, 0, int.Parse(info_program.height_real), 0, info_program.height_area));
                                int width_expect    = (int)Math.Round(Normalize(width, 0, int.Parse(info_program.width_real), 0, info_program.width_area));
                                int height_expect   = (int)Math.Round(Normalize(height, 0, int.Parse(info_program.height_real), 0, info_program.height_area));

                                if ((left_expect + width_expect) > info_program.width_area)
                                {
                                    if (objInput.Name.Equals("textBox1"))
                                        X = int.Parse(info_program.width_real) - int.Parse(this.textBox4.Text) + 0;

                                    if (objInput.Name.Equals("textBox4"))
                                        width = int.Parse(info_program.width_real) - int.Parse(this.textBox1.Text) + 0;

                                    left_expect = (int)Math.Round(Normalize(X, 0, int.Parse(info_program.width_real), 0, info_program.width_area));
                                    width_expect = (int)Math.Round(Normalize(width, 0, int.Parse(info_program.width_real), 0, info_program.width_area));
                                }
                                if (objInput.Name.Equals("textBox1"))
                                    resizablePanel.Left = left_expect;
                                if (objInput.Name.Equals("textBox4"))
                                    resizablePanel.Width = width_expect;

                                if ((top_expect + height_expect) > info_program.height_area)
                                {
                                    if (objInput.Name.Equals("textBox2"))
                                        Y = int.Parse(info_program.height_real) - int.Parse(this.textBox3.Text) + 0;

                                    if (objInput.Name.Equals("textBox3"))
                                        height = int.Parse(info_program.height_real) - int.Parse(this.textBox2.Text) + 0;

                                    top_expect = (int)Math.Round(Normalize(Y, 0, int.Parse(info_program.height_real), 0, info_program.height_area));
                                    height_expect = (int)Math.Round(Normalize(height, 0, int.Parse(info_program.height_real), 0, info_program.height_area));
                                }
                                if (objInput.Name.Equals("textBox3"))
                                    resizablePanel.Height = height_expect;
                                if (objInput.Name.Equals("textBox2"))
                                    resizablePanel.Top = top_expect;

                                // update detail location
                                if (this.textBox4.Text.Length > 0)
                                    infoWindow.windown_width = int.Parse(this.textBox4.Text);
                                else
                                    infoWindow.windown_width = 0;

                                if (this.textBox3.Text.Length > 0)
                                    infoWindow.windown_height = int.Parse(this.textBox3.Text);
                                else
                                    infoWindow.windown_height = 0;

                                if (this.textBox2.Text.Length > 0)
                                    infoWindow.windown_top = int.Parse(this.textBox2.Text);
                                else
                                    infoWindow.windown_top = 0;

                                if (this.textBox1.Text.Length > 0)
                                    infoWindow.windown_left = int.Parse(this.textBox1.Text);
                                else
                                    infoWindow.windown_left = 0;

                                resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                            }
                        }
                    }

                    this.textBox1.Text = X.ToString();
                    this.textBox2.Text = Y.ToString();
                    this.textBox4.Text = width.ToString();
                    this.textBox3.Text = height.ToString();
                }
            }
        }

        private void DeleteItem_Click(object sender, EventArgs e)
        {
            bool flagRemove = true;

            try
            {
                if ((file_select != null) && File.Exists(file_select))
                {
                    List<string> linesToKeep = new List<string>();

                    // Đọc thông tin từ tệp tin khi ứng dụng được khởi động
                    using (StreamReader sr = new StreamReader("material.data"))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (!line.Equals(file_select))
                            {
                                linesToKeep.Add(line);
                            }
                        }
                    }

                    // Ghi lại các dòng còn lại vào tệp "material.data"
                    using (StreamWriter sw = new StreamWriter("material.data"))
                    {
                        foreach (string line in linesToKeep)
                        {
                            sw.WriteLine(line);
                        }
                    }
                }
                else if (file_select == null)
                {
                    flagRemove = false;
                }
            }
            catch (Exception ex)
            {
                flagRemove = false;
                Console.WriteLine($"{ex}");
            }

            if (flagRemove)
            {
                foreach (Control control in this.panel46.Controls)
                {
                    if (control is Panel panel_chill)
                    {
                        // Now, check if there is a TableLayoutPanel within panel_chill
                        TableLayoutPanel tableLayoutPanel = panel_chill.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                        if (tableLayoutPanel != null)
                        {
                            // Get the control in the first cell of the first column (assuming it's a Label)
                            Control controlInFiveColumn = tableLayoutPanel.GetControlFromPosition(5, 0);
                            if (controlInFiveColumn != null && controlInFiveColumn is Label path_label_in_list)
                            {
                                // File have added
                                if (path_label_in_list.Text.Length > 0 && path_label_in_list.Text.Equals(file_select))
                                {
                                    panel_chill.Controls.Remove(tableLayoutPanel);
                                    tableLayoutPanel.Dispose();

                                    int totalHeight = 0;
                                    foreach (Control ctrl in panel_chill.Controls)
                                    {
                                        totalHeight += ctrl.Height + ctrl.Margin.Vertical;
                                    }

                                    // Adjust panel height and scrollbars if needed
                                    panel_chill.Height = totalHeight;

                                    return;
                                }
                            }
                        }
                    }
                }
            }

        }

        public Form1()
        {
            InitializeComponent();

            // Đăng ký sự kiện FormClosing
            this.FormClosing += MainForm_FormClosing;

            // create new object
            controlsListSelect[currentIdxList] = new List<Control>();

            // List Menu
            contextMenu = new ContextMenuStrip();
            ToolStripMenuItem openItem = new ToolStripMenuItem("Delete Item");
            openItem.Click += new EventHandler(DeleteItem_Click);
            contextMenu.Items.AddRange(new ToolStripItem[] { openItem });
            contextMenu.Closed += (sender1, e1) =>
            {
                //file_select = null;

                // Unselect all item
                foreach (Control control in this.panel46.Controls)
                {
                    if (control is Panel panel_chill)
                    {
                        // Now, check if there is a TableLayoutPanel within panel_chill
                        TableLayoutPanel tableLayoutPanel = panel_chill.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                        if (tableLayoutPanel != null)
                        {
                            tableLayoutPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                        }
                    }
                }
            };
            this.panel46.MouseWheel += (sender1, e1) =>
            {
                if ((contextMenu != null) && contextMenu.Visible)
                {
                    contextMenu.Close();
                }
            };

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
            this.new_loop_button.Paint += new PaintEventHandler(DashedBorderButton_Paint);
            this.new_timming_button.Paint += new PaintEventHandler(DashedBorderButton_Paint);
            this.new_command_button.Paint += new PaintEventHandler(DashedBorderButton_Paint);
            this.new_resource.Paint += new PaintEventHandler(DashedBorderButton_Paint);

            this.textBox1.KeyPress += NumericTextBox_KeyPress;
            this.textBox1.TextChanged += TextBox_TextChanged;
            this.textBox2.KeyPress += NumericTextBox_KeyPress;
            this.textBox2.TextChanged += TextBox_TextChanged;
            this.textBox3.KeyPress += NumericTextBox_KeyPress;
            this.textBox3.TextChanged += TextBox_TextChanged;
            this.textBox4.KeyPress += NumericTextBox_KeyPress;
            this.textBox4.TextChanged += TextBox_TextChanged;

            this.entrytime_select.KeyPress += (sender1, e1) =>
            {
                // Kiểm tra nếu ký tự không phải là số hoặc không phải là ký tự điều khiển (ví dụ: backspace)
                if (!char.IsControl(e1.KeyChar) && !char.IsDigit(e1.KeyChar))
                {
                    // Không cho phép nhập ký tự không phải số
                    e1.Handled = true;
                }
            };
            this.entrytime_select.TextChanged += (sender1, e1) =>
            {
                TextBox obj = sender1 as TextBox;
                obj.Text = obj.Text.Length > 0 ? int.Parse(obj.Text).ToString() : "0";
                obj.Select(obj.Text.Length, 0);

                foreach (Control control in controlsListSelect[currentIdxList])
                {
                    if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                    {
                        // Deserialize JSON data from the Name property
                        Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                        foreach (bool selected in infoWindow.selected)
                        {
                            int index = infoWindow.selected.IndexOf(selected);
                            if (selected)
                            {
                                // Update value
                                infoWindow.list_entrytime[index] = obj.Text;
                                resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                            }
                        }
                    }
                }
            };

            this.duration_select.KeyPress += (sender1, e1) =>
            {
                // Kiểm tra nếu ký tự không phải là số hoặc không phải là ký tự điều khiển (ví dụ: backspace)
                if (!char.IsControl(e1.KeyChar) && !char.IsDigit(e1.KeyChar))
                {
                    // Không cho phép nhập ký tự không phải số
                    e1.Handled = true;
                }
            };
            this.duration_select.TextChanged += (sender1, e1) =>
            {
                TextBox obj = sender1 as TextBox;
                obj.Text = obj.Text.Length > 0 ? int.Parse(obj.Text).ToString() : "0";
                obj.Select(obj.Text.Length, 0);

                foreach (Control control in controlsListSelect[currentIdxList])
                {
                    if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                    {
                        // Deserialize JSON data from the Name property
                        Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                        foreach (bool selected in infoWindow.selected)
                        {
                            int index = infoWindow.selected.IndexOf(selected);
                            if (selected)
                            {
                                // Update value
                                infoWindow.list_duration[index] = obj.Text;
                                resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                            }
                        }
                    }
                }
            };

            this.url_select.KeyPress += (sender1, e1) =>
            {
                // Do nothing
            };
            this.url_select.TextChanged += (sender1, e1) =>
            {
                TextBox obj = sender1 as TextBox;

                foreach (Control control in controlsListSelect[currentIdxList])
                {
                    if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                    {
                        // Deserialize JSON data from the Name property
                        Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                        foreach (bool selected in infoWindow.selected)
                        {
                            int index = infoWindow.selected.IndexOf(selected);
                            if (selected)
                            {
                                // Update value
                                infoWindow.list_url[index] = obj.Text;
                                resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                            }
                        }
                    }
                }
            };

            this.General.BackgroundImageLayout = ImageLayout.Stretch;
            this.General.BackgroundImage = normal_button();
            this.General.MouseDown += (sender, e) =>
            {
                (sender as Button).BackgroundImage = select_button();
            };
            this.General.MouseUp += (sender, e) =>
            {
                (sender as Button).BackgroundImage = normal_button();

                List<string> program_list = new List<string> {};

                // Search program is selected
                foreach (Control control in this.panel71.Controls)
                {
                    if (control != this.panel75)
                    {
                        foreach (Control child in control.Controls)
                        {
                            if (control.Controls.IndexOf(child) == 2)
                            {
                                foreach (Control item in child.Controls)
                                {
                                    if ((item as RadioButton).Checked)
                                    {
                                        foreach (Control control1 in this.panel6.Controls)
                                        {
                                            if (this.panel6.Controls.IndexOf(control1) == (this.panel71.Controls.IndexOf(control) + 1))
                                            {
                                                program_list.Add(control1.Name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (program_list.Count > 1)
                {
                    // Active message
                    notify_form popup = new notify_form(false);
                    popup.set_message("Select an excessive number of programs.");
                    popup.ShowDialog();
                }
                else if (program_list.Count == 1)
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
                                    RadioButton radioObj = (RadioButton)tableLayoutPanel.GetControlFromPosition(0, 0);
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
                        // Active message
                        notify_form popup = new notify_form(false);
                        popup.set_message("Please select the device to upload");
                        popup.ShowDialog();
                    }
                    else
                    {
                        var detailPacket = new
                        {
                            IP_client    = IP_client,
                            type         = 0,
                            program_list = program_list
                        };

                        process_form popup = new process_form();
                        popup.Name = JsonConvert.SerializeObject(detailPacket);

                        // Start a new thread for the dialog with parameters
                        Thread dialogThread = new Thread(new ParameterizedThreadStart(SendFileThread));
                        dialogThread.Start(popup);

                        // Show the dialog asynchronously without blocking the UI thread
                        popup.ShowDialog();

                    }
                }
                else
                {
                    // Active message
                    notify_form popup = new notify_form(false);
                    popup.set_message("Please select the program to upload");
                    popup.ShowDialog();
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
            this.Advanced.MouseDown += (sender, e) =>
            {
                (sender as Button).BackgroundImage = select_button();
            };
            this.Advanced.MouseUp += (sender, e) =>
            {
                (sender as Button).BackgroundImage = normal_button();

                String IP_client = "";
                int type = -1;

                foreach (Control control in this.panel84.Controls)
                {
                    if (control is Panel panel)
                    {
                        foreach (Control innerControl in panel.Controls)
                        {
                            if (innerControl is TableLayoutPanel tableLayoutPanel)
                            {
                                RadioButton radioObj = (RadioButton)tableLayoutPanel.GetControlFromPosition(0, 0);
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
                    // Active message
                    notify_form popup = new notify_form(false);
                    popup.set_message("Please select the device to upload");
                    popup.ShowDialog();
                }
                else
                {
                    List<string> program_list = new List<string> { };

                    // Check type
                    if (this.panel96.Controls.Count > 0)
                    {
                        type = 1;

                        foreach (Control control in this.panel96.Controls)
                        {
                            if (control.Controls.Count == 5)
                            {
                                foreach (Control control1 in this.panel6.Controls)
                                {
                                    if ((control != this.panel34) && (control != this.panel33) && (control1.Controls.Count == 3) && (control1.Controls[0] is Panel))
                                    {
                                        if (control1.Controls[0].Controls[1].Text == control.Controls[2].Controls[0].Text)
                                        {
                                            program_list.Add(control1.Name);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }                        
                    else if (this.panel97.Controls.Count > 0)
                        type = 2;
                    else if (this.panel98.Controls.Count > 0)
                        type = 3;

                    if (program_list.Count == 0)
                    {
                        // Active message
                        notify_form popup = new notify_form(false);
                        popup.set_message("Please select the program to upload");
                        popup.ShowDialog();
                    }
                    else if (type > 0)
                    {
                        var detailPacket = new
                        {
                            IP_client = IP_client,
                            type = type,
                            program_list = program_list
                        };

                        process_form popup = new process_form();
                        popup.Name = JsonConvert.SerializeObject(detailPacket);

                        // Start a new thread for the dialog with parameters
                        Thread dialogThread = new Thread(new ParameterizedThreadStart(SendFileThread));
                        dialogThread.Start(popup);

                        // Show the dialog asynchronously without blocking the UI thread
                        popup.ShowDialog();
                    }
                }
            };
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

            // Đăng ký sự kiện DragDrop và DragEnter cho Panel
            this.main_program.AllowDrop = true;
            this.main_program.DragOver += Target_DragOver;

            this.panel43.MouseClick += (sender, e) =>
            {
                unselect_object();
            };

            this.panel74.Paint += (sender, e) =>
            {
                Graphics g = e.Graphics;

                // Thiết lập các màu sắc cho gradient
                Color topColor = Color.FromArgb(64, 64, 64);
                Color bottomColor = Color.FromArgb(54, 54, 54);

                // Tạo LinearGradientBrush để vẽ gradient
                using (LinearGradientBrush brush = new LinearGradientBrush(this.panel74.ClientRectangle, topColor, bottomColor, LinearGradientMode.Vertical))
                {
                    // Vẽ nền với gradient
                    g.FillRectangle(brush, this.panel74.ClientRectangle);
                }
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
            }

            // Create the "Log" directory if it doesn't exist
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log"));
            }
            else
            {
                // Get all log files in the directory
                string[] logFiles = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log"), "*.txt")
                                             .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                                             .ToArray();

                // Keep only the latest 10 log files
                int filesToKeep = 10;
                if (logFiles.Length > filesToKeep)
                {
                    for (int i = filesToKeep; i < logFiles.Length; i++)
                    {
                        File.Delete(logFiles[i]);
                    }
                }
            }
            outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", $"{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}.txt");


            // Tạo một luồng riêng cho việc lắng nghe UDP
            udpListenerThread = new Thread(() => UdpListener(45454));
            udpListenerThread.Start();

            // Create a timer with a 1000ms (1 second) interval
            timerSystem = new System.Threading.Timer((state) =>
            {
                // Kiểm tra xem form đã có handle chưa trước khi gọi Invoke
                if (this.IsHandleCreated)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        if (current_time_box > 0)
                        {
                            DateTimeOffset dateTimeOffset = DateTimeOffset.FromFileTime(current_time_box * 10000);

                            this.current_time.Text = dateTimeOffset.ToString($"dd/MM/{DateTime.Now.Year} HH:mm:ss");
                            current_time_box += 1000;
                        }
                        else
                        {
                            this.current_time.Text = "--/--/-- --:--:--";
                        }

                        // Screenshot
                        var visiblePanels = this.panel43.Controls
                                            .OfType<Panel>()
                                            .Where(panel => panel.Visible);

                        foreach (var destinationPanel in visiblePanels)
                        {
                            // Program tab
                            foreach (Control control in this.panel6.Controls)
                            {
                                if ((control != this.panel34) && (control != this.panel33) && (control.BackColor == System.Drawing.Color.SteelBlue))
                                {
                                    foreach (Control child in control.Controls)
                                    {
                                        if (control.Controls.IndexOf(child) == 1)
                                        {
                                            foreach (Control item in child.Controls)
                                            {
                                                var info_program = JsonConvert.DeserializeObject<Info_Program>(destinationPanel.Name);

                                                // Create a bitmap with the same size as the panel
                                                Bitmap bitmap = new Bitmap(destinationPanel.Width, destinationPanel.Height);
                                                destinationPanel.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

                                                if (((info_program.x_area + info_program.width_area) <= bitmap.Width) && ((info_program.y_area + info_program.height_area) <= bitmap.Height))
                                                {
                                                    // Tạo một Rectangle đại diện cho vùng cần cắt
                                                    Rectangle cropArea = new Rectangle(info_program.x_area, info_program.y_area, info_program.width_area, info_program.height_area);

                                                    // Tạo một bitmap tạm thời để lưu phần cắt
                                                    using (Bitmap croppedBitmap = bitmap.Clone(cropArea, bitmap.PixelFormat))
                                                    {
                                                        // Giải phóng bitmap cũ nếu không cần sử dụng nữa
                                                        bitmap.Dispose();

                                                        // Gán bitmap mới đã được cắt
                                                        bitmap = (Bitmap)croppedBitmap.Clone();
                                                    }
                                                }

                                                // Show capture picture
                                                PictureBox showPictureBox = item as PictureBox;
                                                if (showPictureBox.Image != null)
                                                {
                                                    showPictureBox.Image.Dispose();
                                                    showPictureBox.Image = null;
                                                }

                                                showPictureBox.Image = bitmap;

                                                // Release tab
                                                foreach (Control control1 in this.panel71.Controls)
                                                {
                                                    if ((control1 != this.panel75) && (this.panel71.Controls.IndexOf(control1) == (this.panel6.Controls.IndexOf(control) - 1)))
                                                    {
                                                        foreach (Control child1 in control1.Controls)
                                                        {
                                                            if (control1.Controls.IndexOf(child1) == 1)
                                                            {
                                                                foreach (Control item1 in child1.Controls)
                                                                {
                                                                    PictureBox showPictureBox1 = item1 as PictureBox;
                                                                    if (showPictureBox1.Image != null)
                                                                    {
                                                                        showPictureBox1.Image.Dispose();
                                                                        showPictureBox1.Image = null;
                                                                    }

                                                                    showPictureBox1.Image = bitmap;
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
            }, null, 0, 1000);

            // Icon feature
            this.Webpage.MouseDown += PictureBox_MouseDown;
            this.Text.MouseDown += PictureBox_MouseDown;
        }


        static bool HasAudioStream(string filePath)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = $"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg.exe",
                Arguments = $"-i \"{filePath}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // Check if the output contains an audio stream
                return output.Contains("Audio:");
            }
        }

        private Info_Window removeObjEmpty(Info_Window infoWindow)
        {
            // Check and remove empty obj
            while (true)
            {
                int idx_remove = -1;
                foreach (string obj in infoWindow.list)
                {
                    if ((idx_remove < 0) && (obj.Length == 0))
                    {
                        idx_remove = infoWindow.list.IndexOf(obj);
                        infoWindow.list.Remove(obj);
                        break;
                    }
                }
                foreach (string obj in infoWindow.list_duration)
                {
                    if ((idx_remove >= 0) && (infoWindow.list_duration.IndexOf(obj) == idx_remove))
                    {
                        infoWindow.list_duration.Remove(obj);
                        break;
                    }
                }
                foreach (string obj in infoWindow.list_entrytime)
                {
                    if ((idx_remove >= 0) && (infoWindow.list_entrytime.IndexOf(obj) == idx_remove))
                    {
                        infoWindow.list_entrytime.Remove(obj);
                        break;
                    }
                }
                foreach (bool obj in infoWindow.selected)
                {
                    if ((idx_remove >= 0) && (infoWindow.selected.IndexOf(obj) == idx_remove))
                    {
                        infoWindow.selected.Remove(obj);
                        break;
                    }
                }

                if (idx_remove < 0)
                    break;
            }

            return infoWindow;
        }

        private void SendFileThread(object parameter)
        {
            //using (StreamWriter fileStream = new StreamWriter(outputPath))
            {
                //Console.SetOut(fileStream);

                long total_size = 0;
                bool flag_cancel = false;

                process_form dialog = (process_form)parameter;
                var sendDetailInfo = JsonConvert.DeserializeObject<sendDetailInfo>((string)dialog.Name);
                string IP_client = sendDetailInfo.IP_client;

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
                    // Connect device
                    clientSocket = new TcpClient();
                    clientSocket.Connect(IP_client, 12345);

                    // Get the network stream for receiving data
                    networkStream = clientSocket.GetStream();

                    int cntProgramSended = 0;
                    foreach (string program in sendDetailInfo.program_list)
                    {
                        var info_program = JsonConvert.DeserializeObject<Info_Program>(program);

                        // set lbel process bar
                        dialog.Invoke((MethodInvoker)delegate
                        {
                            // Your UI update code
                            dialog.label5.Text = "Processing... \"" + info_program.Name + "\"";
                            dialog.label5.Location = new System.Drawing.Point((dialog.panel1.Width - dialog.label5.PreferredWidth) / 2, (dialog.panel1.Height - dialog.label5.PreferredHeight) / 2);
                            dialog.progressBar1.Refresh();
                        });

                        // Find list windown
                        List<Control> controlsListSelectTemp = null;
                        foreach (Control control in this.panel6.Controls)
                        {
                            if ((control != this.panel34) && (control != this.panel33))
                            {
                                // get info show area
                                var info_program1 = JsonConvert.DeserializeObject<Info_Program>(control.Name);
                                
                                if (info_program1.Name == info_program.Name)
                                {
                                    foreach (Control chill in control.Controls)
                                    {
                                        if (control.Controls.IndexOf(chill) == 2)
                                        {
                                            foreach (Control item in chill.Controls)
                                            {
                                                if (int.TryParse(item.Text, out int index) && index > 0 &&  controlsListSelect[index] != null)
                                                {
                                                    controlsListSelectTemp = controlsListSelect[index];
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }

                                // Found it !!!
                                if (controlsListSelectTemp != null)
                                    break;
                            }
                        }

                        // List windown empty
                        if (controlsListSelectTemp == null)
                            continue;

                        var flag_convert = false;
                        long longestDuration = 0;

                        // Create folder output
                        String outputBackgroundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", $"{info_program.Name}_{info_program.bittrate_select}");
                        string backgroundFilePath = Path.Combine(outputBackgroundPath, $"Background_{info_program.Name}.mp4");
                        string devideFilePath = Path.Combine(outputBackgroundPath, $"Divide_{info_program.Name}.mp4");
                        string contentFilePath = Path.Combine(outputBackgroundPath, $"{info_program.Name}.mp4");

                        // Convert video
                        if (true && ((controlsListSelectTemp.Count > 1) || (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))))
                        {
                            int windown_left_expected = 0;
                            int percentage = 0, percentageK1 = 0;
                            int counter_windown_empty = 0;
                            List<long> listDuration = new List<long>();

                            if (!Directory.Exists(outputBackgroundPath))
                            {
                                Directory.CreateDirectory(outputBackgroundPath);
                            }
                            else
                            {
                                if (File.Exists(backgroundFilePath) && File.Exists(contentFilePath))
                                {
                                    File.Delete(backgroundFilePath);
                                    File.Delete(contentFilePath);
                                }
                                else if (File.Exists(backgroundFilePath))
                                {
                                    File.Delete(backgroundFilePath);
                                }
                                else if (File.Exists(contentFilePath))
                                {
                                    File.Delete(contentFilePath);
                                }
                            }

                            // Step 1: get all path video in program list
                            foreach (Control control in controlsListSelectTemp)
                            {
                                if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                {
                                    // Deserialize JSON data from the Name property
                                    Info_Window infoWindow = removeObjEmpty(JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name));
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
                                                    // Do nothing
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
                                            longestDurationWindown += (int.Parse(infoWindow.list_entrytime[idx]) * 1000 + int.Parse(infoWindow.list_duration[idx]) * 1000);
                                        }
                                    }

                                    if (longestDuration < longestDurationWindown)
                                    {
                                        longestDuration = longestDurationWindown;
                                    }

                                    listDuration.Add(longestDurationWindown);
                                }
                            }

                            Console.WriteLine($"-> longestDuration: {longestDuration}");

                            // Step 2: convert video
                            for (int i = controlsListSelectTemp.Count - 1; i >= 0; i--)
                            {
                                Control control = controlsListSelectTemp[i];
                                if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                {
                                    //Console.WriteLine(resizablePanel.Name);
                                    Info_Window infoWindow = removeObjEmpty(JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name));
                                    int idx_windown = controlsListSelectTemp.Count - i - 1;

                                    if (idx_windown == 0)
                                    {
                                        using (Process process = new Process())
                                        {
                                            process.StartInfo.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg.exe"; // Assuming "ffmpeg" is in the PATH
                                            process.StartInfo.Arguments = $"-y -f lavfi -i anullsrc=r=44100:cl=stereo -f lavfi -i color=c=black:s={info_program.width_real}x{info_program.height_real}:d=0.1 -t 0.1 -shortest -c:v libx264 -b:v {info_program.bittrate_select} -tune stillimage -c:a aac -b:a 192k -strict experimental \"{backgroundFilePath}\"";
                                            process.StartInfo.UseShellExecute = false;
                                            process.StartInfo.RedirectStandardOutput = true;
                                            process.StartInfo.RedirectStandardError = true;
                                            process.StartInfo.CreateNoWindow = true;

                                            process.OutputDataReceived += (sender, e) =>
                                            {
                                                // Do nothing
                                            };
                                            process.ErrorDataReceived += (sender, e) =>
                                            {
                                                // Do nothing
                                            };
                                            process.Start();
                                            process.BeginOutputReadLine();
                                            process.BeginErrorReadLine();
                                            process.WaitForExit();
                                        }

                                        if (!File.Exists(backgroundFilePath))
                                        {
                                            return;
                                        }
                                    }

                                    using (Process process = new Process())
                                    {
                                        // Init variable
                                        String cmd_ffmpeg = "-y ";
                                        String filter = "";
                                        String overlay = "";
                                        int width_check = (infoWindow.windown_width % 2) == 1 ? infoWindow.windown_width + 1 : infoWindow.windown_width;
                                        int height_check = (infoWindow.windown_height % 2) == 1 ? infoWindow.windown_height + 1 : infoWindow.windown_height;

                                        process.StartInfo.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg.exe"; // Assuming "ffmpeg" is in the PATH

                                        for (int idx = 0; idx < infoWindow.list.Count; idx++)
                                        {
                                            string extension = System.IO.Path.GetExtension(infoWindow.list[idx]).ToLower();


                                            if (idx == 0)
                                            {
                                                cmd_ffmpeg += ($"-i \"{backgroundFilePath}\" ");
                                            }

                                            // Is a video
                                            if (extension == ".mp4" || extension == ".avi" ||
                                                extension == ".wmv" || extension == ".mpg" ||
                                                extension == ".rmvp" || extension == ".mov" ||
                                                extension == ".dat" || extension == ".flv")
                                            {
                                                cmd_ffmpeg += ($"-i \"{infoWindow.list[idx]}\" ");
                                            }
                                            else
                                            {
                                                cmd_ffmpeg += ($"-framerate 25 -t {infoWindow.list_duration[idx]} -loop 1 -i \"{infoWindow.list[idx]}\" ");
                                            }
                                        }

                                        cmd_ffmpeg += "-f lavfi -t 0.1 -i anullsrc -filter_complex ";

                                        for (int idx = 0; idx < infoWindow.list.Count; idx++)
                                        {
                                            filter += ("[" + (idx + 1) + ":v]scale=" + width_check + ":" + height_check + ":flags=lanczos,setsar=1:1[vid" + (idx + 1) + "];");
                                            string extension = System.IO.Path.GetExtension(infoWindow.list[idx]).ToLower();
                                            if (extension == ".jpg" || extension == ".bmp" || extension == ".png" || extension == ".gif")
                                            {
                                                filter += ("color=c=black:s=" + width_check + "x" + height_check + ":d=" + infoWindow.list_entrytime[idx] + "[entry_time" + (idx + 1) + "];");
                                            }
                                        }

                                        int counterImage = 0;

                                        for (int idx = 0; idx < infoWindow.list.Count; idx++)
                                        {
                                            string extension = System.IO.Path.GetExtension(infoWindow.list[idx]).ToLower();
                                            if (extension == ".jpg" || extension == ".bmp" || extension == ".png" || extension == ".gif")
                                            {
                                                filter += ("[entry_time" + (idx + 1) + "]" + "[" + (infoWindow.list.Count + 1) + ":a]");
                                                filter += ("[vid" + (idx + 1) + "]" + "[" + (infoWindow.list.Count + 1) + ":a]");

                                                counterImage += 1;
                                            }
                                            else
                                            {
                                                if (HasAudioStream(infoWindow.list[idx]))
                                                {
                                                    filter += ("[vid" + (idx + 1) + "][" + (idx + 1) + ":a]");
                                                }
                                                else
                                                {
                                                    filter += ("[vid" + (idx + 1) + "][" + (infoWindow.list.Count + 1) + ":a]");
                                                }
                                            }
                                        }
                                        filter += ("concat=n=" + (infoWindow.list.Count + counterImage) + ":v=1:a=1:unsafe=1[windown" + (idx_windown + 1) + "];");

                                        // Calculate "loop" for windown
                                        int loop = 0;
                                        if (longestDuration > listDuration[idx_windown])
                                        {
                                            loop = (int)((longestDuration / listDuration[idx_windown]) - 1);
                                        }
                                        if ((longestDuration % listDuration[idx_windown]) > 0)
                                        {
                                            loop++;
                                        }

                                        filter += ("[windown" + (idx_windown + 1) + "]" + "loop=" + loop + ":32767:0[looped_windown" + (idx_windown + 1) + "_timebase];");

                                        // Add overlay video
                                        if (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))
                                        {
                                            if (windown_left_expected >= infoWindow.windown_left)
                                            {
                                                overlay += ("[0][looped_windown" + (idx_windown + 1) + "_timebase]overlay=" + infoWindow.windown_left + ":" + infoWindow.windown_top + "[output]");
                                                windown_left_expected = infoWindow.windown_width + infoWindow.windown_left;
                                            }
                                            else
                                            {
                                                overlay += ("[0][looped_windown" + (idx_windown + 1) + "_timebase]overlay=" + windown_left_expected + ":" + infoWindow.windown_top + "[output]");
                                                windown_left_expected += infoWindow.windown_width;
                                            }
                                        }
                                        else
                                        {
                                            overlay += ("[0][looped_windown" + (idx_windown + 1) + "_timebase]overlay=" + infoWindow.windown_left + ":" + infoWindow.windown_top + "[output]");
                                        }

                                        cmd_ffmpeg += filter + overlay + " ";
                                        cmd_ffmpeg += $"-map [output] -c:v libx264 -b:v {info_program.bittrate_select} -preset slow -tune film -t {(longestDuration / 1000) + 1} \"{contentFilePath}\"";
                                        //Console.WriteLine(cmd_ffmpeg);
                                        process.StartInfo.Arguments = cmd_ffmpeg;
                                        process.StartInfo.UseShellExecute = false;
                                        process.StartInfo.RedirectStandardOutput = true;
                                        process.StartInfo.RedirectStandardError = true;
                                        process.StartInfo.CreateNoWindow = true;

                                        process.OutputDataReceived += (sender, e) =>
                                        {
                                            // Do nothing
                                        };
                                        process.ErrorDataReceived += (sender, e) =>
                                        {
                                            //Console.WriteLine(e.Data);
                                            if (flag_cancel)
                                            {
                                                process.CancelErrorRead();
                                                process.Kill();
                                            }
                                            else
                                            {
                                                //Console.WriteLine(e.Data);
                                                if (!string.IsNullOrEmpty(e.Data))
                                                {
                                                    // Variables for capturing duration
                                                    string timeProcessPattern = @"time=([0-9:.]+)";
                                                    Regex regex = new Regex(timeProcessPattern);

                                                    // Search for duration pattern in the output
                                                    Match match = regex.Match(e.Data);

                                                    if (match.Success)
                                                    {
                                                        // Extract the matched duration
                                                        string time_str = match.Groups[1].Value;

                                                        double milliseconds = TimeSpan.Parse(time_str).TotalMilliseconds;

                                                        if ((int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution)) || (int.Parse(info_program.height_real) > int.Parse(info_program.width_real)))
                                                            percentage = percentageK1 + (int)((milliseconds * 100) / ((double)longestDuration * (controlsListSelectTemp.Count - counter_windown_empty) * 2));
                                                        else
                                                            percentage = percentageK1 + (int)((milliseconds * 100) / ((double)longestDuration * (controlsListSelectTemp.Count - counter_windown_empty)));

                                                        // set process bar
                                                        dialog.Invoke((MethodInvoker)delegate
                                                        {
                                                            // Your UI update code
                                                            dialog.ProgressValue = percentage + 500;
                                                            dialog.progressBar1.Refresh();
                                                        });

                                                    }
                                                }
                                            }
                                        };
                                        process.Start();
                                        process.BeginOutputReadLine();
                                        process.BeginErrorReadLine();
                                        process.WaitForExit();

                                        if ((int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution)) || (int.Parse(info_program.height_real) > int.Parse(info_program.width_real)))
                                            percentageK1 += (int)((longestDuration * 100) / ((double)longestDuration * (controlsListSelectTemp.Count - counter_windown_empty) * 2));
                                        else
                                            percentageK1 += (int)((longestDuration * 100) / ((double)longestDuration * (controlsListSelectTemp.Count - counter_windown_empty)));


                                        if (((idx_windown + 1) >= controlsListSelectTemp.Count) && File.Exists(backgroundFilePath))
                                        {
                                            File.Delete(backgroundFilePath);
                                        }
                                        if (File.Exists(backgroundFilePath) && File.Exists(contentFilePath))
                                        {
                                            File.Delete(backgroundFilePath);
                                            File.Move(contentFilePath, backgroundFilePath);
                                        }
                                    }
                                }
                            }

                            if (true && File.Exists(contentFilePath) && ((int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution)) || (int.Parse(info_program.height_real) > int.Parse(info_program.height_resolution))))
                            {
                                // Create background
                                using (Process process = new Process())
                                {
                                    process.StartInfo.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg.exe";
                                    if (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))
                                        process.StartInfo.Arguments = $"-y -f lavfi -i color=c=black:s={int.Parse(info_program.width_resolution)}x{int.Parse(info_program.height_real) + 2}:d=0.1 -vf scale={int.Parse(info_program.width_resolution)}x{int.Parse(info_program.height_real) + 2} -t 0.1 \"{backgroundFilePath}\"";
                                    else
                                        process.StartInfo.Arguments = $"-y -f lavfi -i color=c=black:s={int.Parse(info_program.width_real) + 2}x{int.Parse(info_program.height_resolution)}:d=0.1 -vf scale={int.Parse(info_program.width_real) + 2}x{int.Parse(info_program.height_resolution)} -t 0.1 \"{backgroundFilePath}\"";
                                    process.StartInfo.UseShellExecute = false;
                                    process.StartInfo.RedirectStandardOutput = true;
                                    process.StartInfo.RedirectStandardError = true;
                                    process.StartInfo.CreateNoWindow = true;

                                    process.OutputDataReceived += (sender, e) =>
                                    {
                                        // Do nothing
                                    };
                                    process.ErrorDataReceived += (sender, e) =>
                                    {
                                        // Do nothing
                                    };
                                    process.Start();
                                    process.BeginOutputReadLine();
                                    process.BeginErrorReadLine();
                                    process.WaitForExit();
                                }

                                if (!File.Exists(backgroundFilePath))
                                {
                                    return;
                                }

                                // slip file
                                String filter = "";
                                int idx_area = 1;
                                using (Process process = new Process())
                                {
                                    process.StartInfo.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg.exe";
                                    process.StartInfo.Arguments = $"-y -i \"{contentFilePath}\" -i \"{backgroundFilePath}\" -filter_complex ";
                                    if (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))
                                    {
                                        for (int step = 0; step < int.Parse(info_program.width_real); step += int.Parse(info_program.width_resolution))
                                        {
                                            if ((step + int.Parse(info_program.width_resolution)) > int.Parse(info_program.width_real))
                                            {
                                                filter += ($"[0:v]crop={int.Parse(info_program.width_real) - step}:{int.Parse(info_program.height_real)}:{step}:0[raw_area{idx_area}];[1:v][raw_area{idx_area}]overlay=0:0[area{idx_area}];");
                                            }
                                            else
                                            {
                                                filter += ($"[0:v]crop={int.Parse(info_program.width_resolution)}:{int.Parse(info_program.height_real)}:{step}:0[raw_area{idx_area}];[1:v][raw_area{idx_area}]overlay=0:0[area{idx_area}];");
                                            }
                                            idx_area++;
                                        }

                                        for (int step = 0; step < (idx_area - 1); step++)
                                        {
                                            filter += $"[area{step + 1}]";
                                        }

                                        filter += ($"vstack=inputs={idx_area - 1}[mux];[mux][0:a]concat=n=1:v=1:a=1:unsafe=1[out]");
                                    }
                                    else
                                    {
                                        // TODO
                                    }
                                    //Console.WriteLine(filter);
                                    process.StartInfo.Arguments += $"{filter} -map [out] -c:v libx264 -b:v {info_program.bittrate_select} -preset slow -tune film \"{devideFilePath}\"";
                                    process.StartInfo.UseShellExecute = false;
                                    process.StartInfo.RedirectStandardOutput = true;
                                    process.StartInfo.RedirectStandardError = true;
                                    process.StartInfo.CreateNoWindow = true;

                                    process.OutputDataReceived += (sender, e) =>
                                    {
                                        // Do nothing
                                    };
                                    process.ErrorDataReceived += (sender, e) =>
                                    {
                                        if (flag_cancel)
                                        {
                                            process.CancelErrorRead();
                                            process.Kill();
                                        }
                                        else
                                        {
                                            //Console.WriteLine(e.Data);
                                            if (!string.IsNullOrEmpty(e.Data))
                                            {
                                                // Variables for capturing duration
                                                string timeProcessPattern = @"time=([0-9:.]+)";
                                                Regex regex = new Regex(timeProcessPattern);

                                                // Search for duration pattern in the output
                                                Match match = regex.Match(e.Data);

                                                if (match.Success)
                                                {
                                                    // Extract the matched duration
                                                    string time_str = match.Groups[1].Value;

                                                    double milliseconds = TimeSpan.Parse(time_str).TotalMilliseconds;

                                                    percentage = percentageK1 + (int)((milliseconds * 100) / (2 * ((double)longestDuration * (controlsListSelectTemp.Count - counter_windown_empty))));

                                                    // set process bar
                                                    dialog.Invoke((MethodInvoker)delegate
                                                    {
                                                        // Your UI update code
                                                        dialog.ProgressValue = percentage + 300;
                                                        dialog.progressBar1.Refresh();
                                                    });

                                                }
                                            }
                                        }
                                    };
                                    process.Start();
                                    process.BeginOutputReadLine();
                                    process.BeginErrorReadLine();
                                    process.WaitForExit();

                                    // Delete root video
                                    if (File.Exists(contentFilePath))
                                    {
                                        File.Delete(contentFilePath);
                                    }

                                    // Delete background video
                                    if (File.Exists(backgroundFilePath))
                                    {
                                        File.Delete(backgroundFilePath);
                                    }

                                    // Rename divide video
                                    if (File.Exists(devideFilePath))
                                    {
                                        File.Move(devideFilePath, contentFilePath);
                                    }
                                }
                            }

                            flag_convert = true;
                            Console.WriteLine("Convert finish");
                        }

                        bool flagForceSucceed = false;

                        // Carculator total size                          
                        if (!flag_convert && controlsListSelectTemp.Count > 0 && controlsListSelectTemp[0] is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                        {
                            // Deserialize JSON data from the Name property
                            Info_Window infoWindow = removeObjEmpty(JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name));

                            for (int idx = 0; idx < infoWindow.list.Count; idx++)
                            {
                                FileInfo fileInfo = new FileInfo(infoWindow.list[idx]);
                                total_size += fileInfo.Length;
                            }

                        }
                        else if (flag_convert)
                        {
                            if (File.Exists(contentFilePath))
                            {
                                FileInfo fileInfo = new FileInfo(contentFilePath);
                                total_size += fileInfo.Length;
                            }
                        }
                        // Empty windown in program
                        else if (controlsListSelectTemp.Count == 0)
                        {
                            flagForceSucceed = true;
                        }

                        if (flagForceSucceed)
                        {
                            ManualResetEvent resetEvent = new ManualResetEvent(false);

                            // set process bar
                            dialog.Invoke((MethodInvoker)delegate
                            {
                                try
                                {
                                    // Your UI update code
                                    dialog.ProgressValue = 1;
                                    dialog.progressBar1.Refresh();
                                }
                                finally
                                {
                                    // Signal that the UI update is completed
                                    resetEvent.Set();
                                }
                            });

                            // Block until the UI update is completed
                            resetEvent.WaitOne();
                            resetEvent = null;
                        }
                        else if (total_size == 0)
                        {
                            // Active message
                            notify_form popup = new notify_form(false);
                            popup.set_message("File error, please try again");
                            popup.ShowDialog();
                        }
                        else
                        {
                            Boolean send_program = false;
                            long sended_size = 0;
                            int percent = 0, percentK1 = -1;

                            // Send file                        
                            if (!flag_convert && controlsListSelectTemp[0] is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                            {
                                // Deserialize JSON data from the Name property
                                Info_Window infoWindow = removeObjEmpty(JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name));

                                for (int idx = 0; idx < infoWindow.list.Count; idx++)
                                {
                                    using (FileStream receivedVideoFile = new FileStream(infoWindow.list[idx], FileMode.Open, FileAccess.Read))
                                    {
                                        int bytesRead = 0;
                                        int idxChuck = 0;

                                        long length_file = receivedVideoFile.Length;

                                        // Active send plan
                                        send_program = true;

                                        // Receive video data in chunks
                                        while ((bytesRead = receivedVideoFile.Read(buffer, 256, buffer.Length - 256)) > 0)
                                        {
                                            if (flag_cancel)
                                            {
                                                var detailPacket = new
                                                {
                                                    command = "SEND_CANCEL",
                                                    plan = ""
                                                };

                                                byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(detailPacket));
                                                Array.Copy(jsonBytes, buffer, jsonBytes.Length);

                                                networkStream.Write(buffer, 0, buffer.Length);
                                                networkStream.Flush();

                                                // Clean (reset) the buffer
                                                Array.Clear(buffer, 0, buffer.Length);
                                                Array.Clear(responseBuffer, 0, responseBuffer.Length);

                                                // Release memory
                                                jsonBytes = null;
                                                detailPacket = null;

                                                break;
                                            }
                                            else
                                            {
                                                var detailPacket = new
                                                {
                                                    command = "SEND_FILE",
                                                    chuck = idxChuck++,
                                                    path = infoWindow.list[idx],
                                                    sended = bytesRead,
                                                    length = length_file,
                                                    type = ""
                                                };

                                                byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(detailPacket));
                                                Array.Copy(jsonBytes, buffer, Math.Min(jsonBytes.Length, 256));
                                                // Console.WriteLine("-------------- " + Math.Max(bytesRead + 256, buffer.Length));
                                                networkStream.Write(buffer, 0, Math.Max(bytesRead + 256, buffer.Length));
                                                networkStream.Flush();

                                                sended_size += bytesRead;
                                                percent = ((int)Math.Round((double)sended_size * 100 / (double)total_size, 0));
                                                if (percentK1 != percent)
                                                {
                                                    ManualResetEvent resetEvent = new ManualResetEvent(false);

                                                    // set process bar
                                                    dialog.Invoke((MethodInvoker)delegate
                                                    {
                                                        try
                                                        {
                                                            // Your UI update code
                                                            dialog.ProgressValue = percent;
                                                            dialog.progressBar1.Refresh();
                                                        }
                                                        finally
                                                        {
                                                            // Signal that the UI update is completed
                                                            resetEvent.Set();
                                                        }
                                                    });

                                                    // Block until the UI update is completed
                                                    resetEvent.WaitOne();
                                                    resetEvent = null;

                                                    percentK1 = percent;
                                                }

                                                // Wait for the response in first time
                                                if (idxChuck == 1)
                                                {
                                                    int bytesReadResponse = networkStream.Read(responseBuffer, 0, responseBuffer.Length);
                                                    string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesReadResponse);
                                                    Console.WriteLine("Server Response: " + response);

                                                    if (response.Equals("Exist file"))
                                                    {
                                                        sended_size = sended_size - bytesRead + length_file;
                                                        break;
                                                    }
                                                }

                                                // Release memory
                                                jsonBytes = null;
                                                detailPacket = null;

                                                // Clean (reset) the buffer
                                                Array.Clear(buffer, 0, buffer.Length);
                                                Array.Clear(responseBuffer, 0, responseBuffer.Length);
                                            }
                                        }
                                    }
                                }
                            }
                            else if (flag_convert)
                            {
                                // Active send program
                                send_program = true;

                                using (FileStream receivedVideoFile = new FileStream(contentFilePath, FileMode.Open, FileAccess.Read))
                                {
                                    int bytesRead = 0;
                                    int idxChuck = 0;

                                    long length_file = receivedVideoFile.Length;

                                    // Receive video data in chunks
                                    while ((bytesRead = receivedVideoFile.Read(buffer, 256, buffer.Length - 256)) > 0)
                                    {
                                        if (flag_cancel)
                                        {
                                            var detailPacket = new
                                            {
                                                command = "SEND_CANCEL",
                                                plan = ""
                                            };

                                            byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(detailPacket));
                                            Array.Copy(jsonBytes, buffer, jsonBytes.Length);

                                            networkStream.Write(buffer, 0, buffer.Length);
                                            networkStream.Flush();

                                            // Clean (reset) the buffer
                                            Array.Clear(buffer, 0, buffer.Length);
                                            Array.Clear(responseBuffer, 0, responseBuffer.Length);

                                            // Release memory
                                            jsonBytes = null;
                                            detailPacket = null;

                                            break;
                                        }
                                        else
                                        {
                                            var detailPacket = new
                                            {
                                                command = "SEND_FILE",
                                                chuck = idxChuck++,
                                                path = contentFilePath,
                                                sended = bytesRead,
                                                length = length_file,
                                                type = "Convert"
                                            };

                                            byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(detailPacket));
                                            Array.Copy(jsonBytes, buffer, Math.Min(jsonBytes.Length, 256));

                                            networkStream.Write(buffer, 0, Math.Max(bytesRead + 256, buffer.Length));
                                            networkStream.Flush();

                                            sended_size += bytesRead;
                                            percent = ((int)Math.Round((double)sended_size * 100 / (double)total_size, 0));
                                            if (percentK1 != percent)
                                            {
                                                ManualResetEvent resetEvent = new ManualResetEvent(false);

                                                // set process bar
                                                dialog.Invoke((MethodInvoker)delegate
                                                {
                                                    try
                                                    {
                                                        // Your UI update code
                                                        dialog.ProgressValue = percent;
                                                        dialog.progressBar1.Refresh();
                                                    }
                                                    finally
                                                    {
                                                        // Signal that the UI update is completed
                                                        resetEvent.Set();
                                                    }
                                                });

                                                // Block until the UI update is completed
                                                resetEvent.WaitOne();
                                                resetEvent = null;

                                                percentK1 = percent;
                                            }

                                            // Wait for the response in first time
                                            if (idxChuck == 1)
                                            {
                                                int bytesReadResponse = networkStream.Read(responseBuffer, 0, responseBuffer.Length);
                                                string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesReadResponse);
                                                Console.WriteLine("Server Response: " + response);

                                                if (response.Equals("Exist file"))
                                                {
                                                    sended_size = sended_size - bytesRead + length_file;
                                                    break;
                                                }
                                            }

                                            // Release memory
                                            jsonBytes = null;
                                            detailPacket = null;

                                            // Clean (reset) the buffer
                                            Array.Clear(buffer, 0, buffer.Length);
                                            Array.Clear(responseBuffer, 0, responseBuffer.Length);
                                        }
                                    }
                                }
                            }

                            // Send program for device
                            if (send_program)
                            {
                                send_program = false;

                                List<Info_Window> info_windown = new List<Info_Window>();
                                foreach (Control control in controlsListSelectTemp)
                                {
                                    info_windown.Add(removeObjEmpty(JsonConvert.DeserializeObject<Info_Window>((control as ResizablePanel).Name)));
                                }
                                //Console.WriteLine($"---------------------------------------{longestDuration}");

                                var detailPacket = new
                                {
                                    command                 = "SEND_PROGRAM",
                                    durationProgramConvert  = longestDuration,
                                    sync_mode               = false,
                                    info_program            = JsonConvert.DeserializeObject<Info_Program>(program),
                                    info_windown            = info_windown
                                };

                                byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(detailPacket));
                                Array.Copy(jsonBytes, buffer, jsonBytes.Length);
                                networkStream.Write(buffer, 0, buffer.Length);
                                networkStream.Flush();

                                // Clean (reset) the buffer
                                Array.Clear(buffer, 0, buffer.Length);
                                Array.Clear(responseBuffer, 0, responseBuffer.Length);

                                // Release memory
                                jsonBytes = null;
                                detailPacket = null;

                                // Increate counter
                                cntProgramSended++;
                            }
                        }
                    }

                    List<string> program_list = new List<string>();
                    foreach (string program_detail in sendDetailInfo.program_list)
                    {
                        Info_Program infoProgram = JsonConvert.DeserializeObject<Info_Program>(program_detail);
                        program_list.Add(infoProgram.Name);
                    }

                    // Only send submit when all program is sended succeed
                    if ((program_list.Count > 0) && (cntProgramSended == sendDetailInfo.program_list.Count))
                    {
                        List<loop_type> detail_submit = new List<loop_type> {};


                        if (sendDetailInfo.type == 0)
                        {
                            detail_submit.Add(JsonConvert.DeserializeObject<loop_type>(JsonConvert.SerializeObject(new
                            {
                                loop     = "1",
                                timeLoop = ""
                            })));
                        }
                        else if (sendDetailInfo.type == 1)
                        {
                            foreach (string program in program_list)
                            {
                                foreach (Control control in this.panel96.Controls)
                                {
                                    if (control.Controls.Count == 5)
                                    {
                                        if (control.Controls[2].Controls[0].Text == program)
                                        {
                                            if (control.Controls[0].Controls[0].Text.IndexOf(":") >= 0)
                                            {
                                                detail_submit.Add(JsonConvert.DeserializeObject<loop_type>(JsonConvert.SerializeObject(new
                                                {
                                                    loop     = "",
                                                    timeLoop = (int) TimeSpan.Parse(control.Controls[0].Controls[0].Text).TotalMinutes
                                                })));
                                            }
                                            else
                                            {
                                                detail_submit.Add(JsonConvert.DeserializeObject<loop_type>(JsonConvert.SerializeObject(new
                                                {
                                                    loop     = control.Controls[0].Controls[0].Text,
                                                    timeLoop = ""
                                                })));
                                            }                                           
                                            
                                            break;
                                        }
                                    }
                                }
                            }
                                
                        }

                        var detailPacket = new
                        {
                            command       = "SEND_SUBMIT",
                            type          = sendDetailInfo.type,
                            program_list  = program_list,
                            detail_submit = ((sendDetailInfo.type == 0) || (sendDetailInfo.type == 1)) ? detail_submit : (sendDetailInfo.type == 2) ? detail_submit : detail_submit
                        };
                        
                        byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(detailPacket));
                        Array.Copy(jsonBytes, buffer, jsonBytes.Length);
                        // Console.WriteLine(JsonConvert.SerializeObject(detailPacket));
                        networkStream.Write(buffer, 0, buffer.Length);
                        networkStream.Flush();

                        // Clean (reset) the buffer
                        Array.Clear(buffer, 0, buffer.Length);
                        Array.Clear(responseBuffer, 0, responseBuffer.Length);

                        // Release memory
                        jsonBytes = null;
                        detailPacket = null;
                    }

                    dialog.Name = "Send file successfully";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error 1: {ex}");
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

                    Thread.Sleep(1000);

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
        }

        private void unselect_object()
        {
            // Check list item exist
            foreach (Control control in this.panel43.Controls)
            {
                if (control is Panel panel_chill && panel_chill.Visible)
                {
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
            foreach (Control control1 in controlsListSelect[currentIdxList])
            {
                if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                {
                    // Deserialize JSON data from the Name property
                    Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);
                    infoWindow1.selectedMaster = false;

                    for (int i1 = 0; i1 < infoWindow1.selected.Count; i1++)
                    {
                        infoWindow1.selected[i1] = false;
                    }

                    resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                }
            }

            foreach (Control control1 in this.list_windowns.Controls)
            {
                if (control1.Name != null)
                    control1.Refresh();
            }

            this.panel70.Visible = false;
            this.panel70.Name = "";
        }

        public static float Normalize(float x, float minA, float maxA, float minB, float maxB)
        {
            float value = (x - minA) * (maxB - minB) / (maxA - minA) + minB;
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

        private void processSelectProgram()
        {
            int idxSelect = -1;
            String info_program_str = null;

            // Get index program select;
            foreach (Control control in this.panel6.Controls)
            {
                if ((control != this.panel34) && (control != this.panel33) && (control.BackColor == System.Drawing.Color.SteelBlue))
                {
                    // get infor show area
                    info_program_str = control.Name;

                    foreach (Control chill in control.Controls)
                    {
                        if (control.Controls.IndexOf(chill) == 2)
                        {
                            foreach (Control item in chill.Controls)
                            {
                                idxSelect = int.Parse(item.Text);
                                break;
                            }
                        }                        
                    }
                }
            }

            if ((idxSelect > 0) && (info_program_str != null))
            {
                var info_program = JsonConvert.DeserializeObject<Info_Program>(info_program_str);

                if (int.TryParse(info_program.width_real, out int width_real) && int.TryParse(info_program.height_real, out int height_real) && 
                    int.TryParse(info_program.width_resolution, out int width_resolution) && int.TryParse(info_program.height_resolution, out int height_resolution))
                {
                    // Update pointer
                    currentIdxList = idxSelect;

                    // Restart
                    reinit(true);

                    // Get the maximum allowable width and height based on the mainPanel's size
                    int width_contain = this.show.Width;
                    int height_contain = this.show.Height;
                    if ((width_real != width_resolution) || (height_real != height_resolution))
                    {
                        width_contain = this.panel43.Width;
                        height_contain = this.panel43.Height;
                    }
                    float delta = (float)width_real / (float)height_real;
                    float width_config = 0;
                    float height_config = 0;
                    do
                    {
                        height_config += 1;
                        width_config += delta;
                    }
                    while ((width_config < (width_contain - 50)) && (height_config < (height_contain - 50)));

                    // Calculate the position to center the inner panel within the main panel
                    int x = (this.panel43.Width - (int)width_config) / 2;
                    int y = (this.panel43.Height - (int)height_config) / 2;

                    var infoProgram = new
                    {
                        name                = info_program.Name,
                        width_resolution    = info_program.width_resolution,
                        height_resolution   = info_program.height_resolution,
                        width_real          = info_program.width_real,
                        height_real         = info_program.height_real,
                        bittrate_select     = info_program.bittrate_select,
                        x_area              = x,
                        y_area              = y,
                        width_area          = (int) width_config,
                        height_area         = (int) height_config
                    };

                    // Create the inner panel based on the adjusted width and height
                    Panel innerPanel = new Panel
                    {
                        Name        = JsonConvert.SerializeObject(infoProgram),
                        Dock        = DockStyle.Fill,
                        BackColor   = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54))))),
                        AllowDrop   = true
                    };

                    // Đăng ký sự kiện DragDrop và DragEnter cho Panel
                    innerPanel.DragDrop  += TargetPanel_DragDrop;
                    innerPanel.DragEnter += TargetPanel_DragEnter;
                    innerPanel.DragOver  += Target_DragOver;
                    innerPanel.MouseDown += (sender2, e2) =>
                    {
                        unselect_object();
                    };

                    // Xóa tất cả các sự kiện Paint từ innerPanel
                    innerPanel.ClearPaintEventHandlers();
                    innerPanel.Paint += (sender2, e2) =>
                    {
                        // Lấy đối tượng Graphics từ sự kiện Paint
                        Graphics g = e2.Graphics;

                        // Tạo một Brush màu đen
                        using (SolidBrush brush = new SolidBrush(Color.Black))
                        {
                            // Vẽ hình chữ nhật đen giữa panel
                            g.FillRectangle(brush, x, y, width_config, height_config);
                        }
                    };

                    this.show.AutoScrollPosition = new System.Drawing.Point(x - ((this.show.Width - ((int)width_config + 30)) / 2), y - ((this.show.Height - ((int)height_config + 30)) / 2));

                    // Add the inner panel to the main panel
                    this.panel43.Controls.Add(innerPanel);

                    // Load list windown
                    List<Info_Window> info_windown_list = new List<Info_Window>();

                    foreach (Control control1 in controlsListSelect[currentIdxList])
                    {
                        ResizablePanel panel_windown = control1 as ResizablePanel;
                        info_windown_list.Add(JsonConvert.DeserializeObject<Info_Window>(panel_windown.Name));
                    }
                    info_windown_list.Reverse();

                    // Clear list windown after load data
                    controlsListSelect[currentIdxList].Clear();

                    var visiblePanels = this.panel43.Controls
                                    .OfType<Panel>()
                                    .Where(panel => panel.Visible);

                    foreach (var destinationPanel in visiblePanels)
                    {
                        info_program = JsonConvert.DeserializeObject<Info_Program>(destinationPanel.Name);

                        // Draw windows
                        for (int idx_window = 0; idx_window < info_windown_list.Count; idx_window++)
                        {
                            for (int idx_item = 0; idx_item < info_windown_list[idx_window].list.Count; idx_item++)
                            {
                                // Get the object name from the data
                                int lenght_list = idx_window + 1;
                                string objectName = info_windown_list[idx_window].list[idx_item];
                                string[] list_object = { objectName };
                                bool[] list_selected = { false };

                                int max_app_width   = info_program.width_area;
                                int max_app_height  = info_program.height_area;
                                int X               = (int)Math.Round(Normalize(info_windown_list[idx_window].windown_left, 0, int.Parse(info_program.width_real), 0, max_app_width)) + info_program.x_area;
                                int Y               = (int)Math.Round(Normalize(info_windown_list[idx_window].windown_top, 0, int.Parse(info_program.height_real), 0, max_app_height)) + info_program.y_area;
                                int width_windown   = (int)Math.Round(Normalize(info_windown_list[idx_window].windown_width, 0, int.Parse(info_program.width_real), 0, max_app_width));
                                int height_windown  = (int)Math.Round(Normalize(info_windown_list[idx_window].windown_height, 0, int.Parse(info_program.height_real), 0, max_app_height));

                                var info_windown = new
                                {
                                    name            = "Windown " + lenght_list,
                                    path_windown    = "",
                                    windown_height  = info_windown_list[idx_window].windown_height,
                                    windown_width   = info_windown_list[idx_window].windown_width,
                                    windown_top     = info_windown_list[idx_window].windown_top,
                                    windown_left    = info_windown_list[idx_window].windown_left,
                                    list            = list_object,
                                    list_url        = info_windown_list[idx_window].list_url,
                                    list_duration   = info_windown_list[idx_window].list_duration,
                                    list_entrytime  = info_windown_list[idx_window].list_entrytime,
                                    selected        = list_selected
                                };

                                ResizablePanel windown_load = null;
                                if (idx_item == 0)
                                {
                                    windown_load = new ResizablePanel(destinationPanel)
                                    {
                                        Location    = new Point(X, Y),
                                        Size        = new Size(width_windown, height_windown),
                                        BackColor   = Color.Transparent,
                                        Name        = JsonConvert.SerializeObject(info_windown),
                                        AllowDrop   = true
                                    };

                                    windown_load.CustomEventMouseMove += (sender1, e1, X1, Y1, app_width, app_height, active_select, info_other_panel, direction) =>
                                    {
                                        // Resize child panels in program list (panel43)
                                        var visiblePanels1 = this.panel43.Controls
                                            .OfType<Panel>()
                                            .Where(panel => panel.Visible);

                                        foreach (var showPanel in visiblePanels1)
                                        {
                                            // Deserialize JSON data from the Name property
                                            Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(windown_load.Name);

                                            this.panel70.Visible = true;
                                            this.panel70.Name = infoWindow.Name;

                                            this.textBox1.Text = Math.Round(Normalize(X1, 0, info_program.width_area, 0, int.Parse(info_program.width_real))).ToString();
                                            this.textBox2.Text = Math.Round(Normalize(Y1, 0, info_program.height_area, 0, int.Parse(info_program.height_real))).ToString();
                                            if (active_select)
                                            {
                                                this.textBox4.Text = Math.Round(Normalize(app_width, 0, info_program.width_area, 0, int.Parse(info_program.width_real))).ToString();
                                                this.textBox3.Text = Math.Round(Normalize(app_height, 0, info_program.height_area, 0, int.Parse(info_program.height_real))).ToString();
                                            }

                                            if (direction == 1)
                                            {
                                                Info_Window infoOtherWindow = JsonConvert.DeserializeObject<Info_Window>(info_other_panel);
                                                this.textBox1.Text = (infoOtherWindow.windown_left + infoOtherWindow.windown_width).ToString();
                                            }
                                            else if (direction == 3)
                                            {
                                                Info_Window infoOtherWindow = JsonConvert.DeserializeObject<Info_Window>(info_other_panel);
                                                this.textBox2.Text = (infoOtherWindow.windown_top + infoOtherWindow.windown_height).ToString();
                                            }

                                            // Select first item
                                            foreach (Control control1 in controlsListSelect[currentIdxList])
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
                                        }
                                    };

                                    windown_load.CustomEventMouseDown += (sender1, e1, X1, Y1, app_width, app_height, active_select, info_other_panel, direction) =>
                                    {
                                        // Resize child panels in program list (panel43)
                                        var visiblePanels1 = this.panel43.Controls
                                            .OfType<Panel>()
                                            .Where(panel => panel.Visible);

                                        foreach (var showPanel in visiblePanels1)
                                        {
                                            // Deserialize JSON data from the Name property
                                            Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(windown_load.Name);

                                            // Select first item
                                            foreach (Control control1 in controlsListSelect[currentIdxList])
                                            {
                                                if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                                {
                                                    // Deserialize JSON data from the Name property
                                                    Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);


                                                    if (infoWindow1.Name.Equals(infoWindow.Name))
                                                    {
                                                        this.panel70.Visible = true;
                                                        this.panel70.Name = infoWindow.Name;

                                                        this.textBox1.Text = infoWindow1.windown_left.ToString();
                                                        this.textBox2.Text = infoWindow1.windown_top.ToString();
                                                        this.textBox4.Text = infoWindow1.windown_width.ToString();
                                                        this.textBox3.Text = infoWindow1.windown_height.ToString();
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
                                            foreach (Control control1 in controlsListSelect[currentIdxList])
                                            {
                                                if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                                {
                                                    // Deserialize JSON data from the Name property
                                                    Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);
                                                    infoWindow1.selectedMaster = false;

                                                    for (int i = 0; i < infoWindow1.selected.Count; i++)
                                                    {
                                                        infoWindow1.selected[i] = false;
                                                    }

                                                    if (infoWindow1.Name.Equals(infoWindow.Name))
                                                    {
                                                        if (infoWindow1.selected.Count > 0)
                                                        {
                                                            infoWindow1.selected[0] = true;
                                                        }

                                                        // update detai location
                                                        infoWindow1.windown_width = int.Parse(this.textBox4.Text);
                                                        infoWindow1.windown_height = int.Parse(this.textBox3.Text);
                                                        infoWindow1.windown_top = int.Parse(this.textBox2.Text);
                                                        infoWindow1.windown_left = int.Parse(this.textBox1.Text);
                                                    }

                                                    resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                                                }
                                            }

                                            foreach (Control control2 in this.list_windowns.Controls)
                                            {
                                                if (control2.Name != null)
                                                    control2.Refresh();
                                            }

                                            windown_load.InitializeResizeHandles();
                                        }
                                    };

                                    windown_load.CustomEventDragDrop += (sender1, e1) =>
                                    {
                                        // Unselect all
                                        foreach (Control control in controlsListSelect[currentIdxList])
                                        {
                                            if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                            {
                                                // Deserialize JSON data from the Name property
                                                Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);
                                                infoWindow.selectedMaster = false;

                                                for (int i = 0; i < infoWindow.selected.Count; i++)
                                                {
                                                    infoWindow.selected[i] = false;
                                                }

                                                resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);

                                            }
                                        }

                                        // Update controlsListSelect[currentIdxList]
                                        foreach (Control control in controlsListSelect[currentIdxList])
                                        {
                                            if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                            {
                                                // Deserialize JSON data from the Name property
                                                Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                                                if (infoWindow.Name.Equals("Windown " + lenght_list.ToString()))
                                                {
                                                    String name_file = e1.Data.GetData("PictureBoxName") as string;
                                                    string extension1 = System.IO.Path.GetExtension(name_file).ToLower();
                                                    this.name_select.Text = " " + System.IO.Path.GetFileNameWithoutExtension(name_file);

                                                    if (name_file.Equals("Webpage"))
                                                    {
                                                        infoWindow.list_url.Add("toantrungcloud.com");

                                                        infoWindow.list_entrytime.Add("0");
                                                        infoWindow.list_duration.Add("10");
                                                    }
                                                    else if (name_file.Equals("Text"))
                                                    {
                                                        // Data not use
                                                        infoWindow.list_url.Add("");

                                                        infoWindow.list_entrytime.Add("0");
                                                        infoWindow.list_duration.Add("10");
                                                    }
                                                    else
                                                    {
                                                        // Data not use
                                                        infoWindow.list_url.Add("");

                                                        // Is a video
                                                        if (extension1 == ".jpg" || extension1 == ".bmp" || extension1 == ".png" || extension1 == ".gif")
                                                        {
                                                            infoWindow.list_entrytime.Add("0");
                                                            infoWindow.list_duration.Add("10");
                                                        }
                                                        else
                                                        {
                                                            var flag_error = true;
                                                            var mediaInfo = new MediaInfo.DotNetWrapper.MediaInfo();
                                                            mediaInfo.Open(name_file);

                                                            if (double.Parse(mediaInfo.Get(StreamKind.General, 0, "Duration")) > 0)
                                                            {
                                                                double durationMilliseconds = double.Parse(mediaInfo.Get(StreamKind.General, 0, "Duration"));

                                                                flag_error = false;
                                                                infoWindow.list_entrytime.Add("0");
                                                                infoWindow.list_duration.Add(durationMilliseconds.ToString());
                                                            }

                                                            if (flag_error)
                                                            {
                                                                flag_error = false;
                                                                infoWindow.list_entrytime.Add("");
                                                                infoWindow.list_duration.Add("");
                                                            }
                                                        }
                                                    }

                                                    infoWindow.list.Add(name_file);
                                                    infoWindow.selected.Add(true);
                                                    resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                                                }
                                            }
                                        }

                                        // Draw windown list
                                        draw_list_windown(controlsListSelect[currentIdxList]);
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
                                    if (File.Exists(objectName))
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
                                    if (File.Exists(objectName))
                                        windown_load.updateTimer.Interval = 1000 / (int)windown_load.videoFileReader.FrameRate.Value;
                                    windown_load.updateTimer.Tick += (sender1, e1) =>
                                    {
                                        if (InvokeRequired)
                                        {
                                            Invoke(new MethodInvoker(delegate { /* Công việc giao diện người dùng */}));
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
                                    if (File.Exists(objectName))
                                        windown_load.updateTimer.Start();
                                   
                                    controlsListSelect[currentIdxList].Insert(0, windown_load);
                                    destinationPanel.Controls.AddRange(controlsListSelect[currentIdxList].ToArray());
                                }
                                else
                                {
                                    // Update controlsListSelect[currentIdxList]
                                    foreach (Control control in controlsListSelect[currentIdxList])
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
                        draw_list_windown(controlsListSelect[currentIdxList]);
                        unselect_object();
                    }
                }
            }
        }

        private void add_layout_program(Panel parent, String name, int width, int height, String info_program)
        {
            int old = parent.Controls.Count;

            if (parent != this.panel71)
            {
                foreach (Control control in parent.Controls)
                {
                    if ((control != this.panel34) && (control != this.panel33) && (control != this.panel75))
                    {
                        control.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                    }
                }
            }

            Panel index = new Panel();
            if (parent == this.panel71)
            {
                RadioButton select = new RadioButton();
                select.AutoCheck = false;
                select.AutoEllipsis = true;
                select.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
                select.Checked = false;
                select.Dock = System.Windows.Forms.DockStyle.Fill;
                select.FlatAppearance.BorderSize = 0;
                select.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                select.ForeColor = System.Drawing.Color.Transparent;
                select.Location = new System.Drawing.Point(0, 0);
                select.Margin = new System.Windows.Forms.Padding(0);
                select.Size = new System.Drawing.Size(30, 70);
                select.TabIndex = 0;
                select.TabStop = true;
                select.UseVisualStyleBackColor = false;
                select.Click += (sender, e) =>
                {
                    (sender as RadioButton).Checked = !(sender as RadioButton).Checked;
                };
                index.Controls.Add(select);
                index.Dock = System.Windows.Forms.DockStyle.Left;
                index.Location = new System.Drawing.Point(0, 10);
                index.Size = new System.Drawing.Size(30, 70);
                index.TabIndex = 0;
            }
            else
            {
                Label value = new Label();
                value.AutoSize = true;
                value.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                value.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                value.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                value.ForeColor = System.Drawing.Color.White;
                value.Location = new System.Drawing.Point(5, 25);
                value.Size = new System.Drawing.Size(19, 19);
                value.TabIndex = 0;
                value.Text = Convert.ToString(++countProgram);
                value.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

                
                index.Controls.Add(value);
                index.Dock = System.Windows.Forms.DockStyle.Left;
                index.Location = new System.Drawing.Point(0, 10);
                index.Size = new System.Drawing.Size(33, 70);
                index.TabIndex = 0;
            }

            PictureBox pictureBox = new PictureBox();
            pictureBox.BackColor = System.Drawing.Color.Black;
            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            pictureBox.Location = new System.Drawing.Point(0, 0);
            pictureBox.Size = new System.Drawing.Size(70, 70);
            pictureBox.TabIndex = 0;
            pictureBox.TabStop = false;
            pictureBox.MouseClick += (sender, e) =>
            {
                PictureBox box = sender as PictureBox;
                if (box.Parent.Parent.BackColor != System.Drawing.Color.SteelBlue)
                {
                    // Unselect all 
                    foreach (Control control in parent.Controls)
                    {
                        if ((control != this.panel34) && (control != this.panel33) && (control != this.panel75))
                        {
                            control.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                        }
                    }

                    // Select object new
                    box.Parent.Parent.BackColor = System.Drawing.Color.SteelBlue;

                    // Load Program
                    if (parent != this.panel71)
                        processSelectProgram();
                }
            };

            Panel background = new Panel();
            background.Controls.Add(pictureBox);
            background.Dock = System.Windows.Forms.DockStyle.Left;
            background.Location = new System.Drawing.Point(33, 10);
            background.Size = new System.Drawing.Size(70, 70);
            background.TabIndex = 1;

            Label resolution_value = new Label();
            resolution_value.AutoSize = true;
            resolution_value.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            resolution_value.ForeColor = System.Drawing.Color.White;
            resolution_value.Location = new System.Drawing.Point(5, 44);
            resolution_value.Size = new System.Drawing.Size(0, 17);
            resolution_value.TabIndex = 1;
            resolution_value.Text = $"{width}(W) x {height}(H)";
            resolution_value.MouseClick += (sender, e) =>
            {
                Label box = sender as Label;
                if (box.Parent.Parent.BackColor != System.Drawing.Color.SteelBlue)
                {
                    // Unselect all 
                    foreach (Control control in parent.Controls)
                    {
                        if ((control != this.panel34) && (control != this.panel33) && (control != this.panel75))
                        {
                            control.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                        }
                    }

                    // Select object new
                    box.Parent.Parent.BackColor = System.Drawing.Color.SteelBlue;

                    // Load Program
                    if (parent != this.panel71)
                        processSelectProgram();
                }
            };

            Label name_value = new Label();
            name_value.AutoSize = true;
            name_value.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            name_value.ForeColor = System.Drawing.Color.White;
            name_value.Location = new System.Drawing.Point(5, 14);
            name_value.Size = new System.Drawing.Size(0, 17);
            name_value.TabIndex = 0;
            name_value.Text = name;
            name_value.MouseClick += (sender, e) =>
            {
                Label box = sender as Label;
                if (box.Parent.Parent.BackColor != System.Drawing.Color.SteelBlue)
                {
                    // Unselect all 
                    foreach (Control control in parent.Controls)
                    {
                        if ((control != this.panel34) && (control != this.panel33) && (control != this.panel75))
                        {
                            control.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                        }
                    }

                    // Select object new
                    box.Parent.Parent.BackColor = System.Drawing.Color.SteelBlue;

                    // Load Program
                    if (parent != this.panel71)
                        processSelectProgram();
                }
            };

            Panel info = new Panel();
            info.Controls.Add(resolution_value);
            info.Controls.Add(name_value);
            info.Dock = System.Windows.Forms.DockStyle.Fill;
            info.Location = new System.Drawing.Point(103, 10);
            info.Size = new System.Drawing.Size(95, 70);
            info.TabIndex = 2;
            info.MouseClick += (sender, e) =>
            {
                Panel box = sender as Panel;
                if (box.Parent.BackColor != System.Drawing.Color.SteelBlue)
                {
                    // Unselect all 
                    foreach (Control control in parent.Controls)
                    {
                        if ((control != this.panel34) && (control != this.panel33) && (control != this.panel75))
                        {
                            control.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                        }
                    }

                    // Select object new
                    box.Parent.BackColor = System.Drawing.Color.SteelBlue;

                    // Load Program
                    if (parent != this.panel71)
                        processSelectProgram();
                }
            };

            Panel row = new Panel();
            if (parent != this.panel71)
                row.BackColor = System.Drawing.Color.SteelBlue;
            else
                row.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
            row.Dock = System.Windows.Forms.DockStyle.Top;
            row.Location = new System.Drawing.Point(0, 30);
            row.Margin = new System.Windows.Forms.Padding(0);
            row.Padding = new System.Windows.Forms.Padding(0, 10, 0, 10);
            row.Size = new System.Drawing.Size(198, 80);
            row.TabIndex = 2;
            row.Name = info_program;
            row.Controls.Add(info);
            row.Controls.Add(background);
            row.Controls.Add(index);

            parent.Controls.Add(row);
            if (parent == this.panel71)
                parent.Controls.SetChildIndex(row, parent.Controls.Count - old - 1);
            else
                parent.Controls.SetChildIndex(row, parent.Controls.Count - old);

            // Create list windown for program 
            if (parent != this.panel71)
            {
                currentIdxList = countProgram;
                controlsListSelect[currentIdxList] = new List<Control>();
            }
        }

        private void new_program_button_Click(object sender, EventArgs e)
        {
            setting_form popup = new setting_form();

            popup.ConfirmClick += (sender1, e1) =>
            {
                if (int.TryParse(e1.width_real, out int width_real) && int.TryParse(e1.height_real, out int height_real) &&
                    int.TryParse(e1.width_resolution, out int width_resolution) && int.TryParse(e1.height_resolution, out int height_resolution))
                {
                    reinit(true);

                    // Get the maximum allowable width and height based on the mainPanel's size
                    int width_contain = this.show.Width;
                    int height_contain = this.show.Height;
                    if ((width_real != width_resolution) || (height_real != height_resolution))
                    {
                        width_contain = this.panel43.Width;
                        height_contain = this.panel43.Height;
                    }
                    float delta = (float)width_real / (float)height_real;
                    float width_config = 0;
                    float height_config = 0;
                    do
                    {
                        height_config += 1;
                        width_config += delta;
                    }
                    while ((width_config < (width_contain - 50)) && (height_config < (height_contain - 50)));

                    // Calculate the position to center the inner panel within the main panel
                    int x = (this.panel43.Width - (int)width_config) / 2;
                    int y = (this.panel43.Height - (int)height_config) / 2;

                    var infoProgram = new
                    {
                        name                = e1.name,
                        width_resolution    = e1.width_resolution,
                        height_resolution   = e1.height_resolution,
                        width_real          = e1.width_real,
                        height_real         = e1.height_real,
                        bittrate_select     = e1.bittrate_select,
                        x_area              = x,
                        y_area              = y,
                        width_area          = (int) width_config,
                        height_area         = (int) height_config
                    };

                    // Create the inner panel based on the adjusted width and height
                    Panel innerPanel = new Panel
                    {
                        Name                = JsonConvert.SerializeObject(infoProgram),
                        Dock                = DockStyle.Fill,
                        BackColor           = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54))))),
                        AllowDrop           = true
                    };

                    // Đăng ký sự kiện DragDrop và DragEnter cho Panel
                    innerPanel.DragDrop  += TargetPanel_DragDrop;
                    innerPanel.DragEnter += TargetPanel_DragEnter;
                    innerPanel.DragOver  += Target_DragOver;
                    innerPanel.MouseDown += (sender2, e2) =>
                    {
                        unselect_object();
                    };

                    // Xóa tất cả các sự kiện Paint từ innerPanel
                    innerPanel.ClearPaintEventHandlers();
                    innerPanel.Paint += (sender2, e2) =>
                    {
                        // Lấy đối tượng Graphics từ sự kiện Paint
                        Graphics g = e2.Graphics;

                        // Tạo một Brush màu đen
                        using (SolidBrush brush = new SolidBrush(Color.Black))
                        {
                            // Vẽ hình chữ nhật đen giữa panel
                            g.FillRectangle(brush, x, y, width_config, height_config);
                        }
                    };

                    this.show.AutoScrollPosition = new System.Drawing.Point(x - ((this.show.Width - ((int)width_config + 30)) / 2), y - ((this.show.Height - ((int)height_config + 30)) / 2));

                    // Add the inner panel to the main panel
                    this.panel43.Controls.Add(innerPanel);

                    // Create list program
                    add_layout_program(this.panel6, e1.name, width_real, height_real, JsonConvert.SerializeObject(infoProgram));
                    add_layout_program(this.panel71, e1.name, width_real, height_real, null);
                }
            };

            popup.ShowDialog();
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
                Point panelDrop = ((Panel)sender).PointToClient(new Point(e.X, e.Y));
                Panel destinationPanel = sender as Panel;
                var info_program = JsonConvert.DeserializeObject<Info_Program>(destinationPanel.Name);
                int lenght_list = 1;

                // Only Accept in black area
                if ((info_program.x_area <= panelDrop.X) && (panelDrop.X <= (info_program.x_area + info_program.width_area)))
                {
                    if ((info_program.y_area <= panelDrop.Y) && (panelDrop.Y <= (info_program.y_area + info_program.height_area)))
                    {
                        // Unselect all in list video
                        foreach (Control control in controlsListSelect[currentIdxList])
                        {
                            if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                            {
                                // Deserialize JSON data from the Name property
                                Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);
                                infoWindow.selectedMaster = false;

                                if (int.Parse(infoWindow.Name.Substring(8).Trim()) >= lenght_list)
                                    lenght_list = int.Parse(infoWindow.Name.Substring(8).Trim()) + 1;

                                for (int i = 0; i < infoWindow.selected.Count; i++)
                                {
                                    infoWindow.selected[i] = false;
                                }

                                resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                            }
                        }

                        // Get the object name from the data
                        string objectName = e.Data.GetData("PictureBoxName") as string;
                        string extension = System.IO.Path.GetExtension(objectName).ToLower();

                        string[] list_object = {objectName};
                        string[] list_url = { "" };
                        string[] list_duration = { "" };
                        string[] list_entrytime = { "" };
                        bool[] list_selected = { true };
                        bool have_image = false;

                        if (objectName.Equals("Webpage"))
                        {
                            // Type Webpage
                            list_url[0] = "toantrungcloud.com";

                            list_entrytime[0] = "0";
                            list_duration[0] = "10";
                        }
                        else if (objectName.Equals("Text"))
                        {
                            // Data not use
                            list_url[0] = "";

                            // Type Text
                            list_entrytime[0] = "0";
                            list_duration[0] = "10";
                            
                        }
                        else
                        {
                            // Data not use
                            list_url[0] = "";

                            // Is a video
                            if (extension == ".jpg" || extension == ".bmp" || extension == ".png" || extension == ".gif")
                            {
                                list_entrytime[0] = "0";
                                list_duration[0] = "10";
                                have_image = true;
                            }
                            else
                            {
                                var mediaInfo = new MediaInfo.DotNetWrapper.MediaInfo();
                                mediaInfo.Open(objectName);

                                if (double.Parse(mediaInfo.Get(StreamKind.General, 0, "Duration")) > 0)
                                {
                                    double durationMilliseconds = double.Parse(mediaInfo.Get(StreamKind.General, 0, "Duration"));

                                    list_entrytime[0] = "0";
                                    list_duration[0] = durationMilliseconds.ToString();
                                }
                            }
                        }

                        ResizablePanel windown = null;
                        int max_app_width = info_program.width_area;
                        int max_app_height = info_program.height_area;
                        if (controlsListSelect[currentIdxList].Count == 0)
                        {
                            var info_windown = new
                            {
                                name            = "Windown " + lenght_list,
                                path_windown    = "",
                                windown_height  = int.Parse(info_program.height_real),
                                windown_width   = int.Parse(info_program.width_real),
                                windown_top     = 0,
                                windown_left    = 0,
                                list            = list_object,
                                list_url        = list_url,
                                list_duration   = list_duration,
                                list_entrytime  = list_entrytime,
                                selected        = list_selected
                            };

                            windown = new ResizablePanel(destinationPanel)
                            {
                                Location        = new Point(info_program.x_area, info_program.y_area),
                                Size            = new Size(max_app_width, max_app_height),
                                BackColor       = Color.Transparent,
                                Name            = JsonConvert.SerializeObject(info_windown),
                                AllowDrop       = true
                            };

                            this.textBox1.Text  = "0";
                            this.textBox2.Text  = "0";
                            this.textBox4.Text  = Math.Round(Normalize(max_app_width, 0, max_app_width, 0, int.Parse(info_program.width_real))).ToString();
                            this.textBox3.Text  = Math.Round(Normalize(max_app_height, 0, max_app_height, 0, int.Parse(info_program.height_real))).ToString();

                            this.panel70.Visible = true;
                            this.panel70.Name = info_windown.name;
                            this.name_select.Text = " " + System.IO.Path.GetFileNameWithoutExtension(objectName);
                            if (have_image)
                            {
                                this.panel80.Visible = true;
                                this.entrytime_select.Text = list_entrytime[0];
                                this.duration_select.Text = list_duration[0];
                            }
                        }
                        else
                        {
                            int X = (sender as Control).PointToClient(new Point(e.X, e.Y)).X - info_program.x_area;
                            int Y = (sender as Control).PointToClient(new Point(e.X, e.Y)).Y - info_program.y_area;
                            int width_valid = (info_program.width_area - X) >= 100 ? 100 : info_program.width_area - X;
                            int height_valid = (info_program.height_area - Y) >= 50 ? 50 : info_program.height_area - Y;
                            
                            var info_windown = new
                            {
                                name            = "Windown " + lenght_list,
                                path_windown    = "",
                                windown_height  = (int)Math.Round(Normalize(height_valid, 0, max_app_height, 0, int.Parse(info_program.height_real))),
                                windown_width   = (int)Math.Round(Normalize(width_valid, 0, max_app_width, 0, int.Parse(info_program.width_real))),
                                windown_top     = (int)Math.Round(Normalize(Y, 0, max_app_height, 0, int.Parse(info_program.height_real))),
                                windown_left    = (int)Math.Round(Normalize(X, 0, max_app_width, 0, int.Parse(info_program.width_real))),
                                list            = list_object,
                                list_url        = list_url,
                                list_duration   = list_duration,
                                list_entrytime  = list_entrytime,
                                selected        = list_selected
                            };

                            windown = new ResizablePanel(destinationPanel)
                            {
                                Location        = new Point(X + info_program.x_area, Y + info_program.y_area),
                                Size            = new Size(width_valid, height_valid),
                                BackColor       = Color.Transparent,
                                Name            = JsonConvert.SerializeObject(info_windown),
                                AllowDrop       = true
                            };
                        }
                        windown.CustomEventMouseMove += (sender1, e1, X, Y, app_width, app_height, active_select, info_other_panel, direction) =>
                        {
                            // Resize child panels in program list (panel43)
                            var visiblePanels = this.panel43.Controls
                                .OfType<Panel>()
                                .Where(panel => panel.Visible);

                            foreach (var showPanel in visiblePanels)
                            {
                                // Deserialize JSON data from the Name property
                                Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(windown.Name);
                                //Console.WriteLine(info_other_panel);
                                this.panel70.Visible = true;
                                this.panel70.Name = infoWindow.Name;
                                //Console.WriteLine((Normalize(X, 0, showPanel.Width, 0, int.Parse(info_program.width_real))));
                                this.textBox1.Text = Math.Round(Normalize(X - info_program.x_area, 0, info_program.width_area, 0, int.Parse(info_program.width_real))).ToString();
                                this.textBox2.Text = Math.Round(Normalize(Y - info_program.y_area, 0, info_program.height_area, 0, int.Parse(info_program.height_real))).ToString();
                                if (active_select)
                                {
                                    this.textBox4.Text = Math.Round(Normalize(app_width, 0, info_program.width_area, 0, int.Parse(info_program.width_real))).ToString();
                                    this.textBox3.Text = Math.Round(Normalize(app_height, 0, info_program.height_area, 0, int.Parse(info_program.height_real))).ToString();
                                }

                                // Event Collision
                                if (direction == 1)
                                {
                                    Info_Window infoOtherWindow = JsonConvert.DeserializeObject<Info_Window>(info_other_panel);
                                    this.textBox1.Text = (infoOtherWindow.windown_left + infoOtherWindow.windown_width).ToString();
                                }
                                else if (direction == 3)
                                {
                                    Info_Window infoOtherWindow = JsonConvert.DeserializeObject<Info_Window>(info_other_panel);
                                    this.textBox2.Text = (infoOtherWindow.windown_top + infoOtherWindow.windown_height).ToString();
                                }

                                // Select first item
                                foreach (Control control1 in controlsListSelect[currentIdxList])
                                {
                                    if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                    {
                                        // Deserialize JSON data from the Name property
                                        Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);

                                        if (infoWindow1.Name.Equals(infoWindow.Name))
                                        {
                                            // update detail location
                                            infoWindow1.windown_width   = int.Parse(this.textBox4.Text);
                                            infoWindow1.windown_height  = int.Parse(this.textBox3.Text);
                                            infoWindow1.windown_top     = int.Parse(this.textBox2.Text);
                                            infoWindow1.windown_left    = int.Parse(this.textBox1.Text);

                                            resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                                        }
                                    }
                                }
                            }
                        };

                        windown.CustomEventMouseDown += (sender1, e1, X, Y, app_width, app_height, active_select, info_other_panel, direction) =>
                        {
                            // Resize child panels in program list (panel43)
                            var visiblePanels = this.panel43.Controls
                                .OfType<Panel>()
                                .Where(panel => panel.Visible);

                            foreach (var showPanel in visiblePanels)
                            {
                                // Deserialize JSON data from the Name property
                                Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(windown.Name);

                                // Select first item
                                foreach (Control control1 in controlsListSelect[currentIdxList])
                                {
                                    if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                    {
                                        // Deserialize JSON data from the Name property
                                        Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);

                                        if (infoWindow1.Name.Equals(infoWindow.Name))
                                        {
                                            this.panel70.Visible = true;
                                            this.panel70.Name = infoWindow.Name;

                                            this.textBox1.Text = infoWindow1.windown_left.ToString();
                                            this.textBox2.Text = infoWindow1.windown_top.ToString();
                                            this.textBox4.Text = infoWindow1.windown_width.ToString();
                                            this.textBox3.Text = infoWindow1.windown_height.ToString();
                                        }
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
                                foreach (Control control1 in controlsListSelect[currentIdxList])
                                {
                                    if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                    {
                                        // Deserialize JSON data from the Name property
                                        Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);
                                        infoWindow1.selectedMaster = false;

                                        for (int i = 0; i < infoWindow1.selected.Count; i++)
                                        {
                                            infoWindow1.selected[i] = false;
                                        }

                                        if (infoWindow1.Name.Equals(infoWindow.Name))
                                        {
                                            // Select first item
                                            if (infoWindow1.selected.Count > 0)
                                            {
                                                infoWindow1.selected[0] = true;
                                            }

                                            // update detail location
                                            infoWindow1.windown_width   = int.Parse(this.textBox4.Text);
                                            infoWindow1.windown_height  = int.Parse(this.textBox3.Text);
                                            infoWindow1.windown_top     = int.Parse(this.textBox2.Text);
                                            infoWindow1.windown_left    = int.Parse(this.textBox1.Text);
                                        }

                                        resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                                    }
                                }

                                foreach (Control control1 in this.list_windowns.Controls)
                                {
                                    if (control1.Name != null)
                                        control1.Refresh();
                                }

                                windown.InitializeResizeHandles();
                            }
                        };

                        windown.CustomEventDragDrop += (sender1, e1) =>
                        {
                            // Unselect all
                            foreach (Control control in controlsListSelect[currentIdxList])
                            {
                                if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                {
                                    // Deserialize JSON data from the Name property
                                    Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);
                                    infoWindow.selectedMaster = false;

                                    for (int i = 0; i < infoWindow.selected.Count; i++)
                                    {
                                        infoWindow.selected[i] = false;
                                    }

                                    resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                                }
                            }

                            // Update controlsListSelect[currentIdxList]
                            foreach (Control control in controlsListSelect[currentIdxList])
                            {
                                if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                {
                                    // Deserialize JSON data from the Name property
                                    Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);
                                    infoWindow.selectedMaster = false;

                                    if (infoWindow.Name.Equals("Windown " + lenght_list.ToString()))
                                    {
                                        String name_file = e1.Data.GetData("PictureBoxName") as string;
                                        string extension1 = System.IO.Path.GetExtension(name_file).ToLower();
                                        this.name_select.Text = " " + System.IO.Path.GetFileNameWithoutExtension(name_file);

                                        if (name_file.Equals("Webpage"))
                                        {
                                            infoWindow.list_url.Add("toantrungcloud.com");

                                            infoWindow.list_entrytime.Add("0");
                                            infoWindow.list_duration.Add("10");
                                        }
                                        else if (name_file.Equals("Text"))
                                        {
                                            // Data not use
                                            infoWindow.list_url.Add("");

                                            infoWindow.list_entrytime.Add("0");
                                            infoWindow.list_duration.Add("10");
                                        }
                                        else
                                        {
                                            // Data not use
                                            infoWindow.list_url.Add("");

                                            // Is a video
                                            if (extension1 == ".jpg" || extension1 == ".bmp" || extension1 == ".png" || extension1 == ".gif")
                                            {
                                                infoWindow.list_entrytime.Add("0");
                                                infoWindow.list_duration.Add("10");
                                            }
                                            else
                                            {
                                                var flag_error = true;
                                                var mediaInfo = new MediaInfo.DotNetWrapper.MediaInfo();
                                                mediaInfo.Open(name_file);

                                                if (double.Parse(mediaInfo.Get(StreamKind.General, 0, "Duration")) > 0)
                                                {
                                                    flag_error = false;

                                                    double durationMilliseconds = double.Parse(mediaInfo.Get(StreamKind.General, 0, "Duration"));

                                                    infoWindow.list_entrytime.Add("0");
                                                    infoWindow.list_duration.Add(durationMilliseconds.ToString());
                                                }

                                                if (flag_error)
                                                {
                                                    flag_error = false;
                                                    infoWindow.list_entrytime.Add("");
                                                    infoWindow.list_duration.Add("");
                                                }

                                            }
                                        }

                                        infoWindow.list.Add(name_file);
                                        infoWindow.selected.Add(true);
                                        resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                                    }
                                }
                            }

                            // Draw windown list
                            draw_list_windown(controlsListSelect[currentIdxList]);
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

                        long total_frame = 0;

                        // Create PictureBox for the image
                        PictureBox pictureBox = new PictureBox();
                        pictureBox.Dock       = System.Windows.Forms.DockStyle.Fill;
                        pictureBox.SizeMode   = PictureBoxSizeMode.StretchImage;
                        pictureBox.Padding    = new System.Windows.Forms.Padding(1, 1, 1, 1);
                        pictureBox.Name       = "0";
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
                                else if (int.Parse(pictureBox.Name) >= (total_frame >= 2 ? (total_frame - 2) : total_frame))
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

                        controlsListSelect[currentIdxList].Insert(0, windown);
                        destinationPanel.Controls.AddRange(controlsListSelect[currentIdxList].ToArray());
 
                        // Draw windown list
                        draw_list_windown(controlsListSelect[currentIdxList]);
                    }
                }
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
                    title_Panel.MouseDown += (sender, e) =>
                    {
                        this.panel70.Visible = false;

                        // Unselect all
                        foreach (Control control1 in lists)
                        {
                            if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                            {
                                // Deserialize JSON data from the Name property
                                Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);

                                if (((sender as Panel).Controls[0] as Label).Text == infoWindow1.Name)
                                    infoWindow1.selectedMaster = true;
                                else
                                    infoWindow1.selectedMaster = false;

                                for (int i1 = 0; i1 < infoWindow1.selected.Count; i1++)
                                {
                                    infoWindow1.selected[i1] = false;
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
                    title_Panel.Paint += (sender, e) =>
                    {
                        foreach (Control control1 in lists)
                        {
                            if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                            {
                                // Deserialize JSON data from the Name property
                                Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);

                                if ((((sender as Panel).Controls[0] as Label).Text == infoWindow1.Name) && (infoWindow1.selectedMaster))
                                {
                                    // Draw a border with a different color and thickness
                                    using (Pen pen = new Pen(Color.LightBlue, 2)) // You can change Color.Red to your desired color
                                    {
                                        e.Graphics.DrawRectangle(pen, title_Panel.Padding.Left, 0, title_Panel.Width - 1 - title_Panel.Padding.Left, title_Panel.Height - 1);
                                    }

                                    ResizablePanel panel_windown = control1 as ResizablePanel;
                                    panel_windown.UninitializeResizeHandles();

                                    break;
                                }
                            }
                        }
                    };

                    Panel delta_Panel2 = new Panel();
                    delta_Panel2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                    delta_Panel2.Dock = System.Windows.Forms.DockStyle.Top;
                    delta_Panel2.Location = new System.Drawing.Point(0, 0);
                    delta_Panel2.Size = new System.Drawing.Size(946, 2);
                    delta_Panel2.Padding = new System.Windows.Forms.Padding(8, 0, 8, 8);

                    Label windown_name_label = new Label();
                    windown_name_label.AutoSize = true;
                    windown_name_label.Dock = DockStyle.None;
                    windown_name_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                    windown_name_label.Location = new System.Drawing.Point(0, 0);
                    windown_name_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    windown_name_label.ForeColor = System.Drawing.Color.White;
                    windown_name_label.Text = infoWindow.Name;

                    // Now center the label
                    windown_name_label.Location = new System.Drawing.Point(10, (title_Panel.Height - windown_name_label.PreferredHeight) / 2);
                    windown_name_label.MouseDown += (sender, e) =>
                    {
                        this.panel70.Visible = false;

                        // Unselect all
                        foreach (Control control1 in lists)
                        {
                            if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                            {
                                // Deserialize JSON data from the Name property
                                Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);

                                if ((sender as Label).Text == infoWindow1.Name)
                                    infoWindow1.selectedMaster = true;
                                else
                                    infoWindow1.selectedMaster = false;

                                for (int i1 = 0; i1 < infoWindow1.selected.Count; i1++)
                                {
                                    infoWindow1.selected[i1] = false;
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
                    title_Panel.Controls.Add(windown_name_label);

                    for (int i = infoWindow.list.Count - 1; i >= 0; i--)
                    {
                        String selectfilePath = infoWindow.list[i];
                        if (!selectfilePath.Equals("Webpage") && !selectfilePath.Equals("Text") && !File.Exists(selectfilePath))
                            continue;

                        String typeFile = "Video/Image";
                        string extension = System.IO.Path.GetExtension(selectfilePath).ToLower();
                        Image videoFrame = null;

                        if (selectfilePath.Equals("Webpage"))
                            typeFile = "Webpage";
                        else if (selectfilePath.Equals("Text"))
                            typeFile = "Text";

                        Panel item_Panel = new Panel();
                        item_Panel.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
                        item_Panel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                        item_Panel.Dock = System.Windows.Forms.DockStyle.Top;
                        item_Panel.Location = new System.Drawing.Point(0, 0);
                        item_Panel.Size = new System.Drawing.Size(946, 50);
                        item_Panel.Name = i.ToString();
                        item_Panel.Paint += (sender, e) =>
                        {
                            // Unselect all
                            foreach (Control control1 in lists)
                            {
                                if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name) && control1.Parent != null)
                                {
                                    // Deserialize JSON data from the Name property
                                    Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);
                                    if (infoWindow1.Name.Equals(infoWindow.Name))
                                    {
                                        Color initialBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64))))); // Set to your initial color
                                        int index = int.Parse((sender as Control).Name);
                                        if (infoWindow1.selected[index])
                                        {
                                            String path_file = infoWindow1.list[index];
                                            string extension_1 = System.IO.Path.GetExtension(path_file).ToLower();

                                            // Filter type
                                            String type_file = "Video/Image";
                                            if (path_file.Equals("Webpage"))
                                                type_file = "Webpage";
                                            else if (path_file.Equals("Text"))
                                                type_file = "Text";

                                            initialBorderColor = Color.LightBlue;

                                            this.panel70.Visible = true;
                                            this.panel70.Name = infoWindow.Name;
                                            this.name_select.Text = " " + System.IO.Path.GetFileNameWithoutExtension(infoWindow1.list[index]);

                                            int max_app_width = control1.Parent.Width - 0;
                                            int max_app_height = control1.Parent.Height - 0;
                                            var info_program = JsonConvert.DeserializeObject<Info_Program>(control1.Parent.Name);

                                            this.textBox1.Text = infoWindow1.windown_left.ToString();
                                            this.textBox2.Text = infoWindow1.windown_top.ToString();
                                            this.textBox4.Text = infoWindow1.windown_width.ToString();
                                            this.textBox3.Text = infoWindow1.windown_height.ToString();

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
                                            
                                            if (type_file.Equals("Video/Image"))
                                            {
                                                // Is a video
                                                if (extension_1 == ".mp4" || extension_1 == ".avi" ||
                                                    extension_1 == ".wmv" || extension_1 == ".mpg" ||
                                                    extension_1 == ".rmvp" || extension_1 == ".mov" ||
                                                    extension_1 == ".dat" || extension_1 == ".flv")
                                                {
                                                    this.panel80.Visible = false;
                                                    this.panel100.Visible = false;

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
                                                    this.panel80.Visible = true;
                                                    this.panel100.Visible = false;

                                                    this.entrytime_select.Text = infoWindow1.list_entrytime[index];
                                                    this.duration_select.Text = infoWindow1.list_duration[index];

                                                    if (!infoWindow1.path_windown.Equals(path_file))
                                                    {
                                                        // update data
                                                        infoWindow1.path_windown = path_file;
                                                        resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                                                    }

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
                                            }
                                            else if (type_file.Equals("Webpage"))
                                            {
                                                this.panel80.Visible = true;
                                                this.panel100.Visible = true;

                                                this.url_select.Text = infoWindow1.list_url[index];
                                                this.entrytime_select.Text = infoWindow1.list_entrytime[index];
                                                this.duration_select.Text = infoWindow1.list_duration[index];

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
                                                        pictureBoxInPanel.Image = Properties.Resources.browser_icon;

                                                        // Break out of the loop if you only need the first PictureBox
                                                        break;
                                                    }
                                                }
                                            }
                                            else if (type_file.Equals("Text"))
                                            {
                                                this.panel100.Visible = false;

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
                                                        pictureBoxInPanel.Image = Properties.Resources.text_icon;

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
                        picture_Panel.Size = new System.Drawing.Size(60, 80);

                        if (typeFile.Equals("Video/Image"))
                        {
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
                                videoFrame = videoFileReader.ReadVideoFrame(10);

                                // Close the video file reader
                                videoFileReader.Close();
                            }
                            else if (extension == ".jpg" || extension == ".bmp" ||
                                     extension == ".png" || extension == ".gif")
                            {
                                videoFrame = System.Drawing.Image.FromFile(selectfilePath);
                                typeFile = "Image";
                            }
                        }
                        else if (typeFile.Equals("Webpage"))
                        {
                            videoFrame = Properties.Resources.browser_icon;
                        }
                        else if (typeFile.Equals("Text"))
                        {
                            videoFrame = Properties.Resources.text_icon;
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
                                    Console.WriteLine(resizablePanel1.Name);
                                    // Deserialize JSON data from the Name property
                                    Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);
                                    infoWindow1.selectedMaster = false;

                                    for (int i1 = 0; i1 < infoWindow1.selected.Count; i1++)
                                    {
                                        infoWindow1.selected[i1] = false;
                                    }

                                    if (infoWindow1.Name.Equals(infoWindow.Name))
                                    {
                                        Control parentControl = (sender as Control).Parent.Parent;
                                        Console.WriteLine(parentControl.Name);
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
                        name_label.AutoSize = false;
                        name_label.AutoEllipsis = true;
                        name_label.Padding = new System.Windows.Forms.Padding(5, 15, 0, 0);
                        name_label.Dock = System.Windows.Forms.DockStyle.Fill;
                        name_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                        name_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                        name_label.ForeColor = System.Drawing.Color.White;
                        name_label.Text = typeFile.Equals("Video/Image") ? "Video" : typeFile;
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
                                    infoWindow1.selectedMaster = false;

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

            this.screenshort_label.Visible = true;
            if (this.panel12.Controls.Count > 1)
            {
                this.panel12.Controls.RemoveAt(1);
            }
            this.name_device.Text = "--";
            this.name_device.Left = 112;
            this.ip_device.Text = "--:--:--:--";
            this.ip_device.Left = 306;
            this.resolution_device.Text = "--";
            this.screen_status.Text = "--";
            this.screen_status.Left = 306;
            this.screen_status.ForeColor = Color.White;

            running_time_box = 0;
            current_time_box = 0;

            // Tạo một luồng riêng cho việc lắng nghe UDP
            if (udpListenerThread == null || (udpListenerThread != null && !udpListenerThread.IsAlive))
            {
                udpListenerThread = new Thread(() => UdpListener(45454));
                udpListenerThread.Start();
            }

            device_select = "";
        }

        private void getInfoThread(object parameter)
        {
            try
            {
                byte[] buffer = new byte[10240 + 256];
                //Console.WriteLine((string)parameter);
                Command_device cmd_packet = JsonConvert.DeserializeObject<Command_device>((string)parameter);

                // Create a Stopwatch instance
                Stopwatch stopwatch = new Stopwatch();

                // Start the stopwatch
                stopwatch.Start();

                // Get resolution device
                TcpClient client = new TcpClient();
                client.Connect(cmd_packet.ip_address, 12345);

                //client.Connect(cmd_packet.ip_address, 12345);
                NetworkStream stream = client.GetStream();


                // Send the data
                byte[] byteArray = Encoding.UTF8.GetBytes((string)parameter);
                Array.Copy(byteArray, buffer, byteArray.Length);
                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();

                for (int delay = 0; delay < 300; delay++)
                {
                    Thread.Sleep(10);

                    if (stream.DataAvailable)
                    {
                        Array.Clear(buffer, 0, buffer.Length);
                        stream.Read(buffer, 0, buffer.Length);

                        Command_response_device response_device = JsonConvert.DeserializeObject<Command_response_device>(Encoding.UTF8.GetString(buffer));
                        //Console.WriteLine(Encoding.UTF8.GetString(buffer));

                        if (cmd_packet.deviceName.Length == 0)
                        {
                            // Login
                            HttpWebRequest request_login = (HttpWebRequest)WebRequest.Create($"http://{cmd_packet.ip_address}:18080/login");
                            request_login.Method = "POST";

                            // Add parameters to the request body
                            string postData = $"password={response_device.password}";
                            request_login.ContentType = "application/x-www-form-urlencoded";
                            request_login.ContentLength = Encoding.UTF8.GetBytes(postData).Length;

                            using (Stream dataStream = request_login.GetRequestStream())
                            {
                                dataStream.Write(Encoding.UTF8.GetBytes(postData), 0, Encoding.UTF8.GetBytes(postData).Length);
                            }

                            using (HttpWebResponse response_login = (HttpWebResponse)request_login.GetResponse())
                            {
                                if (response_login.StatusCode == HttpStatusCode.OK)
                                {
                                    string session_id = response_login.Headers["Set-Cookie"];

                                    // Get current time
                                    HttpWebRequest request_cur_time = (HttpWebRequest)WebRequest.Create($"http://{cmd_packet.ip_address}:18080/device");
                                    request_cur_time.Method = "POST";
                                    request_cur_time.Headers.Add("Cookie", $"{session_id}");
                                    using (HttpWebResponse response_signnal_input = (HttpWebResponse)request_cur_time.GetResponse())
                                    {
                                        if (response_signnal_input.StatusCode == HttpStatusCode.OK)
                                        {
                                            using (Stream responseStream = response_signnal_input.GetResponseStream())
                                            {
                                                using (StreamReader reader = new StreamReader(responseStream))
                                                {
                                                    device data = JsonConvert.DeserializeObject<device>(reader.ReadToEnd());
                                                    if (data.code == 200)
                                                    {
                                                        panel35.Invoke((MethodInvoker)delegate
                                                        {
                                                            // Create the add panel
                                                            Panel addPanel = new Panel();
                                                            addPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                                                            addPanel.Dock = System.Windows.Forms.DockStyle.Top;
                                                            addPanel.Location = new System.Drawing.Point(0, 0);
                                                            addPanel.Size = new System.Drawing.Size(946, 60);
                                                            addPanel.Padding = new System.Windows.Forms.Padding(8, 0, 8, 8);

                                                            var info_device = new
                                                            {
                                                                deviceName = response_device.UUID,
                                                                selected = false,
                                                                password = "",
                                                                session_id = "",
                                                                ip_address = cmd_packet.ip_address
                                                            };

                                                            // Create the add table panel
                                                            TableLayoutPanel addTablePanel = new TableLayoutPanel();
                                                            addTablePanel.BorderStyle = BorderStyle.None;
                                                            addTablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
                                                            addTablePanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                                                            addTablePanel.ColumnCount = 7;
                                                            addTablePanel.Name = JsonConvert.SerializeObject(info_device);
                                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
                                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
                                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
                                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14F));
                                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
                                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
                                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));

                                                            Label device_name_label = new Label();
                                                            device_name_label.AutoSize = true;
                                                            device_name_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                                            device_name_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                                            device_name_label.Location = new System.Drawing.Point(0, 0);
                                                            device_name_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                                            device_name_label.Size = new System.Drawing.Size(283, 30);
                                                            device_name_label.TabIndex = 0;
                                                            device_name_label.Name = response_device.UUID;
                                                            device_name_label.Text = response_device.UUID;
                                                            device_name_label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                                                            device_name_label.MouseEnter += row_device_MouseEnter;
                                                            device_name_label.MouseLeave += row_device_MouseLeave;
                                                            device_name_label.MouseClick += row_device_MouseClick;
                                                            addTablePanel.Controls.Add(device_name_label, 0, 0);

                                                            Label method_label = new Label();
                                                            method_label.AutoSize = true;
                                                            method_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                                            method_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                                            method_label.Location = new System.Drawing.Point(0, 0);
                                                            method_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                                            method_label.Size = new System.Drawing.Size(283, 30);
                                                            method_label.TabIndex = 0;
                                                            method_label.Name = response_device.UUID;
                                                            method_label.Text = "Wifi AP";
                                                            method_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                                            method_label.MouseEnter += row_device_MouseEnter;
                                                            method_label.MouseLeave += row_device_MouseLeave;
                                                            method_label.MouseClick += row_device_MouseClick;
                                                            addTablePanel.Controls.Add(method_label, 1, 0);

                                                            Label address_label = new Label();
                                                            address_label.AutoSize = true;
                                                            address_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                                            address_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                                            address_label.Location = new System.Drawing.Point(0, 0);
                                                            address_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                                            address_label.Size = new System.Drawing.Size(283, 30);
                                                            address_label.TabIndex = 0;
                                                            address_label.Name = response_device.UUID;
                                                            address_label.Text = cmd_packet.ip_address;
                                                            address_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                                            address_label.MouseEnter += row_device_MouseEnter;
                                                            address_label.MouseLeave += row_device_MouseLeave;
                                                            address_label.MouseClick += row_device_MouseClick;
                                                            addTablePanel.Controls.Add(address_label, 2, 0);

                                                            Label resolution_label = new Label();
                                                            resolution_label.AutoSize = true;
                                                            resolution_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                                            resolution_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                                            resolution_label.Location = new System.Drawing.Point(0, 0);
                                                            resolution_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                                            resolution_label.Size = new System.Drawing.Size(283, 30);
                                                            resolution_label.TabIndex = 0;
                                                            resolution_label.Name = response_device.UUID;
                                                            resolution_label.Text = $"{response_device.width} x {response_device.height}";
                                                            resolution_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                                            resolution_label.MouseEnter += row_device_MouseEnter;
                                                            resolution_label.MouseLeave += row_device_MouseLeave;
                                                            resolution_label.MouseClick += row_device_MouseClick;
                                                            addTablePanel.Controls.Add(resolution_label, 3, 0);

                                                            Label brightness_label = new Label();
                                                            brightness_label.AutoSize = true;
                                                            brightness_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                                            brightness_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                                            brightness_label.Location = new System.Drawing.Point(0, 0);
                                                            brightness_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                                            brightness_label.Size = new System.Drawing.Size(283, 30);
                                                            brightness_label.TabIndex = 0;
                                                            brightness_label.Name = response_device.UUID;
                                                            brightness_label.Text = $"{response_device.bright}";
                                                            brightness_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                                            brightness_label.MouseEnter += row_device_MouseEnter;
                                                            brightness_label.MouseLeave += row_device_MouseLeave;
                                                            brightness_label.MouseClick += row_device_MouseClick;
                                                            addTablePanel.Controls.Add(brightness_label, 4, 0);

                                                            Label voice_label = new Label();
                                                            voice_label.AutoSize = true;
                                                            voice_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                                            voice_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                                            voice_label.Location = new System.Drawing.Point(0, 0);
                                                            voice_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                                            voice_label.Size = new System.Drawing.Size(283, 30);
                                                            voice_label.TabIndex = 0;
                                                            voice_label.Name = response_device.UUID;
                                                            voice_label.Text = $"{response_device.voice}";
                                                            voice_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                                            voice_label.MouseEnter += row_device_MouseEnter;
                                                            voice_label.MouseLeave += row_device_MouseLeave;
                                                            voice_label.MouseClick += row_device_MouseClick;
                                                            addTablePanel.Controls.Add(voice_label, 5, 0);

                                                            Label version_label = new Label();
                                                            version_label.AutoSize = true;
                                                            version_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                                            version_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                                            version_label.Location = new System.Drawing.Point(0, 0);
                                                            version_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                                            version_label.Size = new System.Drawing.Size(283, 30);
                                                            version_label.TabIndex = 0;
                                                            version_label.Name = response_device.UUID;
                                                            version_label.Text = "v" + data.data.systemVersion + "-" + data.data.appVersion;
                                                            version_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                                            version_label.MouseEnter += row_device_MouseEnter;
                                                            version_label.MouseLeave += row_device_MouseLeave;
                                                            version_label.MouseClick += row_device_MouseClick;
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
                                                            addPanelRelease.Padding = new System.Windows.Forms.Padding(30, 0, 8, 8);

                                                            // Create the add table panel
                                                            TableLayoutPanel addTablePanelRelease = new TableLayoutPanel();
                                                            addTablePanelRelease.BorderStyle = BorderStyle.None;
                                                            addTablePanelRelease.Dock = System.Windows.Forms.DockStyle.Fill;
                                                            addTablePanelRelease.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                                                            addTablePanelRelease.ColumnCount = 4;
                                                            addTablePanelRelease.Name = response_device.UUID;
                                                            addTablePanelRelease.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 5F));
                                                            addTablePanelRelease.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                                                            addTablePanelRelease.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
                                                            addTablePanelRelease.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
                                                            addTablePanelRelease.MouseEnter += row_device_release_MouseEnter;
                                                            addTablePanelRelease.MouseLeave += row_device_release_MouseLeave;

                                                            RadioButton radioButton1 = new RadioButton();
                                                            radioButton1.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                                            radioButton1.Dock = System.Windows.Forms.DockStyle.Fill;
                                                            radioButton1.FlatAppearance.BorderSize = 0;
                                                            radioButton1.Font = new System.Drawing.Font("Microsoft Sans Serif", 50F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                                                            radioButton1.Location = new System.Drawing.Point(0, 0);
                                                            radioButton1.Margin = new System.Windows.Forms.Padding(0);
                                                            radioButton1.Name = response_device.UUID;
                                                            radioButton1.Size = new System.Drawing.Size(100, 70);
                                                            radioButton1.TabIndex = 0;
                                                            radioButton1.TabStop = true;
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
                                                            device_name_release_label.Name = response_device.UUID;
                                                            device_name_release_label.Text = response_device.UUID;
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
                                                            address_release_label.Name = response_device.UUID;
                                                            address_release_label.Text = cmd_packet.ip_address;
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
                                                            remain_release_label.Name = response_device.UUID;
                                                            remain_release_label.Text = Math.Round(((float)data.data.usableSpace / 1024), 2).ToString();
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
                                                            this.total_pc.Text = "Total 1";
                                                            this.online_pc.Text = "Total 1";
                                                        });
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            panel35.Invoke((MethodInvoker)delegate
                            {
                                // Update list data
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

                                            if (controlInFirstColumn != null && response_device.UUID != null && response_device.UUID.Equals(controlInFirstColumn.Text))
                                            {
                                                if (response_device.password.Length > 0)
                                                {
                                                    Label device_name_label = controlInFirstColumn as Label;
                                                    device_name_label.Image = global::WindowsFormsApp.Properties.Resources.lock_icon;
                                                    device_name_label.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
                                                }

                                                if (response_device.width != null && response_device.height != null)
                                                {
                                                    Label resolution_label = tableLayoutPanel.GetControlFromPosition(3, 0) as Label;
                                                    resolution_label.Text = $"{response_device.width} x {response_device.height}";
                                                }

                                                if (response_device.voice != null)
                                                {
                                                    Label brightness_label = tableLayoutPanel.GetControlFromPosition(4, 0) as Label;
                                                    brightness_label.Text = $"{response_device.bright}";
                                                }

                                                if (response_device.voice != null)
                                                {
                                                    Label voice_label = tableLayoutPanel.GetControlFromPosition(5, 0) as Label;
                                                    voice_label.Text = $"{response_device.voice}";
                                                }

                                                break;
                                            }
                                        }
                                    }
                                }
                            });
                        }

                        break;
                    }
                }

                stream.Close();
                client.Close();

                // Display the elapsed time
                Console.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds} ms");

            }
            catch (Exception e1)
            {
                Console.WriteLine($"{e1}");
            }
        }

        private void UdpListener(int udpPort)
        {
            //using (StreamWriter fileStream = new StreamWriter(outputPath))
            {
                try
                {
                    // Redirect Console.Out to the StreamWriter
                    //Console.SetOut(fileStream);

                    Console.WriteLine("Start scan device");
                    using (UdpClient udpListener = new UdpClient(udpPort))
                    {
                        IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, udpPort);

                        // Reinit
                        flagTermianlUDPThread = false;

                        Stopwatch time_finish = new Stopwatch();
                        int counter_device = 0;
                        time_finish.Start();

                        while (!flagTermianlUDPThread)
                        {
                            try
                            {
                                if (udpListener.Available < 256)
                                {
                                    if (time_finish.ElapsedMilliseconds > 2000)
                                    {
                                        time_finish.Stop();
                                        Console.WriteLine($"Scan device finished with \"{time_finish.ElapsedMilliseconds} ms\"");
                                        break;
                                    }
                                    else
                                    {
                                        Thread.Sleep(10);
                                        continue;
                                    }
                                }


                                // Use Task.Factory.StartNew to run the asynchronous operation with a cancellation token
                                byte[] receivedBytes = udpListener.Receive(ref endPoint);
                                string receivedMessage = Encoding.ASCII.GetString(receivedBytes);
                                //Console.WriteLine(receivedMessage);
                                //Console.WriteLine("First device");


                                Boolean have_obj = false;
                                counter_device = 1;

                                adv_packet data = JsonConvert.DeserializeObject<adv_packet>(receivedMessage);

                                // Replace
                                data.deviceId = data.deviceId.Replace("K", "TT");

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

                                                    if (device_name_label_in_list.Text.Equals(data.deviceId))
                                                    {
                                                        have_obj = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (!have_obj)
                                {
                                    String resolution_str = "--";
                                    String brightness = "--";
                                    String voice = "--";
                                    try
                                    {
                                        var cmd_for_device = new
                                        {
                                            deviceName = data.deviceId,
                                            type = "SOCKET",
                                            command = "GET_INFO",
                                            ip_address = endPoint.Address.ToString()
                                        };

                                        // Start a new thread for the dialog with parameters
                                        Thread dialogThread = new Thread(new ParameterizedThreadStart(getInfoThread));
                                        dialogThread.Start(JsonConvert.SerializeObject(cmd_for_device));

                                        // Add new device
                                        panel35.Invoke((MethodInvoker)delegate
                                        {
                                            // Create the add panel
                                            Panel addPanel = new Panel();
                                            addPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                                            addPanel.Dock = System.Windows.Forms.DockStyle.Top;
                                            addPanel.Location = new System.Drawing.Point(0, 0);
                                            addPanel.Size = new System.Drawing.Size(946, 60);
                                            addPanel.Padding = new System.Windows.Forms.Padding(8, 0, 8, 8);

                                            var info_device = new
                                            {
                                                deviceName = data.deviceId,
                                                selected = false,
                                                password = "",
                                                session_id = "",
                                                ip_address = endPoint.Address.ToString()
                                            };

                                            // Create the add table panel
                                            TableLayoutPanel addTablePanel = new TableLayoutPanel();
                                            addTablePanel.BorderStyle = BorderStyle.None;
                                            addTablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
                                            addTablePanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                                            addTablePanel.ColumnCount = 7;
                                            addTablePanel.Name = JsonConvert.SerializeObject(info_device);
                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14F));
                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
                                            addTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));

                                            Label device_name_label = new Label();
                                            device_name_label.AutoSize = true;
                                            device_name_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                            device_name_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                            device_name_label.Location = new System.Drawing.Point(0, 0);
                                            device_name_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                            device_name_label.Size = new System.Drawing.Size(283, 30);
                                            device_name_label.TabIndex = 0;
                                            device_name_label.Name = data.deviceId;
                                            device_name_label.Text = data.deviceId;
                                            device_name_label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                                            device_name_label.MouseEnter += row_device_MouseEnter;
                                            device_name_label.MouseLeave += row_device_MouseLeave;
                                            device_name_label.MouseClick += row_device_MouseClick;
                                            addTablePanel.Controls.Add(device_name_label, 0, 0);

                                            Label method_label = new Label();
                                            method_label.AutoSize = true;
                                            method_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                            method_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                            method_label.Location = new System.Drawing.Point(0, 0);
                                            method_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                            method_label.Size = new System.Drawing.Size(283, 30);
                                            method_label.TabIndex = 0;
                                            method_label.Name = data.deviceId;
                                            method_label.Text = "LAN";
                                            if (endPoint.Address.ToString().Equals("192.168.43.1"))
                                            {
                                                method_label.Text = "Wifi AP";
                                            }
                                            method_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                            method_label.MouseEnter += row_device_MouseEnter;
                                            method_label.MouseLeave += row_device_MouseLeave;
                                            method_label.MouseClick += row_device_MouseClick;
                                            addTablePanel.Controls.Add(method_label, 1, 0);

                                            Label address_label = new Label();
                                            address_label.AutoSize = true;
                                            address_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                            address_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                            address_label.Location = new System.Drawing.Point(0, 0);
                                            address_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                            address_label.Size = new System.Drawing.Size(283, 30);
                                            address_label.TabIndex = 0;
                                            address_label.Name = data.deviceId;
                                            address_label.Text = endPoint.Address.ToString();
                                            address_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                            address_label.MouseEnter += row_device_MouseEnter;
                                            address_label.MouseLeave += row_device_MouseLeave;
                                            address_label.MouseClick += row_device_MouseClick;
                                            addTablePanel.Controls.Add(address_label, 2, 0);

                                            Label resolution_label = new Label();
                                            resolution_label.AutoSize = true;
                                            resolution_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                            resolution_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                            resolution_label.Location = new System.Drawing.Point(0, 0);
                                            resolution_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                            resolution_label.Size = new System.Drawing.Size(283, 30);
                                            resolution_label.TabIndex = 0;
                                            resolution_label.Name = data.deviceId;
                                            resolution_label.Text = resolution_str;
                                            resolution_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                            resolution_label.MouseEnter += row_device_MouseEnter;
                                            resolution_label.MouseLeave += row_device_MouseLeave;
                                            resolution_label.MouseClick += row_device_MouseClick;
                                            addTablePanel.Controls.Add(resolution_label, 3, 0);

                                            Label brightness_label = new Label();
                                            brightness_label.AutoSize = true;
                                            brightness_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                            brightness_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                            brightness_label.Location = new System.Drawing.Point(0, 0);
                                            brightness_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                            brightness_label.Size = new System.Drawing.Size(283, 30);
                                            brightness_label.TabIndex = 0;
                                            brightness_label.Name = data.deviceId;
                                            brightness_label.Text = brightness;
                                            brightness_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                            brightness_label.MouseEnter += row_device_MouseEnter;
                                            brightness_label.MouseLeave += row_device_MouseLeave;
                                            brightness_label.MouseClick += row_device_MouseClick;
                                            addTablePanel.Controls.Add(brightness_label, 4, 0);

                                            Label voice_label = new Label();
                                            voice_label.AutoSize = true;
                                            voice_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                            voice_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                            voice_label.Location = new System.Drawing.Point(0, 0);
                                            voice_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                            voice_label.Size = new System.Drawing.Size(283, 30);
                                            voice_label.TabIndex = 0;
                                            voice_label.Name = data.deviceId;
                                            voice_label.Text = voice;
                                            voice_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                            voice_label.MouseEnter += row_device_MouseEnter;
                                            voice_label.MouseLeave += row_device_MouseLeave;
                                            voice_label.MouseClick += row_device_MouseClick;
                                            addTablePanel.Controls.Add(voice_label, 5, 0);

                                            Label version_label = new Label();
                                            version_label.AutoSize = true;
                                            version_label.Dock = System.Windows.Forms.DockStyle.Fill;
                                            version_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                                            version_label.Location = new System.Drawing.Point(0, 0);
                                            version_label.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
                                            version_label.Size = new System.Drawing.Size(283, 30);
                                            version_label.TabIndex = 0;
                                            version_label.Name = data.deviceId;
                                            version_label.Text = "v" + data.systemVersion + "-" + data.appVersion;
                                            version_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                            version_label.MouseEnter += row_device_MouseEnter;
                                            version_label.MouseLeave += row_device_MouseLeave;
                                            version_label.MouseClick += row_device_MouseClick;
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
                                            addPanelRelease.Padding = new System.Windows.Forms.Padding(30, 0, 8, 8);

                                            // Create the add table panel
                                            TableLayoutPanel addTablePanelRelease = new TableLayoutPanel();
                                            addTablePanelRelease.BorderStyle = BorderStyle.None;
                                            addTablePanelRelease.Dock = System.Windows.Forms.DockStyle.Fill;
                                            addTablePanelRelease.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
                                            addTablePanelRelease.ColumnCount = 4;
                                            addTablePanelRelease.Name = data.deviceId;
                                            addTablePanelRelease.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 5F));
                                            addTablePanelRelease.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                                            addTablePanelRelease.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
                                            addTablePanelRelease.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
                                            addTablePanelRelease.MouseEnter += row_device_release_MouseEnter;
                                            addTablePanelRelease.MouseLeave += row_device_release_MouseLeave;

                                            RadioButton radioButton1 = new RadioButton();
                                            radioButton1.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
                                            radioButton1.Dock = System.Windows.Forms.DockStyle.Fill;
                                            radioButton1.FlatAppearance.BorderSize = 0;
                                            radioButton1.Font = new System.Drawing.Font("Microsoft Sans Serif", 50F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                                            radioButton1.Location = new System.Drawing.Point(0, 0);
                                            radioButton1.Margin = new System.Windows.Forms.Padding(0);
                                            radioButton1.Name = data.deviceId;
                                            radioButton1.Size = new System.Drawing.Size(100, 70);
                                            radioButton1.TabIndex = 0;
                                            radioButton1.TabStop = true;
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
                                            device_name_release_label.Name = data.deviceId;
                                            device_name_release_label.Text = data.deviceId;
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
                                            address_release_label.Name = data.deviceId;
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
                                            remain_release_label.Name = data.deviceId;
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
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Error {e}");
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

                        if (counter_device == 0)
                        {
                            var cmd_for_device = new
                            {
                                deviceName = "",
                                type = "SOCKET",
                                command = "GET_INFO",
                                ip_address = "192.168.43.1"
                            };

                            using (Ping ping = new Ping())
                            {
                                PingReply reply = ping.Send(cmd_for_device.ip_address);

                                if (reply.Status == IPStatus.Success)
                                {
                                    // Start a new thread for the dialog with parameters
                                    Thread dialogThread = new Thread(new ParameterizedThreadStart(getInfoThread));
                                    dialogThread.Start(JsonConvert.SerializeObject(cmd_for_device));
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Lỗi: {e}");
                }
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

                                this.show.AutoScrollPosition = new System.Drawing.Point(Math.Abs(this.show.AutoScrollPosition.X), Math.Abs(this.show.AutoScrollPosition.Y));
                            }
                        }
                    }
                }
                else if (parentControl is Panel)
                {
                    Button featureIcon = (sender as Button);

                    // Check list item exist
                    foreach (Control control in this.panel43.Controls)
                    {
                        if (control is Panel panel_chill && panel_chill.Visible)
                        {
                            // Show image
                            if (featureIcon.Name.Equals(this.Webpage.Name))
                            {
                                drappPictureBox.Image = Properties.Resources.browser_icon;
                                drappPictureBox.Visible = true;
                                drappPictureBox.Location = new Point(Cursor.Position.X - this.Location.X, Cursor.Position.Y - this.Location.Y - 80);

                                // Tạo một DataObject để chứa thông tin cần truyền đi
                                DataObject data = new DataObject();
                                data.SetData("PictureBoxImage", Properties.Resources.browser_icon);     // Chuyển hình ảnh
                                data.SetData("PictureBoxName", featureIcon.Name);                       // Chuyển tên

                                featureIcon.DoDragDrop(data, DragDropEffects.Move);

                                // Hide image
                                drappPictureBox.Visible = false;

                                this.show.AutoScrollPosition = new System.Drawing.Point(Math.Abs(this.show.AutoScrollPosition.X), Math.Abs(this.show.AutoScrollPosition.Y));
                            }
                            else if (featureIcon.Name.Equals(this.Text.Name))
                            {
                                drappPictureBox.Image = Properties.Resources.text_icon;
                                drappPictureBox.Visible = true;
                                drappPictureBox.Location = new Point(Cursor.Position.X - this.Location.X, Cursor.Position.Y - this.Location.Y - 80);

                                // Tạo một DataObject để chứa thông tin cần truyền đi
                                DataObject data = new DataObject();
                                data.SetData("PictureBoxImage", Properties.Resources.text_icon);        // Chuyển hình ảnh
                                data.SetData("PictureBoxName", featureIcon.Name);                       // Chuyển tên

                                featureIcon.DoDragDrop(data, DragDropEffects.Move);

                                // Hide image
                                drappPictureBox.Visible = false;

                                this.show.AutoScrollPosition = new System.Drawing.Point(Math.Abs(this.show.AutoScrollPosition.X), Math.Abs(this.show.AutoScrollPosition.Y));
                            }
                        }
                    }
                }
            }
        }

        private void add_detail_file(string selectfilePath, bool allow_write)
        {
            try
            {
                if (File.Exists(selectfilePath))
                {
                    string extension = System.IO.Path.GetExtension(selectfilePath).ToLower();

                    Image videoFrame = null;
                    var mediaInfo = new MediaInfo.DotNetWrapper.MediaInfo();
                    mediaInfo.Open(selectfilePath);

                    string fileName = System.IO.Path.GetFileName(selectfilePath);
                    String typeFile = "Video";
                    String resolution = mediaInfo.Get(StreamKind.Video, 0, "Width") + "*" + mediaInfo.Get(StreamKind.Video, 0, "Height");

                    String duration = "00:00:00";

                    // Is a video
                    if (extension == ".mp4" || extension == ".avi" ||
                        extension == ".wmv" || extension == ".mpg" ||
                        extension == ".rmvp" || extension == ".mov" ||
                        extension == ".dat" || extension == ".flv")
                    {
                        try
                        {
                            if (double.Parse(mediaInfo.Get(StreamKind.General, 0, "Duration")) > 0)
                            {
                                // Convert duration to TimeSpan
                                TimeSpan durationTimeSpan = TimeSpan.FromMilliseconds(double.Parse(mediaInfo.Get(StreamKind.General, 0, "Duration")));
                                duration = $"{(int)durationTimeSpan.TotalHours:D2}:{durationTimeSpan.Minutes:D2}:{durationTimeSpan.Seconds:D2}";
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"{e}");
                            return;
                        }
                    }
                    else if (extension == ".jpg" || extension == ".bmp" ||
                             extension == ".png" || extension == ".gif")
                    {
                        typeFile = "Image";
                    }
                    else
                    {
                        return;
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

                    if (typeFile == "Video")
                    {
                        // Load the video file
                        Accord.Video.FFMPEG.VideoFileReader videoFileReader = new Accord.Video.FFMPEG.VideoFileReader();
                        videoFileReader.Open(selectfilePath);

                        // Get the first frame
                        videoFrame = videoFileReader.ReadVideoFrame();

                        // Close the video file reader
                        videoFileReader.Close();
                    }
                    else if (typeFile == "Image")
                    {
                        videoFrame = System.Drawing.Image.FromFile(selectfilePath);

                        resolution = System.Drawing.Image.FromFile(selectfilePath).Width.ToString() + "*" + System.Drawing.Image.FromFile(selectfilePath).Height.ToString();
                    }

                    if (allow_write)
                    {
                        // Lưu thông tin vào tệp tin
                        using (StreamWriter sw = new StreamWriter("material.data", true))
                        {
                            sw.WriteLine(selectfilePath);
                        }
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
                    pictureBox.MouseUp += row_file_MouseUp;
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
                    device_name_label.MouseUp += row_file_MouseUp;
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
                    type_label.MouseUp += row_file_MouseUp;
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
                    resolution_label.MouseUp += row_file_MouseUp;
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
                    duration_label.MouseUp += row_file_MouseUp;
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
                    path_label.MouseUp += row_file_MouseUp;
                    path_label.MouseDown += PictureBox_MouseDown;
                    addTablePanel.Controls.Add(path_label, 5, 0);

                    row_file.Controls.Add(addTablePanel);
                    this.panel46.Controls.Add(row_file);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e}");
            }
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
            foreach (Control control in this.panel6.Controls)
            {
                if ((control != this.panel34) && (control != this.panel33) && (control.BackColor == System.Drawing.Color.SteelBlue))
                {
                    setting_form popup = new setting_form();
                    popup.ConfirmClick += (sender1, e1) =>
                    {
                        if (int.TryParse(e1.width_real, out int width_real) && int.TryParse(e1.height_real, out int height_real) &&
                            int.TryParse(e1.width_resolution, out int width_resolution) && int.TryParse(e1.height_resolution, out int height_resolution))
                        {
                            reinit(true);

                            // Get the maximum allowable width and height based on the mainPanel's size
                            int width_contain = this.show.Width;
                            int height_contain = this.show.Height;
                            if ((width_real != width_resolution) || (height_real != height_resolution))
                            {
                                width_contain = this.panel43.Width;
                                height_contain = this.panel43.Height;
                            }
                            float delta = (float)width_real / (float)height_real;
                            float width_config = 0;
                            float height_config = 0;
                            do
                            {
                                height_config += 1;
                                width_config += delta;
                            }
                            while ((width_config < (width_contain - 50)) && (height_config < (height_contain - 50)));

                            // Calculate the position to center the inner panel within the main panel
                            int x = (this.panel43.Width - (int)width_config) / 2;
                            int y = (this.panel43.Height - (int)height_config) / 2;

                            var infoProgram = new
                            {
                                name                = e1.name,
                                width_resolution    = e1.width_resolution,
                                height_resolution   = e1.height_resolution,
                                width_real          = e1.width_real,
                                height_real         = e1.height_real,
                                bittrate_select     = e1.bittrate_select,
                                x_area              = x,
                                y_area              = y,
                                width_area          = (int) width_config,
                                height_area         = (int) height_config
                            };

                            // Create the inner panel based on the adjusted width and height
                            Panel innerPanel = new Panel
                            {
                                Name        = JsonConvert.SerializeObject(infoProgram),
                                Dock        = DockStyle.Fill,
                                BackColor   = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54))))),
                                AllowDrop   = true
                            };
                            
                            // Đăng ký sự kiện DragDrop và DragEnter cho Panel
                            innerPanel.DragDrop  += TargetPanel_DragDrop;
                            innerPanel.DragEnter += TargetPanel_DragEnter;
                            innerPanel.DragOver  += Target_DragOver;
                            innerPanel.MouseDown += (sender2, e2) =>
                            {
                                unselect_object();
                            };
                            // Xóa tất cả các sự kiện Paint từ innerPanel
                            innerPanel.ClearPaintEventHandlers();
                            innerPanel.Paint += (sender2, e2) =>
                            {
                                // Lấy đối tượng Graphics từ sự kiện Paint
                                Graphics g = e2.Graphics;

                                // Tạo một Brush màu đen
                                using (SolidBrush brush = new SolidBrush(Color.Black))
                                {
                                    // Vẽ hình chữ nhật đen giữa panel
                                    g.FillRectangle(brush, x, y, width_config, height_config);
                                }
                            };

                            this.show.AutoScrollPosition = new System.Drawing.Point(x - ((this.show.Width - ((int)width_config + 30)) / 2), y - ((this.show.Height - ((int)height_config + 30)) / 2));

                            // Add the inner panel to the main panel
                            this.panel43.Controls.Add(innerPanel);

                            // Update program info
                            control.Name = JsonConvert.SerializeObject(infoProgram);

                            // Edit info program tab
                            foreach (Control child in control.Controls)
                            {
                                foreach (Control item in child.Controls)
                                {
                                    if (child.Controls.IndexOf(item) == 0)
                                    {
                                        item.Text = $"{width_real}(W) x {height_real}(H)";
                                    }                                       
                                    else if (child.Controls.IndexOf(item) == 1)
                                    {
                                        item.Text = e1.name;
                                    }                                        
                                }
                                break;
                            }

                            // Edit info release tab
                            foreach (Control control1 in this.panel71.Controls)
                            {
                                if ((control1 != this.panel75) && (this.panel71.Controls.IndexOf(control1) == (this.panel6.Controls.IndexOf(control) - 1)))
                                {
                                    foreach (Control child in control1.Controls)
                                    {
                                        foreach (Control item in child.Controls)
                                        {
                                            if (child.Controls.IndexOf(item) == 0)
                                            {
                                                item.Text = $"{width_real}(W) x {height_real}(H)";
                                            }
                                            else if (child.Controls.IndexOf(item) == 1)
                                            {
                                                item.Text = e1.name;
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    };

                    var info_program = JsonConvert.DeserializeObject<Info_Program>(control.Name);

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
            int idxSelect = -1;

            // Get index program select;
            foreach (Control control in this.panel6.Controls)
            {
                if ((control != this.panel34) && (control != this.panel33) && (control.BackColor == System.Drawing.Color.SteelBlue))
                {
                    foreach (Control chill in control.Controls)
                    {
                        if (control.Controls.IndexOf(chill) == 2)
                        {
                            foreach (Control item in chill.Controls)
                            {
                                idxSelect = int.Parse(item.Text);

                                // Remove layout and data
                                this.panel71.Controls.RemoveAt((this.panel6.Controls.IndexOf(control) - 1));
                                this.panel6.Controls.Remove(control);
                                controlsListSelect[idxSelect].Clear();
                                controlsListSelect[idxSelect] = null;

                                do
                                {
                                    --currentIdxList;
                                }
                                while (controlsListSelect[currentIdxList] == null);

                                // Is empty list
                                bool flagEmpty = true;
                                for (int i = 1; i < controlsListSelect.Length; i++)
                                {
                                    if (controlsListSelect[i] != null)
                                    {
                                        flagEmpty = false;
                                    }
                                }

                                if (flagEmpty)
                                {
                                    // Clear counter
                                    countProgram = 0;
                                }

                                reinit(true);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void reinit(Boolean flagIgnore)
        {
            foreach (Control control1 in controlsListSelect[currentIdxList])
            {
                ResizablePanel panel_windown = control1 as ResizablePanel;

                if (panel_windown.updateTimer != null)
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

            if (!flagIgnore)
            {
                // Clear counter
                countProgram = 0;

                // Clear data windown of program
                for (int i = 1; i < controlsListSelect.Length; i++)
                {
                    if (controlsListSelect[i] != null)
                    {
                        controlsListSelect[i].Clear();
                        controlsListSelect[i] = null;
                    }
                }
            }

            this.list_windowns.Controls.Clear();
            this.panel43.Controls.Clear();

            this.panel70.Visible = false;
            this.panel70.Name = "";
            this.panel80.Visible = false;

            if (!flagIgnore)
            {
                List<Control> controlsToRemove = new List<Control>();

                // Clear List program
                foreach (Control control in this.panel6.Controls)
                {
                    if ((control != this.panel34) && (control != this.panel33))
                    {
                        controlsToRemove.Add(control);
                    }
                }
                foreach (Control control in controlsToRemove)
                {
                    this.panel6.Controls.Remove(control);
                }

                controlsToRemove.Clear();

                foreach (Control control in this.panel71.Controls)
                {
                    if (control != this.panel75)
                    {
                        controlsToRemove.Add(control);
                    }
                }
                foreach (Control control in controlsToRemove)
                {
                    this.panel71.Controls.Remove(control);
                }
            }

            currentScale = 1.0f;
        }

        private void refresh_program_design(float scale)
        {
            // Resize child panels in program list (panel43)
            var visiblePanels = this.panel43.Controls
                .OfType<Panel>()
                .Where(panel => panel.Visible);

            foreach (var panel_chill in visiblePanels)
            {
                var info_program = JsonConvert.DeserializeObject<Info_Program>(panel_chill.Name);
                int widthK1 = info_program.width_area;
                int heightK1 = info_program.height_area;

                // Get the maximum allowable width and height based on the mainPanel's size
                int width_contain = this.show.Width;
                int height_contain = this.show.Height;
                if ((int.Parse(info_program.width_real) != int.Parse(info_program.width_resolution)) || (int.Parse(info_program.height_real) != int.Parse(info_program.height_resolution)))
                {
                    width_contain = this.panel43.Width;
                    height_contain = this.panel43.Height;
                }
                float delta = (float)int.Parse(info_program.width_real) / (float)int.Parse(info_program.height_real);
                float width_config = 0;
                float height_config = 0;
                do
                {
                    height_config += 1;
                    width_config += delta;
                }
                while ((width_config < (width_contain - 50)) && (height_config < (height_contain - 50)));

                // Calculate the position to center the inner panel within the main panel
                int x = (this.panel43.Width - (int)width_config) / 2;
                int y = (this.panel43.Height - (int)height_config) / 2;

                // Xóa tất cả các sự kiện Paint từ innerPanel
                panel_chill.ClearPaintEventHandlers();
                panel_chill.Paint += (sender2, e2) =>
                {
                    // Lấy đối tượng Graphics từ sự kiện Paint
                    Graphics g = e2.Graphics;

                    // Tạo một Brush màu đen
                    using (SolidBrush brush = new SolidBrush(Color.Black))
                    {
                        // Vẽ hình chữ nhật đen giữa panel
                        g.FillRectangle(brush, x, y, (int)(width_config * scale), (int)(height_config * scale));
                    }
                };

                // Re-Paint
                panel_chill.Refresh();

                foreach (Control control1 in controlsListSelect[currentIdxList])
                {                   
                    // Resize 
                    control1.Left   = (int)Math.Round(Normalize(control1.Location.X - info_program.x_area, 0, widthK1, 0, (int)(width_config * scale)) + x);
                    control1.Top    = (int)Math.Round(Normalize(control1.Location.Y - info_program.y_area, 0, heightK1, 0, (int)(height_config * scale)) + y);
                    control1.Width  = (int)Math.Round(Normalize(control1.Width, 0, widthK1, 0, (int)(width_config * scale)));
                    control1.Height = (int)Math.Round(Normalize(control1.Height, 0, heightK1, 0, (int)(height_config * scale)));
                }

                this.show.AutoScrollPosition = new System.Drawing.Point(x - ((this.show.Width - ((int)width_config + 30)) / 2), y - ((this.show.Height - ((int)height_config + 30)) / 2));

                // Update info
                info_program.x_area = x;
                info_program.y_area = y;
                info_program.width_area = (int)(width_config * scale);
                info_program.height_area = (int)(height_config * scale);

                // Update info in panel
                panel_chill.Name = JsonConvert.SerializeObject(info_program);

            }

            unselect_object();
        }

        private void button_function(object sender, EventArgs e)
        {
            if ((sender as Button).Name.Equals("Save"))
            {
                List<Info_stored> info_stored = new List<Info_stored>();

                // Get index program select;
                foreach (Control control in this.panel6.Controls)
                {
                    if ((control != this.panel34) && (control != this.panel33))
                    {
                        foreach (Control chill in control.Controls)
                        {
                            if (control.Controls.IndexOf(chill) == 2)
                            {
                                foreach (Control item in chill.Controls)
                                {
                                    var info_program = JsonConvert.DeserializeObject<Info_Program>(control.Name);
                                    List<Info_Window> info_windown = new List<Info_Window>();

                                    foreach (Control control1 in controlsListSelect[int.Parse(item.Text)])
                                    {
                                        ResizablePanel panel_windown = control1 as ResizablePanel;

                                        info_windown.Add(JsonConvert.DeserializeObject<Info_Window>(panel_windown.Name));
                                    }
                                    info_windown.Reverse();

                                    var save_info = new
                                    {
                                        info_program = info_program,
                                        info_windown = info_windown
                                    };

                                    info_stored.Add(JsonConvert.DeserializeObject<Info_stored>(JsonConvert.SerializeObject(save_info, Formatting.None)));
                                }
                            }
                        }
                    }
                }

                // Have data to Store
                if (info_stored.Count > 0)
                {
                    info_stored.Reverse();

                    // Open a file dialog to select the output file path
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "PROGRAM files (*.data)|*.data|All files (*.*)|*.*";
                    saveFileDialog.Title = "Save PROGRAM File";
                    saveFileDialog.FileName = $"Program_{DateTime.Now.ToString("dd_MM_yyyy")}";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Save the program string to the selected file path
                        string filePath = saveFileDialog.FileName;
                        File.WriteAllText(filePath, JsonConvert.SerializeObject(info_stored, Formatting.None));
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
                        List<Info_stored> info_stored = JsonConvert.DeserializeObject<List<Info_stored>>(File.ReadAllText(filePath));

                        // Set up show area
                        reinit(false);

                        foreach (Info_stored program in info_stored)
                        {
                            this.panel43.Controls.Clear();

                            // Get the maximum allowable width and height based on the mainPanel's size
                            int width_contain = this.show.Width;
                            int height_contain = this.show.Height;
                            if ((int.Parse(program.info_program.width_real) != int.Parse(program.info_program.width_resolution)) || (int.Parse(program.info_program.height_real) != int.Parse(program.info_program.height_resolution)))
                            {
                                width_contain = this.panel43.Width;
                                height_contain = this.panel43.Height;
                            }
                            float delta = (float)int.Parse(program.info_program.width_real) / (float)int.Parse(program.info_program.height_real);
                            float width_config = 0;
                            float height_config = 0;
                            do
                            {
                                height_config += 1;
                                width_config += delta;
                            }
                            while ((width_config < (width_contain - 50)) && (height_config < (height_contain - 70)));

                            // Calculate the position to center the inner panel within the main panel
                            int x = (this.panel43.Width - (int)width_config) / 2;
                            int y = (this.panel43.Height - (int)height_config) / 2;

                            var infoProgram = new
                            {
                                name                = program.info_program.Name,
                                width_resolution    = program.info_program.width_resolution,
                                height_resolution   = program.info_program.height_resolution,
                                width_real          = program.info_program.width_real,
                                height_real         = program.info_program.height_real,
                                bittrate_select     = program.info_program.bittrate_select,
                                x_area              = x,
                                y_area              = y,
                                width_area          = (int) width_config,
                                height_area         = (int) height_config
                            };

                            // Create the inner panel based on the adjusted width and height
                            Panel innerPanel = new Panel
                            {
                                Name        = JsonConvert.SerializeObject(infoProgram),
                                Dock        = DockStyle.Fill,
                                BackColor   = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54))))),
                                AllowDrop   = true
                            };

                            // Đăng ký sự kiện DragDrop và DragEnter cho Panel
                            innerPanel.DragDrop  += TargetPanel_DragDrop;
                            innerPanel.DragEnter += TargetPanel_DragEnter;
                            innerPanel.DragOver  += Target_DragOver;
                            innerPanel.MouseDown += (sender2, e2) =>
                            {
                                unselect_object();
                            };

                            // Xóa tất cả các sự kiện Paint từ innerPanel
                            innerPanel.ClearPaintEventHandlers();
                            innerPanel.Paint += (sender2, e2) =>
                            {
                                // Lấy đối tượng Graphics từ sự kiện Paint
                                Graphics g = e2.Graphics;

                                // Tạo một Brush màu đen
                                using (SolidBrush brush = new SolidBrush(Color.Black))
                                {
                                    // Vẽ hình chữ nhật đen giữa panel
                                    g.FillRectangle(brush, x, y, width_config, height_config);
                                }
                            };

                            this.show.AutoScrollPosition = new System.Drawing.Point(x - ((this.show.Width - ((int)width_config + 30)) / 2), y - ((this.show.Height - ((int)height_config + 30)) / 2));

                            // Add the inner panel to the main panel
                            this.panel43.Controls.Add(innerPanel);

                            // Create list program
                            add_layout_program(this.panel6, program.info_program.Name, int.Parse(program.info_program.width_real), int.Parse(program.info_program.height_real), JsonConvert.SerializeObject(infoProgram));
                            add_layout_program(this.panel71, program.info_program.Name, int.Parse(program.info_program.width_real), int.Parse(program.info_program.height_real), null);

                            var visiblePanels = this.panel43.Controls
                                .OfType<Panel>()
                                .Where(panel => panel.Visible);

                            foreach (var destinationPanel in visiblePanels)
                            {
                                var info_program = JsonConvert.DeserializeObject<Info_Program>(destinationPanel.Name);

                                // Draw windows
                                for (int idx_window = 0; idx_window < program.info_windown.Count; idx_window++)
                                {
                                    for (int idx_item = 0; idx_item < program.info_windown[idx_window].list.Count; idx_item++)
                                    {
                                        // Get the object name from the data
                                        int lenght_list = idx_window + 1;
                                        string objectName = program.info_windown[idx_window].list[idx_item];
                                        string[] list_object = { objectName };
                                        bool[] list_selected = { false };

                                        int max_app_width   = info_program.width_area;
                                        int max_app_height  = info_program.height_area;
                                        int X               = (int)Math.Round(Normalize(program.info_windown[idx_window].windown_left, 0, int.Parse(program.info_program.width_real), 0, max_app_width)) + info_program.x_area;
                                        int Y               = (int)Math.Round(Normalize(program.info_windown[idx_window].windown_top, 0, int.Parse(program.info_program.height_real), 0, max_app_height)) + info_program.y_area;
                                        int width_windown   = (int)Math.Round(Normalize(program.info_windown[idx_window].windown_width, 0, int.Parse(program.info_program.width_real), 0, max_app_width));
                                        int height_windown  = (int)Math.Round(Normalize(program.info_windown[idx_window].windown_height, 0, int.Parse(program.info_program.height_real), 0, max_app_height));

                                        var info_windown = new
                                        {
                                            name            = "Windown " + lenght_list,
                                            path_windown    = "",
                                            windown_height  = program.info_windown[idx_window].windown_height,
                                            windown_width   = program.info_windown[idx_window].windown_width,
                                            windown_top     = program.info_windown[idx_window].windown_top,
                                            windown_left    = program.info_windown[idx_window].windown_left,
                                            list            = list_object,
                                            list_url        = program.info_windown[idx_window].list_url,
                                            list_duration   = program.info_windown[idx_window].list_duration,
                                            list_entrytime  = program.info_windown[idx_window].list_entrytime,
                                            selected        = list_selected
                                        };

                                        ResizablePanel windown_load = null;
                                        if (idx_item == 0)
                                        {
                                            windown_load = new ResizablePanel(destinationPanel)
                                            {
                                                Location    = new Point(X, Y),
                                                Size        = new Size(width_windown, height_windown),
                                                BackColor   = Color.Transparent,
                                                Name        = JsonConvert.SerializeObject(info_windown),
                                                AllowDrop   = true
                                            };

                                            windown_load.CustomEventMouseMove += (sender1, e1, X1, Y1, app_width, app_height, active_select, info_other_panel, direction) =>
                                            {
                                                // Resize child panels in program list (panel43)
                                                var visiblePanels1 = this.panel43.Controls
                                                    .OfType<Panel>()
                                                    .Where(panel => panel.Visible);

                                                foreach (var showPanel in visiblePanels1)
                                                {
                                                    // Deserialize JSON data from the Name property
                                                    Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(windown_load.Name);

                                                    this.panel70.Visible = true;
                                                    this.panel70.Name = infoWindow.Name;

                                                    this.textBox1.Text = Math.Round(Normalize(X1, 0, info_program.width_area, 0, int.Parse(program.info_program.width_real))).ToString();
                                                    this.textBox2.Text = Math.Round(Normalize(Y1, 0, info_program.height_area, 0, int.Parse(program.info_program.height_real))).ToString();
                                                    if (active_select)
                                                    {
                                                        this.textBox4.Text = Math.Round(Normalize(app_width, 0, info_program.width_area, 0, int.Parse(program.info_program.width_real))).ToString();
                                                        this.textBox3.Text = Math.Round(Normalize(app_height, 0, info_program.height_area, 0, int.Parse(program.info_program.height_real))).ToString();
                                                    }

                                                    if (direction == 1)
                                                    {
                                                        Info_Window infoOtherWindow = JsonConvert.DeserializeObject<Info_Window>(info_other_panel);
                                                        this.textBox1.Text = (infoOtherWindow.windown_left + infoOtherWindow.windown_width).ToString();
                                                    }
                                                    else if (direction == 3)
                                                    {
                                                        Info_Window infoOtherWindow = JsonConvert.DeserializeObject<Info_Window>(info_other_panel);
                                                        this.textBox2.Text = (infoOtherWindow.windown_top + infoOtherWindow.windown_height).ToString();
                                                    }

                                                    // Select first item
                                                    foreach (Control control1 in controlsListSelect[currentIdxList])
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
                                                }
                                            };

                                            windown_load.CustomEventMouseDown += (sender1, e1, X1, Y1, app_width, app_height, active_select, info_other_panel, direction) =>
                                            {
                                                // Resize child panels in program list (panel43)
                                                var visiblePanels1 = this.panel43.Controls
                                                    .OfType<Panel>()
                                                    .Where(panel => panel.Visible);

                                                foreach (var showPanel in visiblePanels1)
                                                {
                                                    // Deserialize JSON data from the Name property
                                                    Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(windown_load.Name);

                                                    // Select first item
                                                    foreach (Control control1 in controlsListSelect[currentIdxList])
                                                    {
                                                        if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                                        {
                                                            // Deserialize JSON data from the Name property
                                                            Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);


                                                            if (infoWindow1.Name.Equals(infoWindow.Name))
                                                            {
                                                                this.panel70.Visible = true;
                                                                this.panel70.Name  = infoWindow.Name;

                                                                this.textBox1.Text = infoWindow1.windown_left.ToString();
                                                                this.textBox2.Text = infoWindow1.windown_top.ToString();
                                                                this.textBox4.Text = infoWindow1.windown_width.ToString();
                                                                this.textBox3.Text = infoWindow1.windown_height.ToString();
                                                            }

                                                            resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                                                        }
                                                    }

                                                    if (!active_select)
                                                    {
                                                        return;
                                                    }

                                                    // Check again
                                                    if ((int.Parse(this.textBox1.Text) + int.Parse(this.textBox4.Text)) > int.Parse(program.info_program.width_real))
                                                    {
                                                        this.textBox4.Text = (int.Parse(program.info_program.width_real) - int.Parse(this.textBox1.Text)).ToString();
                                                    }

                                                    if ((int.Parse(this.textBox2.Text) + int.Parse(this.textBox3.Text)) > int.Parse(program.info_program.height_real))
                                                    {
                                                        this.textBox3.Text = (int.Parse(program.info_program.height_real) - int.Parse(this.textBox2.Text)).ToString();
                                                    }

                                                    // Select first item
                                                    foreach (Control control1 in controlsListSelect[currentIdxList])
                                                    {
                                                        if (control1 is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                                                        {
                                                            // Deserialize JSON data from the Name property
                                                            Info_Window infoWindow1 = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);
                                                            infoWindow1.selectedMaster = false;

                                                            for (int i = 0; i < infoWindow1.selected.Count; i++)
                                                            {
                                                                infoWindow1.selected[i] = false;
                                                            }

                                                            if (infoWindow1.Name.Equals(infoWindow.Name))
                                                            {
                                                                if (infoWindow1.selected.Count > 0)
                                                                {
                                                                    infoWindow1.selected[0] = true;
                                                                }

                                                                // update detai location
                                                                infoWindow1.windown_width = int.Parse(this.textBox4.Text);
                                                                infoWindow1.windown_height = int.Parse(this.textBox3.Text);
                                                                infoWindow1.windown_top = int.Parse(this.textBox2.Text);
                                                                infoWindow1.windown_left = int.Parse(this.textBox1.Text);
                                                            }

                                                            resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);
                                                        }
                                                    }

                                                    foreach (Control control2 in this.list_windowns.Controls)
                                                    {
                                                        if (control2.Name != null)
                                                            control2.Refresh();
                                                    }

                                                    windown_load.InitializeResizeHandles();
                                                }
                                            };

                                            windown_load.CustomEventDragDrop += (sender1, e1) =>
                                            {
                                                // Unselect all
                                                foreach (Control control in controlsListSelect[currentIdxList])
                                                {
                                                    if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                                    {
                                                        // Deserialize JSON data from the Name property
                                                        Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);
                                                        infoWindow.selectedMaster = false;

                                                        for (int i = 0; i < infoWindow.selected.Count; i++)
                                                        {
                                                            infoWindow.selected[i] = false;
                                                        }

                                                        resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);

                                                    }
                                                }

                                                // Update controlsListSelect[currentIdxList]
                                                foreach (Control control in controlsListSelect[currentIdxList])
                                                {
                                                    if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                                    {
                                                        // Deserialize JSON data from the Name property
                                                        Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                                                        if (infoWindow.Name.Equals("Windown " + lenght_list.ToString()))
                                                        {
                                                            String name_file = e1.Data.GetData("PictureBoxName") as string;
                                                            string extension1 = System.IO.Path.GetExtension(name_file).ToLower();
                                                            this.name_select.Text = " " + System.IO.Path.GetFileNameWithoutExtension(name_file);

                                                            if (name_file.Equals("Webpage"))
                                                            {
                                                                infoWindow.list_url.Add("toantrungcloud.com");

                                                                infoWindow.list_entrytime.Add("0");
                                                                infoWindow.list_duration.Add("10");
                                                            }
                                                            else if (name_file.Equals("Text"))
                                                            {
                                                                // Data not use
                                                                infoWindow.list_url.Add("");

                                                                infoWindow.list_entrytime.Add("0");
                                                                infoWindow.list_duration.Add("10");
                                                            }
                                                            else
                                                            {
                                                                // Data not use
                                                                infoWindow.list_url.Add("");

                                                                // Is a video
                                                                if (extension1 == ".jpg" || extension1 == ".bmp" || extension1 == ".png" || extension1 == ".gif")
                                                                {
                                                                    infoWindow.list_entrytime.Add("0");
                                                                    infoWindow.list_duration.Add("10");
                                                                }
                                                                else
                                                                {
                                                                    var flag_error = true;
                                                                    var mediaInfo = new MediaInfo.DotNetWrapper.MediaInfo();
                                                                    mediaInfo.Open(name_file);

                                                                    if (double.Parse(mediaInfo.Get(StreamKind.General, 0, "Duration")) > 0)
                                                                    {
                                                                        double durationMilliseconds = double.Parse(mediaInfo.Get(StreamKind.General, 0, "Duration"));

                                                                        flag_error = false;
                                                                        infoWindow.list_entrytime.Add("0");
                                                                        infoWindow.list_duration.Add(durationMilliseconds.ToString());
                                                                    }

                                                                    if (flag_error)
                                                                    {
                                                                        flag_error = false;
                                                                        infoWindow.list_entrytime.Add("");
                                                                        infoWindow.list_duration.Add("");
                                                                    }
                                                                }
                                                            }

                                                            infoWindow.list.Add(name_file);
                                                            infoWindow.selected.Add(true);
                                                            resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                                                        }
                                                    }
                                                }

                                                // Draw windown list
                                                draw_list_windown(controlsListSelect[currentIdxList]);
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
                                            if (File.Exists(objectName))
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
                                            if (File.Exists(objectName))
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
                                            if (File.Exists(objectName))
                                                windown_load.updateTimer.Start();

                                            controlsListSelect[currentIdxList].Insert(0, windown_load);
                                            destinationPanel.Controls.AddRange(controlsListSelect[currentIdxList].ToArray());
                                        }
                                        else
                                        {
                                            // Update controlsListSelect[currentIdxList]
                                            foreach (Control control in controlsListSelect[currentIdxList])
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
                                draw_list_windown(controlsListSelect[currentIdxList]);
                                unselect_object();
                            }
                        }
                    }
                    else
                    {
                        // Active message
                        notify_form popup = new notify_form(false);
                        popup.set_message("The file is not in the correct format");
                        popup.ShowDialog();
                    }

                }
            }
            else
            {
                int cntIgnore = 0;

                foreach (Control control1 in controlsListSelect[currentIdxList])
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
                                break;
                            }
                        }

                        // None object select
                        if (!infoWindow1.selectedMaster && idx_select < 0)
                        {
                            cntIgnore++;
                            continue;
                        }
                            
                        if ((sender as Button).Name.Equals("Up"))
                        {
                            if (idx_select > 0)
                            {
                                infoWindow1.path_windown = "";
                                string data_temp;
                                Control firstControl = null;
                                Control secondControl = null;

                                // Switch data
                                data_temp = infoWindow1.list[idx_select];
                                infoWindow1.list[idx_select] = infoWindow1.list[idx_select - 1];
                                infoWindow1.list[idx_select - 1] = data_temp;

                                // Switch data
                                data_temp = infoWindow1.list_duration[idx_select];
                                infoWindow1.list_duration[idx_select] = infoWindow1.list_duration[idx_select - 1];
                                infoWindow1.list_duration[idx_select - 1] = data_temp;

                                // Switch data
                                data_temp = infoWindow1.list_entrytime[idx_select];
                                infoWindow1.list_entrytime[idx_select] = infoWindow1.list_entrytime[idx_select - 1];
                                infoWindow1.list_entrytime[idx_select - 1] = data_temp;

                                infoWindow1.selected[idx_select] = false;
                                infoWindow1.selected[idx_select - 1] = true;
                                resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);

                                // Change layout
                                foreach (Control control2 in this.list_windowns.Controls)
                                {
                                    if ((control2.Controls.Count == 1) && (cntIgnore > 0))
                                    {
                                        cntIgnore--;
                                        continue;
                                    }

                                    if (cntIgnore > 0)
                                        continue;

                                    //Console.WriteLine($"{cntIgnore} {control2.Name} {idx_select}");
                                    if ((control2.Name.Length > 0) && (int.Parse(control2.Name) == idx_select))
                                    {
                                        firstControl = control2;
                                    }
                                    else if (firstControl != null)
                                    {
                                        secondControl = control2;

                                        // Switch data
                                        String temp_data = firstControl.Name;
                                        firstControl.Name = secondControl.Name;
                                        secondControl.Name = temp_data;
                                        temp_data = null;

                                        break;
                                    }
                                }

                                if (firstControl != null && secondControl != null)
                                {
                                    int firstIndex = this.list_windowns.Controls.GetChildIndex(firstControl);
                                    int secondIndex = this.list_windowns.Controls.GetChildIndex(secondControl);

                                    this.list_windowns.Controls.SetChildIndex(firstControl, secondIndex);
                                    this.list_windowns.Controls.SetChildIndex(secondControl, firstIndex);
                                }

                                data_temp = null;
                                firstControl = null;
                                secondControl = null;
                            }
                        }
                        else if ((sender as Button).Name.Equals("Down"))
                        {
                            if ((0 <= idx_select) && (idx_select < (infoWindow1.selected.Count - 1)))
                            {
                                infoWindow1.path_windown = "";
                                string data_temp;
                                Control firstControl = null;
                                Control secondControl = null;

                                // Switch data
                                data_temp = infoWindow1.list[idx_select];
                                infoWindow1.list[idx_select] = infoWindow1.list[idx_select + 1];
                                infoWindow1.list[idx_select + 1] = data_temp;

                                // Switch data
                                data_temp = infoWindow1.list_duration[idx_select];
                                infoWindow1.list_duration[idx_select] = infoWindow1.list_duration[idx_select + 1];
                                infoWindow1.list_duration[idx_select + 1] = data_temp;

                                // Switch data
                                data_temp = infoWindow1.list_entrytime[idx_select];
                                infoWindow1.list_entrytime[idx_select] = infoWindow1.list_entrytime[idx_select + 1];
                                infoWindow1.list_entrytime[idx_select + 1] = data_temp;

                                infoWindow1.selected[idx_select] = false;
                                infoWindow1.selected[idx_select + 1] = true;
                                resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);

                                // Change layout
                                foreach (Control control2 in this.list_windowns.Controls)
                                {
                                    if ((control2.Controls.Count == 1) && (cntIgnore > 0))
                                    {
                                        cntIgnore--;
                                        continue;
                                    }

                                    if (cntIgnore > 0)
                                        continue;

                                    if ((control2.Name.Length > 0) && (int.Parse(control2.Name) == (idx_select + 1)))
                                    {
                                        firstControl = control2;
                                    }
                                    else if (firstControl != null)
                                    {
                                        secondControl = control2;

                                        // Switch data
                                        String temp_data = firstControl.Name;
                                        firstControl.Name = secondControl.Name;
                                        secondControl.Name = temp_data;
                                        temp_data = null;

                                        break;
                                    }
                                }

                                if (firstControl != null && secondControl != null)
                                {
                                    int firstIndex = this.list_windowns.Controls.GetChildIndex(firstControl);
                                    int secondIndex = this.list_windowns.Controls.GetChildIndex(secondControl);

                                    this.list_windowns.Controls.SetChildIndex(firstControl, secondIndex);
                                    this.list_windowns.Controls.SetChildIndex(secondControl, firstIndex);
                                }

                                data_temp = null;
                                firstControl = null;
                                secondControl = null;
                            }
                        }
                        else if ((sender as Button).Name.Equals("Delete"))
                        {
                            if (infoWindow1.selectedMaster)
                            {
                                controlsListSelect[currentIdxList].Remove(control1);

                                // Clear List program
                                List<Control> controlsToRemove = new List<Control>();

                                foreach (Control control2 in this.list_windowns.Controls)
                                {
                                    if ((control2.Controls.Count == 1) && (cntIgnore > 0))
                                    {
                                        cntIgnore--;
                                        continue;
                                    }

                                    if (cntIgnore > 0)
                                        continue;

                                    controlsToRemove.Add(control2);
                                    if (control2.Controls.Count == 1)
                                    {
                                        break;
                                    }
                                }
                                foreach (Control control2 in controlsToRemove)
                                {
                                    this.list_windowns.Controls.Remove(control2);
                                }
                                controlsToRemove.Clear();

                                var visiblePanels = this.panel43.Controls
                                    .OfType<Panel>()
                                    .Where(panel => panel.Visible);

                                foreach (var destinationPanel in visiblePanels)
                                {
                                    foreach (Control control in destinationPanel.Controls)
                                    {
                                        Info_Window infoWindow2 = JsonConvert.DeserializeObject<Info_Window>(control.Name);
                                        if (infoWindow2.Name ==  infoWindow1.Name)
                                        {
                                            destinationPanel.Controls.Remove(control);
                                            break;
                                        }                                            
                                    }
                                }

                                break;
                            }
                            else if (infoWindow1.selected[idx_select])
                            {
                                infoWindow1.list[idx_select] = "";
                                infoWindow1.list_duration[idx_select] = "";
                                infoWindow1.list_entrytime[idx_select] = "";
                                infoWindow1.selected[idx_select] = false;

                                infoWindow1.path_windown = "";

                                resizablePanel1.Name = JsonConvert.SerializeObject(infoWindow1);

                                // Change layout
                                foreach (Control control2 in this.list_windowns.Controls)
                                {
                                    if ((control2.Controls.Count == 1) && (cntIgnore > 0))
                                    {
                                        cntIgnore--;
                                        continue;
                                    }

                                    if (cntIgnore > 0)
                                        continue;

                                    if ((control2.Name.Length > 0) && (int.Parse(control2.Name) == idx_select))
                                    {
                                        this.list_windowns.Controls.Remove(control2);
                                        break;
                                    }
                                }

                                // Set blackImage background
                                if (resizablePanel1.updateTimer.Enabled)
                                {
                                    resizablePanel1.updateTimer.Stop();
                                }

                                foreach (Control control2 in resizablePanel1.Controls)
                                {
                                    if (control2 is PictureBox)
                                    {
                                        PictureBox pictureBox = control2 as PictureBox;

                                        if (pictureBox.Image != null)
                                        {
                                            pictureBox.Image.Dispose();
                                        }

                                        // Tạo một Bitmap với kích thước mong muốn
                                        int width = pictureBox.Width;
                                        int height = pictureBox.Height;
                                        Bitmap blackImage = new Bitmap(width, height);

                                        // Tô màu đen cho toàn bộ hình ảnh
                                        using (Graphics g = Graphics.FromImage(blackImage))
                                        {
                                            g.Clear(Color.Black);
                                        }

                                        // Đặt hình ảnh nền đen cho PictureBox
                                        pictureBox.Image = blackImage;

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void export_project(object sender, EventArgs e)
        {
            // Resize child panels in program list (panel43)
            var visiblePanels = this.panel43.Controls
                .OfType<Panel>()
                .Where(panel => panel.Visible);

            foreach (var panel_chill in visiblePanels)
            {
                export_form popup = new export_form();
                Boolean flag_Action = false;

                popup.ConfirmClick += (sender1, e1) =>
                {
                    flag_Action = true;
                };

                popup.ShowDialog();

                if (flag_Action)
                {
                    var save_info = new
                    {
                        root_path       = popup.textBox1.Text,
                        name_folder     = popup.textBox3.Text,
                        mode_usb        = popup.comboBox2.SelectedIndex,
                        info_program    = panel_chill.Name
                    };

                    process_form popup_process = new process_form();
                    popup_process.Name = JsonConvert.SerializeObject(save_info);

                    // Start a new thread for the dialog with parameters
                    Thread dialogThread = new Thread(new ParameterizedThreadStart(SendFileUSBThread));
                    dialogThread.Start(popup_process);

                    popup_process.ShowDialog();
                }
            }
        }

        private void SendFileUSBThread(object parameter)
        {
            //using (StreamWriter fileStream = new StreamWriter(outputPath))
            {
                //Console.SetOut(fileStream);

                const int bufferSize = 1024 * 1024;
                Boolean flag_succeed = true;

                process_form dialog = (process_form)parameter;
                var info_packet = JsonConvert.DeserializeObject<export_packet>(dialog.Name);

                String folderPath = Path.Combine(info_packet.root_path, info_packet.name_folder);
                String subFolderPath = Path.Combine(info_packet.root_path, info_packet.name_folder, "Material");

                // Delet old folder
                if (Directory.Exists(folderPath))
                    Directory.Delete(folderPath, true);

                for (int idx = 0; idx < 100; idx++)
                {
                    if (!Directory.Exists(folderPath))
                        break;
                    Thread.Sleep(100);
                }

                Directory.CreateDirectory(folderPath);
                Directory.CreateDirectory(subFolderPath);

                var info_program = JsonConvert.DeserializeObject<Info_Program>(info_packet.info_program);
                List<Info_Window> info_windown = new List<Info_Window>();
                long totalBytes = 0;
                long totalBytesRead = 0;

                long longestDuration = 0;
                int windown_left_expected = 0;
                int percentage = 0, percentageK1 = 0;
                int counter_windown_empty = 0;
                int entry_time = 0;
                int duration_time = 1;
                List<long> listDuration = new List<long>();
                bool flag_cancel = false;

                dialog.CloseClick += (sender, e) =>
                {
                    flag_cancel = true;
                };

                // Create folder output
                String outputBackgroundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", $"{info_program.Name}_{info_program.bittrate_select}");
                string backgroundFilePath = Path.Combine(outputBackgroundPath, $"Background_{info_program.Name}.mp4");
                string devideFilePath = Path.Combine(outputBackgroundPath, $"Divide_{info_program.Name}.mp4");
                string contentFilePath = Path.Combine(outputBackgroundPath, $"{info_program.Name}.mp4");

                // Convert video
                if (true && ((controlsListSelect[currentIdxList].Count > 1) || (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))))
                {
                    if (!Directory.Exists(outputBackgroundPath))
                    {
                        Directory.CreateDirectory(outputBackgroundPath);
                    }
                    else
                    {
                        if (File.Exists(backgroundFilePath) && File.Exists(contentFilePath))
                        {
                            File.Delete(backgroundFilePath);
                            File.Delete(contentFilePath);
                        }
                        else if (File.Exists(backgroundFilePath))
                        {
                            File.Delete(backgroundFilePath);
                        }
                        else if (File.Exists(contentFilePath))
                        {
                            File.Delete(contentFilePath);
                        }
                    }

                    // Step 1: get all path video in program list
                    foreach (Control control in controlsListSelect[currentIdxList])
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
                                if (extension == ".mp4" || extension == ".avi" ||
                                    extension == ".wmv" || extension == ".mpg" ||
                                    extension == ".rmvp" || extension == ".mov" ||
                                    extension == ".dat" || extension == ".flv")
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
                                            // Do nothing
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
                                    longestDurationWindown += (entry_time * 1000 + duration_time * 1000);
                                }
                            }

                            if (longestDuration < longestDurationWindown)
                            {
                                longestDuration = longestDurationWindown;
                            }

                            listDuration.Add(longestDurationWindown);
                        }
                    }

                    // Step 2: convert video
                    foreach (Control control in controlsListSelect[currentIdxList])
                    {
                        if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                        {
                            Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);
                            int idx_windown = controlsListSelect[currentIdxList].IndexOf(control);

                            if (idx_windown == 0)
                            {
                                using (Process process = new Process())
                                {
                                    process.StartInfo.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg.exe"; // Assuming "ffmpeg" is in the PATH
                                    process.StartInfo.Arguments = $"-y -f lavfi -i anullsrc=r=44100:cl=stereo -f lavfi -i color=c=black:s={info_program.width_real}x{info_program.height_real}:d=0.1 -t 0.1 -shortest -c:v libx264 -b:v {info_program.bittrate_select} -tune stillimage -c:a aac -b:a 192k -strict experimental \"{backgroundFilePath}\"";
                                    process.StartInfo.UseShellExecute = false;
                                    process.StartInfo.RedirectStandardOutput = true;
                                    process.StartInfo.RedirectStandardError = true;
                                    process.StartInfo.CreateNoWindow = true;

                                    process.OutputDataReceived += (sender, e) =>
                                    {
                                        // Do nothing
                                    };
                                    process.ErrorDataReceived += (sender, e) =>
                                    {
                                        // Do nothing
                                    };
                                    process.Start();
                                    process.BeginOutputReadLine();
                                    process.BeginErrorReadLine();
                                    process.WaitForExit();
                                }

                                if (!File.Exists(backgroundFilePath))
                                {
                                    return;
                                }
                            }

                            using (Process process = new Process())
                            {
                                // Init variable
                                String cmd_ffmpeg = "-y ";
                                String filter = "";
                                String overlay = "";
                                int width_check = (infoWindow.windown_width % 2) == 1 ? infoWindow.windown_width + 1 : infoWindow.windown_width;
                                int height_check = (infoWindow.windown_height % 2) == 1 ? infoWindow.windown_height + 1 : infoWindow.windown_height;

                                process.StartInfo.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg.exe"; // Assuming "ffmpeg" is in the PATH

                                for (int idx = 0; idx < infoWindow.list.Count; idx++)
                                {
                                    string extension = System.IO.Path.GetExtension(infoWindow.list[idx]).ToLower();


                                    if (idx == 0)
                                    {
                                        cmd_ffmpeg += ($"-i \"{backgroundFilePath}\" ");
                                    }

                                    // Is a video
                                    if (extension == ".mp4" || extension == ".avi" ||
                                        extension == ".wmv" || extension == ".mpg" ||
                                        extension == ".rmvp" || extension == ".mov" ||
                                        extension == ".dat" || extension == ".flv")
                                    {
                                        cmd_ffmpeg += ($"-i \"{infoWindow.list[idx]}\" ");
                                    }
                                    else
                                    {
                                        cmd_ffmpeg += ($"-framerate 25 -t {infoWindow.list_duration[idx]} -loop 1 -i \"{infoWindow.list[idx]}\" ");
                                    }
                                }

                                cmd_ffmpeg += "-f lavfi -t 0.1 -i anullsrc -filter_complex ";

                                for (int idx = 0; idx < infoWindow.list.Count; idx++)
                                {
                                    filter += ("[" + (idx + 1) + ":v]scale=" + width_check + ":" + height_check + ":flags=lanczos,setsar=1:1[vid" + (idx + 1) + "];");
                                    string extension = System.IO.Path.GetExtension(infoWindow.list[idx]).ToLower();
                                    if (extension == ".jpg" || extension == ".bmp" || extension == ".png" || extension == ".gif")
                                    {
                                        filter += ("color=c=black:s=" + width_check + "x" + height_check + ":d=" + infoWindow.list_entrytime[idx] + "[entry_time" + (idx + 1) + "];");
                                    }
                                }

                                int counterImage = 0;

                                for (int idx = 0; idx < infoWindow.list.Count; idx++)
                                {
                                    string extension = System.IO.Path.GetExtension(infoWindow.list[idx]).ToLower();
                                    if (extension == ".jpg" || extension == ".bmp" || extension == ".png" || extension == ".gif")
                                    {
                                        filter += ("[entry_time" + (idx + 1) + "]" + "[" + (infoWindow.list.Count + 1) + ":a]");
                                        filter += ("[vid" + (idx + 1) + "]" + "[" + (infoWindow.list.Count + 1) + ":a]");

                                        counterImage += 1;
                                    }
                                    else
                                    {
                                        if (HasAudioStream(infoWindow.list[idx]))
                                        {
                                            filter += ("[vid" + (idx + 1) + "][" + (idx + 1) + ":a]");
                                        }
                                        else
                                        {
                                            filter += ("[vid" + (idx + 1) + "][" + (infoWindow.list.Count + 1) + ":a]");
                                        }
                                    }
                                }
                                filter += ("concat=n=" + (infoWindow.list.Count + counterImage) + ":v=1:a=1:unsafe=1[windown" + (idx_windown + 1) + "];");

                                // Calculate "loop" for windown
                                int loop = 0;
                                if (longestDuration > listDuration[idx_windown])
                                {
                                    loop = (int)((longestDuration / listDuration[idx_windown]) - 1);
                                }
                                if ((longestDuration % listDuration[idx_windown]) > 0)
                                {
                                    loop++;
                                }

                                filter += ("[windown" + (idx_windown + 1) + "]" + "loop=" + loop + ":32767:0[looped_windown" + (idx_windown + 1) + "_timebase];");

                                // Add overlay video
                                if (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))
                                {
                                    if (windown_left_expected >= infoWindow.windown_left)
                                    {
                                        overlay += ("[0][looped_windown" + (idx_windown + 1) + "_timebase]overlay=" + infoWindow.windown_left + ":" + infoWindow.windown_top + "[output]");
                                        windown_left_expected = infoWindow.windown_width + infoWindow.windown_left;
                                    }
                                    else
                                    {
                                        overlay += ("[0][looped_windown" + (idx_windown + 1) + "_timebase]overlay=" + windown_left_expected + ":" + infoWindow.windown_top + "[output]");
                                        windown_left_expected += infoWindow.windown_width;
                                    }
                                }
                                else
                                {
                                    overlay += ("[0][looped_windown" + (idx_windown + 1) + "_timebase]overlay=" + infoWindow.windown_left + ":" + infoWindow.windown_top + "[output]");
                                }

                                cmd_ffmpeg += filter + overlay + " ";
                                cmd_ffmpeg += $"-map [output] -c:v libx264 -b:v {info_program.bittrate_select} -preset slow -tune film -t {(longestDuration / 1000) + 1} \"{contentFilePath}\"";
                                //Console.WriteLine(cmd_ffmpeg);
                                process.StartInfo.Arguments = cmd_ffmpeg;
                                process.StartInfo.UseShellExecute = false;
                                process.StartInfo.RedirectStandardOutput = true;
                                process.StartInfo.RedirectStandardError = true;
                                process.StartInfo.CreateNoWindow = true;

                                process.OutputDataReceived += (sender, e) =>
                                {
                                    // Do nothing
                                };
                                process.ErrorDataReceived += (sender, e) =>
                                {
                                    if (flag_cancel)
                                    {
                                        process.CancelErrorRead();
                                        process.Kill();
                                    }
                                    else
                                    {
                                        //Console.WriteLine(e.Data);
                                        if (!string.IsNullOrEmpty(e.Data))
                                        {
                                            // Variables for capturing duration
                                            string timeProcessPattern = @"time=([0-9:.]+)";
                                            Regex regex = new Regex(timeProcessPattern);

                                            // Search for duration pattern in the output
                                            Match match = regex.Match(e.Data);

                                            if (match.Success)
                                            {
                                                // Extract the matched duration
                                                string time_str = match.Groups[1].Value;

                                                double milliseconds = TimeSpan.Parse(time_str).TotalMilliseconds;

                                                percentage = percentageK1 + (int)((milliseconds * 100) / ((double)longestDuration * (controlsListSelect[currentIdxList].Count - counter_windown_empty)));

                                                // set process bar
                                                dialog.Invoke((MethodInvoker)delegate
                                                {
                                                    // Your UI update code
                                                    dialog.ProgressValue = percentage + 500;
                                                    dialog.progressBar1.Refresh();
                                                });

                                            }
                                        }
                                    }
                                };
                                process.Start();
                                process.BeginOutputReadLine();
                                process.BeginErrorReadLine();
                                process.WaitForExit();

                                percentageK1 += (int)((longestDuration * 100) / ((double)longestDuration * (controlsListSelect[currentIdxList].Count - counter_windown_empty)));


                                if (((idx_windown + 1) >= controlsListSelect[currentIdxList].Count) && File.Exists(backgroundFilePath))
                                {
                                    File.Delete(backgroundFilePath);
                                }
                                if (File.Exists(backgroundFilePath) && File.Exists(contentFilePath))
                                {
                                    File.Delete(backgroundFilePath);
                                    File.Move(contentFilePath, backgroundFilePath);
                                }
                            }
                        }
                    }

                    if (File.Exists(contentFilePath) && ((int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution)) || (int.Parse(info_program.height_real) > int.Parse(info_program.height_resolution))))
                    {
                        // Create background
                        using (Process process = new Process())
                        {
                            process.StartInfo.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg.exe";
                            if (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))
                                process.StartInfo.Arguments = $"-y -f lavfi -i color=c=black:s={int.Parse(info_program.width_resolution)}x{int.Parse(info_program.height_real) + 2}:d=0.1 -vf scale={int.Parse(info_program.width_resolution)}x{int.Parse(info_program.height_real) + 2} -t 0.1 \"{backgroundFilePath}\"";
                            else
                                process.StartInfo.Arguments = $"-y -f lavfi -i color=c=black:s={int.Parse(info_program.width_real) + 2}x{int.Parse(info_program.height_resolution)}:d=0.1 -vf scale={int.Parse(info_program.width_real) + 2}x{int.Parse(info_program.height_resolution)} -t 0.1 \"{backgroundFilePath}\"";
                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.RedirectStandardError = true;
                            process.StartInfo.CreateNoWindow = true;

                            process.OutputDataReceived += (sender, e) =>
                            {
                                // Do nothing
                            };
                            process.ErrorDataReceived += (sender, e) =>
                            {
                                // Do nothing
                            };
                            process.Start();
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();
                            process.WaitForExit();
                        }

                        if (!File.Exists(backgroundFilePath))
                        {
                            return;
                        }

                        // slip file
                        String filter = "";
                        int idx_area = 1;
                        using (Process process = new Process())
                        {
                            process.StartInfo.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg.exe";
                            process.StartInfo.Arguments = $"-y -i \"{contentFilePath}\" -i \"{backgroundFilePath}\" -filter_complex ";
                            if (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))
                            {
                                for (int step = 0; step < int.Parse(info_program.width_real); step += int.Parse(info_program.width_resolution))
                                {
                                    if ((step + int.Parse(info_program.width_resolution)) > int.Parse(info_program.width_real))
                                    {
                                        filter += ($"[0:v]crop={int.Parse(info_program.width_real) - step}:{int.Parse(info_program.height_real)}:{step}:0[raw_area{idx_area}];[1:v][raw_area{idx_area}]overlay=0:0[area{idx_area}];");
                                    }
                                    else
                                    {
                                        filter += ($"[0:v]crop={int.Parse(info_program.width_resolution)}:{int.Parse(info_program.height_real)}:{step}:0[raw_area{idx_area}];[1:v][raw_area{idx_area}]overlay=0:0[area{idx_area}];");
                                    }
                                    idx_area++;
                                }

                                for (int step = 0; step < (idx_area - 1); step++)
                                {
                                    filter += $"[area{step + 1}]";
                                }

                                filter += ($"vstack=inputs={idx_area - 1}[mux];[mux][0:a]concat=n=1:v=1:a=1:unsafe=1[out]");
                            }
                            else
                            {
                                // TODO
                            }
                            process.StartInfo.Arguments += $"{filter} -map [out] -c:v libx264 -b:v {info_program.bittrate_select} -preset slow -tune film \"{devideFilePath}\"";
                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.RedirectStandardError = true;
                            process.StartInfo.CreateNoWindow = true;

                            process.OutputDataReceived += (sender, e) =>
                            {
                                // Do nothing
                            };
                            process.ErrorDataReceived += (sender, e) =>
                            {
                                if (flag_cancel)
                                {
                                    process.CancelErrorRead();
                                    process.Kill();
                                }
                                else
                                {
                                    //Console.WriteLine(e.Data);
                                    if (!string.IsNullOrEmpty(e.Data))
                                    {
                                        // Variables for capturing duration
                                        string timeProcessPattern = @"time=([0-9:.]+)";
                                        Regex regex = new Regex(timeProcessPattern);

                                        // Search for duration pattern in the output
                                        Match match = regex.Match(e.Data);

                                        if (match.Success)
                                        {
                                            // Extract the matched duration
                                            string time_str = match.Groups[1].Value;

                                            double milliseconds = TimeSpan.Parse(time_str).TotalMilliseconds;

                                            percentage = percentageK1 + (int)((milliseconds * 100) / (2 * ((double)longestDuration * (controlsListSelect[currentIdxList].Count - counter_windown_empty))));

                                            // set process bar
                                            dialog.Invoke((MethodInvoker)delegate
                                            {
                                                // Your UI update code
                                                dialog.ProgressValue = percentage + 300;
                                                dialog.progressBar1.Refresh();
                                            });

                                        }
                                    }
                                }
                            };
                            process.Start();
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();
                            process.WaitForExit();

                            // Delete root video
                            if (File.Exists(contentFilePath))
                            {
                                File.Delete(contentFilePath);
                            }

                            // Delete background video
                            if (File.Exists(backgroundFilePath))
                            {
                                File.Delete(backgroundFilePath);
                            }

                            // Rename divide video
                            if (File.Exists(devideFilePath))
                            {
                                File.Move(devideFilePath, contentFilePath);
                            }
                        }
                    }
                }


                foreach (Control control1 in controlsListSelect[currentIdxList])
                {
                    ResizablePanel panel_windown = control1 as ResizablePanel;
                    Info_Window Info_Window = JsonConvert.DeserializeObject<Info_Window>(panel_windown.Name);
                    info_windown.Add(Info_Window);

                    if (true && ((controlsListSelect[currentIdxList].Count > 1) || (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))))
                    {
                        if (totalBytes == 0)
                        {
                            FileInfo fileInfo = new FileInfo(contentFilePath);
                            totalBytes = fileInfo.Length;
                        }
                    }
                    else
                    {
                        foreach (String filePath in Info_Window.list)
                        {
                            FileInfo fileInfo = new FileInfo(filePath);
                            if (fileInfo.Exists)
                            {
                                totalBytes += fileInfo.Length;
                            }
                        }
                    }
                }
                info_windown.Reverse();

                if (totalBytes > 0)
                {
                    if (true && ((controlsListSelect[currentIdxList].Count > 1) || (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))))
                    {
                        using (var sourceStream = new FileStream(contentFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true))
                        using (var destinationStream = new FileStream(Path.Combine(subFolderPath, Path.GetFileName(contentFilePath)), FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
                        {
                            var buffer = new byte[bufferSize];
                            int bytesRead;

                            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                destinationStream.Write(buffer, 0, bytesRead);

                                totalBytesRead += bytesRead;

                                if (!flag_cancel)
                                {
                                    dialog.Invoke((MethodInvoker)delegate
                                    {
                                        // Your UI update code
                                        dialog.ProgressValue = (int)((double)totalBytesRead / totalBytes * 100);
                                        dialog.progressBar1.Refresh();
                                    });
                                }
                                else
                                {
                                    break;
                                }

                            }
                        }
                    }
                    else
                    {
                        foreach (Control control1 in controlsListSelect[currentIdxList])
                        {
                            ResizablePanel panel_windown = control1 as ResizablePanel;
                            Info_Window Info_Window = JsonConvert.DeserializeObject<Info_Window>(panel_windown.Name);

                            foreach (String filePath in Info_Window.list)
                            {
                                using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true))
                                using (var destinationStream = new FileStream(Path.Combine(subFolderPath, Path.GetFileName(filePath)), FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
                                {
                                    var buffer = new byte[bufferSize];
                                    int bytesRead;

                                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        destinationStream.Write(buffer, 0, bytesRead);

                                        totalBytesRead += bytesRead;

                                        if (!flag_cancel)
                                        {
                                            dialog.Invoke((MethodInvoker)delegate
                                            {
                                                // Your UI update code
                                                dialog.ProgressValue = (int)((double)totalBytesRead / totalBytes * 100);
                                                dialog.progressBar1.Refresh();
                                            });
                                        }

                                    }
                                }
                            }
                        }
                    }
                }

                var save_info = new
                {
                    durationProgramConvert  = longestDuration,
                    sync_mode               = info_packet.mode_usb == 0 ? false:true,
                    info_program            = info_program,
                    info_windown            = info_windown
                };

                // Save the program string to the selected file path
                File.WriteAllText(Path.Combine(folderPath, "info.txt"), JsonConvert.SerializeObject(save_info, Formatting.None));

                if (!flag_cancel)
                {
                    dialog.Invoke((MethodInvoker)delegate
                    {
                        if (flag_succeed)
                        {
                            dialog.ProgressValue = 200;
                            dialog.progressBar1.Refresh();
                            Thread.Sleep(500);
                            dialog.Close();
                        }
                        else
                        {
                            dialog.ProgressValue = -1;
                            dialog.progressBar1.Refresh();
                        }
                    });
                }

            }
        }

        private void zoom_function(object sender, EventArgs e)
        {
            var visiblePanels = this.panel43.Controls
                .OfType<Panel>()
                .Where(panel => panel.Visible);

            foreach (var panel_chill in visiblePanels)
            {
                Button obj = sender as Button;
                if (obj.Name.Equals("ZoomIn"))
                {
                    currentScale += 0.1f;
                }
                else
                {
                    currentScale -= 0.1f;
                }
                currentScale = Math.Max(0.1f, Math.Min(3.0f, currentScale));

                refresh_program_design(currentScale);
            }
        }

        private void screen_function(object sender, EventArgs e)
        {
            Boolean flag_error = true;
            Button obj = sender as Button;

            // Find device select
            // Save password and session ID
            foreach (Control control in this.panel35.Controls)
            {
                if (control is Panel panel_chill)
                {
                    // Now, check if there is a TableLayoutPanel within panel_chill
                    TableLayoutPanel tableLayoutPanel = panel_chill.Controls.OfType<TableLayoutPanel>().FirstOrDefault();

                    if (tableLayoutPanel != null)
                    {
                        Info_device infoDevice = JsonConvert.DeserializeObject<Info_device>(tableLayoutPanel.Name);
                        if (infoDevice.selected)
                        {
                            flag_error = false;

                            if (obj.Name.Equals("brightness_button"))
                            {

                                TextBox edit = new TextBox();
                                edit.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                                edit.Name = "edit";
                                edit.Size = new System.Drawing.Size(50, 26);
                                edit.TabIndex = 0;
                                edit.Text = "0";
                                edit.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
                                edit.Top = this.panel36.Location.Y - 30;
                                edit.Left = this.panel36.Location.X + (this.panel36.Width - 500) / 2 + 30;
                                edit.Visible = false;
                                edit.MaxLength = 3;
                                edit.MouseEnter += (sender1, e1) =>
                                {
                                    edit.Name = "";
                                };
                                edit.MouseLeave += (sender1, e1) =>
                                {
                                    edit.Name = "edit";
                                };
                                edit.TextChanged += (sender1, e1) =>
                                {
                                    if (!edit.Visible)
                                        return;

                                    String value = int.Parse(edit.Text) >= 100 ? "100" : int.Parse(edit.Text) >= 0 ? edit.Text : "0";

                                    // Get params
                                    HttpWebRequest request_set_bright = (HttpWebRequest)WebRequest.Create($"http://{infoDevice.ip_address}:18080/setting/bright");
                                    request_set_bright.Method = "POST";
                                    request_set_bright.Headers.Add("Cookie", $"{infoDevice.session_id}");

                                    // Add parameters to the request body
                                    string postData = $"bright={value}";
                                    request_set_bright.ContentType = "application/x-www-form-urlencoded";
                                    request_set_bright.ContentLength = Encoding.UTF8.GetBytes(postData).Length;

                                    using (Stream dataStream = request_set_bright.GetRequestStream())
                                    {
                                        dataStream.Write(Encoding.UTF8.GetBytes(postData), 0, Encoding.UTF8.GetBytes(postData).Length);
                                    }

                                    using (HttpWebResponse response_set_bright = (HttpWebResponse)request_set_bright.GetResponse())
                                    {
                                        if (response_set_bright.StatusCode == HttpStatusCode.OK)
                                        {
                                            using (Stream responseStream = response_set_bright.GetResponseStream())
                                            {
                                                using (StreamReader reader = new StreamReader(responseStream))
                                                {
                                                    //Console.WriteLine(reader.ReadToEnd());
                                                }
                                            }
                                        }
                                    }
                                };
                                this.main_terminal.Controls.Add(edit);
                                edit.BringToFront();

                                // Get params
                                HttpWebRequest request_param = (HttpWebRequest)WebRequest.Create($"http://{infoDevice.ip_address}:18080/getScreenParams");
                                request_param.Method = "POST";
                                request_param.Headers.Add("Cookie", $"{infoDevice.session_id}");
                                using (HttpWebResponse response_signnal_input = (HttpWebResponse)request_param.GetResponse())
                                {
                                    if (response_signnal_input.StatusCode == HttpStatusCode.OK)
                                    {
                                        using (Stream responseStream = response_signnal_input.GetResponseStream())
                                        {
                                            using (StreamReader reader = new StreamReader(responseStream))
                                            {
                                                getScreenParams data_parse = JsonConvert.DeserializeObject<getScreenParams>(reader.ReadToEnd());

                                                edit.Text = data_parse.bright.ToString();
                                                edit.Visible = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else if (obj.Name.Equals("volume_button"))
                            {
                                TextBox edit = new TextBox();
                                edit.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                                edit.Name = "edit";
                                edit.Size = new System.Drawing.Size(50, 26);
                                edit.TabIndex = 0;
                                edit.Text = "0";
                                edit.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
                                edit.Top = this.panel36.Location.Y - 30;
                                edit.Left = this.panel36.Location.X + (this.panel36.Width - 500) / 2 + 130;
                                edit.Visible = false;
                                edit.MaxLength = 3;
                                edit.MouseEnter += (sender1, e1) =>
                                {
                                    edit.Name = "";
                                };
                                edit.MouseLeave += (sender1, e1) =>
                                {
                                    edit.Name = "edit";
                                };
                                edit.TextChanged += (sender1, e1) =>
                                {
                                    if (!edit.Visible)
                                        return;

                                    String value = int.Parse(edit.Text) >= 100 ? "100" : int.Parse(edit.Text) >= 0 ? edit.Text : "0";

                                    // Get params
                                    HttpWebRequest request_set_bright = (HttpWebRequest)WebRequest.Create($"http://{infoDevice.ip_address}:18080/setting/voice");
                                    request_set_bright.Method = "POST";
                                    request_set_bright.Headers.Add("Cookie", $"{infoDevice.session_id}");

                                    // Add parameters to the request body
                                    string postData = $"voice={float.Parse(value) / 100}";
                                    request_set_bright.ContentType = "application/x-www-form-urlencoded";
                                    request_set_bright.ContentLength = Encoding.UTF8.GetBytes(postData).Length;

                                    using (Stream dataStream = request_set_bright.GetRequestStream())
                                    {
                                        dataStream.Write(Encoding.UTF8.GetBytes(postData), 0, Encoding.UTF8.GetBytes(postData).Length);
                                    }

                                    using (HttpWebResponse response_set_bright = (HttpWebResponse)request_set_bright.GetResponse())
                                    {
                                        if (response_set_bright.StatusCode == HttpStatusCode.OK)
                                        {
                                            using (Stream responseStream = response_set_bright.GetResponseStream())
                                            {
                                                using (StreamReader reader = new StreamReader(responseStream))
                                                {
                                                    //Console.WriteLine(reader.ReadToEnd());
                                                }
                                            }
                                        }
                                    }
                                };

                                this.main_terminal.Controls.Add(edit);
                                edit.BringToFront();

                                // Get params
                                HttpWebRequest request_param = (HttpWebRequest)WebRequest.Create($"http://{infoDevice.ip_address}:18080/getScreenParams");
                                request_param.Method = "POST";
                                request_param.Headers.Add("Cookie", $"{infoDevice.session_id}");
                                using (HttpWebResponse response_signnal_input = (HttpWebResponse)request_param.GetResponse())
                                {
                                    if (response_signnal_input.StatusCode == HttpStatusCode.OK)
                                    {
                                        using (Stream responseStream = response_signnal_input.GetResponseStream())
                                        {
                                            using (StreamReader reader = new StreamReader(responseStream))
                                            {
                                                getScreenParams data_parse = JsonConvert.DeserializeObject<getScreenParams>(reader.ReadToEnd());

                                                edit.Text = (data_parse.voice * 100).ToString();
                                                edit.Visible = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else if ((obj.Name.Equals("open_button")) || (obj.Name.Equals("close_button")))
                            {
                                // Get params
                                HttpWebRequest request_set_bright = (HttpWebRequest)WebRequest.Create($"http://{infoDevice.ip_address}:18080/setting/screen");
                                request_set_bright.Method = "POST";
                                request_set_bright.Headers.Add("Cookie", $"{infoDevice.session_id}");

                                // Add parameters to the request body
                                string postData = $"screen=true";
                                if (obj.Name.Equals("close_button"))
                                    postData = $"screen=false";

                                request_set_bright.ContentType = "application/x-www-form-urlencoded";
                                request_set_bright.ContentLength = Encoding.UTF8.GetBytes(postData).Length;

                                using (Stream dataStream = request_set_bright.GetRequestStream())
                                {
                                    dataStream.Write(Encoding.UTF8.GetBytes(postData), 0, Encoding.UTF8.GetBytes(postData).Length);
                                }

                                using (HttpWebResponse response_set_bright = (HttpWebResponse)request_set_bright.GetResponse())
                                {
                                    if (response_set_bright.StatusCode == HttpStatusCode.OK)
                                    {
                                        using (Stream responseStream = response_set_bright.GetResponseStream())
                                        {
                                            using (StreamReader reader = new StreamReader(responseStream))
                                            {
                                                setting data_parse = JsonConvert.DeserializeObject<setting>(reader.ReadToEnd());

                                                if (data_parse.code == 200)
                                                {
                                                    if (!this.screen_status.Text.Equals("--"))
                                                    {
                                                        this.screen_status.Text = "Open";
                                                        this.screen_status.ForeColor = Color.Green;
                                                        if (obj.Name.Equals("close_button"))
                                                        {
                                                            this.screen_status.Text = "Close";
                                                            this.screen_status.ForeColor = Color.Red;

                                                            this.screenshort_label.Visible = true;
                                                            if (this.panel12.Controls.Count > 1)
                                                            {
                                                                this.panel12.Controls.RemoveAt(1);
                                                            }
                                                        }
                                                    }

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (obj.Name.Equals("restart_button"))
                            {
                                // Get params
                                HttpWebRequest request_param = (HttpWebRequest)WebRequest.Create($"http://{infoDevice.ip_address}:18080/reboot");
                                request_param.Method = "POST";
                                request_param.Headers.Add("Cookie", $"{infoDevice.session_id}");
                                using (HttpWebResponse response_signnal_input = (HttpWebResponse)request_param.GetResponse())
                                {
                                    if (response_signnal_input.StatusCode == HttpStatusCode.OK)
                                    {
                                        using (Stream responseStream = response_signnal_input.GetResponseStream())
                                        {
                                            using (StreamReader reader = new StreamReader(responseStream))
                                            {
                                                setting data_parse = JsonConvert.DeserializeObject<setting>(reader.ReadToEnd());

                                                if (data_parse.code == 200)
                                                {
                                                    string device_id = null;
                                                    int counter_device = 0;
                                                    foreach (Control control1 in this.panel35.Controls)
                                                    {
                                                        if (control1 is Panel panel_chill1)
                                                        {
                                                            counter_device++;

                                                            // Now, check if there is a TableLayoutPanel within panel_chill
                                                            TableLayoutPanel tableLayoutPanel1 = panel_chill1.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                                                            if (tableLayoutPanel1 != null)
                                                            {
                                                                Info_device infoDevice1 = JsonConvert.DeserializeObject<Info_device>(tableLayoutPanel1.Name);
                                                                if (infoDevice1.ip_address != null && infoDevice1.ip_address.Equals(infoDevice.ip_address))
                                                                {
                                                                    device_id = infoDevice1.deviceName;
                                                                    this.panel35.Controls.Remove(control1);
                                                                    counter_device--;
                                                                }
                                                            }
                                                        }
                                                    }

                                                    foreach (Control control1 in this.panel84.Controls)
                                                    {
                                                        if (control1 is Panel panel_chill1)
                                                        {
                                                            // Now, check if there is a TableLayoutPanel within panel_chill
                                                            TableLayoutPanel tableLayoutPanel1 = panel_chill1.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                                                            if (tableLayoutPanel1 != null && device_id != null && device_id.Length > 0)
                                                            {
                                                                this.panel84.Controls.Remove(control1);
                                                            }
                                                        }
                                                    }

                                                    if (device_id != null && device_id.Length > 0)
                                                    {
                                                        // Counter device
                                                        this.total_pc.Text = $"Total {counter_device}";
                                                        this.online_pc.Text = $"Total {counter_device}";

                                                        this.screenshort_label.Visible = true;
                                                        if (this.panel12.Controls.Count > 1)
                                                        {
                                                            this.panel12.Controls.RemoveAt(1);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (flag_error)
            {
                flag_error = false;

                // Active message
                notify_form popup = new notify_form(false);
                popup.set_message("No device is chosen");
                popup.ShowDialog();
            }
        }

        private void popup_Click(object sender, EventArgs e)
        {
            Button obj = sender as Button;

            // Find device select
            // Save password and session ID
            foreach (Control control in this.panel35.Controls)
            {
                if (control is Panel panel_chill)
                {
                    // Now, check if there is a TableLayoutPanel within panel_chill
                    TableLayoutPanel tableLayoutPanel = panel_chill.Controls.OfType<TableLayoutPanel>().FirstOrDefault();

                    if (tableLayoutPanel != null)
                    {
                        Info_device infoDevice = JsonConvert.DeserializeObject<Info_device>(tableLayoutPanel.Name);
                        if (infoDevice.selected)
                        {
                            byte[] buffer = new byte[10240 + 256];

                            if (obj.Name.Equals("usb_popup"))
                            {
                                TcpClient client = new TcpClient();
                                client.Connect(infoDevice.ip_address, 12345);
                                NetworkStream stream = client.GetStream();

                                usb_form popup = new usb_form();
                                popup.ConfirmClick += (sender1, e1) =>
                                {
                                    TcpClient client1 = new TcpClient();
                                    client1.Connect(infoDevice.ip_address, 12345);
                                    NetworkStream stream1 = client1.GetStream();

                                    var cmd_for_device1 = new
                                    {
                                        deviceName = infoDevice.deviceName,
                                        type = "SOCKET",
                                        command = "SET_NAME_FOLDER",
                                        value = popup.textBox1.Text
                                    };

                                    // Send the data
                                    byte[] byteArray1 = Encoding.UTF8.GetBytes((string)JsonConvert.SerializeObject(cmd_for_device1));
                                    Array.Clear(buffer, 0, buffer.Length);
                                    Array.Copy(byteArray1, buffer, byteArray1.Length);
                                    stream1.Write(buffer, 0, buffer.Length);
                                    stream1.Flush();

                                    stream1.Close();
                                    client1.Close();
                                };

                                var cmd_for_device = new
                                {
                                    deviceName = infoDevice.deviceName,
                                    type = "SOCKET",
                                    command = "GET_INFO",
                                    ip_address = infoDevice.ip_address.ToString()
                                };

                                // Send the data
                                byte[] byteArray = Encoding.UTF8.GetBytes((string)JsonConvert.SerializeObject(cmd_for_device));
                                Array.Clear(buffer, 0, buffer.Length);
                                Array.Copy(byteArray, buffer, byteArray.Length);
                                stream.Write(buffer, 0, buffer.Length);
                                stream.Flush();

                                for (int delay = 0; delay < 300; delay++)
                                {
                                    Thread.Sleep(10);

                                    if (stream.DataAvailable)
                                    {
                                        Array.Clear(buffer, 0, buffer.Length);
                                        stream.Read(buffer, 0, buffer.Length);
                                        Command_response_device response_device = JsonConvert.DeserializeObject<Command_response_device>(Encoding.UTF8.GetString(buffer));

                                        popup.textBox1.Text = response_device.name_folder;
                                        break;
                                    }
                                }

                                stream.Close();
                                client.Close();

                                if (popup.textBox1.Text.Length > 0)
                                    popup.ShowDialog();
                            }
                            else if (obj.Name.Equals("device_popup"))
                            {
                                TcpClient client = new TcpClient();
                                client.Connect(infoDevice.ip_address, 12345);
                                NetworkStream stream = client.GetStream();

                                device_form popup = new device_form();
                                popup.ConfirmClick += (sender1, e1) =>
                                {
                                    TcpClient client1 = new TcpClient();
                                    client1.Connect(infoDevice.ip_address, 12345);
                                    NetworkStream stream1 = client1.GetStream();

                                    var cmd_for_device1 = new
                                    {
                                        deviceName = infoDevice.deviceName,
                                        type = "SOCKET",
                                        command = "SET_RESOLUTION",
                                        width_screen = popup.textBox1.Text,
                                        height_screen = popup.textBox2.Text
                                    };

                                    // Send the data
                                    byte[] byteArray1 = Encoding.UTF8.GetBytes((string)JsonConvert.SerializeObject(cmd_for_device1));
                                    Array.Clear(buffer, 0, buffer.Length);
                                    Array.Copy(byteArray1, buffer, byteArray1.Length);
                                    stream1.Write(buffer, 0, buffer.Length);
                                    stream1.Flush();

                                    stream1.Close();
                                    client1.Close();

                                    // Remove label 
                                    string device_id = null;
                                    int counter_device = 0;
                                    foreach (Control control1 in this.panel35.Controls)
                                    {
                                        if (control1 is Panel panel_chill1)
                                        {
                                            counter_device++;
                                            // Now, check if there is a TableLayoutPanel within panel_chill
                                            TableLayoutPanel tableLayoutPanel1 = panel_chill1.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                                            if (tableLayoutPanel1 != null)
                                            {
                                                Info_device infoDevice1 = JsonConvert.DeserializeObject<Info_device>(tableLayoutPanel1.Name);
                                                if (infoDevice1.ip_address != null && infoDevice1.ip_address.Equals(infoDevice.ip_address))
                                                {
                                                    device_id = infoDevice1.deviceName;
                                                    this.panel35.Controls.Remove(control1);
                                                    counter_device--;
                                                }
                                            }
                                        }
                                    }

                                    foreach (Control control1 in this.panel84.Controls)
                                    {
                                        if (control1 is Panel panel_chill1)
                                        {
                                            // Now, check if there is a TableLayoutPanel within panel_chill
                                            TableLayoutPanel tableLayoutPanel1 = panel_chill1.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                                            if (tableLayoutPanel1 != null && device_id != null && device_id.Length > 0)
                                            {
                                                this.panel84.Controls.Remove(control1);
                                            }
                                        }
                                    }

                                    if (device_id != null && device_id.Length > 0)
                                    {
                                        // Counter device
                                        this.total_pc.Text = $"Total {counter_device}";
                                        this.online_pc.Text = $"Total {counter_device}";

                                        this.screenshort_label.Visible = true;
                                        if (this.panel12.Controls.Count > 1)
                                        {
                                            this.panel12.Controls.RemoveAt(1);
                                        }
                                    }
                                };

                                var cmd_for_device = new
                                {
                                    deviceName = infoDevice.deviceName,
                                    type = "SOCKET",
                                    command = "GET_INFO",
                                    ip_address = infoDevice.ip_address.ToString()
                                };

                                // Send the data
                                byte[] byteArray = Encoding.UTF8.GetBytes((string)JsonConvert.SerializeObject(cmd_for_device));
                                Array.Clear(buffer, 0, buffer.Length);
                                Array.Copy(byteArray, buffer, byteArray.Length);
                                stream.Write(buffer, 0, buffer.Length);
                                stream.Flush();

                                for (int delay = 0; delay < 300; delay++)
                                {
                                    Thread.Sleep(10);

                                    if (stream.DataAvailable)
                                    {
                                        Array.Clear(buffer, 0, buffer.Length);
                                        stream.Read(buffer, 0, buffer.Length);
                                        Command_response_device response_device = JsonConvert.DeserializeObject<Command_response_device>(Encoding.UTF8.GetString(buffer));

                                        popup.textBox1.Text = response_device.width;
                                        popup.textBox2.Text = response_device.height;
                                        break;
                                    }
                                }

                                stream.Close();
                                client.Close();

                                if (popup.textBox1.Text.Length > 0)
                                    popup.ShowDialog();
                            }
                        }
                    }
                }
            }
        }

        private void selectRow(object sender, EventArgs e)
        {
            Panel child = null;
            Panel row = null;

            // Select Object
            if (sender is PictureBox)
            {
                child = ((sender as PictureBox).Parent as Panel);
                row = child.Parent as Panel;
            }
            else if (sender is Label)
            {
                child = ((sender as Label).Parent as Panel);
                row = child.Parent as Panel;
            }
            else if (sender is Panel)
            {
                row = sender as Panel;
                if (row.Controls.Count < 3)
                    row = ((sender as Panel).Parent as Panel);
            }
            else if (sender is RadioButton)
            {
                child = ((sender as RadioButton).Parent as Panel);
                row = child.Parent as Panel;
            }

            // Exception case
            if (row == null)
                return;

            // Clean object select
            if ((row.BackColor != System.Drawing.Color.SteelBlue))
            {
                foreach (Control control in row.Parent.Controls)
                {
                    if ((control.BackColor == System.Drawing.Color.SteelBlue))
                    {
                        control.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                    }
                }

                row.BackColor = System.Drawing.Color.SteelBlue;
            }
        }

        private void DoubleClick_MasterRow(object sender, EventArgs e)
        {
            Panel child = null;
            Panel row = null;

            // active effect row
            selectRow(sender, e);

            // Select Object
            if (sender is PictureBox)
            {
                child = ((sender as PictureBox).Parent as Panel);
                row = child.Parent as Panel;
            }
            else if (sender is Label)
            {
                child = ((sender as Label).Parent as Panel);
                row = child.Parent as Panel;
            }
            else if (sender is Panel)
            {
                row = sender as Panel;
                if (row.Controls.Count < 3)
                    row = ((sender as Panel).Parent as Panel);
            }
            else if (sender is RadioButton)
            {
                child = ((sender as RadioButton).Parent as Panel);
                row = child.Parent as Panel;
            }

            // Exception case
            if (row == null)
                return;

            Panel destinationPanel = row.Parent as Panel;
            List<int> position = findPositionMasterRow(destinationPanel);
            if (position.Count > 0)
            {
                int master = position[0];
                int masterNext = position[1];
                int hide = position[2];

                PictureBox pictureBox = row.Controls[2].Controls[0] as PictureBox;
                if (pictureBox.Image != null)
                {
                    if (((master - masterNext) - hide) >= 2)
                    {
                        // Rotate the image by 270 degrees
                        pictureBox.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);

                        // Hide row
                        foreach (Control control in destinationPanel.Controls)
                        {
                            if ((masterNext < destinationPanel.Controls.IndexOf(control)) && (destinationPanel.Controls.IndexOf(control) < master))
                            {
                                control.Visible = false;
                            }
                        }
                    }
                    else if (((master - masterNext) - hide) == 1)
                    {
                        // Rotate the image by 90 degrees
                        pictureBox.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);

                        // UnHide row
                        foreach (Control control in destinationPanel.Controls)
                        {
                            if ((masterNext < destinationPanel.Controls.IndexOf(control)) && (destinationPanel.Controls.IndexOf(control) < master))
                            {
                                control.Visible = true;
                            }
                        }
                    }
                }

                // Refresh the PictureBox to display the updated image
                pictureBox.Refresh();
            }
        }

        private void DoubleClick_ClientRow(object sender, EventArgs e)
        {
            Panel child = null;
            Panel row = null;

            if (sender is Panel)
            {
                row = sender as Panel;
                if (row.Controls.Count < 4)
                    row = ((sender as Panel).Parent as Panel);
            }
            else if (sender is PictureBox)
            {
                child = ((sender as PictureBox).Parent as Panel);
                row = child.Parent as Panel;
            }
            else if (sender is Label)
            {
                child = ((sender as Label).Parent as Panel);
                row = child.Parent as Panel;
            }

            // Exception case
            if (row == null)
                return;

            List<String> list_program = new List<String>();

            // Get list program
            foreach (Control control in this.panel6.Controls)
            {
                foreach (Control child1 in control.Controls)
                {
                    if (control.Controls.IndexOf(child1) == 0)
                    {
                        foreach (Control item in child1.Controls)
                        {
                            if (child1.Controls.IndexOf(item) == 1)
                                list_program.Add(item.Text);
                        }
                    }
                }
            }

            if (row.Controls[1].Controls[0].Text == "Play times")
            {
                infoPlayTimes info = JsonConvert.DeserializeObject<infoPlayTimes>(row.Name);

                loop_form popup = new loop_form(list_program, info.program, info.value);
                popup.ConfirmClick += (sender1, e1) =>
                {
                    if (e1.program.Length > 0)
                    {
                        info.program = e1.program;
                        info.value = e1.value;

                        row.Controls[2].Controls[0].Text = info.program;
                        row.Controls[0].Controls[0].Text = info.value;

                        row.Name = JsonConvert.SerializeObject(info);
                    }
                };
                popup.ShowDialog();
            }
            else if (row.Controls[1].Controls[0].Text == "Play shows")
            {
                infoPlayShows info = JsonConvert.DeserializeObject<infoPlayShows>(row.Name);

                timing_loop popup = new timing_loop(list_program, info.startTime, info.endTime, info.loop, info.startDate, info.endDate, info.weeks);
                popup.ConfirmClick += (sender1, e1) =>
                {
                    if (e1.program.Length > 0)
                    {
                        info.program = e1.program;
                        info.startTime = e1.startTime;
                        info.endTime = e1.endTime;
                        info.loop = e1.loop;
                        info.startDate = e1.startDate;
                        info.endTime = e1.endTime;
                        info.weeks = e1.weeks;

                        row.Controls[2].Controls[0].Text = info.program;
                        row.Controls[0].Controls[0].Text = info.startTime + " - " + info.endTime;

                        row.Name = JsonConvert.SerializeObject(info);
                    }
                };
                popup.ShowDialog();
            }
            else if (row.Controls[1].Controls[0].Text == "Play instructions")
            {
                infoPlayInstructions info = JsonConvert.DeserializeObject<infoPlayInstructions>(row.Name);

                instruction_form popup = new instruction_form(list_program, info.program, info.value);
                popup.ConfirmClick += (sender1, e1) =>
                {
                    if (e1.program.Length > 0)
                    {
                        info.program = e1.program;
                        info.value = e1.value;

                        row.Controls[2].Controls[0].Text = info.program;
                        row.Controls[0].Controls[0].Text = info.value;

                        row.Name = JsonConvert.SerializeObject(info);
                    }
                };
                popup.ShowDialog();
            }
        }

        private List<int> findPositionMasterRow(Panel destinationPanel)
        {
            List<int> returnValue = new List<int>();

            int master = -1;
            int masterNext = -1;
            int hide = 0;
            int delta = 1;
            int counter = 0;

            // Find start poind
            foreach (Control control in destinationPanel.Controls)
            {
                if ((control.BackColor == System.Drawing.Color.SteelBlue))
                {
                    if (control.Controls.Count != 3)
                    {
                        // Need to find row master
                        delta = 1;
                        counter = 0;

                        do
                        {
                            foreach (Control control1 in destinationPanel.Controls)
                            {

                                if ((destinationPanel.Controls.IndexOf(control1) == (destinationPanel.Controls.IndexOf(control) + delta)) && (control1.Controls.Count == 3))
                                {
                                    master = destinationPanel.Controls.IndexOf(control1);
                                    break;
                                }
                            }
                            delta++;
                        }
                        while ((master == -1) && (counter++ < 50));
                    }
                    else
                    {
                        // Found row master
                        master = destinationPanel.Controls.IndexOf(control);
                    }
                }
            }

            // Need to find row master
            delta = 1;
            counter = 0;

            // Find end point
            do
            {
                if ((destinationPanel.Controls[master - delta].Controls.Count == 3) || (destinationPanel.Controls[master - delta].Controls.Count == 1))
                {
                    masterNext = master - delta;
                    break;
                }
                else if (!destinationPanel.Controls[master - delta].Visible)
                {
                    hide++;
                }

                delta++;
            }
            while ((masterNext == -1) && (counter++ < 50));

            returnValue.Add(master);
            returnValue.Add(masterNext);
            returnValue.Add(hide);
            // Console.WriteLine($"Final {master} {masterNext} {hide}");
            return returnValue;
        }
        private void process_button_advanced_list(Button obj)
        {
            String type = obj.Text;
            String mark_text = "";
            if (type == new_loop_button.Text)
            {
                mark_text = "Loop " + this.panel96.Controls.Count.ToString();
            }
            else if (type == new_timming_button.Text)
            {
                mark_text = "Timming play " + this.panel97.Controls.Count.ToString();
            }
            else if (type == new_command_button.Text)
            {
                mark_text = "Timming instruction " + this.panel98.Controls.Count.ToString();
            }

            // Tạo PictureBox cho biểu tượng (icon)
            PictureBox show = new PictureBox();
            show.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            show.Image = global::WindowsFormsApp.Properties.Resources.arrow;
            show.SizeMode = PictureBoxSizeMode.CenterImage; // Hiển thị hình ảnh theo trung tâm
            show.Dock = DockStyle.Fill;
            show.Name = mark_text;
            show.Visible = false;
            show.Click += DoubleClick_MasterRow;


            Panel P1 = new Panel();
            P1.Controls.Add(show);
            P1.Dock = System.Windows.Forms.DockStyle.Left;
            P1.Location = new System.Drawing.Point(0, 0);
            P1.Size = new System.Drawing.Size(16, 32);
            P1.TabIndex = 0;

            RadioButton radio = new RadioButton();
            radio.AutoSize = true;
            radio.Dock = System.Windows.Forms.DockStyle.Fill;
            radio.FlatAppearance.BorderSize = 0;
            radio.Location = new System.Drawing.Point(0, 0);
            radio.Margin = new System.Windows.Forms.Padding(13, 3, 3, 3);
            radio.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            radio.Size = new System.Drawing.Size(34, 32);
            radio.TabIndex = 0;
            radio.TabStop = true;
            radio.AutoCheck = false;
            radio.Checked = true;
            radio.UseVisualStyleBackColor = true;
            radio.Click += (sender, e) =>
            {
                // Select object
                (sender as RadioButton).Checked = !(sender as RadioButton).Checked;

                // active effect row
                selectRow(sender, e);
            };

            Panel P2 = new Panel();
            P2.Controls.Add(radio);
            P2.Dock = System.Windows.Forms.DockStyle.Left;
            P2.Location = new System.Drawing.Point(16, 0);
            P2.Size = new System.Drawing.Size(34, 32);
            P2.TabIndex = 1;


            // Tạo PictureBox cho biểu tượng (icon)
            PictureBox iconPictureBox = new PictureBox();
            iconPictureBox.Image = global::WindowsFormsApp.Properties.Resources.calendar; // Thiết lập hình ảnh từ resource của bạn
            iconPictureBox.SizeMode = PictureBoxSizeMode.CenterImage; // Hiển thị hình ảnh theo trung tâm
            iconPictureBox.Dock = DockStyle.Left;
            iconPictureBox.Size = new Size(32, 32); // Thiết lập kích thước của PictureBox
            iconPictureBox.Click += selectRow;

            // Tạo Label cho nhãn (label)
            Label label = new Label();
            label.Text = mark_text;
            label.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0); // Thiết lập font chữ
            label.ForeColor = Color.White; // Thiết lập màu chữ
            label.Dock = DockStyle.Fill; ; // Chỉnh sửa .
            label.TextAlign = ContentAlignment.MiddleLeft; // Canh giữa nội dung của Label
            label.Click += selectRow;
            label.MouseDoubleClick += DoubleClick_MasterRow;

            Panel P3 = new Panel();
            P3.Controls.Add(label);
            P3.Controls.Add(iconPictureBox);
            P3.Dock = System.Windows.Forms.DockStyle.Left;
            P3.ForeColor = System.Drawing.Color.White;
            P3.Location = new System.Drawing.Point(50, 0);
            P3.Size = new System.Drawing.Size(250, 32);
            P3.TabIndex = 2;

            Panel rowMaster = new Panel();
            rowMaster.Controls.Add(P3);
            rowMaster.Controls.Add(P2);
            rowMaster.Controls.Add(P1);
            rowMaster.Name = "row_master";
            rowMaster.Dock = System.Windows.Forms.DockStyle.Top;
            rowMaster.Location = new System.Drawing.Point(0, 59);
            rowMaster.Size = new System.Drawing.Size(694, 32);
            rowMaster.TabIndex = 5;
            rowMaster.BackColor = System.Drawing.Color.SteelBlue;
            rowMaster.Click += selectRow;
            rowMaster.MouseDoubleClick += DoubleClick_MasterRow;

            if (type == new_loop_button.Text)
            {
                // Only 1 type at a time
                if ((this.panel97.Controls.Count > 1) || (this.panel98.Controls.Count > 1))
                {
                    notify_form popup = new notify_form(true);
                    popup.set_message("Just one kind at once");
                    Boolean flagConfirm = false;
                    popup.ConfirmClick += (sender1, e1) =>
                    {
                        flagConfirm = true;
                    };

                    popup.ShowDialog();

                    if (flagConfirm)
                    {
                        List<Control> controlsToRemove = new List<Control>();

                        // Clean list
                        foreach (Control control in this.panel97.Controls)
                        {
                            if (this.panel97.Controls.IndexOf(control) > 0)
                                controlsToRemove.Add(control);
                        }
                        foreach (Control control in controlsToRemove)
                        {
                            this.panel97.Controls.Remove(control);
                        }
                        controlsToRemove.Clear();

                        // Clean list
                        foreach (Control control in this.panel98.Controls)
                        {
                            if (this.panel98.Controls.IndexOf(control) > 0)
                                controlsToRemove.Add(control);
                        }
                        foreach (Control control in controlsToRemove)
                        {
                            this.panel98.Controls.Remove(control);
                        }
                        controlsToRemove.Clear();
                    }
                    else
                        return;
                }
                else if (this.panel96.Controls.Count > 1)
                {
                    notify_form popup = new notify_form(false);
                    popup.set_message("Setting is now in it's maximum loop");
                    popup.ShowDialog();

                    return;
                }

                // Clean object select
                foreach (Control control in this.panel96.Controls)
                {
                    if ((control.BackColor == System.Drawing.Color.SteelBlue))
                    {
                        control.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                    }
                }

                this.panel96.Controls.Add(rowMaster);

                // Sử dụng SetChildIndex để đưa row lên đầu
                this.panel96.Controls.SetChildIndex(rowMaster, 1);
            }
            else if (type == new_timming_button.Text)
            {
                // Only 1 type at a time
                if ((this.panel96.Controls.Count > 1) || (this.panel98.Controls.Count > 1))
                {
                    notify_form popup = new notify_form(true);
                    popup.set_message("Just one kind at once");
                    Boolean flagConfirm = false;
                    popup.ConfirmClick += (sender1, e1) =>
                    {
                        flagConfirm = true;
                    };

                    popup.ShowDialog();

                    if (flagConfirm)
                    {
                        List<Control> controlsToRemove = new List<Control>();

                        // Clean list
                        foreach (Control control in this.panel96.Controls)
                        {
                            if (this.panel96.Controls.IndexOf(control) > 0)
                                controlsToRemove.Add(control);
                        }
                        foreach (Control control in controlsToRemove)
                        {
                            this.panel96.Controls.Remove(control);
                        }
                        controlsToRemove.Clear();

                        // Clean list
                        foreach (Control control in this.panel98.Controls)
                        {
                            if (this.panel98.Controls.IndexOf(control) > 0)
                                controlsToRemove.Add(control);
                        }
                        foreach (Control control in controlsToRemove)
                        {
                            this.panel98.Controls.Remove(control);
                        }
                        controlsToRemove.Clear();
                    }
                    else
                        return;
                }

                // Clean object select
                foreach (Control control in this.panel97.Controls)
                {
                    if ((control.BackColor == System.Drawing.Color.SteelBlue))
                    {
                        control.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                    }
                }

                this.panel97.Controls.Add(rowMaster);

                // Sử dụng SetChildIndex để đưa row lên đầu
                this.panel97.Controls.SetChildIndex(rowMaster, 1);
            }
            else if (type == new_command_button.Text)
            {
                // Only 1 type at a time
                if ((this.panel96.Controls.Count > 1) || (this.panel97.Controls.Count > 1))
                {
                    notify_form popup = new notify_form(true);
                    popup.set_message("Just one kind at once");
                    Boolean flagConfirm = false;
                    popup.ConfirmClick += (sender1, e1) =>
                    {
                        flagConfirm = true;
                    };

                    popup.ShowDialog();

                    if (flagConfirm)
                    {
                        List<Control> controlsToRemove = new List<Control>();

                        // Clean list
                        foreach (Control control in this.panel96.Controls)
                        {
                            if (this.panel96.Controls.IndexOf(control) > 0)
                                controlsToRemove.Add(control);
                        }
                        foreach (Control control in controlsToRemove)
                        {
                            this.panel96.Controls.Remove(control);
                        }
                        controlsToRemove.Clear();

                        // Clean list
                        foreach (Control control in this.panel97.Controls)
                        {
                            if (this.panel97.Controls.IndexOf(control) > 0)
                                controlsToRemove.Add(control);
                        }
                        foreach (Control control in controlsToRemove)
                        {
                            this.panel97.Controls.Remove(control);
                        }
                        controlsToRemove.Clear();
                    }
                    else
                        return;
                }
                else if (this.panel98.Controls.Count > 1)
                {
                    notify_form popup = new notify_form(false);
                    popup.set_message("Setting is now in it's maximum loop");
                    popup.ShowDialog();

                    return;
                }

                // Clean object select
                foreach (Control control in this.panel98.Controls)
                {
                    if ((control.BackColor == System.Drawing.Color.SteelBlue))
                    {
                        control.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                    }
                }

                this.panel98.Controls.Add(rowMaster);

                // Sử dụng SetChildIndex để đưa row lên đầu
                this.panel98.Controls.SetChildIndex(rowMaster, 1);
            }
        }

        private void new_loop_button_Click(object sender, EventArgs e)
        {
            process_button_advanced_list(sender as Button);
        }

        private void new_timming_button_Click(object sender, EventArgs e)
        {
            process_button_advanced_list(sender as Button);
        }

        private void new_command_button_Click(object sender, EventArgs e)
        {
            process_button_advanced_list(sender as Button);
        }

        private void add_layout_and_info(Panel destinationPanel, String infoDisplay, String infoBackup)
        {
            List<int> position = findPositionMasterRow(destinationPanel);

            if (position.Count > 0)
            {
                int master = position[0];
                int masterNext = position[1];
                
                // Convert data
                infoProgramFromPopup infoD = JsonConvert.DeserializeObject<infoProgramFromPopup>(infoDisplay);

                Label name_program = new Label();
                name_program.AutoSize = true;
                name_program.Dock = System.Windows.Forms.DockStyle.None;
                name_program.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                name_program.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                name_program.Location = new System.Drawing.Point(0, 0);
                name_program.TabIndex = 0;
                name_program.Text = infoD.program;
                name_program.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                name_program.Click += selectRow;
                name_program.MouseDoubleClick += DoubleClick_ClientRow;

                Label target = new Label();
                target.AutoSize = true;
                target.Dock = System.Windows.Forms.DockStyle.None;
                target.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                target.Location = new System.Drawing.Point(0, 0);
                target.TabIndex = 0;
                if (infoD.type == 1)
                    target.Text = "Play times";
                else if (infoD.type == 2)
                    target.Text = "Play shows";
                else if (infoD.type == 3)
                    target.Text = "Play instructions";
                else
                    target.Text = "Unknown1";
                target.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                target.Click += selectRow;
                target.MouseDoubleClick += DoubleClick_ClientRow;

                Label execution_time = new Label();
                execution_time.AutoSize = true;
                execution_time.Dock = System.Windows.Forms.DockStyle.None;
                execution_time.Location = new System.Drawing.Point(0, 0);
                execution_time.RightToLeft = System.Windows.Forms.RightToLeft.No;
                execution_time.TabIndex = 0;
                execution_time.Text = infoD.value;
                execution_time.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                execution_time.Click += selectRow;
                execution_time.MouseDoubleClick += DoubleClick_ClientRow;

                // Tạo PictureBox cho biểu tượng (icon)
                PictureBox iconPictureBox = new PictureBox();
                iconPictureBox.Image = global::WindowsFormsApp.Properties.Resources.program_icon1; // Thiết lập hình ảnh từ resource của bạn
                iconPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                iconPictureBox.Dock = DockStyle.Fill;
                iconPictureBox.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
                iconPictureBox.Click += selectRow;
                iconPictureBox.MouseDoubleClick += DoubleClick_ClientRow;

                Panel P0 = new Panel();
                P0.Dock = System.Windows.Forms.DockStyle.Left;
                P0.Location = new System.Drawing.Point(0, 0);
                P0.Size = new System.Drawing.Size(32, 32);
                P0.TabIndex = 0;
                P0.Click += selectRow;
                P0.MouseDoubleClick += DoubleClick_ClientRow;

                Panel P1 = new Panel();
                P1.Controls.Add(iconPictureBox);
                P1.Dock = System.Windows.Forms.DockStyle.Left;
                P1.Location = new System.Drawing.Point(54, 0);
                P1.Size = new System.Drawing.Size(32, 32);
                P1.TabIndex = 1;

                Panel P2 = new Panel();
                P2.Dock = System.Windows.Forms.DockStyle.Left;
                P2.Location = new System.Drawing.Point(129, 0);
                P2.Size = new System.Drawing.Size(194, 32);
                P2.TabIndex = 2;
                // Calculate the position to center the label in the panel
                name_program.Location = new System.Drawing.Point((P2.Width - name_program.PreferredWidth) / 2, (P2.Height - name_program.PreferredHeight) / 2);
                P2.Controls.Add(name_program);
                P2.Click += selectRow;
                P2.MouseDoubleClick += DoubleClick_ClientRow;

                Panel P3 = new Panel();
                P3.Dock = System.Windows.Forms.DockStyle.Left;
                P3.Location = new System.Drawing.Point(220, 0);
                P3.Size = new System.Drawing.Size(100, 32);
                P3.TabIndex = 3;
                // Calculate the position to center the label in the panel
                target.Location = new System.Drawing.Point((P3.Width - target.PreferredWidth) / 2, (P3.Height - target.PreferredHeight) / 2);
                P3.Controls.Add(target);
                P3.Click += selectRow;
                P3.MouseDoubleClick += DoubleClick_ClientRow;

                Panel P4 = new Panel();
                P4.Dock = System.Windows.Forms.DockStyle.Left;
                P4.Location = new System.Drawing.Point(308, 0);
                P4.Size = new System.Drawing.Size(150, 32);
                P4.TabIndex = 4;
                // Calculate the position to center the label in the panel
                execution_time.Location = new System.Drawing.Point((P4.Width - execution_time.PreferredWidth) / 2, (P4.Height - execution_time.PreferredHeight) / 2);
                P4.Controls.Add(execution_time);
                P4.Click += selectRow;
                P4.MouseDoubleClick += DoubleClick_ClientRow;

                Panel rowMaster = new Panel();
                rowMaster.Controls.Add(P4);
                rowMaster.Controls.Add(P3);
                rowMaster.Controls.Add(P2);
                rowMaster.Controls.Add(P1);
                rowMaster.Controls.Add(P0);
                rowMaster.Dock = System.Windows.Forms.DockStyle.Top;
                rowMaster.Location = new System.Drawing.Point(0, 60);
                rowMaster.Size = new System.Drawing.Size(694, 32);
                rowMaster.TabIndex = 3;
                rowMaster.BackColor = System.Drawing.Color.SteelBlue;
                rowMaster.Click += selectRow;
                rowMaster.MouseDoubleClick += DoubleClick_ClientRow;
                rowMaster.Name = infoBackup;

                // Clean object select
                foreach (Control control in destinationPanel.Controls)
                {
                    if ((control.BackColor == System.Drawing.Color.SteelBlue))
                    {
                        control.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
                    }
                }

                destinationPanel.Controls.Add(rowMaster);

                // Sử dụng SetChildIndex để đưa row lên đầu
                destinationPanel.Controls.SetChildIndex(rowMaster, masterNext + 1);

                // Active arrow icon in first row
                if (((master + 1) - masterNext) == 2)
                {
                    foreach (Control child in destinationPanel.Controls[master + 1].Controls)
                    {
                        if (destinationPanel.Controls[master + 1].Controls.IndexOf(child) == 2)
                        {
                            PictureBox pictureBox = child.Controls[0] as PictureBox;

                            if (pictureBox.Image != null)
                            {
                                pictureBox.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                pictureBox.Refresh();
                            }

                            pictureBox.Visible = true;
                        }
                    }
                }
            }
        }

        private void process_button_edit_list(Button obj)
        {
            List<String> list_program = new List<String>();

            // Get list program
            foreach (Control control in this.panel6.Controls)
            {
                foreach (Control child in control.Controls)
                {
                    if (control.Controls.IndexOf(child) == 0)
                    {
                        foreach (Control item in child.Controls)
                        {
                            if (child.Controls.IndexOf(item) == 1)
                                list_program.Add(item.Text);
                        }
                    }
                }
            }

            if (obj.Name == this.loop_edit.Name)
            {
                if (this.panel96.Controls.Count > 1)
                {
                    loop_form popup = new loop_form(list_program, null, null);
                    popup.ConfirmClick += (sender, e) =>
                    {
                        if (e.program.Length > 0)
                        {
                            add_layout_and_info(this.panel96, 
                                JsonConvert.SerializeObject(
                                new {
                                    type    = 1,
                                    program = e.program,
                                    value   = e.value,
                                }), 
                                JsonConvert.SerializeObject(
                                new
                                {
                                    type = 1,
                                    program = e.program,
                                    value = e.value,
                                }));
                        }
                    };
                    popup.ShowDialog();
                }
            }
            else if (obj.Name == this.timing_edit.Name)
            {
                if (this.panel97.Controls.Count > 1)
                {
                    timing_loop popup = new timing_loop(list_program, null, null, null, null, null, null);
                    popup.ConfirmClick += (sender, e) =>
                    {
                        if (e.program.Length > 0)
                        {
                            add_layout_and_info(this.panel97, 
                                JsonConvert.SerializeObject(
                                new
                                {
                                    type = 2,
                                    program = e.program,
                                    value = e.startTime + " - " + e.endTime,
                                }),
                                JsonConvert.SerializeObject(
                                new
                                {
                                    type = 2,
                                    program = e.program,
                                    startTime = e.startTime,
                                    endTime = e.endTime,
                                    loop = e.loop,
                                    startDate = e.startDate,
                                    endDate = e.endDate,
                                    weeks = e.weeks
                                }));
                        }
                    };
                    popup.ShowDialog();
                }
            }
            else if (obj.Name == this.command_edit.Name)
            {
                if (this.panel98.Controls.Count > 1)
                {
                    instruction_form popup = new instruction_form(list_program, null, null);
                    popup.ConfirmClick += (sender, e) =>
                    {
                        if (e.program.Length > 0)
                        {
                            add_layout_and_info(this.panel98,
                                JsonConvert.SerializeObject(
                                new
                                {
                                    type = 3,
                                    program = e.program,
                                    value = e.value,
                                }),
                                JsonConvert.SerializeObject(
                                new
                                {
                                    type = 3,
                                    program = e.program,
                                    value = e.value,
                                }));
                        }
                    };
                    popup.ShowDialog();
                }
            }
        }

        private void loop_edit_Click(object sender, EventArgs e)
        {
            process_button_edit_list(sender as Button);
        }

        private void timing_edit_Click(object sender, EventArgs e)
        {
            process_button_edit_list(sender as Button);
        }

        private void command_edit_Click(object sender, EventArgs e)
        {
            process_button_edit_list(sender as Button);
        }

        private void process_button_delete_list(Button obj)
        {
            Panel destinationPanel = null;

            if (obj.Name == this.loop_delete.Name)
            {
                destinationPanel = this.panel96;
            }
            else if (obj.Name == this.timing_delete.Name)
            {
                destinationPanel = this.panel97;
            }
            else if (obj.Name == this.command_delete.Name)
            {
                destinationPanel = this.panel98;
            }

            if (destinationPanel != null && destinationPanel.Controls.Count > 1)
            {
                // delete row
                foreach (Control control in destinationPanel.Controls)
                {
                    if ((control.BackColor == System.Drawing.Color.SteelBlue))
                    {
                        if (control.Controls.Count != 3)
                        {
                            destinationPanel.Controls[destinationPanel.Controls.IndexOf(control) + 1].BackColor = System.Drawing.Color.SteelBlue;
                            destinationPanel.Controls.Remove(control);
                        }
                        else
                        {
                            List<int> position = findPositionMasterRow(destinationPanel);
                            if (position.Count > 0)
                            {
                                if ((position[0] + 1) < destinationPanel.Controls.Count)
                                    destinationPanel.Controls[position[0] + 1].BackColor = System.Drawing.Color.SteelBlue;

                                List<Control> controlsToRemove = new List<Control>();

                                foreach (Control control1 in destinationPanel.Controls)
                                {
                                    if ((position[1] < destinationPanel.Controls.IndexOf(control1)) && (destinationPanel.Controls.IndexOf(control1) <= position[0]))
                                    {
                                        controlsToRemove.Add(control1);
                                    }
                                }
                                foreach (Control control1 in controlsToRemove)
                                {
                                    destinationPanel.Controls.Remove(control1);
                                }

                                controlsToRemove.Clear();
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void loop_delete_Click(object sender, EventArgs e)
        {
            process_button_delete_list(sender as Button);
        }

        private void timing_delete_Click(object sender, EventArgs e)
        {
            process_button_delete_list(sender as Button);
        }

        private void command_delete_Click(object sender, EventArgs e)
        {
            process_button_delete_list(sender as Button);
        }
    }

    public static class ControlExtensions
    {
        public static void ClearPaintEventHandlers(this Control control)
        {
            // Lấy trường EventPaint thông qua Reflection
            FieldInfo eventPaintField = typeof(Control).GetField("EventPaint", BindingFlags.Static | BindingFlags.NonPublic);
            if (eventPaintField == null)
            {
                throw new InvalidOperationException("Field 'EventPaint' not found in Control class.");
            }

            // Lấy giá trị của trường EventPaint từ control
            object eventPaintKey = eventPaintField.GetValue(control);

            // Sử dụng PropertyInfo để lấy EventHandlerList
            PropertyInfo eventsProperty = typeof(Control).GetProperty("Events", BindingFlags.NonPublic | BindingFlags.Instance);
            if (eventsProperty == null)
            {
                throw new InvalidOperationException("Property 'Events' not found in Control class.");
            }

            // Lấy EventHandlerList từ control
            EventHandlerList eventHandlerList = (EventHandlerList)eventsProperty.GetValue(control, null);

            // Xóa tất cả các sự kiện Paint đã đăng ký
            eventHandlerList.RemoveHandler(eventPaintKey, eventHandlerList[eventPaintKey]);
        }
    }
    public class export_packet
    {
        public string root_path { get; set; }
        public string name_folder { get; set; }
        public int mode_usb { get; set; }
        public string info_program { get; set; }
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
        public int x_area { get; set; }
        public int y_area { get; set; }
        public int width_area { get; set; }
        public int height_area { get; set; }
    }

    public class Info_Window
    {
        public string Name { get; set; }
        public bool selectedMaster { get; set; }
        public string path_windown { get; set; }
        public int windown_height { get; set; }
        public int windown_width { get; set; }
        public int windown_top { get; set; }
        public int windown_left { get; set; }
        public List<string> list { get; set; }
        public List<string> list_url { get; set; }
        public List<string> list_duration { get; set; }
        public List<string> list_entrytime { get; set; }
        public List<bool> selected { get; set; }
    }

    public class Info_stored
    {
        public Info_Program info_program { get; set; }
        public List<Info_Window> info_windown { get; set; }
    }

    public class Info_device
    {
        public string deviceName { get; set; }
        public Boolean selected { get; set; }
        public string password { get; set; }
        public string session_id { get; set; }
        public string ip_address { get; set; }
    }

    public class Command_device
    {
        public string deviceName { get; set; }
        public string type { get; set; }
        public string command { get; set; }
        public string ip_address { get; set; }
    }

    public class Command_response_device
    {
        public string command { get; set; }
        public string password { get; set; }
        public string width { get; set; }
        public string height { get; set; }
        public string name_folder { get; set; }
        public string UUID { get; set; }
        public string bright { get; set; }
        public string voice { get; set; }
    }

    public class getCurTime
    {
        public long curTimeMills { get; set; }
        public Boolean isInternetTime { get; set; }
        public String timeZoneId { get; set; }
        public int code { get; set; }
        public String message { get; set; }
    }

    public class getResolution
    {
        public int height { get; set; }
        public int hz { get; set; }
        public int width { get; set; }
        public int code { get; set; }
        public String message { get; set; }
    }

    public class getScreenParams
    {
        public int bright { get; set; }
        public int contrast { get; set; }
        public Boolean screenOn { get; set; }
        public int screenType { get; set; }
        public float voice { get; set; }
        public int code { get; set; }
        public String message { get; set; }
    }

    public class setting
    {
        public int code { get; set; }
        public String message { get; set; }
    }

    public class device
    {
        public int code { get; set; }
        public adv_packet data { get; set; }
    }

    public class sendDetailInfo
    {
        public String IP_client { get; set; }
        public int type { get; set; }
        public List<string> program_list { get; set; }
    }

    public class infoProgramFromPopup
    {
        public int type { get; set; }
        public string program { get; set; }
        public string value { get; set; }
    }

    public class infoPlayTimes
    {
        public int type { get; set; }
        public string program { get; set; }
        public string value { get; set; }
    }
    public class infoPlayShows
    {
        public int type { get; set; }
        public string program { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public string loop { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public List<string> weeks { get; set; }
    }

    public class infoPlayInstructions
    {
        public int type { get; set; }
        public string program { get; set; }
        public string value { get; set; }
    }

    public class loop_type
    {
        public string loop { get; set; }
        public string timeLoop { get; set; }
    }
}
