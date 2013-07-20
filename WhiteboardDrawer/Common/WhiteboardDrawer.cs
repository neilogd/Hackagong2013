﻿using System;
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

        public class StepperReadyArgs: EventArgs
        {
            public int StepperIndex;
        }
        public delegate void StepperReady(object o, StepperReadyArgs e);
        public event StepperReady OnStepperReady = null;

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

        #endregion 

        public Drawer(Vector[] lineFeedPositions, Vector[] cradleMountPoints, Vector cradleDimensions, int[] stepperSerialNos)
        {
            _lineFeedPoints = lineFeedPositions;
            _cradleMountPoints = cradleMountPoints;
            _cradleDimensions = cradleDimensions;

            _steppers = new Phidgets.Stepper[_lineFeedPoints.Length];
            for (int idx = 0; idx < _steppers.Length; ++idx)
            {
                var stepper = new Phidgets.Stepper();
                var serialNo = stepperSerialNos[idx];

                stepper.Attach += new Phidgets.Events.AttachEventHandler(Stepper_Attach);
                stepper.Detach += new Phidgets.Events.DetachEventHandler(Stepper_Detach);
                stepper.Error += new Phidgets.Events.ErrorEventHandler(Stepper_Error);

                stepper.CurrentChange += new Phidgets.Events.CurrentChangeEventHandler(Stepper_CurrentChange);
                stepper.PositionChange += new Phidgets.Events.StepperPositionChangeEventHandler(Stepper_PositionChange);
                stepper.VelocityChange += new Phidgets.Events.VelocityChangeEventHandler(Stepper_VelocityChange);
                stepper.InputChange += new Phidgets.Events.InputChangeEventHandler(Stepper_InputChange);
                
                stepper.open(serialNo);


                _steppers[idx] = stepper;
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

        /// <summary>
        /// Move the cradle.
        /// </summary>
        /// <param name="targetPosition"></param>
        public void MoveCradle(Vector targetPosition)
        {
            // Get line lengths for cradle.
            double[] lengths = new double[_lineFeedPoints.Length];
            GetLineLengthsForCradle(targetPosition, out lengths);

            // Convert to positions for the stepper, and pass to steppers.
            for (int idx = 0; idx < _lineFeedPoints.Length; ++idx)
            {
                long currentStepperPosition = _steppers[idx].steppers[0].CurrentPosition;
                long targetStepperPosition = _targetLengthToStepperPosition(lengths[idx]);
                if (currentStepperPosition != targetStepperPosition)
                {
                    _steppers[idx].steppers[0].TargetPosition = targetStepperPosition;
                    _steppers[idx].steppers[0].Engaged = true;
                }
            }
        }

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
            attachedStepper.steppers[0].Acceleration = attachedStepper.steppers[0].AccelerationMax / 8;
            attachedStepper.steppers[0].VelocityLimit = attachedStepper.steppers[0].VelocityMax / 8;
            attachedStepper.steppers[0].CurrentLimit = attachedStepper.steppers[0].CurrentMax / 8.0;
            attachedStepper.steppers[0].CurrentPosition = 0;
            attachedStepper.steppers[0].TargetPosition = 0;

            // Event callback.
            if (OnStepperReady != null)
            {
                OnStepperReady(this, new StepperReadyArgs
                {
                    StepperIndex = GetStepperIndex(attachedStepper)
                });
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

            if (attachedStepper.steppers[0].CurrentPosition == attachedStepper.steppers[0].TargetPosition)
            {
                attachedStepper.steppers[0].Engaged = false;
            }


            // Event callback.
            if (OnLineFeedLengthChanged != null)
            {
                OnLineFeedLengthChanged(this, new LineFeedLengthChangedArgs
                {
                    LineIndex = GetStepperIndex(attachedStepper),        // stepper index is same as line index.
                    Length = _stepperPositionToTargetLength(attachedStepper.steppers[0].CurrentPosition)
                });
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
