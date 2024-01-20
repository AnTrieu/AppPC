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
        private int width_panel = 0;
        private int height_panel = 0;
        private long current_time_box = 0;
        private long running_time_box = 0;
        private string device_select = "";

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
                this.panel6.Width = 350;
                this.panel14.Width = 150;
                this.panel40.Width = 300;

                this.show_file.Height = 250;

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
                this.panel40.Width = 260;

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
                            if(infoDevice.deviceName.Equals(label.Name))
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
                            if(infoDevice.deviceName.Equals(label.Name) && !infoDevice.selected)
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
                            if(infoDevice.deviceName.Equals(label.Name))
                            {
                                infoDevice.selected = true;
                                tableLayoutPanel.Name = JsonConvert.SerializeObject(infoDevice);

                                if(!device_select.Equals(infoDevice.deviceName))
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

            if(cmd_packet.type.Equals("SOCKET"))
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
                        Array.Clear(data,0, data.Length);
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
                                                    if(data_parse.screenOn)
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

                    foreach (Control control in controlsList)
                    {
                        if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                        {
                            Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                            if(infoWindow.Name.Equals(this.panel70.Name))
                            {
                                int left_expect = (int)Math.Round(Normalize(X, 0, int.Parse(info_program.width_real), 0, panel_chill.Width - 0));
                                int top_expect = (int)Math.Round(Normalize(Y, 0, int.Parse(info_program.height_real), 0, panel_chill.Height - 0));
                                int width_expect = (int)Math.Round(Normalize(width, 0, int.Parse(info_program.width_real), 0, panel_chill.Width - 0));
                                int height_expect = (int)Math.Round(Normalize(height, 0, int.Parse(info_program.height_real), 0, panel_chill.Height - 0));

                                if ((left_expect + width_expect) > panel_chill.Width - 0)
                                {                                  
                                    if (objInput.Name.Equals("textBox1"))
                                        X = int.Parse(info_program.width_real) - int.Parse(this.textBox4.Text) + 0;

                                    if (objInput.Name.Equals("textBox4"))
                                        width = int.Parse(info_program.width_real) - int.Parse(this.textBox1.Text) + 0;

                                    left_expect = (int)Math.Round(Normalize(X, 0, int.Parse(info_program.width_real), 0, panel_chill.Width - 0));
                                    width_expect = (int)Math.Round(Normalize(width, 0, int.Parse(info_program.width_real), 0, panel_chill.Width - 0));
                                }
                                if (objInput.Name.Equals("textBox1"))
                                    resizablePanel.Left = left_expect;
                                if(objInput.Name.Equals("textBox4"))
                                    resizablePanel.Width = width_expect;

                                if ((top_expect + height_expect) > (panel_chill.Height - 0))
                                {
                                    if (objInput.Name.Equals("textBox2"))
                                        Y = int.Parse(info_program.height_real) - int.Parse(this.textBox3.Text) + 0;
                                                                                                        
                                    if (objInput.Name.Equals("textBox3"))
                                        height = int.Parse(info_program.height_real) - int.Parse(this.textBox2.Text) + 0;
                                                                                                      
                                    top_expect = (int)Math.Round(Normalize(Y, 0, int.Parse(info_program.height_real), 0, panel_chill.Height - 0));
                                    height_expect = (int)Math.Round(Normalize(height, 0, int.Parse(info_program.height_real), 0, panel_chill.Height - 0));
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

                foreach (Control control in controlsList)
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

                foreach (Control control in controlsList)
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
            Timer timer = new Timer();
            timer.Interval = 100;
            // Hook up the Elapsed event for the timer
            timer.Tick += (sender1, e1) =>
            {
                if(current_time_box > 0)
                {
                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromFileTime(current_time_box * 10000);

                    this.current_time.Text = dateTimeOffset.ToString($"dd/MM/{DateTime.Now.Year} HH:mm:ss");

                    current_time_box += 100;
                }
                else
                {
                    this.current_time.Text = "--/--/-- --:--:--";
                }
            };

            // Start the timer
            timer.Start();
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

        private void SendFileThread(object parameter)
        {
            //using (StreamWriter fileStream = new StreamWriter(outputPath))
            {
                //Console.SetOut(fileStream);
                
                long total_size = 0;
                bool flag_cancel = false;

                process_form dialog = (process_form)parameter;
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
                    // Resize child panels in program list (panel43)
                    var visiblePanels = this.panel43.Controls
                        .OfType<Panel>();

                    foreach (var panel_chill in visiblePanels)
                    {
                        var info_program = JsonConvert.DeserializeObject<Info_Program>(panel_chill.Name);
                        var flag_convert = false;
                        long longestDuration = 0;

                        // Create folder output
                        String outputBackgroundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", $"{info_program.Name}_{info_program.bittrate_select}");
                        string backgroundFilePath = Path.Combine(outputBackgroundPath, $"Background_{info_program.Name}.mp4");
                        string devideFilePath = Path.Combine(outputBackgroundPath, $"Divide_{info_program.Name}.mp4");
                        string contentFilePath = Path.Combine(outputBackgroundPath, $"{info_program.Name}.mp4");
                        
                        // Convert video
                        if (true && ((controlsList.Count > 1) || (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))))
                        {
                            int windown_left_expected = 0;
                            int percentage = 0, percentageK1 = 0;
                            int counter_windown_empty = 0;
                            //int entry_time = 0;
                            //int duration_time = 1;
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

                            // Step 2: convert video
                            for (int i = controlsList.Count - 1; i >= 0; i--)
                            {
                                Control control = controlsList[i];
                                if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                                {
                                    //Console.WriteLine(resizablePanel.Name);
                                    Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);
                                    int idx_windown = controlsList.Count - i - 1;
                                    
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
                                        cmd_ffmpeg += $"-map [output] -c:v libx264 -b:v {info_program.bittrate_select} -preset slow -tune film -t {longestDuration / 1000} \"{contentFilePath}\"";
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
                                                            percentage = percentageK1 + (int)((milliseconds * 100) / ((double)longestDuration * (controlsList.Count - counter_windown_empty) * 2));
                                                        else
                                                            percentage = percentageK1 + (int)((milliseconds * 100) / ((double)longestDuration * (controlsList.Count - counter_windown_empty)));

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
                                            percentageK1 += (int)((longestDuration * 100) / ((double)longestDuration * (controlsList.Count - counter_windown_empty) * 2));
                                        else
                                            percentageK1 += (int)((longestDuration * 100) / ((double)longestDuration * (controlsList.Count - counter_windown_empty)));
                           
                                        if (((idx_windown + 1) >= controlsList.Count) && File.Exists(backgroundFilePath))
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

                                                    percentage = percentageK1 + (int)((milliseconds * 100) / ((double)longestDuration * (controlsList.Count - counter_windown_empty)));

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

                        // Carculator total size                          
                        if (!flag_convert && controlsList[0] is ResizablePanel resizablePanel1 && !string.IsNullOrEmpty(resizablePanel1.Name))
                        {
                            // Deserialize JSON data from the Name property
                            Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel1.Name);

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

                        if (total_size == 0)
                        {
                            MessageBox.Show("File error, please try again", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            clientSocket = new TcpClient();
                            clientSocket.Connect(IP_client, 12345);
                            
                            // Get the network stream for receiving data
                            networkStream = clientSocket.GetStream();

                            Boolean send_plan = false;
                            long sended_size = 0;
                            int percent = 0, percentK1 = -1;

                            // Send file                        
                            if (!flag_convert && controlsList[0] is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                            {
                                // Deserialize JSON data from the Name property
                                Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);

                                for (int idx = 0; idx < infoWindow.list.Count; idx++)
                                {
                                    using (FileStream receivedVideoFile = new FileStream(infoWindow.list[idx], FileMode.Open, FileAccess.Read))
                                    {
                                        int bytesRead = 0;
                                        int idxChuck = 0;

                                        long length_file = receivedVideoFile.Length;

                                        // Active send plan
                                        send_plan = true;

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
                                                //Console.WriteLine("-------------- " + Math.Max(bytesRead + 256, buffer.Length));
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
                                // Active send plan
                                send_plan = true;

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

                            // Send plan for device
                            if (send_plan)
                            {
                                send_plan = false;

                                List<Info_Window> info_windown = new List<Info_Window>();
                                foreach (Control control in controlsList)
                                {
                                    info_windown.Add(JsonConvert.DeserializeObject<Info_Window>((control as ResizablePanel).Name));
                                    //Console.WriteLine((control as ResizablePanel).Name);
                                }
                                
                                var detailPacket = new
                                {
                                    command = "SEND_PLAN",
                                    durationProgramConvert = longestDuration,
                                    info_program = JsonConvert.DeserializeObject<Info_Program>(panel_chill.Name),
                                    info_windown = info_windown
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
                            }
                        }
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
                        int width_contain = this.show.Width;
                        int height_contain = this.show.Height;
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
                        while ((width_config < (width_contain - 50)) && (height_config < (height_contain - 50)));

                        width_panel = (int)width_config;
                        height_panel = (int)height_config;

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
                        int x = (this.panel43.Width - (int)width_config) / 2;
                        int y = (this.panel43.Height - (int)height_config) / 2;

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

                        this.show.AutoScrollPosition = new System.Drawing.Point(x - (int)((this.show.Width - width_config) / 2), y - (int)((this.show.Height - height_config) / 4));
 
                        // Add the inner panel to the main panel
                        this.panel43.Controls.Add(innerPanel);
                    
                        // Create list program
                        this.panel47.Visible = true;
                        label36.Text = $"{width}(W) x {height}(H)";
                        label35.Text = e1.name;

                        // Create list program
                        this.panel72.Visible = true;
                        label43.Text = $"{width}(W) x {height}(H)";
                        label44.Text = e1.name;

                        currentScale = 1.0f;
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
                string extension = System.IO.Path.GetExtension(objectName).ToLower();

                string[] list_object = { objectName };
                string[] list_duration = { "" };
                string[] list_entrytime = { "" };
                bool[] list_selected = { true };
                bool have_image = false;

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

                Panel destinationPanel = sender as Panel;
                ResizablePanel windown = null;
                var info_program = JsonConvert.DeserializeObject<Info_Program>(destinationPanel.Name);

                int max_app_width = destinationPanel.Width - 0;
                int max_app_height = destinationPanel.Height - 0;
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
                        list_duration = list_duration,
                        list_entrytime = list_entrytime,
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
                    this.textBox4.Text = Math.Round(Normalize(max_app_width, 0, max_app_width, 0, int.Parse(info_program.width_real))).ToString();
                    this.textBox3.Text = Math.Round(Normalize(max_app_height, 0, max_app_height, 0, int.Parse(info_program.height_real))).ToString();

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
                    int X = (sender as Control).PointToClient(new Point(e.X, e.Y)).X;
                    int Y = (sender as Control).PointToClient(new Point(e.X, e.Y)).Y;
                    var info_windown = new
                    {
                        name = "Windown " + lenght_list,
                        path_windown = "",
                        windown_height = (int) Math.Round(Normalize(50, 0, max_app_height, 0, int.Parse(info_program.height_real))),
                        windown_width = (int) Math.Round(Normalize(100, 0, max_app_width, 0, int.Parse(info_program.width_real))),
                        windown_top = (int) Math.Round(Normalize(Y, 0, max_app_height, 0, int.Parse(info_program.height_real))),
                        windown_left = (int) Math.Round(Normalize(X, 0, max_app_width, 0, int.Parse(info_program.width_real))),
                        list = list_object,
                        list_duration = list_duration,
                        list_entrytime = list_entrytime,
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
                        this.textBox1.Text = Math.Round(Normalize(X, 0, showPanel.Width, 0, int.Parse(info_program.width_real))).ToString();
                        this.textBox2.Text = Math.Round(Normalize(Y, 0, showPanel.Height, 0, int.Parse(info_program.height_real))).ToString();
                        if(active_select)
                        {
                            this.textBox4.Text = Math.Round(Normalize(app_width, 0, showPanel.Width, 0, int.Parse(info_program.width_real))).ToString();
                            this.textBox3.Text = Math.Round(Normalize(app_height, 0, showPanel.Height, 0, int.Parse(info_program.height_real))).ToString();
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
                        foreach (Control control1 in controlsList)
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
                                    // Select first item
                                    if (infoWindow1.selected.Count > 0)
                                    {
                                        infoWindow1.selected[0] = true;
                                    }

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
                            if (control1.Name.Length > 0)
                                control1.Refresh();
                        }

                        windown.InitializeResizeHandles();
                    }
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
                                String name_file = e1.Data.GetData("PictureBoxName") as string;
                                string extension1 = System.IO.Path.GetExtension(name_file).ToLower();
                                this.name_select.Text = " " + System.IO.Path.GetFileNameWithoutExtension(name_file);

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

                                    if(flag_error)
                                    {
                                        flag_error = false;
                                        infoWindow.list_entrytime.Add("");
                                        infoWindow.list_duration.Add("");
                                    }

                                }

                                infoWindow.list.Add(name_file);
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
                        item_Panel.Size = new System.Drawing.Size(946, 50);
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
                                        int index = int.Parse((sender as Control).Name);
                                        if (infoWindow1.selected[index])
                                        {
                                            String path_file = infoWindow1.list[int.Parse((sender as Control).Name)];
                                            string extension_1 = System.IO.Path.GetExtension(path_file).ToLower();
                                            
                                            initialBorderColor = Color.LightBlue;

                                            this.panel70.Visible = true;
                                            this.panel70.Name = infoWindow.Name;
                                            this.name_select.Text = " " + System.IO.Path.GetFileNameWithoutExtension(infoWindow1.list[index]);
                                            if (extension_1 == ".jpg" || extension_1 == ".bmp" || extension_1 == ".png" || extension_1 == ".gif")
                                            {
                                                this.panel80.Visible = true;
                                                this.entrytime_select.Text = infoWindow1.list_entrytime[index];
                                                this.duration_select.Text = infoWindow1.list_duration[index];
                                            }
                                            else
                                            {
                                                this.panel80.Visible = false;
                                            }

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
                        name_label.AutoSize = false;
                        name_label.AutoEllipsis = true;
                        name_label.Padding = new System.Windows.Forms.Padding(5, 15, 0, 0);
                        name_label.Dock = System.Windows.Forms.DockStyle.Fill;
                        name_label.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                        name_label.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
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
                                if (control1.Name.Length > 0)
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
            if(this.panel12.Controls.Count > 1)
            {
                this.panel12.Controls.RemoveAt(1);
            }
            this.name_device.Text = "--";
            this.name_device.Left = 112;
            this.ip_device.Text = "--";
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
                                                    if(data.code == 200)
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

                        while (!flagTermianlUDPThread)
                        {
                            try
                            {
                                if(udpListener.Available < 256)
                                {
                                    if (time_finish.ElapsedMilliseconds > 1000)
                                    {
                                        time_finish.Stop();                                    
                                        Console.WriteLine($"Scan device finished with \"{time_finish.ElapsedMilliseconds} ms\"");
                                        break;
                                    }
                                    else
                                    {
                                        break;
                                        continue;
                                    }                                    
                                }    


                                // Use Task.Factory.StartNew to run the asynchronous operation with a cancellation token
                                byte[] receivedBytes = udpListener.Receive(ref endPoint);
                                string receivedMessage = Encoding.ASCII.GetString(receivedBytes);
                                //Console.WriteLine(receivedMessage);
                                //Console.WriteLine("First device");
                                time_finish.Start();

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
                                            if(endPoint.Address.ToString().Equals("192.168.43.1"))
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
                                    catch(Exception e)
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

                        if(counter_device == 0)
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
            }
        }

        private void add_detail_file(string selectfilePath, bool allow_write)
        {
            try
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
                catch (Exception e)
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
            catch(Exception e)
            {
                Console.WriteLine(e);
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

            this.panel70.Visible = false;
            this.panel70.Name = "";
            this.panel80.Visible = false;

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
                int widthK1 = panel_chill.Width;
                int heightK1 = panel_chill.Height;
                var info_program = JsonConvert.DeserializeObject<Info_Program>(panel_chill.Name);
            
                // Get the maximum allowable width and height based on the mainPanel's size
                int width_contain = this.show.Width;
                int height_contain = this.show.Height;
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
                while ((width_config < (width_contain - 50)) && (height_config < (height_contain - 50)));
            
                // Calculate the position to center the inner panel within the main panel
                int x = (this.panel43.Width - (int)width_config) / 2;
                int y = (this.panel43.Height - (int)height_config) / 2;

                width_panel = (int)width_config;
                height_panel = (int)height_config;

                panel_chill.Width = (int)(width_config * scale);
                panel_chill.Height = (int)(height_config * scale);
                panel_chill.Location = new Point(x, y);

                int max_app_width = panel_chill.Width - 0;
                int max_app_height = panel_chill.Height - 0;

                foreach (Control control1 in controlsList)
                {
                    // Resize 
                    control1.Left = (int)Math.Round(Normalize(control1.Location.X, 0, widthK1, 0, panel_chill.Width));
                    control1.Top = (int)Math.Round(Normalize(control1.Location.Y, 0, heightK1, 0, panel_chill.Height));
                    control1.Width = (int)Math.Round(Normalize(control1.Width, 0, widthK1, 0, panel_chill.Width));
                    control1.Height = (int)Math.Round(Normalize(control1.Height, 0, heightK1, 0, panel_chill.Height));
                }

                this.show.AutoScrollPosition = new System.Drawing.Point(x - (int)((this.show.Width - (width_config * scale)) / 2), y - (int)((this.show.Height - (height_config * scale)) / 4));
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
                    info_windown.Reverse();

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
                        int width_contain = this.show.Width;
                        int height_contain = this.show.Height;
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
                        int x = (this.panel43.Width - (int)width_config) / 2;
                        int y = (this.panel43.Height - (int)height_config) / 2;
                        
                        
                        // Set the location of the inner panel
                        innerPanel.Location = new Point(x, y);

                        this.show.AutoScrollPosition = new System.Drawing.Point(x - (int)((this.show.Width - width_config) / 2), y - (int)((this.show.Height - height_config) / 4));

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
                        label36.Text = int.Parse(info_stored.info_program.width_real) + "*" + height;
                        label35.Text = info_stored.info_program.Name;

                        // Create list program
                        this.panel72.Visible = true;
                        label43.Text = int.Parse(info_stored.info_program.width_real) + "*" + height;
                        label44.Text = info_stored.info_program.Name;

                        var visiblePanels = this.panel43.Controls
                            .OfType<Panel>()
                            .Where(panel => panel.Visible);

                        foreach (var destinationPanel in visiblePanels)
                        {
                            // Draw windows
                            for (int idx_window = 0; idx_window < info_stored.info_windown.Count; idx_window++)
                            {
                                for (int idx_item = 0; idx_item < info_stored.info_windown[idx_window].list.Count; idx_item++)
                                {
                                    // Get the object name from the data
                                    int lenght_list = idx_window + 1;
                                    string objectName = info_stored.info_windown[idx_window].list[idx_item];
                                    string[] list_object = {objectName};
                                    bool[] list_selected = {false};

                                    int max_app_width = destinationPanel.Width - 0;
                                    int max_app_height = destinationPanel.Height - 0;
                                    int X = (int)Math.Round(Normalize(info_stored.info_windown[idx_window].windown_left, 0, int.Parse(info_stored.info_program.width_real), 0, max_app_width));
                                    int Y = (int)Math.Round(Normalize(info_stored.info_windown[idx_window].windown_top, 0, int.Parse(info_stored.info_program.height_real), 0, max_app_height));
                                    int width_windown = (int)Math.Round(Normalize(info_stored.info_windown[idx_window].windown_width, 0, int.Parse(info_stored.info_program.width_real), 0, max_app_width));
                                    int height_windown = (int)Math.Round(Normalize(info_stored.info_windown[idx_window].windown_height, 0, int.Parse(info_stored.info_program.height_real), 0, max_app_height));
                     
                                    var info_windown = new
                                    {
                                        name = "Windown " + lenght_list,
                                        path_windown = "",
                                        windown_height = info_stored.info_windown[idx_window].windown_height,
                                        windown_width = info_stored.info_windown[idx_window].windown_width,
                                        windown_top = info_stored.info_windown[idx_window].windown_top,
                                        windown_left = info_stored.info_windown[idx_window].windown_left,
                                        list = list_object,
                                        list_duration = info_stored.info_windown[idx_window].list_duration,
                                        list_entrytime = info_stored.info_windown[idx_window].list_entrytime,
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

                                                this.textBox1.Text = Math.Round(Normalize(X1, 0, showPanel.Width, 0, int.Parse(info_stored.info_program.width_real))).ToString();
                                                this.textBox2.Text = Math.Round(Normalize(Y1, 0, showPanel.Height, 0, int.Parse(info_stored.info_program.height_real))).ToString();
                                                if (active_select)
                                                {
                                                    this.textBox4.Text = Math.Round(Normalize(app_width, 0, showPanel.Width, 0, int.Parse(info_stored.info_program.width_real))).ToString();
                                                    this.textBox3.Text = Math.Round(Normalize(app_height, 0, showPanel.Height, 0, int.Parse(info_stored.info_program.height_real))).ToString();
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
                                                foreach (Control control1 in controlsList)
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
                                                    if (control2.Name.Length > 0)
                                                        control2.Refresh();
                                                }

                                                windown_load.InitializeResizeHandles();
                                            }
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
                                                        String name_file = e1.Data.GetData("PictureBoxName") as string;
                                                        string extension1 = System.IO.Path.GetExtension(name_file).ToLower();
                                                        this.name_select.Text = " " + System.IO.Path.GetFileNameWithoutExtension(name_file);

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

                                                        infoWindow.list.Add(name_file);
                                                        infoWindow.selected.Add(true);
                                                        resizablePanel.Name = JsonConvert.SerializeObject(infoWindow);
                                                    }
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
                        root_path = popup.textBox1.Text,
                        name_folder = popup.textBox3.Text,
                        info_program = panel_chill.Name
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
                if (true && ((controlsList.Count > 1) || (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))))
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
                    foreach (Control control in controlsList)
                    {
                        if (control is ResizablePanel resizablePanel && !string.IsNullOrEmpty(resizablePanel.Name))
                        {
                            Info_Window infoWindow = JsonConvert.DeserializeObject<Info_Window>(resizablePanel.Name);
                            int idx_windown = controlsList.IndexOf(control);
                
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
                                cmd_ffmpeg += $"-map [output] -c:v libx264 -b:v {info_program.bittrate_select} -preset slow -tune film -t {longestDuration / 1000} \"{contentFilePath}\"";
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
                
                                                percentage = percentageK1 + (int)((milliseconds * 100) / ((double)longestDuration * (controlsList.Count - counter_windown_empty)));
                
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

                                percentageK1 += (int)((longestDuration * 100) / ((double)longestDuration * (controlsList.Count - counter_windown_empty)));


                                if (((idx_windown + 1) >= controlsList.Count) && File.Exists(backgroundFilePath))
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
                
                                            percentage = percentageK1 + (int)((milliseconds * 100) / ((double)longestDuration * (controlsList.Count - counter_windown_empty)));
                
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
                
                
                foreach (Control control1 in controlsList)
                {
                    ResizablePanel panel_windown = control1 as ResizablePanel;
                    Info_Window Info_Window = JsonConvert.DeserializeObject<Info_Window>(panel_windown.Name);
                    info_windown.Add(Info_Window);
                
                    if (true && ((controlsList.Count > 1) || (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))))
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
                    if (true && ((controlsList.Count > 1) || (int.Parse(info_program.width_real) > int.Parse(info_program.width_resolution))))
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
                        foreach (Control control1 in controlsList)
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
                    durationProgramConvert = longestDuration,
                    info_program = info_program,
                    info_windown = info_windown
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
                if(obj.Name.Equals("ZoomIn"))
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
                        if(infoDevice.selected)
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

                                                if(data_parse.code == 200)
                                                {
                                                    if(!this.screen_status.Text.Equals("--"))
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
                                                                if(infoDevice1.ip_address != null && infoDevice1.ip_address.Equals(infoDevice.ip_address))
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

                                                    if(device_id != null && device_id.Length > 0)
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

            if(flag_error)
            {
                flag_error = false;
                MessageBox.Show("No device is chosen.", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                            if(obj.Name.Equals("usb_popup"))
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
    }

    public class export_packet
    {
        public string root_path { get; set; }
        public string name_folder { get; set; }
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
}
