using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ACS.SPiiPlusNET;
using System.Windows.Media.Media3D;

namespace RamanMapping.Model
{
    public enum State
    {
        Enable = 0,
        Moving,
        InPosition
    }

    public enum Limit
    {
        Right = 0,
        Left
    }

    public class AxisLimitEventArg : EventArgs
    {
        public AxisLimitEventArg( int _axisNo, Limit _limit, bool _value)
        {
            axisNo = _axisNo;
            limit = _limit;
            value = _value;
        }
        public int axisNo;
        public Limit limit;
        public bool value;
    }

    public class MotorStateEventArg : EventArgs
    {
        public MotorStateEventArg(int _axisNo, State _state, bool _value)
        {
            axisNo = _axisNo;
            state = _state;
            value = _value;
        }
        public int axisNo;
        public State state;
        public bool value;
    }

    static class LASER
    {
        public const int PORT = 1;      // 0 from laser inscription
        public const int PIN = 1;       // 0 from laser inscription
    }

    static class LED
    {
        public const int PORT = 1;
        public const int PIN = 0;
    }

    public class MotorPositionEventArg : EventArgs
    {
        public MotorPositionEventArg(int _axisNo, double _rValue, double _fValue)
        {
            axisNo = _axisNo;
            rValue = _rValue;
            fValue = _fValue;
        }
        public int axisNo;
        public double rValue;
        public double fValue;
    }

    class MotorState
    {
        public bool[] enabled = { false, false, false };
        public bool[] moving = { false, false, false };
        public bool[] inPosition = { false, false, false };
    }

    class AxisLimit
    {
        public bool[] limitL = { false, false, false };
        public bool[] limitR = { false, false, false };
    }

    public sealed class MotorManager
    {
        private static MotorManager _instance;

        private Api _ACS;
        public delegate void SegmentInscriptionComplete();
        SegmentInscriptionComplete _segmentInscriptionComplete;
        ManualResetEvent[] _motionStopped = new ManualResetEvent[] { new ManualResetEvent(false),
                                                new ManualResetEvent(false) };

        public event EventHandler<MotorStateEventArg> MotorStateChanged;
        public event EventHandler<MotorPositionEventArg> MotorPositionChanged;
        public event EventHandler<bool> LaserStateChanged;
        public event EventHandler<bool> LedStateChanged;
        public event EventHandler<AxisLimitEventArg> AxisLimitChanged;

        private MotorState motorState = new MotorState();
        private bool LaserStateON = false;
        private bool LedStateON = false;

        private AxisLimit axisLimit = new AxisLimit();

        Thread motorMonitorWorker;
        bool _stopMonitoring = false;
        public string SerialNumber;

        private MotorManager()
        {
            _ACS = new Api();

            _ACS.PHYSICALMOTIONEND += _ACS_PHYSICALMOTIONEND;
            _ACS.PROGRAMEND += _ACS_PROGRAMEND;

        }

        public static MotorManager getInstance()
        {
            if (_instance == null)
            {
                _instance = new MotorManager();
            }

            return _instance;
        }


        public bool Open()
        {
            try
            {
                // Clear connection list from SPiiPlus UserMode-Driver (UMD)
                TernminateUMD_Connection();

                if (!GlobalVariables.motorSettings.UseSimulator)
                {
                    // TCP/IP (Ethernet) 
                    _ACS.OpenCommEthernetTCP(
                        GlobalVariables.motorSettings.IPAddress,  // IP Address (Default : 10.0.0.100)
                        GlobalVariables.motorSettings.Port        // TCP/IP Port nubmer (default : 701)
                        );
                    SerialNumber = _ACS.GetSerialNumber();
                }
                else
                {
                    _ACS.OpenCommSimulator();
                    SerialNumber = "simulator";
                }

                
                motorMonitorWorker = new Thread(MotorMonitoring);
                motorMonitorWorker.Start();
                // If you want to enable several axes, 
                // 
                // Ex) Eanble three axes (0, 1, 6)
                //
                //Axis[] AxisList = new Axis[] { (Axis)0, (Axis)1, (Axis)2, (Axis)(-1) }; //     !!!! Important !! Must insert '-1' at the last
                //_ACS.EnableM(AxisList);

                //InitialMotionProfile();
                SetLaserOnOff(false);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error starting Motor manager");
            }

            return false;
        }

