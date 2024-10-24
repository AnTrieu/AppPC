﻿using Accord.Video.FFMPEG;
using Newtonsoft.Json;
//using Accord.Video.FFMPEG;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using WindowsFormsApp;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Timer = System.Windows.Forms.Timer;

public class ResizablePanel : Panel
{
    private const int ResizeHandleSize = 10;
    private bool isResizing;
    private bool isMove;
    private bool isAutoMove;
    private Point lastMousePosition;
    private Panel DestinationPanel { get; set; }
    private Label activeResizeHandle;

    public VideoFileReader videoFileReader = null;

    // Determine the dynamic snap distance based on the distance between centers
    private int snapDistance = 10;

    // Define a delegate for the event handler
    public delegate void CustomEventHandler(object sender, DragEventArgs e);
    public delegate void CustomMouseEventHandler(object sender, EventArgs e, int X, int Y, int real_width, int real_height, bool active_select, string info_other_panel, int direction);

    // Define the custom event using the delegate
    public event CustomEventHandler CustomEventDragOver;
    public event CustomEventHandler CustomEventDragEnter;
    public event CustomEventHandler CustomEventDragDrop;
    public event CustomMouseEventHandler CustomEventMouseDown;
    public event CustomMouseEventHandler CustomEventMouseMove;
    // Method to raise the custom event
    protected virtual void OnCustomDragOverEvent(DragEventArgs e)
    {
        CustomEventHandler handler = CustomEventDragOver;
        handler?.Invoke(this, e);
    }

    protected virtual void OnCustomDragEnterEvent(DragEventArgs e)
    {
        CustomEventHandler handler = CustomEventDragEnter;
        handler?.Invoke(this, e);
    }

    protected virtual void OnCustomDragDropEvent(DragEventArgs e)
    {
        CustomEventHandler handler = CustomEventDragDrop;
        handler?.Invoke(this, e);
    }

    protected virtual void OnCustomMouseDownEvent(EventArgs e, int X, int y, int real_width, int real_height, bool active_select, string info_other_panel, int direction)
    {
        CustomMouseEventHandler handler = CustomEventMouseDown;
        handler?.Invoke(this, e, X, y, real_width, real_height, active_select, info_other_panel, direction);
    }

    protected virtual void OnCustomMouseMoveEvent(EventArgs e, int X, int y, int real_width, int real_height, bool active_select, string info_other_panel, int direction)
    {
        CustomMouseEventHandler handler = CustomEventMouseMove;
        handler?.Invoke(this, e, X, y, real_width, real_height, active_select, info_other_panel, direction);
    }

    public ResizablePanel(Panel destinationPanel)
    {
        ResizeRedraw = true;
        SetStyle(ControlStyles.ResizeRedraw, true);

        DestinationPanel = destinationPanel;

        this.DragDrop += (sender1, e) =>
        {
            // Event callback
            OnCustomDragDropEvent(e);
        };

        this.DragEnter += (sender1, e) =>
        {
            if (e.Data.GetDataPresent("PictureBoxImage") && e.Data.GetDataPresent("PictureBoxName") && (e.AllowedEffect & DragDropEffects.Move) != 0)
            {
                e.Effect = DragDropEffects.Move;

                // Event callback
                OnCustomDragEnterEvent(e);
            }
        };

        this.DragOver += ResizablePane_DragOver;
        DestinationPanel.DragOver += ResizablePane_DragOver;

        this.SizeChanged += ResizablePanel_SizeChanged;


        this.MouseDown += (sender, e) =>
        {
            maunalActiveMouseDown(sender, e);
        };
        this.MouseUp += (sender, e) =>
        {
            maunalActiveMouseUp(sender, e);
        };
        this.MouseMove += (sender, e) =>
        {
            maunalActiveMouseMove(sender, e);
        };
    }


