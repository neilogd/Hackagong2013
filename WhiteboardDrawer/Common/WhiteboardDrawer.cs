using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
namespace WhiteboardDrawer.Common
{
    public class Drawer
    {
        #region Events

        public class DrawerReadyArgs: EventArgs
        {
            
        }
        public delegate void DrawerReady(object o, DrawerReadyArgs e);
        public event DrawerReady OnDrawerReady = null;

        public class LineFeedLengthChangedArgs : EventArgs
        {
            public int LineIndex;
            public double Length;
        }
        public delegate void LineFeedLengthChanged(object o, LineFeedLengthChangedArgs e);
        public event LineFeedLengthChanged OnLineFeedLengthChanged = null;

        #endregion

        #region Members

        /// <summary>
        /// Position of line feeds in millimetres.
        /// </summary>
        public Vector[] _lineFeedPoints = null;

        /// <summary>
        /// Cradle mount points in millimetres.
        /// Relative to cradle origin.
        /// </summary>
        public Vector[] _cradleMountPoints = null;

        /// <summary>
        /// Steppers to draw with.
        /// </summary>
        public Phidgets.Stepper[] _steppers = null;

        /// <summary>
        /// Acceleration multiplier.
        /// </summary>
        public double _accelerationMultiplier = 1.0 / 256.0;

        /// <summary>
        /// Velocity multiplier.
        /// </summary>
        public double _velocityMultiplier = 1.0 / 64.0;

        /// <summary>
        /// Current line feed length in millimetres.
        /// </summary>
        private double[] _lineFeedLength = null;

        /// <summary>
        /// Length to stepper position conversion.
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public delegate long TargetLengthToStepperPosition(double length);
        public TargetLengthToStepperPosition _targetLengthToStepperPosition = null;

        /// <summary>
        /// Stepper position to length conversion.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public delegate double StepperPositionToTargetLength(long stepperPosition);
        public StepperPositionToTargetLength _stepperPositionToTargetLength = null;

        /// <summary>
        /// Cradle dimensions.
        /// </summary>
        public Vector _cradleDimensions;


        /// <summary>
        /// Serial numbers.
        /// </summary>
        private int[] _stepperSerialNos;


        /// <summary>
        /// Target position.
        /// </summary>
        private Vector _targetPosition = new Vector(0.0, 0.0);

        /// <summary>
        /// Estimated position.
        /// </summary>
        private Vector _estimatedPosition = new Vector(0.0, 0.0);

        /// <summary>
        /// Target waypoint list.
        /// </summary>
        private List<Vector> _targetWaypointList = new List<Vector>();

        #endregion 

        public Drawer(Vector[] lineFeedPositions, Vector[] cradleMountPoints, Vector cradleDimensions, int[] stepperSerialNos)
        {
            _lineFeedPoints = lineFeedPositions;
            _cradleMountPoints = cradleMountPoints;
            _cradleDimensions = cradleDimensions;

            _lineFeedLength = new double[_lineFeedPoints.Length];

            _steppers = new Phidgets.Stepper[_lineFeedPoints.Length];
            _stepperSerialNos = stepperSerialNos;
            for (int idx = 0; idx < _steppers.Length; ++idx)
            {
                var stepper = new Phidgets.Stepper();

                stepper.Attach += new Phidgets.Events.AttachEventHandler(Stepper_Attach);
                stepper.Detach += new Phidgets.Events.DetachEventHandler(Stepper_Detach);
                stepper.Error += new Phidgets.Events.ErrorEventHandler(Stepper_Error);

                stepper.CurrentChange += new Phidgets.Events.CurrentChangeEventHandler(Stepper_CurrentChange);
                stepper.PositionChange += new Phidgets.Events.StepperPositionChangeEventHandler(Stepper_PositionChange);
                stepper.VelocityChange += new Phidgets.Events.VelocityChangeEventHandler(Stepper_VelocityChange);
                stepper.InputChange += new Phidgets.Events.InputChangeEventHandler(Stepper_InputChange);
               
                _steppers[idx] = stepper;

                // Reset line feed length.
                _lineFeedLength[idx] = 0.0;
            }
        }

        public void Open()
        {
            for (int idx = 0; idx < _steppers.Length; ++idx)
            {
                var stepper = _steppers[idx];
                stepper.open(_stepperSerialNos[idx]);
            }
        }

