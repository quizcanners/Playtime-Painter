using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    public struct FrameGate
    {
        private bool _initialized;
        private int _frameIndex;

        public bool Enter()
        {
            if (DoneThisFrame)
                return false;
            
            DoneThisFrame = true;
            return true;
        }

        private int _editorFrame;
        private ValueGateDouble _editorTime;
        private int CurrentFrame
        {   get
            {
                if (Application.isPlaying)
                    return Time.frameCount;
                else
                {
                    if (_editorTime.IsChange(QcUnity.TimeSinceStartup()))
                        _editorFrame += 1;

                    return _editorFrame;
                }
            }
        }

        public bool DoneThisFrame
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
    public struct ValueGateInt
    {
        [SerializeField] private bool _initialized;
        [SerializeField] private int _previousValue;

        public bool IsChange(int value)
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
    }

    [Serializable]
    public struct ValueGateDouble
    {
        [SerializeField] private bool isSet;
        [SerializeField] private double _previousValue;

        public double Value => _previousValue;

        public bool IsChange(double value)
        {
            if (!isSet)
            {
                isSet = true;
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

        public bool IsChange(double value, double changeTreshold) 
        {
            if (!isSet)
            {
                isSet = true;
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
    }
}