    private bool CheckCollision(int deltaX, int deltaY)
    {
        bool ret = false;

        foreach (Control control in this.Parent.Controls)
        {
            // Check if the object is not itself and is a ResizablePanel
            if (control != this && control is ResizablePanel)
            {
                ResizablePanel otherPanel = (ResizablePanel)control;
        
        
                bool isColliding = !(this.Right + deltaX < otherPanel.Left - snapDistance ||
                                     this.Left + deltaX > otherPanel.Right + snapDistance ||
                                     this.Bottom + deltaY < otherPanel.Top - snapDistance ||
                                     this.Top + deltaY > otherPanel.Bottom + snapDistance);
        
                bool isCollidingRight = Math.Abs((this.Right + deltaX) - (otherPanel.Left - snapDistance)) <= snapDistance &&
                                        this.Left + deltaX < otherPanel.Left - snapDistance;
        
        
                bool isCollidingLeft = Math.Abs((this.Left + deltaX) - (otherPanel.Right + snapDistance)) <= snapDistance &&
                                       this.Right + deltaX > otherPanel.Right + snapDistance;
        
                bool isCollidingTop = Math.Abs((this.Bottom + deltaY) - (otherPanel.Top - snapDistance)) <= snapDistance &&
                                      this.Top + deltaY < otherPanel.Top - snapDistance;
        
                bool isCollidingBottom = Math.Abs((this.Top + deltaY) - (otherPanel.Bottom + snapDistance)) <= snapDistance &&
                                         this.Bottom + deltaY > otherPanel.Bottom + snapDistance;
        
        
                if (isColliding)
                {
                    int direction = -1;
                    // Điều chỉnh vị trí của this và otherPanel để chúng nằm sát nhau mà không chồng lên nhau
                    if (isCollidingRight)
                    {
                        this.Left = otherPanel.Left - this.Width;
                        ret = true;
                        direction = 0;
                    }
                    else if (isCollidingLeft)
                    {
                        this.Left = otherPanel.Right;
                        ret = true;
                        direction = 1;
                    }
                    else if (isCollidingTop)
                    {
                        this.Top = otherPanel.Top - this.Height;
                        ret = true;
                        direction = 2;
                    }
                    else if (isCollidingBottom)
                    {
                        this.Top = otherPanel.Bottom;
                        ret = true;
                        direction = 3;
                    }

                    // Event callback
                    OnCustomMouseMoveEvent(EventArgs.Empty, this.Left, this.Top, this.Width, this.Height, false, otherPanel.Name, direction);
                }
            }
        }

        return ret;
    }

    public void maunalActiveMouseUp(object sender, MouseEventArgs e)
    {
        isAutoMove = false;
        isMove = false;

        // Event callback
        OnCustomMouseDownEvent(EventArgs.Empty, this.Left, this.Top, this.Width, this.Height, true, null, -1);
    }

    public void maunalActiveMouseDown(object sender, MouseEventArgs e)
    {
        isAutoMove = true;
        isMove = true;
        lastMousePosition = e.Location;

        // Event callback
        OnCustomMouseDownEvent(EventArgs.Empty, this.Left, this.Top, this.Width, this.Height, true, null, -1);
    }

    public void maunalActiveMouseMove(object sender, MouseEventArgs e)
    {
        if (isMove)
        {
            var info_program = JsonConvert.DeserializeObject<Info_Program>(this.Parent.Name);
            int deltaX = e.X - lastMousePosition.X;
            int deltaY = e.Y - lastMousePosition.Y;

            int newLeft = this.Left + deltaX;
            int newTop = this.Top + deltaY;

            int maxX = info_program.width_area - this.Width;
            int maxY = info_program.height_area - this.Height;

            newLeft = Math.Max(info_program.x_area, Math.Min(newLeft, maxX + info_program.x_area));
            newTop = Math.Max(info_program.y_area, Math.Min(newTop, maxY + info_program.y_area));


            // Kiểm tra va chạm với các đối tượng khác
            if (isAutoMove && !CheckCollision(deltaX, deltaY))
            {
                if(deltaX != 0 || deltaY != 0)
                {
                    // Event callback
                    OnCustomMouseMoveEvent(EventArgs.Empty, this.Left, this.Top, this.Width, this.Height, false, null, -1);

                    this.Location = new Point(newLeft, newTop);
                }
            }
            else if (!isAutoMove)
            {
                if ((Math.Abs(deltaX) > snapDistance) || (Math.Abs(deltaY) > snapDistance))
                {
                    isAutoMove = true;
                }
            }
            else
            {
                isAutoMove = false;
            }
        }
    }
    public void InitializeResizeHandles()
    {
        for (int panelIndex = DestinationPanel.Controls.Count - 1; panelIndex >= 0; panelIndex--)
        {
            Control panel = DestinationPanel.Controls[panelIndex];

            // Check if the control is a Panel
            if (panel is Panel)
            {
                // Loop through controls in the panel in reverse order
                for (int i = panel.Controls.Count - 1; i >= 0; i--)
                {
                    Control control = panel.Controls[i];

                    // Check if the control is a Label
                    if (control is Label)
                    {
                        // Remove the Label from the panel
                        panel.Controls.RemoveAt(i);
                    }
                }
            }
        }

        Label top_left_point = CreateResizeHandle(0, 0, "top-left");
        Controls.Add(top_left_point); // Top-left
        Controls.SetChildIndex(top_left_point, 0);

        Label top_right_point = CreateResizeHandle(Width - ResizeHandleSize, 0, "top-right");
        Controls.Add(top_right_point); // Top-right
        Controls.SetChildIndex(top_right_point, 0);

        Label bottom_left_point = CreateResizeHandle(0, Height - ResizeHandleSize, "bottom-left");
        Controls.Add(bottom_left_point); // Bottom-left
        Controls.SetChildIndex(bottom_left_point, 0);

        Label bottom_right_point = CreateResizeHandle(Width - ResizeHandleSize, Height - ResizeHandleSize, "bottom-right");
        Controls.Add(bottom_right_point); // Bottom-right
        Controls.SetChildIndex(bottom_right_point, 0);
    }

