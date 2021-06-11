using System;
using System.Collections;
using UnityEngine;

namespace QuizCanners.Utils
{
    public class LoopLock : IEnumerator
    {
        private volatile bool _lLock;

        public SkipLock Lock()
        {
            if (_lLock)
                Debug.LogError("Should check it is Unlocked before calling a Lock");

            return new SkipLock(this);
        }

        public bool Unlocked => !_lLock;

        public object Current => _lLock;

        public class SkipLock : IDisposable
        {
            public void Dispose()
            {
                creator._lLock = false;
            }

            private volatile LoopLock creator;

            public SkipLock(LoopLock make)
            {
                creator = make;
                make._lLock = true;
            }
        }

        public bool MoveNext() => _lLock;

        public void Reset() { }
    }
}