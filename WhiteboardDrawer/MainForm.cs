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
        private int _drawPadding = 16;
        private int _feedSize = 4;
        private Vector _boardDimensions = new Vector(1000.0, 750.0);

        public MainForm()
        {
            InitializeComponent();

            // Dummy mount points and line feeds.
            Vector[] lineFeeds = new Vector[]
            {
                new Vector(-500.0, -375.0),
                new Vector(500.0, -375.0)
            };

            Vector cradleDimensions = new Vector(150.0, 75.0);

            Vector[] cradleMountPoints = new Vector[]
            {
                new Vector(-cradleDimensions.X * 0.5, -cradleDimensions.Y * 0.5),
                new Vector(cradleDimensions.X * 0.5, -cradleDimensions.Y * 0.5)
            };

            int[] serialNos = new int[]
            {
                304726,
                304740
            };

            // Create drawer.
            _drawer = new Common.Drawer(lineFeeds, cradleMountPoints, cradleDimensions, serialNos);

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
        /// <param name="cradlePosition"></param>
        private void DrawSimulation(Vector cradlePosition)
        {
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

                g.DrawLine(Pens.LightGray, PhysicalToPixelPoint(new Vector(-_boardDimensions.X * 0.5, 0.0)), PhysicalToPixelPoint(new Vector(_boardDimensions.X * 0.5, 0.0)));
                g.DrawLine(Pens.LightGray, PhysicalToPixelPoint(new Vector(0.0, -_boardDimensions.Y * 0.5)), PhysicalToPixelPoint(new Vector(0.0, _boardDimensions.Y * 0.5)));

                // Draw the line feed points, cradle mount points, and lines between.
                var feedSize = new System.Drawing.Size(_feedSize, _feedSize);
                var physicalSize = PixelSizeToPhysical(feedSize);
                for (int idx = 0; idx < _drawer._lineFeedPoints.Length; ++idx)
                {
                    var lineFeedPoint = PhysicalToPixelPoint(_drawer._lineFeedPoints[idx] - (physicalSize * 0.5));
                    var cradleMountPoint = PhysicalToPixelPoint(_drawer._cradleMountPoints[idx] + cradlePosition - (physicalSize * 0.5));

                    g.DrawEllipse(Pens.Red, new System.Drawing.Rectangle(lineFeedPoint, feedSize));
                    g.DrawEllipse(Pens.Green, new System.Drawing.Rectangle(cradleMountPoint, feedSize));

                    var lineFeedCentrePoint = PhysicalToPixelPoint(_drawer._lineFeedPoints[idx]);
                    var cradleMountCentrePoint = PhysicalToPixelPoint(_drawer._cradleMountPoints[idx] + cradlePosition);

                    g.DrawLine(Pens.Purple, lineFeedCentrePoint, cradleMountCentrePoint);
                }

                // Draw cradle.
                var cradlePoint = PhysicalToPixelPoint(cradlePosition - (_drawer._cradleDimensions * 0.5));
                var cradleSize = PhysicalToPixelSize(_drawer._cradleDimensions);
                g.DrawRectangle(Pens.Blue, new System.Drawing.Rectangle(cradlePoint, cradleSize));
            }

            PictureSimulation.Invalidate();
        }

        private void PictureSimulation_MouseClick(object sender, MouseEventArgs e)
        {
            DrawSimulation(PixelPointToPhysical(new System.Drawing.Point(e.X, e.Y)));
        }

        private void PictureSimulation_MouseMove(object sender, MouseEventArgs e)
        {
            DrawSimulation(PixelPointToPhysical(new System.Drawing.Point(e.X, e.Y)));
        }

        private void PictureInput_DoubleClick(object sender, EventArgs e)
        {
            var openDialog = new OpenFileDialog();

            openDialog.Filter = "Raster Image Files (*.bmp,*.jpg,*.png)|*.bmp;*.jpg;*.png";
            openDialog.Title = "Open Input Image File";

            var result = openDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    var image = Image.FromFile(openDialog.FileName);
                    PictureInput.Image = image;
                }
                catch (Exception)
                {
                    MessageBox.Show(string.Format("Unable to load image \"{0}\"", openDialog.FileName), "Error Loading Image");
                }
            }
        }

        private void PictureInput_Click(object sender, EventArgs e)
        {

        }
    }
}
