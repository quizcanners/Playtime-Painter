using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    [Serializable]
    public struct SerializableDateTime
    {
        [SerializeField] private bool _isSet;
        [SerializeField] private long _ticks;

        [NonSerialized] private bool _convertedUpdated;
        [NonSerialized] private DateTime _converted;

        public bool IsSet => _isSet;

        public DateTime Value
        {
            get
            {
                if (!_isSet)
                {
                    Debug.LogError("Trying to get invalid Date Time");
                    return DateTime.Now;
                }

                if (!_convertedUpdated)
                {
                    _converted = new DateTime(ticks: _ticks);
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

        public static implicit operator DateTime(SerializableDateTime d) => d.Value;

        public static implicit operator SerializableDateTime(DateTime d)
        {
            var val = new SerializableDateTime
            {
                Value = d
            };
            return val;
        }

    }

}