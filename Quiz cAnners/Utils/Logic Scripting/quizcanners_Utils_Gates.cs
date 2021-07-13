using QuizCanners.Inspect;
using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class Gate
    {
        public abstract class GateBase 
        {
            protected bool initialized;

            public bool ValueIsDefined => initialized;
        }

        public class Frame : GateBase
        {
            private int _frameIndex;
            private int _editorFrame;
            private readonly Double _editorTime = new Double();

            public bool TryEnter()
            {
                if (DoneThisFrame)
                    return false;

                DoneThisFrame = true;
                return true;
            }

            private int CurrentFrame
            {
                get
                {
                    if (Application.isPlaying)
                        return UnityEngine.Time.frameCount;
                    else
                    {
                        if (_editorTime.TryChange(QcUnity.TimeSinceStartup()))
                            _editorFrame += 1;

                        return _editorFrame;
                    }
                }
            }

            public bool DoneThisFrame
            {
                get
                {
                    if (!initialized)
                        return false;

                    return _frameIndex == CurrentFrame;
                }
                set
                {
                    initialized = true;

                    if (value)
                        _frameIndex = CurrentFrame;
                    else
                        _frameIndex = CurrentFrame - 1;
                }
            }
        }

        public class Integer : GateBase, IGotReadOnlyName
        {
            private int _previousValue;

            public bool TryChange(int value)
            {
                if (!initialized)
                {
                    initialized = true;
                    _previousValue = value;
                    return true;
                }

                if (value == _previousValue)
                {
                    return false;
                }

                _previousValue = value;

                return true;
            }

            public int CurrentValue => _previousValue;

            public Integer()
            {

            }
            public Integer(int initialValue)
            {
                _previousValue = initialValue;
                initialized = true;
            }

            public string GetNameForInspector() => initialized ? _previousValue.ToString() : "NOT INIT";
        }

        public class Double : GateBase
        {
            private double _previousValue;

            public double Value => _previousValue;

            public bool TryChange(double value)
            {
                if (!initialized)
                {
                    initialized = true;
                    _previousValue = value;
                    return true;
                }

                if (Math.Abs(value - _previousValue) < double.Epsilon * 10)
                {
                    _previousValue = value;
                    return false;
                }

                _previousValue = value;

                return true;
            }

            public bool TryChange(double value, double changeTreshold)
            {
                if (!initialized)
                {
                    initialized = true;
                    _previousValue = value;
                    return true;
                }

                if (Math.Abs(value - _previousValue) >= changeTreshold)
                {
                    _previousValue = value;
                    return true;
                }

                return false;
            }

            public Double()
            {

            }
            public Double(double initialValue)
            {
                TryChange(initialValue);
            }
        }

        public class Time : GateBase
        {
            private SerializableDateTime _lastTime = new SerializableDateTime();
            private readonly Frame _frameGate = new Frame();
            private double _delta;

            public double GetDeltaAndUpdate()
            {
                _delta = GetDeltaWithoutUpdate();
                _lastTime = DateTime.Now;

                return _delta;
            }

            public bool TryUpdateIfTimePassed(double secondsPassed)
            {
                if (!WasInitialized())
                    return false;

                var delta = GetDeltaWithoutUpdate();
                if (delta >= secondsPassed)
                {
                    _lastTime = DateTime.Now;
                    return true;
                }

                return false;
            }

            public double GetDeltaWithoutUpdate()
            {
                if (!WasInitialized())
                    return 0;

                if (_frameGate.TryEnter())
                {
                    _delta = Math.Max(0, (DateTime.Now - _lastTime).TotalSeconds);
                }

                return _delta;
            }

            private bool WasInitialized()
            {
                if (initialized)
                    return true;

                initialized = true;
                _frameGate.DoneThisFrame = true;
                _lastTime = DateTime.Now;
                return false;

            }

        }

        public class Bool : GateBase
        {
            private bool _value;

            public bool CurrentValue => _value;

            public bool TryChange(bool newValue) 
            {
                if (!initialized) 
                {
                    initialized = true;
                    _value = newValue;
                    return true;
                }

                if (_value != newValue) 
                {
                    _value = newValue;
                    return true;
                }

                return false;
            }

            public Bool() { }

            public Bool (bool value) 
            {
                initialized = true;
                _value = value;
            }

        }
    }
}
