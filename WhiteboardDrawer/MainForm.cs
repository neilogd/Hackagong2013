using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace WhiteboardDrawer
{
    public partial class MainForm : Form
    {
        private Common.Drawer _drawer = null;
        private int _drawPadding = 20;
        private int _feedSize = 4;
        private Vector _cradlePosition = new Vector(0.0, 0.0);
        private Vector _boardDimensions = new Vector(1800.0, 900.0);

        private DateTime _lastDrawTime = DateTime.Now;
        private DateTime _lastMoveCommand = DateTime.Now;

        private double[] _estimatedLineLengths = null;

        private List<string> _commandStream = new List<string>();

        public MainForm()
        {
            InitializeComponent();

            // Dummy mount points and line feeds.
            Vector[] lineFeeds = new Vector[]
            {
                new Vector(-_boardDimensions.X * 0.5, (-_boardDimensions.Y * 0.5) - 30.0),
                new Vector(_boardDimensions.X * 0.5, (-_boardDimensions.Y * 0.5) - 30.0)
            };

            Vector cradleDimensions = new Vector(150.0, 75.0);

            Vector[] cradleMountPoints = new Vector[]
            {
                new Vector(-cradleDimensions.X * 0.5, -cradleDimensions.Y * 0.5),
                new Vector(cradleDimensions.X * 0.5, -cradleDimensions.Y * 0.5)
            };

            int[] serialNos = new int[]
            {
                304740,
                304726
            };

            _estimatedLineLengths = new double[2];

            // Create drawer.
            _drawer = new Common.Drawer(lineFeeds, cradleMountPoints, cradleDimensions, serialNos, "COM3");

            _drawer.OnLineFeedLengthChanged += OnLineFeedLengthChangedArgs;
            _drawer.OnDrawerArrived += OnDrawerArrived;

            // ~27mm * 3.14159 = 84.55293
            // 3200 stepper positions per 360 degrees.
            // 3200 / 84.55293 = 37.84611603642831.

            double multiplier = 37.84611603642831; //hackahackahacka...

            _drawer._targetLengthToStepperPosition = (double length) =>
            {
                return (long)(length * multiplier);
            };

            _drawer._stepperPositionToTargetLength = (long stepperPosition) =>
            {
                return (double)stepperPosition / multiplier;
            };

            _drawer.OnDrawerReady += OnDrawerReady;

            _drawer.Open();

            TimerHaveArrivedTimer.Enabled = true;
            TimerHaveArrivedTimer.Start();

        }

        delegate void DrawSimulationDelegate();

        private void OnLineFeedLengthChangedArgs(object o, Common.Drawer.LineFeedLengthChangedArgs e)
        {
            _estimatedLineLengths[e.LineIndex] = e.Length;

            var ticksElapsed = (DateTime.Now - _lastDrawTime).Ticks;
            if (ticksElapsed > (1000 * 60))
            {
                Invoke(new DrawSimulationDelegate(DrawSimulation));
            }
        }

        private void OnDrawerReady(object o, Common.Drawer.DrawerReadyArgs e)
        {
            Invoke(new DrawSimulationDelegate(_drawer.Reset));
            Invoke(new DrawSimulationDelegate(DrawSimulation));
        }

        private void OnDrawerArrived(object o, Common.Drawer.DrawerArrivedArgs e)
        {
            if (_commandStream.Count > 0)
            {
                bool hasExecuted = false;
                var offset = -_boardDimensions / 2.0;

                do
                {
                    var line = _commandStream[0];
                    _commandStream.RemoveAt(0);
                    var parts = line.Split(' ');

                    if (parts.Length > 0 && parts[0] == "M")
                    {
                        var point = (new Vector(System.Convert.ToDouble(parts[1]), System.Convert.ToDouble(parts[2])) + offset);
    
                        hasExecuted = true;
                        _drawer.MoveCradle(point);
                    }
                    else if (parts.Length > 0 && parts[0] == "P")
                    {
                        var pen = System.Convert.ToInt32(parts[1]);

                        hasExecuted = true;
                        if (pen == 0)
                        {
                            _drawer.DeactivatePens();
                        }
                        else
                        {
                            _drawer.ActivatePen(pen - 1);
                        }
                    }
                }
                while(_commandStream.Count > 0 && hasExecuted == false);
            }

            //Invoke(new DrawSimulationDelegate(_drawer.NextWaypoint));
        }

        /// <summary>
        /// Get scale values for physical/pixel conversion.
        /// </summary>
        /// <param name="outOffset"></param>
        /// <param name="outScale"></param>
        private void GetScaleValues(out Vector outOffset, out Vector outScale)
        {
            var dimensions = new Vector(PictureSimulation.Width, PictureSimulation.Height) - new Vector((double)_drawPadding * 2.0, (double)_drawPadding * 2.0);
            outOffset = (dimensions * 0.5) + new Vector((double)_drawPadding, (double)_drawPadding);
            outScale = new Vector(dimensions.X / _boardDimensions.X, dimensions.Y / _boardDimensions.Y);
        }

        /// <summary>
        /// Physical coordinates to pixel coordinates.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private System.Drawing.Point PhysicalToPixelPoint(Vector value)
        {
            Vector offset, scale;
            GetScaleValues(out offset, out scale);
            var point = new Vector(value.X * scale.X, value.Y * scale.Y) + offset;
            return new System.Drawing.Point((int)point.X, (int)point.Y);
        }

        /// <summary>
        /// Physical coordinates to pixel size.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private System.Drawing.Size PhysicalToPixelSize(Vector value)
        {
            Vector offset, scale;
            GetScaleValues(out offset, out scale);
            var point = new Vector(value.X * scale.X, value.Y * scale.Y);
            return new System.Drawing.Size((int)point.X, (int)point.Y);
        }

        /// <summary>
        /// Pixel coordinates to physical coordinates.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private Vector PixelPointToPhysical(System.Drawing.Point value)
        {
            Vector offset, scale;
            GetScaleValues(out offset, out scale);
            return new Vector(((double)value.X - offset.X) / scale.X, ((double)value.Y - offset.Y) / scale.Y);
        }

        /// <summary>
        /// Pixel size to physical coordinates.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private Vector PixelSizeToPhysical(System.Drawing.Size value)
        {
            Vector offset, scale;
            GetScaleValues(out offset, out scale);
            return new Vector(((double)value.Width) / scale.X, ((double)value.Height) / scale.Y);
        }

        /// <summary>
        /// Draw simulation.
        /// </summary>
        private void DrawSimulation()
        {
            _lastDrawTime = DateTime.Now;

            // Create image for picture simulation.
            if (PictureSimulation.Image == null)
            {
                Bitmap bmp = new Bitmap(PictureSimulation.Width, PictureSimulation.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                }
                PictureSimulation.Image = bmp;
            }

            // Draw!
            using (Graphics g = Graphics.FromImage(PictureSimulation.Image))
            {
                // Clear.
                g.Clear(Color.White);

                // Reset clipping.
                g.Clip = new Region();

                // Draw the origin guides.
                g.DrawLine(Pens.LightGray, PhysicalToPixelPoint(new Vector(-_boardDimensions.X * 0.5, 0.0)), PhysicalToPixelPoint(new Vector(_boardDimensions.X * 0.5, 0.0)));
                g.DrawLine(Pens.LightGray, PhysicalToPixelPoint(new Vector(0.0, -_boardDimensions.Y * 0.5)), PhysicalToPixelPoint(new Vector(0.0, _boardDimensions.Y * 0.5)));

                // Draw the board + some origin guides.
                var points = new System.Drawing.Point[]
                {
                    PhysicalToPixelPoint(new Vector(-_boardDimensions.X * 0.5f, -_boardDimensions.Y * 0.5f)),
                    
                    PhysicalToPixelPoint(new Vector( _boardDimensions.X * 0.5f, -_boardDimensions.Y * 0.5f)),
                    PhysicalToPixelPoint(new Vector( _boardDimensions.X * 0.5f,  _boardDimensions.Y * 0.5f)),
                    PhysicalToPixelPoint(new Vector( -_boardDimensions.X * 0.5f,  _boardDimensions.Y * 0.5f)),
                    PhysicalToPixelPoint(new Vector(-_boardDimensions.X * 0.5f, -_boardDimensions.Y * 0.5f)),
                };
                g.DrawLines(Pens.Black, points);

                // Draw the line feed points, cradle mount points, lines between, and estimated length arcs.
                var feedSize = new System.Drawing.Size(_feedSize, _feedSize);
                var physicalSize = PixelSizeToPhysical(feedSize);

                g.Clip = new System.Drawing.Region(new System.Drawing.Rectangle(points[0], PhysicalToPixelSize(_boardDimensions)));

                var estimatedCradlePosition =  _drawer.GetEstimatedCradlePosition();

                for (int idx = 0; idx < _drawer._lineFeedPoints.Length; ++idx)
                {
                    var lineFeedPoint = PhysicalToPixelPoint(_drawer._lineFeedPoints[idx] - (physicalSize * 0.5));
                    var cradleMountPoint = PhysicalToPixelPoint(_drawer._cradleMountPoints[idx] + estimatedCradlePosition - (physicalSize * 0.5));

                    g.DrawEllipse(Pens.Red, new System.Drawing.Rectangle(lineFeedPoint, feedSize));
                    g.DrawEllipse(Pens.Green, new System.Drawing.Rectangle(cradleMountPoint, feedSize));

                    var lineFeedCentrePoint = PhysicalToPixelPoint(_drawer._lineFeedPoints[idx]);
                    var cradleMountCentrePoint = PhysicalToPixelPoint(_drawer._cradleMountPoints[idx] + estimatedCradlePosition);

                    g.DrawLine(Pens.Purple, lineFeedCentrePoint, cradleMountCentrePoint);

                    var lineLength = new Vector(_estimatedLineLengths[idx], _estimatedLineLengths[idx]) * 2.0;
                    var lineFeedLengthPoint = PhysicalToPixelPoint(_drawer._lineFeedPoints[idx] - (lineLength * 0.5));
                    var lineLengthSize = PhysicalToPixelSize(lineLength);
                    g.DrawEllipse(Pens.LightBlue, new System.Drawing.Rectangle(lineFeedLengthPoint, lineLengthSize));
                }

                // Draw estimated cradle position.
                DrawCradle(g, estimatedCradlePosition);
            }

            PictureSimulation.Invalidate();
        }

        private void DrawCradle(Graphics g, Vector position)
        {
            // Draw cradle.
            var cradlePoint = PhysicalToPixelPoint(position - (_drawer._cradleDimensions * 0.5));
            var cradleSize = PhysicalToPixelSize(_drawer._cradleDimensions);
            g.DrawRectangle(Pens.Blue, new System.Drawing.Rectangle(cradlePoint, cradleSize));
        }

        private void PictureSimulation_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                _cradlePosition = PixelPointToPhysical(new System.Drawing.Point(e.X, e.Y));
                var ticksElapsed = (DateTime.Now - _lastMoveCommand).Ticks;
                if (ticksElapsed > (10000 * 60))
                {
                    _lastMoveCommand = DateTime.Now;
                    _drawer.MoveCradleStraight(_cradlePosition, true);
                }
            }
            DrawSimulation();
        }

        private void PictureInput_Click(object sender, EventArgs e)
        {

        }

        private void PictureSimulation_Click(object sender, EventArgs e)
        {

        }

        private void PictureSimulation_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                _cradlePosition = PixelPointToPhysical(new System.Drawing.Point(e.X, e.Y));
                _drawer.MoveCradleStraight(_cradlePosition, true);
            }
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                _cradlePosition = PixelPointToPhysical(new System.Drawing.Point(e.X, e.Y));
                _drawer.MoveCradleStraight(_cradlePosition, false);
            }
            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                _drawer.Reset();
            }
            DrawSimulation();
        }
        
        private void TrackBarMaxVelocity_Scroll(object sender, EventArgs e)
        {

            _drawer._velocityMultiplier = (double)TrackBarMaxVelocity.Value / (double)TrackBarMaxVelocity.Maximum;
        }

        private void ButtonOpenImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Files (*.*)|*.*";
            openFileDialog.Title = "Open Image File.";
            var result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var lines = System.IO.File.ReadAllLines(openFileDialog.FileName);
                _commandStream.AddRange(lines);
            }
        }

        private void TimerHaveArrivedTimer_Tick(object sender, EventArgs e)
        {
            if (_drawer.AreWeThereYet())
            {
                OnDrawerArrived(this, new Common.Drawer.DrawerArrivedArgs());
            }            
        }
    }
}
