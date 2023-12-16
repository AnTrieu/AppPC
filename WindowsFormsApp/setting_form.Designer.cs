using WindowsFormsApp.Properties;

namespace WindowsFormsApp
{
    partial class setting_form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.name_program = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.bittrate_select = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.width_resolution = new System.Windows.Forms.TextBox();
            this.height_resolution = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.confirm_vutton = new System.Windows.Forms.Button();
            this.cancel_button = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.close_button = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.height_real = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.width_real = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(12, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 22);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name: ";
            // 
            // name_program
            // 
            this.name_program.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
            this.name_program.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.name_program.CausesValidation = false;
            this.name_program.Font = new System.Drawing.Font("Microsoft Tai Le", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.name_program.ForeColor = System.Drawing.Color.White;
            this.name_program.HideSelection = false;
            this.name_program.Location = new System.Drawing.Point(135, 28);
            this.name_program.Name = "name_program";
            this.name_program.Size = new System.Drawing.Size(235, 30);
            this.name_program.TabIndex = 5;
            this.name_program.WordWrap = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(12, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 22);
            this.label2.TabIndex = 2;
            this.label2.Text = "Bitrate:";
            // 
            // bittrate_select
            // 
            this.bittrate_select.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
            this.bittrate_select.DisplayMember = "256 kbps";
            this.bittrate_select.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.bittrate_select.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bittrate_select.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bittrate_select.ForeColor = System.Drawing.Color.White;
            this.bittrate_select.FormattingEnabled = true;
            this.bittrate_select.Items.AddRange(new object[] {
            "256k",
            "512k",
            "1M",
            "1.5M",
            "2M",
            "4M"});
            this.bittrate_select.Location = new System.Drawing.Point(135, 77);
            this.bittrate_select.Name = "bittrate_select";
            this.bittrate_select.Size = new System.Drawing.Size(235, 28);
            this.bittrate_select.TabIndex = 3;
            this.bittrate_select.Tag = "";
            this.bittrate_select.ValueMember = "256 kbps";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(11, 151);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(111, 22);
            this.label3.TabIndex = 4;
            this.label3.Text = "Resolution:";
            // 
            // width_resolution
            // 
            this.width_resolution.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
            this.width_resolution.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.width_resolution.Font = new System.Drawing.Font("Microsoft Tai Le", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.width_resolution.ForeColor = System.Drawing.Color.White;
            this.width_resolution.Location = new System.Drawing.Point(135, 149);
            this.width_resolution.MaxLength = 4;
            this.width_resolution.Name = "width_resolution";
            this.width_resolution.Size = new System.Drawing.Size(99, 30);
            this.width_resolution.TabIndex = 5;
            // 
            // height_resolution
            // 
            this.height_resolution.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
            this.height_resolution.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.height_resolution.Font = new System.Drawing.Font("Microsoft Tai Le", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.height_resolution.ForeColor = System.Drawing.Color.White;
            this.height_resolution.Location = new System.Drawing.Point(271, 149);
            this.height_resolution.MaxLength = 4;
            this.height_resolution.Name = "height_resolution";
            this.height_resolution.Size = new System.Drawing.Size(99, 30);
            this.height_resolution.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Location = new System.Drawing.Point(245, 151);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(20, 22);
            this.label4.TabIndex = 7;
            this.label4.Text = "x";
            // 
            // confirm_vutton
            // 
            this.confirm_vutton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.LightBlue;
            this.confirm_vutton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.confirm_vutton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.confirm_vutton.ForeColor = System.Drawing.Color.White;
            this.confirm_vutton.Location = new System.Drawing.Point(85, 249);
            this.confirm_vutton.Name = "confirm_vutton";
            this.confirm_vutton.Size = new System.Drawing.Size(100, 30);
            this.confirm_vutton.TabIndex = 8;
            this.confirm_vutton.Text = "Confirm";
            this.confirm_vutton.UseVisualStyleBackColor = true;
            this.confirm_vutton.Click += new System.EventHandler(this.Confirm_Click);
            // 
            // cancel_button
            // 
            this.cancel_button.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.cancel_button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancel_button.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancel_button.ForeColor = System.Drawing.Color.White;
            this.cancel_button.Location = new System.Drawing.Point(224, 249);
            this.cancel_button.Name = "cancel_button";
            this.cancel_button.Size = new System.Drawing.Size(100, 30);
            this.cancel_button.TabIndex = 9;
            this.cancel_button.Text = "Cancel";
            this.cancel_button.UseVisualStyleBackColor = true;
            this.cancel_button.Click += new System.EventHandler(this.button2_Click);
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
            this.panel1.Size = new System.Drawing.Size(398, 30);
            this.panel1.TabIndex = 10;
            // 
            // close_button
            // 
            this.close_button.BackgroundImage = global::WindowsFormsApp.Properties.Resources.close_white_icon;
            this.close_button.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.close_button.Dock = System.Windows.Forms.DockStyle.Right;
            this.close_button.FlatAppearance.BorderSize = 0;
            this.close_button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.close_button.Location = new System.Drawing.Point(366, 0);
            this.close_button.Name = "close_button";
            this.close_button.Size = new System.Drawing.Size(30, 28);
            this.close_button.TabIndex = 1;
            this.close_button.UseVisualStyleBackColor = true;
            this.close_button.Click += new System.EventHandler(this.button3_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.White;
            this.label5.Location = new System.Drawing.Point(172, 6);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(93, 17);
            this.label5.TabIndex = 0;
            this.label5.Text = "New Program";
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.label9);
            this.panel2.Controls.Add(this.label8);
            this.panel2.Controls.Add(this.height_real);
            this.panel2.Controls.Add(this.label7);
            this.panel2.Controls.Add(this.width_real);
            this.panel2.Controls.Add(this.label6);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.name_program);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.bittrate_select);
            this.panel2.Controls.Add(this.label3);
            this.panel2.Controls.Add(this.width_resolution);
            this.panel2.Controls.Add(this.height_resolution);
            this.panel2.Controls.Add(this.label4);
            this.panel2.Controls.Add(this.confirm_vutton);
            this.panel2.Controls.Add(this.cancel_button);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 30);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(398, 294);
            this.panel2.TabIndex = 11;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.White;
            this.label9.Location = new System.Drawing.Point(302, 120);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(38, 22);
            this.label9.TabIndex = 15;
            this.label9.Text = "(H)";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Location = new System.Drawing.Point(165, 120);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(42, 22);
            this.label8.TabIndex = 14;
            this.label8.Text = "(W)";
            // 
            // height_real
            // 
            this.height_real.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
            this.height_real.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.height_real.Font = new System.Drawing.Font("Microsoft Tai Le", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.height_real.ForeColor = System.Drawing.Color.White;
            this.height_real.Location = new System.Drawing.Point(271, 192);
            this.height_real.MaxLength = 4;
            this.height_real.Name = "height_real";
            this.height_real.Size = new System.Drawing.Size(99, 30);
            this.height_real.TabIndex = 13;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(245, 198);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(20, 22);
            this.label7.TabIndex = 12;
            this.label7.Text = "x";
            // 
            // width_real
            // 
            this.width_real.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(54)))), ((int)(((byte)(54)))));
            this.width_real.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.width_real.Font = new System.Drawing.Font("Microsoft Tai Le", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.width_real.ForeColor = System.Drawing.Color.White;
            this.width_real.Location = new System.Drawing.Point(135, 190);
            this.width_real.MaxLength = 4;
            this.width_real.Name = "width_real";
            this.width_real.Size = new System.Drawing.Size(99, 30);
            this.width_real.TabIndex = 11;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.White;
            this.label6.Location = new System.Drawing.Point(11, 192);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(57, 22);
            this.label6.TabIndex = 10;
            this.label6.Text = "Real:";
            // 
            // setting_form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(398, 324);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "setting_form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New Program";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox name_program;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox bittrate_select;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox width_resolution;
        private System.Windows.Forms.TextBox height_resolution;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button confirm_vutton;
        private System.Windows.Forms.Button cancel_button;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button close_button;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox height_real;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox width_real;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
    }
}