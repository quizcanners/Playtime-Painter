using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    [Serializable]
    public struct SerializableTimeSpan
    {
        [SerializeField] private bool _isSet;
        [SerializeField] private long _ticks;

        [NonSerialized] private bool _convertedUpdated;
        [NonSerialized] private TimeSpan _converted;

        public bool IsSet => _isSet;

        public TimeSpan Value
        {
            get
            {
                if (!_isSet)
                {
                    Debug.LogError("Trying to get invalid Date Time");
                    return TimeSpan.Zero;
                }

                if (!_convertedUpdated)
                {
                    _converted = new TimeSpan(ticks: _ticks);
                    _convertedUpdated = true;
                }

                return _converted;
            }

            set
            {
                _ticks = value.Ticks;
                _isSet = true;
                _convertedUpdated = false;
            }
        }

        public static implicit operator TimeSpan(SerializableTimeSpan d) => d.Value;

        public static implicit operator SerializableTimeSpan(TimeSpan d)
        {
            var val = new SerializableTimeSpan
            {
                Value = d
            };
            return val;
        }

        public static SerializableTimeSpan operator +(SerializableTimeSpan thisOne, TimeSpan toAdd)
        {
            thisOne.Value += toAdd;
            return thisOne;
        }

        public static SerializableTimeSpan operator -(SerializableTimeSpan thisOne, TimeSpan toSubtract)
        {
            thisOne.Value -= toSubtract;
            return thisOne;
        }

    }

}