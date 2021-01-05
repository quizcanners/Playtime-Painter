using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerAndEditorGUI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace QuizCannersUtilities
{

    public static class QcAsync
    {

        public static TimedEnumeration.CallAgain CallAgain() => new TimedEnumeration.CallAgain();

        public static TimedEnumeration.CallAgain CallAgain(string message) => new TimedEnumeration.CallAgain(message: message);

        public static TimedEnumeration.CallAgain CallAfter(float seconds) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(TimeSpan.FromMilliseconds(QcMath.Seconds_To_Miliseconds(seconds))));

        public static TimedEnumeration.CallAgain CallAfter(float seconds, string message) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(TimeSpan.FromMilliseconds(QcMath.Seconds_To_Miliseconds(seconds))), message: message);

        public static TimedEnumeration.CallAgain CallAfter(TimeSpan timeSpan) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(timeSpan));

        public static TimedEnumeration.CallAgain CallAfter(TimeSpan timeSpan, string message) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(timeSpan), message: message);

        public static TimedEnumeration.CallAgain CallAfter_Thread(Action afterThisTask) =>
            new TimedEnumeration.CallAgain(task: Task.Run(afterThisTask));

        public static TimedEnumeration.CallAgain CallAfter_Thread(Action afterThisTask, string message) =>
            new TimedEnumeration.CallAgain(task: Task.Run(afterThisTask), message: message);

        private static bool _debugPauseCoroutines;

        private static List<TimedEnumeration> enumerators = new List<TimedEnumeration>();

        public static TimedEnumeration StartManagedCoroutine(IEnumerator enumerator, Action onDone = null)
        {

            var enm = new TimedEnumeration(enumerator);
            enm.onDoneFully = onDone;
            enumerators.Insert(0, enm);
            return enm;
        }

        public static Coroutine StartTimedCoroutine(this IEnumerator enumerator, MonoBehaviour behaviour, Action onDone = null) =>
            behaviour.StartCoroutine(new TimedEnumeration(enumerator).Start(onDone));

        public static void UpdateManagedCoroutines()
        {
            if (!_debugPauseCoroutines)
                for (int i = enumerators.Count - 1; i >= 0; i--)
                {
                    if (!enumerators[i].MoveNext())
                        enumerators.RemoveAt(i);
                }
        }

        #region Inspector

        private static string CalculatePi_Test(int digits)
        {

            //Stanley Rabinowitz and Stan Wagon - Spigot Algorithm

            digits++;

            uint xlen = (uint)(digits * 10 / 3 + 2);

            uint[] x = new uint[xlen];
            uint[] r = new uint[digits * 10 / 3 + 2];

            uint[] pi = new uint[digits];

            for (int j = 0; j < x.Length; j++)
                x[j] = 20;

            for (int i = 0; i < digits; i++)
            {
                uint carry = 0;
                for (int j = 0; j < xlen; j++)
                {
                    uint num = (uint)(xlen - j - 1);
                    uint dem = num * 2 + 1;

                    x[j] += carry;

                    uint q = x[j] / dem;
                    r[j] = x[j] % dem;

                    carry = q * num;
                }


                pi[i] = (x[xlen - 1] / 10);


                r[xlen - 1] = x[xlen - 1] % 10;

                for (int j = 0; j < xlen; j++)
                    x[j] = r[j] * 10;
            }

            var sb = new StringBuilder(pi.Length);

            uint c = 0;

            for (int i = pi.Length - 1; i >= 0; i--)
            {
                var p = pi[i];
                p += c;
                c = p / 10;

                sb.Append(p.ToString());
            }

            return new string(sb.ToString().Reverse().ToArray());

        }

        private static IEnumerator NestedCoroutine_Test()
        {

            Debug.Log("Starting nested coroutine. Frame: {0}".F(Time.frameCount));

            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                sum += i;

                yield return CallAgain();
            }

            Debug.Log("Done with nested coroutine. Frame: {0}; Result: {1}".F(Time.frameCount, sum));

        }

        public static IEnumerator Coroutine_Test()
        {

            for (int i = 0; i < 5; i++)
            {
                Debug.Log("{0}: Frame: {1}".F(i, Time.frameCount));
                yield return CallAgain("Asking to execute this function again if we have enough time this frame"); // Communication token
            }

            for (int i = 0; i < 5; i++)
            {
                Debug.Log("With wait {0}. Frame: {1}".F(i, Time.frameCount));
                yield return CallAfter(0.3f, "Sending communication token that will ask to delay execution by 0.3 seconds");
            }

            Debug.Log("Will start Nested Coroutine. Works only if using MonoBehaviour's StartCoroutine");

            yield return NestedCoroutine_Test();

            Debug.Log("Calculating Pi");

            string pi = "";

            yield return CallAfter_Thread(() =>
            {
                pi = CalculatePi_Test(10000);
            }, "Now we are calculating Pi in a task");

            //  yield return CallAgain_StoreReturnData(pi);

            Debug.Log("Done calculating Pi : {0}".F(pi));

        }

        public static int GetActiveCoroutinesCount => enumerators.Count;

        private static readonly ListMetaData coroutinesListMeta = new ListMetaData("Managed Coroutines", showAddButton: false);

        public static bool InspectManagedCoroutines()
        {

            var changed = false;

            if (!coroutinesListMeta.Inspecting)
            {

                if ("Run an Example Managed  Coroutine".Click().nl())
                    StartManagedCoroutine(Coroutine_Test()//,                        (string returnValue) => { Debug.Log("Finished Managed Coroutine. The Pi is: {0}".F(returnValue)); }
                        );

            }

            coroutinesListMeta.edit_List(ref enumerators).nl();

            "Pause Coroutines".toggleIcon(ref _debugPauseCoroutines).nl();

            if (!coroutinesListMeta.Inspecting)
            {

                ("Managed Timed coroutines can run in Editor, but need an object to send an update call to them every frame: QcAsync.UpdateManagedCoroutines()." +
                 " Alternatively a TimedEnumerator can be started with Unity's " +
                    "StartCoroutine(new TimedEnumeration(enumerator)). It will in turn call yield on it multiple times with care for performance.").writeHint();

                ("Examples are in QcAsync.cs class").writeHint();
            }





            return changed;
        }

        #endregion

        /* public class TimedEnumeration<T> : TimedEnumeration
         {
             public Action<T> onDoneFullyReturnData;

             protected override void OnDone()
             {
                 base.OnDone();

                 if (!_stopAndCancel)
                     onDoneFullyReturnData?.Invoke((T)returnedData);
             }

             public IEnumerator Start(Action<T> onDoneFullyAction = null, Action onExitAction = null)
             {
                 onDoneFullyReturnData = onDoneFullyAction;
                 onExit = onExitAction;

                 for (var e = base.Start(); e.MoveNext();)
                     yield return e.Current;

             }

             public TimedEnumeration(IEnumerator enumerator, bool logUnoptimizedSections = false) : base(enumerator, logUnoptimizedSections) { }

         }*/

        public class TimedEnumeration : IPEGI_ListInspect, IPEGI, IGotName
        {
            public class CallAgain
            {
                // public object returnData;

                public string message;

                public Task task;

                public CallAgain()
                {
                }

                public CallAgain(string message)
                {
                    this.message = message;
                }

                public CallAgain(Task task)
                {
                    this.task = task;
                }

                public CallAgain(Task task, string message)
                {
                    this.task = task;
                    this.message = message;
                }

                /*  public CallAgain(object returnData)
                  {
                      this.returnData = returnData;
                  }*/
            }

            private const float maxMilisecondsPerFrame = 1000f * 0.5f / 60f;

            private static float TotalTimeUsedThisFrame;
            private static int FrameIndex = -1;

            public bool DoneFully { get; private set; }
            public bool Exited { get; private set; }
            public int EnumeratorVersion { get; private set; }

            public bool StoppedOnError { get; private set; }

            public Action onExit;
            public Action onDoneFully;
            // public object returnedData;

            private List<IEnumerator> _enumerator = new List<IEnumerator>();
            private Task _task;
            private int _runningVersion;
            private CallAgain _currentCallAgainRequest;
            private object _current;
            private bool _enumeratorStackChanged;
            protected bool _stopAndCancel;
            private Stopwatch timer = new Stopwatch();


            protected virtual void Cancel() => _stopAndCancel = true;

            protected virtual void OnDone()
            {

                Exited = true;

                if (onExit != null)
                {
                    try
                    {
                        onExit?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        _state = "Eception in OnExit of TimedEnumerator: " + _state + ex;
                        Debug.LogError(_state);
                    }
                }

                if (_stopAndCancel)
                {
                    _state = "Stopped and cancelled after " + _state;
                }
                else
                {
                    DoneFully = true;
                    if (onDoneFully != null)
                    {
                        try
                        {
                            onDoneFully.Invoke();
                        }
                        catch (Exception ex)
                        {
                            _state = "Exception in OnFully Done " + ex;
                            Debug.LogError(_state);
                        }
                    }
                }
            }

            private bool NextYieldInternal()
            {
                _currentCallAgainRequest = null;
                _enumeratorStackChanged = false;

                if (_task != null)
                {
                    if (!_task.IsCompleted)
                    {
                        return true;
                    }

                    _task = null;
                }

                try
                {
                    IEnumerator en = _enumerator[_enumerator.Count - 1];

                    if (en.MoveNext())
                    {
                        _yields++;

                        _current = en.Current;

                        if (_current is string)
                        {
                            _state = _current as string;
                        }
                        else
                        {
                            _currentCallAgainRequest = _current as CallAgain;

                            if (_currentCallAgainRequest != null)
                            {
                                if (_currentCallAgainRequest.message != null)
                                {
                                    _state = _currentCallAgainRequest.message;
                                }

                                if (_currentCallAgainRequest.task != null)
                                {
                                    _task = _currentCallAgainRequest.task;
                                }
                            }
                            else
                            {
                                if (_current is IEnumerator)
                                {
                                    _enumerator.Add(_current as IEnumerator);
                                    _current = null;
                                    _currentCallAgainRequest = new CallAgain();
                                    _enumeratorStackChanged = true;
                                }
                            }
                        }
                        return true;
                    }
                    else
                    {
                        if (_enumerator.Count > 1)
                        {
                            _enumerator.RemoveAt(_enumerator.Count - 1);
                            _enumeratorStackChanged = true;
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {

                    _state = "Error after {0}: {1}".F(_state, ex.ToString());

                    Debug.LogError("Managed Exception in Timed Enumerator: " + _state);

                    _task = null;
                    _stopAndCancel = true;
                    StoppedOnError = true;
                }

                return false;
            }

            private bool NeedToStopYielding()
            {

                var el = timer.ElapsedMilliseconds;

                if (_enumeratorStackChanged)
                {
                    return false;
                }

                if ((TotalTimeUsedThisFrame > maxMilisecondsPerFrame) || (el > (maxMilisecondsPerFrame - TotalTimeUsedThisFrame)) || _currentCallAgainRequest == null)
                {

                    TotalTimeUsedThisFrame += el;

                    if (_logUnoptimizedSections && Application.isEditor && el > maxMilisecondsPerFrame * 2)
                    {
                        Debug.Log("{0} Needs x{1} segmentation".F(_state, el / maxMilisecondsPerFrame));
                    }

                    _frames++;

                    return true;
                }

                TotalTimeUsedThisFrame += el;

                return false;
            }

            private void ResetTimer()
            {
                timer.Restart();

                if (FrameIndex != Time.frameCount)
                {
                    TotalTimeUsedThisFrame = 0;
                    FrameIndex = Time.frameCount;
                }

            }

            public IEnumerator Start(Action onDoneFully = null, Action onExit = null)
            {

                if (_enumerator.IsNullOrEmptyCollection())
                {
                    Debug.LogError("No enumerator");
                    yield break;
                }

                this.onDoneFully = onDoneFully;

                this.onExit = onExit;

                int thisVersion = EnumeratorVersion;

                if (thisVersion == _runningVersion)
                {
                    Debug.LogError("This enumerator is already running");
                    yield break;
                }

                _runningVersion = thisVersion;

                _stopAndCancel = false;

                ResetTimer();

                while (!_stopAndCancel && NextYieldInternal())
                {

                    if (NeedToStopYielding())
                    {
                        yield return _current;
                        ResetTimer();
                    }

                    if (EnumeratorVersion != thisVersion)
                    {
                        yield break;
                    }
                }

                OnDone();

            }

            public bool MoveNext()
            {
                ResetTimer();

                if (!_stopAndCancel)
                {
                    while (NextYieldInternal())
                    {
                        if (NeedToStopYielding())
                        {
                            return true;
                        }
                    }
                }

                OnDone();

                return false;
            }

            public TimedEnumeration Reset(IEnumerator enumerator, string nameForInspector = "")
            {
                EnumeratorVersion += 1;
                _enumerator.Clear();
                _enumerator.Add(enumerator);
                //returnedData = null;
                _state = "Starting: " + enumerator;
                NameForPEGI = nameForInspector.IsNullOrEmpty() ? enumerator.ToString() : nameForInspector;

                return this;
            }

            #region Inspector
            protected bool _logUnoptimizedSections;
            private string _state = "";
            private int _yields;
            private int _frames;

            public string NameForPEGI { get; set; }

            public bool InspectInList(IList list, int ind, ref int edited)
            {

                if (icon.Enter.Click())
                    edited = ind;

                if (Exited)
                    (DoneFully ? icon.Done : icon.Empty).write();

                "{4}: {5} {2} {3} [{0}y {1}f]".F(
                    _yields, // 0
                    _frames, // 1
                    EnumeratorVersion > 1 ? ("v: " + EnumeratorVersion) : "", //2
                    _task == null ? "[CORO]" : "[TASK]", //3
                    NameForPEGI, // 4
                    _state // 5
                    ) // 4
                    .write(_state);
                
                return false;
            }

            public bool Inspect()
            {
                if (!Exited && !_stopAndCancel && "Stop & Cancel".Click().nl())
                    _stopAndCancel = true;

                if (!Exited && "Yield".Click().nl())
                    MoveNext();

                _state.writeBig();

                return false;
            }

            #endregion

            public TimedEnumeration(bool logUnoptimizedSections = false, string nameForInspector = "")
            {
                _logUnoptimizedSections = logUnoptimizedSections;
                NameForPEGI = nameForInspector;
            }

            public TimedEnumeration(IEnumerator enumerator, bool logUnoptimizedSections = false, string nameForInspector = "")
            {
                _logUnoptimizedSections = logUnoptimizedSections;
                Reset(enumerator, nameForInspector);
            }

        }

    }
}