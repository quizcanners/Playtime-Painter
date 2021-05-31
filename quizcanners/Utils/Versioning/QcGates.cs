using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    public class FrameGate
    {
        private bool _initialized;
        private int _frameIndex;

        public bool TryEnter()
        {
            if (DoneThisFrame)
                return false;
            
            DoneThisFrame = true;
            return true;
        }

        private int _editorFrame;
        private readonly ValueGateDouble _editorTime;
        private int CurrentFrame
        {   get
            {
                if (Application.isPlaying)
                    return Time.frameCount;
                else
                {
                    if (_editorTime.TryChange(QcUnity.TimeSinceStartup()))
                        _editorFrame += 1;

                    return _editorFrame;
                }
            }
        }

        private bool DoneThisFrame
        {
            get
            {
                if (!_initialized) 
                    return false;
                
                return _frameIndex == CurrentFrame;
            }
            set
            {
                _initialized = true;

                if (value)
                    _frameIndex = CurrentFrame;
                else
                    _frameIndex = CurrentFrame - 1;
            }
        }
    }

    [Serializable]
    public class ValueGateInt
    {
        [SerializeField] private bool _initialized;
        [SerializeField] private int _previousValue;

        public bool TryChange(int value)
        {
            if (!_initialized)
            {
                _initialized = true;
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

        public ValueGateInt() 
        {
            
        }
        public ValueGateInt(int initialValue) 
        {
            _previousValue = initialValue;
            _initialized = true;
        }
    }

    [Serializable]
    public class ValueGateDouble
    {
        [SerializeField] private bool _initialized;
        [SerializeField] private double _previousValue;

        public double Value => _previousValue;

        public bool TryChange(double value)
        {
            if (!_initialized)
            {
                _initialized = true;
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
            if (!_initialized)
            {
                _initialized = true;
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

        public ValueGateDouble()
        {

        }
        public ValueGateDouble(double initialValue)
        {
            TryChange(initialValue);
        }
    }
}