        public bool IsReady()
        {
            for (int idx = 0; idx < _steppers.Length; ++idx)
            {
                var stepper = _steppers[idx];

                if (stepper.Attached == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get line length
        /// </summary>
        /// <param name="feedPosition">Feed position</param>
        /// <param name="targetPosition">Target position</param>
        /// <returns></returns>
        public double GetLineLength(Vector feedPosition, Vector targetPosition)
        {
            return (targetPosition - feedPosition).Length;
        }

        /// <summary>
        /// Get line lengths.
        /// </summary>
        /// <param name="feedPositions">Feed positions</param>
        /// <param name="targetPosition">Target positions</param>
        /// <param name="outLengths">Out lengths</param>
        /// <returns></returns>
        public void GetLineLengths(Vector[] feedPositions, Vector[] targetPositions, out double[] outLengths)
        {
            if (feedPositions.Length != targetPositions.Length)
            {
                throw new Exception("Line feed positions and target positions are mismatched.");
            }
            outLengths = new double[feedPositions.Length];
            for(int idx = 0; idx < feedPositions.Length; ++idx)
            {
                outLengths[idx] = GetLineLength(feedPositions[idx], targetPositions[idx]);
            }
        }

        /// <summary>
        /// Get line lengths for the cradle.
        /// Takes mount points into account.
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <param name="outLengths"></param>
        public void GetLineLengthsForCradle(Vector targetPosition, out double[] outLengths)
        {
            if(_lineFeedPoints.Length != _cradleMountPoints.Length)
            {
                throw new Exception("Line feed positions and cradle mount points are mismatched.");
            }
            var targetPositions = new Vector[_lineFeedPoints.Length];
            for (int idx = 0; idx < _lineFeedPoints.Length; ++idx)
            {
                var lineFeedPosition = _lineFeedPoints[idx];
                var cradleMountPoint = _cradleMountPoints[idx];
                targetPositions[idx] = targetPosition + cradleMountPoint;
            }

            GetLineLengths(_lineFeedPoints, targetPositions, out outLengths);
        }

        /// <summary>
        /// Move the cradle.
        /// </summary>
        /// <param name="targetPosition"></param>
        public void MoveCradle(Vector targetPosition)
        {
            // Early out avoid crash.
            if (IsReady() == false)
            {
                return;
            }

            _targetPosition = targetPosition;

            // Get line lengths for cradle.
            double[] targetLengths = new double[_lineFeedPoints.Length];
            GetLineLengthsForCradle(targetPosition, out targetLengths);

            // Calculate the length each needs to travel (velocity calculation)
            double maxLengthDifference = 0.0;
            double[] lengthDifferences = new double[_lineFeedLength.Length];
            for(int idx = 0; idx < _lineFeedLength.Length; ++idx)
            {
                lengthDifferences[idx] = System.Math.Abs(targetLengths[idx] - _lineFeedLength[idx]);
                maxLengthDifference = System.Math.Max(lengthDifferences[idx], maxLengthDifference);
            }

            // Convert to positions for the stepper, and pass to steppers.
            for (int idx = 0; idx < _lineFeedPoints.Length; ++idx)
            {
                long currentStepperPosition = _steppers[idx].steppers[0].CurrentPosition;
                long targetStepperPosition = _targetLengthToStepperPosition(targetLengths[idx]);
                double motionFraction = lengthDifferences[idx] / maxLengthDifference;
                if (currentStepperPosition != targetStepperPosition)
                {
                    _steppers[idx].steppers[0].Acceleration = _steppers[idx].steppers[0].AccelerationMin;
                    _steppers[idx].steppers[0].VelocityLimit = _steppers[idx].steppers[0].VelocityMax * motionFraction * _velocityMultiplier;
                    _steppers[idx].steppers[0].TargetPosition = targetStepperPosition;
                    _steppers[idx].steppers[0].Engaged = true;
                }
            }
        }

        #region Utility

        /// <summary>
        /// Get estimated cradle position.
        /// </summary>
        /// <returns></returns>
        public Vector GetEstimatedCradlePosition()
        {
            return _estimatedPosition;
        }

        /// <summary>
        /// Estimate the cradle position.
        /// </summary>
        /// <param name="iterations"></param>
        /// <returns></returns>
        public Vector EstimatedCradlePosition(int iterations, double stepSize, Vector estimatedPosition)
        {
            for (int iterationIdx = 0; iterationIdx < iterations; ++iterationIdx)
            {
                // For each mount point, move estimated position slightly in the right direction.
                for (int mountPointIdx = 0; mountPointIdx < _cradleMountPoints.Length; ++mountPointIdx)
                {
                    var mointPointRelative = _cradleMountPoints[mountPointIdx];
                    var mountPointPosition = estimatedPosition + mointPointRelative;
                    var mountPointDifference = (_lineFeedPoints[mountPointIdx] - mountPointPosition);
                    var mountPointDistance = mountPointDifference.Length;
                    var lineLengthDifference = mountPointDistance - _lineFeedLength[mountPointIdx];

                    mountPointDifference.Normalize();
                    estimatedPosition += mountPointDifference * stepSize * lineLengthDifference;
                }
            }

            return estimatedPosition;
        }

        #endregion

        #region Stepper Events

        int GetStepperIndex(Phidgets.Stepper stepper)
        {
            // Find in list.
            for (int idx = 0; idx < _steppers.Length; ++idx)
            {
                if (stepper == _steppers[idx])
                {
                    return idx;
                }
            }
            throw new Exception("Unable to find stepper.");
        }

        //Stepper attach event handler...populate the available fields and controls
        void Stepper_Attach(object sender, Phidgets.Events.AttachEventArgs e)
        {
            Phidgets.Stepper attachedStepper = (Phidgets.Stepper)sender;

            // Set 1/8th of max for the mean time. Will tweak later on.
            attachedStepper.steppers[0].Acceleration = attachedStepper.steppers[0].AccelerationMax;
            attachedStepper.steppers[0].VelocityLimit = attachedStepper.steppers[0].VelocityMax;
            attachedStepper.steppers[0].CurrentLimit = attachedStepper.steppers[0].CurrentMax / 1.0;
            attachedStepper.steppers[0].CurrentPosition = 0;
            attachedStepper.steppers[0].TargetPosition = 0;
            attachedStepper.steppers[0].Engaged = false;

            // Event callback.
            if (OnDrawerReady != null)
            {
                if (IsReady())
                {
                    // Centre plz.
                    double[] targetLengths = new double[_lineFeedPoints.Length];
                    GetLineLengthsForCradle(_targetPosition, out targetLengths);

                    for (int idx = 0; idx < _lineFeedPoints.Length; ++idx)
                    {
                        var stepper = _steppers[idx];

                        stepper.steppers[0].CurrentPosition = _targetLengthToStepperPosition(targetLengths[idx]);
                        stepper.steppers[0].TargetPosition = stepper.steppers[0].CurrentPosition;
                    }

                    // Call drawer ready event.
                    OnDrawerReady(this, new DrawerReadyArgs
                    {
                    });
                }
            }
        }

        //Stepper Detach event handler...Clear all the fields and disable all the controls
        void Stepper_Detach(object sender, Phidgets.Events.DetachEventArgs e)
        {
            Phidgets.Stepper detachedStepper = (Phidgets.Stepper)sender;

        }

        void Stepper_Error(object sender, Phidgets.Events.ErrorEventArgs e)
        {
            /*
            Phidgets.Phidget phid = (Phidgets.Phidget)sender;
            DialogResult result;
            switch (e.Type)
            {
                case PhidgetException.ErrorType.PHIDGET_ERREVENT_BADPASSWORD:
                    phid.close();
                    TextInputBox dialog = new TextInputBox("Error Event",
                        "Authentication error: This server requires a password.", "Please enter the password, or cancel.");
                    result = dialog.ShowDialog();
                    if (result == DialogResult.OK)
                        openCmdLine(phid, dialog.password);
                    else
                        Environment.Exit(0);
                    break;
                default:
                    if (!errorBox.Visible)
                        errorBox.Show();
                    break;
            }
            errorBox.addMessage(DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + ": " + e.Description);
             * */
        }

        void Stepper_CurrentChange(object sender, Phidgets.Events.CurrentChangeEventArgs e)
        {
        }

        void Stepper_PositionChange(object sender, Phidgets.Events.StepperPositionChangeEventArgs e)
        {
            Phidgets.Stepper attachedStepper = (Phidgets.Stepper)sender;

            /*
            if (attachedStepper.steppers[0].CurrentPosition == attachedStepper.steppers[0].TargetPosition)
            {
                attachedStepper.steppers[0].Engaged = false;
            }
             **/

            var lineIndex = GetStepperIndex(attachedStepper);
            _lineFeedLength[lineIndex] = _stepperPositionToTargetLength(attachedStepper.steppers[0].CurrentPosition);

            // Event callback.
            if (OnLineFeedLengthChanged != null)
            {
                OnLineFeedLengthChanged(this, new LineFeedLengthChangedArgs
                {
                    LineIndex = lineIndex,        // stepper index is same as line index.
                    Length = _lineFeedLength[lineIndex]
                });
            }

            // Estimate cradle position.
            _estimatedPosition = EstimatedCradlePosition(32, 0.2f, _estimatedPosition);

            //
            if (_targetWaypointList.Count >= 2)
            {
                var waypointDiff = _targetWaypointList[1] - _targetWaypointList[0];
                var waypointDist = waypointDiff.Length;
                var estimatedDiff = _targetWaypointList[1] - _estimatedPosition;
                var estimatedDist = estimatedDiff.Length;

                if (estimatedDist < (waypointDist * 0.5f))
                {

                }
            }
        }

        void Stepper_VelocityChange(object sender, Phidgets.Events.VelocityChangeEventArgs e)
        {
        }

        void Stepper_InputChange(object sender, Phidgets.Events.InputChangeEventArgs e)
        {
        }

        #endregion

    }
}