    public void UninitializeResizeHandles()
    {
        for (int panelIndex = DestinationPanel.Controls.Count - 1; panelIndex >= 0; panelIndex--)
        {
            Control panel = DestinationPanel.Controls[panelIndex];

            // Check if the control is a Panel
            if (panel is Panel)
            {
                // Loop through controls in the panel in reverse order
                for (int i = panel.Controls.Count - 1; i >= 0; i--)
                {
                    Control control = panel.Controls[i];

                    // Check if the control is a Label
                    if (control is Label)
                    {
                        // Remove the Label from the panel
                        panel.Controls.RemoveAt(i);
                    }
                }
            }
        }
    }

    private Label CreateResizeHandle(int x, int y, String Name)
    {
        Label handle = new Label
        {
            Size = new Size(ResizeHandleSize, ResizeHandleSize),
            Location = new Point(x, y),
            BackColor = Color.LightSkyBlue,
            Name = Name,
            AllowDrop = true
        };
 
        handle.MouseDown += ResizeHandle_MouseDown;
        
        return handle;
    }

    private void ResizablePane_DragOver(object sender, DragEventArgs e)
    {
        if (isResizing)
        {
            Point currentMousePosition = Cursor.Position;
            int deltaX = currentMousePosition.X - lastMousePosition.X;
            int deltaY = currentMousePosition.Y - lastMousePosition.Y;

            var info_program = JsonConvert.DeserializeObject<Info_Program>(this.Parent.Name);

            // Áp dụng thay đổi kích thước dựa trên handle đang được kéo
            if (activeResizeHandle.Name.Equals("top-left")) // Top-left
            {
                int newLeft = this.Left + deltaX;
                int newTop = this.Top + deltaY;

                if (((info_program.x_area <= newLeft) && (newLeft <= (info_program.x_area + info_program.width_area))) &&
                    ((info_program.y_area <= newTop) && (newTop <= (info_program.y_area + info_program.height_area))))
                {
                    this.Left += deltaX;
                    this.Top += deltaY;
                    this.Width -= deltaX;
                    this.Height -= deltaY;
                }
            }
            else if (activeResizeHandle.Name.Equals("top-right")) // Top-right
            {
                int newLeft = this.Left + deltaX + this.Width;
                int newTop = this.Top + deltaY;

                if (((info_program.x_area <= newLeft) && (newLeft <= (info_program.x_area + info_program.width_area))) &&
                    ((info_program.y_area <= newTop) && (newTop <= (info_program.y_area + info_program.height_area))))
                {
                    this.Top += deltaY;
                    this.Height -= deltaY;
                    this.Width += deltaX;
                }                          
            }   
            else if (activeResizeHandle.Name.Equals("bottom-left")) // Bottom-left
            {
                int newLeft = this.Left + deltaX;
                int newTop = this.Top + deltaY + this.Height;

                if (((info_program.x_area <= newLeft) && (newLeft <= (info_program.x_area + info_program.width_area))) &&
                    ((info_program.y_area <= newTop) && (newTop <= (info_program.y_area + info_program.height_area))))
                {
                    this.Left += deltaX;
                    this.Width -= deltaX;
                    this.Height += deltaY;
                }

            }
            else if (activeResizeHandle.Name.Equals("bottom-right")) // Bottom-right
            {
                int newLeft = this.Left + deltaX + this.Width;
                int newTop = this.Top + deltaY + this.Height;

                if (((info_program.x_area <= newLeft) && (newLeft <= (info_program.x_area + info_program.width_area))) &&
                    ((info_program.y_area <= newTop) && (newTop <= (info_program.y_area + info_program.height_area))))
                {
                    this.Width += deltaX;
                    this.Height += deltaY;
                }           
            }
            //Console.WriteLine(this.Width);
            if (deltaX != 0 || deltaY != 0)
            {
                // Event callback
                OnCustomMouseMoveEvent(EventArgs.Empty, this.Left, this.Top, this.Width, this.Height, true, null, -1);
            }

            lastMousePosition = currentMousePosition;
        }
        else
        {
            OnCustomDragOverEvent(e);
        }
    }

    private void ResizeHandle_MouseDown(object sender, MouseEventArgs e)
    {
        activeResizeHandle = sender as Label;

        // Active resize event
        isResizing = true;

        lastMousePosition = Cursor.Position;
        activeResizeHandle.DoDragDrop(activeResizeHandle, DragDropEffects.Move);

        // De-Active resize event
        isResizing = false;
        activeResizeHandle = null;

        // Re-paint
        this.Refresh();
    }

    private void ResizablePanel_SizeChanged(object sender, EventArgs e)
    {
        // Gọi lại để cập nhật ResizeHandles khi kích thước thay đổi
        InitializeResizeHandles();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using (Pen borderPen = new Pen(Color.White, 1))
        {
            e.Graphics.DrawRectangle(borderPen, new Rectangle(0, 0, Width - 1, Height - 1));
        }
    }
}