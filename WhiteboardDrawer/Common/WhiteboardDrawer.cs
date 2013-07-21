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

        public class DrawerArrivedArgs : EventArgs
        {

        }
        public delegate void DrawerArrived(object o, DrawerArrivedArgs e);
        public event DrawerArrived OnDrawerArrived = null;

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
        public double _accelerationMultiplier = 1.0 / 128.0;

        /// <summary>
        /// Velocity multiplier.
        /// </summary>
        public double _velocityMultiplier = 1.0 / 64.0;

        /// <summary>
        /// Subdivision prevision.
        /// </summary>
        public double _subdivisionPrecision = 5.0;

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
        /// COM port.
        /// </summary>
        private string _comPort;

        /// <summary>
        /// Serial port.
        /// </summary>
        private System.IO.Ports.SerialPort _serialPort = null;

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

        public Drawer(Vector[] lineFeedPositions, Vector[] cradleMountPoints, Vector cradleDimensions, int[] stepperSerialNos, string comPort)
        {
            _lineFeedPoints = lineFeedPositions;
            _cradleMountPoints = cradleMountPoints;
            _cradleDimensions = cradleDimensions;

            _lineFeedLength = new double[_lineFeedPoints.Length];

            _steppers = new Phidgets.Stepper[_lineFeedPoints.Length];
            _stepperSerialNos = stepperSerialNos;
            _comPort = comPort;
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
            // Open COM port.
            _serialPort = new System.IO.Ports.SerialPort(_comPort);
            _serialPort.BaudRate = 9600;
            _serialPort.Parity = System.IO.Ports.Parity.None;
            _serialPort.ReadTimeout = 1000;
            _serialPort.Encoding = Encoding.ASCII;
            try 
            {
                _serialPort.Open();
                _serialPort.Write(new char[] { '0' }, 0, 1);
                //_serialPort.WriteLine("0\n");
                try
                {
                    System.Console.WriteLine(_serialPort.ReadLine());
                }
                catch (Exception)
                {
                }
                System.Threading.Thread.Sleep(2000);
                _serialPort.Write(new char[] { '1' }, 0, 1);
                //_serialPort.WriteLine("1\n");
                try
                {
                    System.Console.WriteLine(_serialPort.ReadLine());
                }
                catch (Exception)
                {
                }
                System.Threading.Thread.Sleep(2000);
                _serialPort.WriteLine("2\n");
                try
                {
                    System.Console.WriteLine(_serialPort.ReadLine());
                }
                catch (Exception)
                {
                }
                System.Threading.Thread.Sleep(2000);
                _serialPort.WriteLine("3\n");
                try
                {
                    System.Console.WriteLine(_serialPort.ReadLine());
                }
                catch (Exception)
                {
                }
                _serialPort.DiscardInBuffer();
            }
            catch (Exception)
            {
            } 
            
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
        /// Activate pen.
        /// </summary>
        /// <param name="pen"></param>
        /// <returns></returns>
        public void ActivatePen(int pen)
        {
            try
            {
                if (pen < 3)
                {
                    char[] penIds = new char[] { '0', '1', '2' };
                    _serialPort.Write(new char[] { penIds[pen] }, 0, 1);
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Deactivate all pens.
        /// </summary>
        public void DeactivatePens()
        {
            try
            {
                _serialPort.Write(new char[] { '3' }, 0, 1);
            }
            catch (Exception)
            {
            }
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

        public void AddWaypoint(Vector targetPosition)
        {
            _targetWaypointList.Add(targetPosition);
        }

        public void StartMoving()
        {
            MoveCradle(_targetWaypointList[0]);
            _targetWaypointList.RemoveAt(0);
        }

        /// <summary>
        /// Move cradle straight.
        /// </summary>
        public void MoveCradleStraight(Vector targetPosition, bool append = false)
        {
            {
                // Estimate position first. Important :)
                _estimatedPosition = EstimatedCradlePosition(256, 0.5, _estimatedPosition);
                var currPosition = _estimatedPosition;

                bool shouldMove = true;

                // If we need to append, change curr position.
                if (append && _targetWaypointList.Count > 0)
                {
                    currPosition = _targetWaypointList[_targetWaypointList.Count - 1];
                }
                else
                {
                    _targetWaypointList.Clear();
                }


                //
                var direction = (targetPosition - currPosition);
                var distanceToMove = direction.Length;
                direction.Normalize();

                for (double alpha = _subdivisionPrecision; alpha < distanceToMove; alpha += _subdivisionPrecision)
                {
                    var waypointPosition = currPosition + direction * alpha;

                    _targetWaypointList.Add(waypointPosition);
                }

                // Add end.
                _targetWaypointList.Add(targetPosition);

                // Should be this.
                if (_targetWaypointList.Count > 0 && shouldMove)
                {
                    MoveCradle(_targetWaypointList[0]);
                    _targetWaypointList.RemoveAt(0);
                }
            }
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
            for (int idx = 0; idx < _lineFeedLength.Length; ++idx)
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
                    _steppers[idx].steppers[0].Acceleration = Math.Max(_steppers[idx].steppers[0].AccelerationMax * _accelerationMultiplier, _steppers[idx].steppers[0].AccelerationMin);
                    _steppers[idx].steppers[0].VelocityLimit = Math.Max(_steppers[idx].steppers[0].VelocityMax * motionFraction * _velocityMultiplier, _steppers[idx].steppers[0].VelocityMin);
                    _steppers[idx].steppers[0].TargetPosition = targetStepperPosition;
                    _steppers[idx].steppers[0].Engaged = true;
                }
            }

            // Estimate position.
            _estimatedPosition = EstimatedCradlePosition(128, 0.5, targetPosition);
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
        /// ARE WE THERE YET ARE WE THERE YET ARE WE THERE YET
        /// </summary>
        /// <returns></returns>
        public bool AreWeThereYet()
        {
            return (_estimatedPosition - _targetPosition).Length < (_subdivisionPrecision * 0.5);
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

        public void NextWaypoint()
        {
            // waypointing. Check if we are about half way to the target position (this is so we stay accelerated).
            // If we are, try to move to the next waypoint.
            // Only do on the second line.
            if (_targetWaypointList.Count > 0)
            {
                var estimatedDiff = _targetPosition - _estimatedPosition;
                var estimatedDist = estimatedDiff.Length;

                while (estimatedDist < (_subdivisionPrecision * 0.5))
                {
                    if (_targetWaypointList.Count > 0)
                    {
                        MoveCradle(_targetWaypointList[0]);

                        System.Console.WriteLine(string.Format("Moving to: {0}", _targetWaypointList[0].ToString()));
                        estimatedDiff = _targetWaypointList[0] - _estimatedPosition;
                        estimatedDist = estimatedDiff.Length;
                        _targetWaypointList.RemoveAt(0);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public void Reset()
        {
            // Centre plz.
            double[] targetLengths = new double[_lineFeedPoints.Length];
            GetLineLengthsForCradle(_targetPosition, out targetLengths);

            for (int idx2 = 0; idx2 < 4; ++idx2)
            {
                for (int idx = 0; idx < _lineFeedPoints.Length; ++idx)
                {
                    var stepper = _steppers[idx];

                    stepper.steppers[0].CurrentPosition = _targetLengthToStepperPosition(targetLengths[idx]);
                    stepper.steppers[0].TargetPosition = stepper.steppers[0].CurrentPosition;
                    stepper.steppers[0].Engaged = true;

                    System.Threading.Thread.Sleep(30);

                    _lineFeedLength[idx] = targetLengths[idx];
                }
            }

            // Estimate stuff.
            //EstimatedCradlePosition(256, 0.5, _estimatedPosition);

            _estimatedPosition = _targetPosition;
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
            //lock (_steppers)
            {
                Phidgets.Stepper attachedStepper = (Phidgets.Stepper)sender;

                attachedStepper.steppers[0].Acceleration = attachedStepper.steppers[0].AccelerationMax;
                attachedStepper.steppers[0].VelocityLimit = attachedStepper.steppers[0].VelocityMax;
                attachedStepper.steppers[0].CurrentLimit = attachedStepper.steppers[0].CurrentMax;
                //attachedStepper.steppers[0].Engaged = false;

                // Event callback.
                if (OnDrawerReady != null)
                {
                    if (IsReady())
                    {
                        // Call drawer ready event.
                        OnDrawerReady(this, new DrawerReadyArgs
                        {
                        });
                    }
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

        }

        void Stepper_CurrentChange(object sender, Phidgets.Events.CurrentChangeEventArgs e)
        {
        }

        void Stepper_PositionChange(object sender, Phidgets.Events.StepperPositionChangeEventArgs e)
        {
            //lock (_steppers)
            {
                Phidgets.Stepper attachedStepper = (Phidgets.Stepper)sender;

                var lineIndex = GetStepperIndex(attachedStepper);
                _lineFeedLength[lineIndex] = _stepperPositionToTargetLength(attachedStepper.steppers[0].CurrentPosition);

                //System.Console.WriteLine(string.Format("Line {0}, Pos {1}", lineIndex, _lineFeedLength[lineIndex]));
                
                // Estimate cradle position.
                _estimatedPosition = EstimatedCradlePosition(32, 0.5, _estimatedPosition);

                // Event callback.
                if (OnLineFeedLengthChanged != null)
                {
                    OnLineFeedLengthChanged(this, new LineFeedLengthChangedArgs
                    {
                        LineIndex = lineIndex,        // stepper index is same as line index.
                        Length = _lineFeedLength[lineIndex]
                    });
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