        private void RaiseMotorStateChangedEvent(MotorStateEventArg eventArgs)
        {
            MotorStateChanged?.Invoke(this, eventArgs);
        }

        private void RaiseMotorPositionChangedEvent(MotorPositionEventArg eventArgs)
        {
            MotorPositionChanged?.Invoke(this, eventArgs);
        }

        private void RaiseLaserStateChangedEvent(bool state)
        {
            LaserStateChanged?.Invoke(this, state);
        }

        private void RaiseLedStateChangedEvent(bool state)
        {
            LedStateChanged?.Invoke(this, state);
        }

        private void RaiseAxisLimitChangedEvent(AxisLimitEventArg eventArgs)
        {
            AxisLimitChanged?.Invoke(this, eventArgs);
        }

        void MotorMonitoring()
        {
            MotorStates state;
            double refPos;
            double fbPos;

            object objReadVar = null;
            Array arrReadVector = null;
            const int TotalAxis = 3;

            while (!_stopMonitoring)
            {
                try
                {
                    for (int i = 0; i < TotalAxis; i++)
                    {
                        state = GetMotorState(i);
                        bool isEnabled = false;
                        if ((state & MotorStates.ACSC_MST_ENABLE) != 0)
                        {
                            isEnabled = true;
                        }
                        if (motorState.enabled[i] != isEnabled)
                        {
                            motorState.enabled[i] = isEnabled;
                            //todo: fire the change
                            RaiseMotorStateChangedEvent(new MotorStateEventArg(i, State.Enable, isEnabled));
                        }

                        if (motorState.moving[i])
                        {
                            refPos = GetReferencePosition(i);
                            fbPos = GetFeedBackPosition(i);
                            RaiseMotorPositionChangedEvent(new MotorPositionEventArg(i, refPos, fbPos));
                        }

                        if ((state & MotorStates.ACSC_MST_MOVE) != 0)
                        {
                            isEnabled = true;
                        }
                        else
                        {
                            isEnabled = false;
                        }
                        if (motorState.moving[i] != isEnabled)
                        {
                            motorState.moving[i] = isEnabled;
                            //todo: fire the change
                            RaiseMotorStateChangedEvent(new MotorStateEventArg(i, State.Moving, isEnabled));
                        }

                        if ((state & MotorStates.ACSC_MST_INPOS) != 0)
                        {
                            isEnabled = true;
                        } else
                        {
                            isEnabled = false;
                        }
                        if(motorState.inPosition[i] != isEnabled)
                        {
                            motorState.inPosition[i] = isEnabled;
                            RaiseMotorStateChangedEvent(new MotorStateEventArg(i, State.InPosition, isEnabled));
                        }
                    }

                    // check the axis limit
                    {
                        objReadVar = _ACS.ReadVariableAsVector("FAULT", ProgramBuffer.ACSC_NONE, 0, TotalAxis - 1, -1, -1);
                        if (objReadVar != null)
                        {
                            arrReadVector = objReadVar as Array;
                            if (arrReadVector != null)
                            {
                                bool bLimitL = false;
                                bool bLimitR = false;
                                for (int i = 0; i < TotalAxis; i++)
                                {
                                    int fault = (int)arrReadVector.GetValue(i);
                                    
                                    if ((fault & (int)SafetyControlMasks.ACSC_SAFETY_LL) != 0)
                                    {
                                        bLimitL = true;
                                    }
                                    else
                                    {
                                        bLimitL = false;
                                    }
                                    if ((fault & (int)SafetyControlMasks.ACSC_SAFETY_RL) != 0)
                                    {
                                        bLimitR = true;
                                    }
                                    else
                                    {
                                        bLimitR = false;
                                    }

                                    if(axisLimit.limitL[i] != bLimitL)
                                    {
                                        axisLimit.limitL[i] = bLimitL;
                                        // raise the event
                                        RaiseAxisLimitChangedEvent(new AxisLimitEventArg(i, Limit.Left, bLimitL));
                                    }
                                    if (axisLimit.limitR[i] != bLimitR)
                                    {
                                        axisLimit.limitR[i] = bLimitR;
                                        // raise the event
                                        RaiseAxisLimitChangedEvent(new AxisLimitEventArg(i, Limit.Right, bLimitR));
                                    }
                                }
                            }
                        }
                    }

                    {
                        bool isLaserOn = GetLaserState();
                        if(LaserStateON != isLaserOn)
                        {
                            LaserStateON = isLaserOn;
                            RaiseLaserStateChangedEvent(LaserStateON);
                        }
                    }

                    {
                        bool isLedOn = GetLedState();
                        if (LedStateON != isLedOn)
                        {
                            LedStateON = isLedOn;
                            RaiseLedStateChangedEvent(LedStateON);
                        }
                    }
                    Thread.Sleep(300);
                } catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
        }

        public void InitialMotionProfile()
        {
            try
            {
                double vel = GlobalVariables.motorSettings.Velocity;
                double accVel = GlobalVariables.motorSettings.Acceleration;
                double decVel = GlobalVariables.motorSettings.Deceleration;
                // for X Y Z: set vel from config file and acc and  dec to 5 times of vel
                for (int i = 0; i < 3; i++)
                {
                    _ACS.SetVelocityImm((Axis)i, vel);
                    _ACS.SetAccelerationImm((Axis)i, accVel);
                    _ACS.SetDecelerationImm((Axis)i, decVel);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("MotorManager exception: " + e.Message);
            }
        }

        double GetReferencePosition(int axis)
        {
            return _ACS.GetRPosition((Axis)axis);
        }

        double GetFeedBackPosition(int axis)
        {
            return _ACS.GetFPosition((Axis)axis);
        }

        #region acs_callbacks
        void _ACS_PHYSICALMOTIONEND(AxisMasks axis)
        {
            if ((axis & AxisMasks.ACSC_MASK_AXIS_0) > 0)
                _motionStopped[0].Set();
            if ((axis & AxisMasks.ACSC_MASK_AXIS_1) > 0)
                _motionStopped[1].Set();
        }

        void _ACS_PROGRAMEND(BufferMasks buffer)
        {
            int bit = 0x01;
            int bufferNo = 0;//buffer that ended
            // Param value is bit number 
            // Bit Number = Axis Number
            for (int i = 0; i < 32; i++)
            {
                if ((int)buffer == bit)
                {
                    bufferNo = i;
                    break;
                }
                bit = bit << 1;
            }
        }
        #endregion

        public bool GetVelocity(out double xVel, out double yVel, out double zVel)
        {
            xVel = yVel = zVel = 0;

            try
            {
                xVel = _ACS.GetVelocity(Axis.ACSC_AXIS_0);
                yVel = _ACS.GetVelocity(Axis.ACSC_AXIS_1);
                zVel = _ACS.GetVelocity(Axis.ACSC_AXIS_2);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("MotorManager exception: " + e.Message);
            }

            return false;
        }

        public bool GoToHome(int buffer, bool refresh = true)
        {
            try
            {
                var t = Task.Run(() =>
                {
                    bool retry = false;
                    if (!GlobalVariables.motorSettings.UseSimulator)
                    {
                        //run buffer 1
                        try
                        {
                            _ACS.RunBuffer((ProgramBuffer)buffer, null);
                        } catch(Exception ex)
                        {
                            Console.WriteLine("GoToHome Error, reboot controller");
                            _ACS.ControllerReboot(120000);
                            retry = true;
                        }
                    }
                    if (retry)
                    {
                        _ACS.RunBuffer((ProgramBuffer)buffer, null);
                    }
                    //set zero position - do we need or does the buffer program do it?
                    if (refresh)
                    {
                        //SetZeroPosition();
                    }
                });
                t.Wait();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Homing error");
            }

            return false;
        }

        public bool Homing()
        {
            try
            {
                var t = Task.Run(() =>
                {
                    if (!GlobalVariables.motorSettings.UseSimulator)
                    {
                        double[] pos = { 0, 0, 0};
                        MoveToPointXYZ_Absolute(pos);
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Homing error");
            }

            return false;
        }

        void SetZeroPosition()
        {
            _ACS.SetFPosition((Axis)0, 0);
            _ACS.SetFPosition((Axis)1, 0);
            _ACS.SetFPosition((Axis)2, 0);
        }

        public void SetLaserOnOff(bool state)
        {
            _ACS.SetOutput(LASER.PORT, LASER.PIN, state ? 1 : 0);
        }

        public void SetLedOnOff(bool state)
        {
            _ACS.SetOutput(LED.PORT, LED.PIN, state ? 1 : 0);
        }

        public bool GetStatus(out bool isEnabled, out bool isMoving, out bool inPosition,
            out string rpos, out string fpos)
        {
            // Instruction 1. Using library functions - acsc_GetFPosition, acsc_GetRPosition, ....
            // Instruction 2. Read ACS variable - Already defined almost things (FPOS, RPOS, ...)
            //                Motion parameters and state is array (Max length is total number of axes)
            //
            // * Library function can read only 1 axis information, so if you want to read several axes, you have to call the function many times.
            //   (This may cause communication delay.)
            //   Recommand (if you want to read many axes) : read/write variable using ReadVariable, ReadVariableScalar, ReadVariableVector, ReadVariableMatrix

            //   
            // Get Motor State 
            // ACSPL+ Variable : MST (integer)
            //m_nMotorState = _ACS.GetMotorState((Axis)iAxisNo);

            isEnabled = isMoving = inPosition = false;
            rpos = fpos = "";

            try
            {
                var status = (int[])_ACS.ReadVariable("MST");
                isEnabled = ((status[0] & (int)MotorStates.ACSC_MST_ENABLE) != 0) &&
                    ((status[1] & (int)MotorStates.ACSC_MST_ENABLE) != 0) &&
                    ((status[2] & (int)MotorStates.ACSC_MST_ENABLE) != 0);
                isMoving = ((status[0] & (int)MotorStates.ACSC_MST_MOVE) != 0) ||
                    ((status[1] & (int)MotorStates.ACSC_MST_MOVE) != 0) ||
                    ((status[2] & (int)MotorStates.ACSC_MST_MOVE) != 0);
                inPosition = ((status[0] & (int)MotorStates.ACSC_MST_INPOS) != 0) &&
                    ((status[1] & (int)MotorStates.ACSC_MST_INPOS) != 0) &&
                    ((status[2] & (int)MotorStates.ACSC_MST_INPOS) != 0);

                var pos = (double[])_ACS.ReadVariable("RPOS");
                rpos = String.Format("{0:0.0000},{1:0.0000},{2:0.0000}", pos[0], pos[1], pos[2]);
                pos = (double[])_ACS.ReadVariable("FPOS");
                fpos = String.Format("{0:0.0000},{1:0.0000},{2:0.0000}", pos[0], pos[1], pos[2]);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("MotorManager exception: " + ex.Message);
            }

            return false;
        }

        private MotorStates GetMotorState(int axisNo)
        {
            return _ACS.GetMotorState((Axis)axisNo);
        }

        private bool GetLaserState()
        {
            return _ACS.GetOutput(LASER.PORT, LASER.PIN) == 0 ? false : true;
        }

        private bool GetLedState()
        {
            return _ACS.GetOutput(LED.PORT, LED.PIN) == 0 ? false : true;
        }

        public bool MoveToPoint(Point3D pt, bool wait)
        {
            try
            {
                Axis[] axes = { Axis.ACSC_AXIS_0, Axis.ACSC_AXIS_1, Axis.ACSC_AXIS_2, Axis.ACSC_NONE };
                double[] points = { pt.X, pt.Y, pt.Z };

                // Immediately start the motion of the axes X,Y to the
                // absolute target points
                _ACS.ToPointM(MotionFlags.ACSC_NONE, axes, points);

                // Finish the motion
                // End of the multi-point motion
                _ACS.EndSequenceM(axes);

                if (wait)
                {
                    foreach (var axis in axes)
                        _ACS.WaitMotionEnd(axis, 10000);
                }

                return true;
            }
            catch( Exception e)
            {
                Console.WriteLine("MotorManager exception: " + e.Message);
            }

            return false;
        }

        public bool MoveToPoint_Relative(int axisNo, double pos)
        {
            try
            {
                _ACS.ToPoint(
                        MotionFlags.ACSC_AMF_RELATIVE,      // Flat
                        (Axis)axisNo,      // Axis number
                        pos                         // Move distance
                        );

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("MotorManager exception: " + e.Message);
            }

            return false;
        }

        public bool MoveToPointXY_Relative(double[] pos)
        {
            try
            {
                Axis[] axes = { Axis.ACSC_AXIS_0, Axis.ACSC_AXIS_1, Axis.ACSC_NONE };
                _ACS.ToPointM(
                        MotionFlags.ACSC_AMF_RELATIVE,      // Flat
                        axes,                               // Axis number
                        pos                                 // Move distance
                        );

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("MotorManager exception: " + e.Message);
            }

            return false;
        }

        public bool MoveToPointXYZ_Relative(double[] pos)
        {
            try
            {
                Axis[] axes = { Axis.ACSC_AXIS_0, Axis.ACSC_AXIS_1, Axis.ACSC_AXIS_2, Axis.ACSC_NONE };
                _ACS.ToPointM(
                        MotionFlags.ACSC_AMF_RELATIVE,      // Flat
                        axes,                               // Axis number
                        pos                                 // Move distance
                        );

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("MotorManager exception: " + e.Message);
            }

            return false;
        }

        public bool MoveToPoint_Absolute(int axisNo, double pos)
        {
            try
            {
                _ACS.ToPoint(
                        0,                          // absolute
                        (Axis)axisNo,               // Axis number
                        pos                         // Move distance
                        );

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("MotorManager exception: " + e.Message);
            }

            return false;
        }

        public bool MoveToPointXYZ_Absolute(double[] pos)
        {
            try
            {
                Axis[] axes = { Axis.ACSC_AXIS_0, Axis.ACSC_AXIS_1, Axis.ACSC_AXIS_2, Axis.ACSC_NONE };
                _ACS.ToPointM(
                        0,                                  // absolute position
                        axes,                               // Axis number
                        pos                                 // Move distance
                        );

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("MotorManager exception: " + e.Message);
            }

            return false;
        }

        public bool SetInscriptionSegmentPath(List<System.Windows.Media.Media3D.Point3D> pts, double dwellms)
        {
            try
            {
                Axis[] axes = { Axis.ACSC_AXIS_0, Axis.ACSC_AXIS_1, Axis.ACSC_AXIS_2, Axis.ACSC_NONE };
                double[] points = { 0, 0, 0 };
                // Create multi-point motion of axis 0 and 1 with default
                // velocity without
                // dwell in the points
                //wait for GO command
                _ACS.MultiPointM(MotionFlags.ACSC_AMF_WAIT, axes, dwellms);
                // Add some points
                for (int index = 0; index < pts.Count; index++)
                {
                    points[0] = pts[index].X;
                    points[1] = pts[index].Y;
                    points[2] = pts[index].Z;
                    _ACS.AddPointM(axes, points);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("MotorManager exception: " + ex.Message);
            }

            return false;
        }

        public bool StartInscriptionSegment(SegmentInscriptionComplete inscriptionComplete)
        {
            try
            {
                _segmentInscriptionComplete = inscriptionComplete;
                _motionStopped[0].Reset();
                _motionStopped[1].Reset();

                Thread thread1 = new Thread(WaitForSegmentInscrptionComplete);
                thread1.Start();

                Axis[] axes = { Axis.ACSC_AXIS_0, Axis.ACSC_AXIS_1, Axis.ACSC_NONE };

                _ACS.EnableEvent(Interrupts.ACSC_INTR_PHYSICAL_MOTION_END);

                // Start the motion of 0 and 1
                _ACS.GoM(axes);

                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine("MotorManager exception: " + ex.Message);
            }

            return false;
        }

        public void WaitForSegmentInscrptionComplete()
        {
            WaitHandle.WaitAll(_motionStopped);

            _segmentInscriptionComplete();
        }

        public void EndInscriptionSegmentPath()
        {

            _ACS.DisableEvent(Interrupts.ACSC_INTR_PHYSICAL_MOTION_END);
            _segmentInscriptionComplete = null;

            Axis[] axes = { Axis.ACSC_AXIS_0, Axis.ACSC_AXIS_1, Axis.ACSC_NONE };
            // Finish the motion
            // End of the multi-point motion
            _ACS.EndSequenceM(axes);
        }

        public void EnableAxis(int axisNo)
        {
            try
            {
                // Enable selected axis
                _ACS.Enable((Axis)axisNo);
            } catch (Exception e)
            {
                Console.WriteLine("MotorManager exception: " + e.Message);
            }
        }

        public void DisableAxis(int axisNo)
        {
            try
            {
                // Disable selected axis
                _ACS.Disable((Axis)axisNo);
                // Disable multi axes : DisableM(int[] axisList)
            } catch (Exception e)
            {
                Console.WriteLine("MotorManager exception: " + e.Message);
            }
        }

        private void DisableAll()
        {
            try
            {
                // Disable all of axes
                _ACS.DisableAll();
            } catch(Exception e)
            {
                Console.WriteLine("MotorManager exception: " + e.Message);
            }
        }

        public void Jog(int axisNo, double vel, int posDir)
        {
            try
            {

                //_ACS.Jog(0, (Axis)axisNo, (double)posDir);
                _ACS.Jog(
                        MotionFlags.ACSC_AMF_VELOCITY,      // Velocity flag
                        (Axis)axisNo,  // Axis number
                        posDir * vel                  // Velocity
                        );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public void Halt(int axisNo = -1)
        {
            try
            {
                if (axisNo == -1)
                {
                    // There is no halt all command, so you need to user HaltM function
                    // 
                    // ex) You want to stop 0, 2, 5 axis
                    //     int[] m_arrAxisList = new int[] { 0, 2, 5, -1 };
                    // 
                    Axis[] arrAxisList = new Axis[] { (Axis)0, (Axis)1, (Axis)2, (Axis)(-1) };
                    _ACS.HaltM(arrAxisList);
                }
                else
                    _ACS.Halt((Axis)axisNo);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }


        private void TernminateUMD_Connection()
        {
            try
            {
                string terminateExceptionConnName = "ACS.Framework.exe";

                ACSC_CONNECTION_DESC[] connectionList = _ACS.GetConnectionsList();
                for (int index = 0; index < connectionList.Length; index++)
                {

                    if (terminateExceptionConnName.CompareTo((string)connectionList[index].Application) != 0)
                        _ACS.TerminateConnection(connectionList[index]);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        public void Close()
        {
            try
            {
                // stop motion
                Halt();
                // disable axis
                DisableAll();
                // stop the monitor
                _stopMonitoring = true;
                //turn off laser if on
                //stop motion if moving

                _ACS.CloseComm();

                // Clear connection list from SPiiPlus UserMode-Driver (UMD)
                TernminateUMD_Connection();
            }
            catch( Exception e)
            {
                Console.WriteLine("MotorManager exception: " + e.Message);
            }
        }
    }
}
