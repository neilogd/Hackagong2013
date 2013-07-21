namespace WhiteboardDrawer
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.TrackBarMaxVelocity = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.PictureSimulation = new System.Windows.Forms.PictureBox();
            this.ButtonOpenImage = new System.Windows.Forms.Button();
            this.TimerHaveArrivedTimer = new System.Windows.Forms.Timer(this.components);
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TrackBarMaxVelocity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PictureSimulation)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox3
            // 
            this.groupBox3.AutoSize = true;
            this.groupBox3.Controls.Add(this.TrackBarMaxVelocity);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.PictureSimulation);
            this.groupBox3.Location = new System.Drawing.Point(12, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(997, 701);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Simulation";
            // 
            // TrackBarMaxVelocity
            // 
            this.TrackBarMaxVelocity.Location = new System.Drawing.Point(82, 637);
            this.TrackBarMaxVelocity.Maximum = 64;
            this.TrackBarMaxVelocity.Minimum = 1;
            this.TrackBarMaxVelocity.Name = "TrackBarMaxVelocity";
            this.TrackBarMaxVelocity.Size = new System.Drawing.Size(909, 45);
            this.TrackBarMaxVelocity.TabIndex = 2;
            this.TrackBarMaxVelocity.Value = 1;
            this.TrackBarMaxVelocity.Scroll += new System.EventHandler(this.TrackBarMaxVelocity_Scroll);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 657);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Max Velocity:";
            // 
            // PictureSimulation
            // 
            this.PictureSimulation.Location = new System.Drawing.Point(7, 19);
            this.PictureSimulation.Name = "PictureSimulation";
            this.PictureSimulation.Size = new System.Drawing.Size(984, 612);
            this.PictureSimulation.TabIndex = 0;
            this.PictureSimulation.TabStop = false;
            this.PictureSimulation.Click += new System.EventHandler(this.PictureSimulation_Click);
            this.PictureSimulation.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PictureSimulation_MouseClick);
            this.PictureSimulation.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PictureSimulation_MouseMove);
            // 
            // ButtonOpenImage
            // 
            this.ButtonOpenImage.Location = new System.Drawing.Point(19, 720);
            this.ButtonOpenImage.Name = "ButtonOpenImage";
            this.ButtonOpenImage.Size = new System.Drawing.Size(101, 23);
            this.ButtonOpenImage.TabIndex = 3;
            this.ButtonOpenImage.Text = "Open Image";
            this.ButtonOpenImage.UseVisualStyleBackColor = true;
            this.ButtonOpenImage.Click += new System.EventHandler(this.ButtonOpenImage_Click);
            // 
            // TimerHaveArrivedTimer
            // 
            this.TimerHaveArrivedTimer.Interval = 250;
            this.TimerHaveArrivedTimer.Tick += new System.EventHandler(this.TimerHaveArrivedTimer_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1015, 758);
            this.Controls.Add(this.ButtonOpenImage);
            this.Controls.Add(this.groupBox3);
            this.Name = "MainForm";
            this.Text = "Hackagong 2013 Whiteboard Drawer";
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TrackBarMaxVelocity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PictureSimulation)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.PictureBox PictureSimulation;
        private System.Windows.Forms.TrackBar TrackBarMaxVelocity;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button ButtonOpenImage;
        private System.Windows.Forms.Timer TimerHaveArrivedTimer;
    }
}

