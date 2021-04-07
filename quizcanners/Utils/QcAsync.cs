using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuizCanners.Inspect;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace QuizCanners.Utils
{

    public static class QcAsync
    {
        public static TimedEnumeration.CallAgainRequest CallAgain() => new TimedEnumeration.CallAgainRequest();

        public static TimedEnumeration.CallAgainRequest CallAgain(string message) => new TimedEnumeration.CallAgainRequest(message: message);

        /*
        public static TimedEnumeration.CallAgain CallAfter(float seconds) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(TimeSpan.FromMilliseconds(QcMath.Seconds_To_Miliseconds(seconds))));

        public static TimedEnumeration.CallAgain CallAfter(float seconds, string message) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(TimeSpan.FromMilliseconds(QcMath.Seconds_To_Miliseconds(seconds))), message: message);

        public static TimedEnumeration.CallAgain CallAfter(TimeSpan timeSpan) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(timeSpan));

        public static TimedEnumeration.CallAgain CallAfter(TimeSpan timeSpan, string message) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(timeSpan), message: message);*/

        public static TimedEnumeration.CallAgainRequest CallAfter_Thread(Action afterThisTask) =>
            new TimedEnumeration.CallAgainRequest(task: Task.Run(afterThisTask));

        public static TimedEnumeration.CallAgainRequest CallAfter_Thread(Action afterThisTask, string message) =>
            new TimedEnumeration.CallAgainRequest(task: Task.Run(afterThisTask), message: message);
        
      
        public class TimedCoroutinesManager : IPEGI
        {
            private static readonly List<TimedEnumeration> pool = new List<TimedEnumeration>();
            
            private List<TimedEnumeration> enumerators = new List<TimedEnumeration>();
            
            public int GetActiveCoroutinesCount => enumerators.Count;

          /*  private class DoneToken { public bool isDone; }
            private async Task WaitTask(DoneToken token) {
                while (!token.isDone)
                    await Task.Yield();
            }*/
            
            public TimedEnumeration Add(IEnumerator enumerator, Action onExit = null, Action onFullyDone = null)
            {
               /* DoneToken token = new DoneToken();
                var task = WaitTask(token);
                
                var tmp = onExit;
                onExit = () =>
                {
                    token.isDone = true;
                    tmp?.Invoke();                      
                };*/
                
                var enm = (pool.Count>0) ? pool.TryTake(0) : new TimedEnumeration();
                enm.Reset(enumerator, onExitAction: onExit, onDoneFullyAction: onFullyDone);
                enumerators.Insert(0, enm);
                return enm;
            }
            
            public void UpdateManagedCoroutines()
            {
                for (int i = enumerators.Count - 1; i >= 0; i--)
                    if (!enumerators[i].MoveNext())
                        pool.Add(enumerators.TryTake(i));
            }

            #region Inspector 
            private readonly ListMetaData coroutinesListMeta = new ListMetaData("Managed Coroutines", showAddButton: false);

            private Task _debugTask;
            public void Inspect()
            {
                pegi.nl();
                
                if (!coroutinesListMeta.Inspecting)
                {
                    "Pool Size: {0}".F(pool.Count).nl();
                    
                    if ("Run an Example Managed Coroutine".Click().nl())
                        DefaultCoroutineManager.Add(Coroutine_Test());

                    if (_debugTask != null)
                    {
                        "Task status:{0}".F(_debugTask.Status).nl();
                        
                        if (_debugTask.Exception != null)
                            _debugTask.Exception.ToString().writeBig();
                        
                        if ("Clear".Click())
                            _debugTask = null;
                    } else if ("Run an Example Task".Click().nl())
                    {
                        var tmp = new TimedEnumeration(Coroutine_Test());
                        _debugTask = tmp.StartTask();
                    }
                }

                if ("Yield 1 frame".Click().nl())
                    UpdateManagedCoroutines();
                
                coroutinesListMeta.edit_List(ref enumerators).nl();
                
                if (!coroutinesListMeta.Inspecting)
                {
                    ("Managed Timed coroutines can run in Editor, but need an object to send an update call to them every frame: QcAsync.UpdateManagedCoroutines()." +
                     " Alternatively a TimedEnumerator can be started with Unity's " +
                     "StartCoroutine(new TimedEnumeration(enumerator)). It will in turn call yield on it multiple times with care for performance.").writeHint();

                    ("Examples are in QcAsync.cs class").writeHint();
                }
            }
            
            #endregion
        }
        
        public static Coroutine StartTimedCoroutine(this IEnumerator enumerator, MonoBehaviour behaviour, Action onExit = null) =>
            behaviour.StartCoroutine(new TimedEnumeration(enumerator).GetCoroutine(onExitAction: onExit));

        #region Inspector

        public static TimedCoroutinesManager DefaultCoroutineManager = new TimedCoroutinesManager();
        
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

        private static IEnumerator Coroutine_Test()
        {

            for (int i = 0; i < 5; i++)
            {
                Debug.Log("{0}: Frame: {1}".F(i, Time.frameCount));
                yield return CallAgain("Asking to execute this function again if we have enough time this frame"); // Communication token
            }

            for (int i = 0; i < 5; i++)
            {
                Debug.Log("With wait {0}. Frame: {1}".F(i, Time.frameCount));
                yield return new TimedEnumeration.CallAgainRequest(task: Task.Delay(TimeSpan.FromMilliseconds(QcMath.Seconds_To_Miliseconds(0.3f))), message: "Sending communication token that will ask to delay execution by 0.3 seconds");
               // yield return CallAfter(0.3f, "Sending communication token that will ask to delay execution by 0.3 seconds");
            }

            Debug.Log("Will start Nested Coroutine.");

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
        #endregion

        public class TimedEnumeration : IPEGI_ListInspect, IPEGI, IGotName
        {
            public class CallAgainRequest
            {
                public string message;

                public Task task;

                public CallAgainRequest()
                {
                }

                public CallAgainRequest(string message)
                {
                    this.message = message;
                }

                public CallAgainRequest(Task task)
                {
                    this.task = task;
                }

                public CallAgainRequest(Task task, string message)
                {
                    this.task = task;
                    this.message = message;
                }
            }

            #region FrameTiming
            private const float maxMilisecondsPerFrame = 1000f * 0.5f / 60f;
            private static float TotalTimeUsedThisFrame;
            private static int FrameIndex = -1;
            #endregion
            
            public bool DoneFully { get; private set; }
            public bool Exited { get; private set; }
            public int EnumeratorVersion { get; private set; }

            public Action onExit;
            internal Action onDoneFully;

            private List<IEnumerator> _enumeratorStack = new List<IEnumerator>();
            private Task _task;
            private int _runningVersion;
            private CallAgainRequest _currentCallAgainRequestRequest;
            private object _current;
            private bool _enumeratorStackChanged;
            protected bool _stopAndCancel;
            private Stopwatch timer = new Stopwatch();

            public void Stop() => EnumeratorVersion +=1;
            
            private void ResetInternal(IEnumerator enumerator) 
            {
                EnumeratorVersion += 1; // To stop any active coroutines
                _enumeratorStack.Clear(); 
                DoneFully = false;
                Exited = false;
                _task = null;
                _stopAndCancel = false;
                _enumeratorStack.Add(enumerator);
            }

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
                if (_stopAndCancel) 
                {
                    return false;
                }

                _currentCallAgainRequestRequest = null;
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
                    IEnumerator en = _enumeratorStack[_enumeratorStack.Count - 1];

                    if (en.MoveNext())
                    {
                        _yields++;

                        _current = en.Current;

                        if (_current == null)
                            return true;

                        if (_current is string)
                        {
                            _state = _current as string;
                            return true;
                        }
                        
                        var enm = _current as IEnumerator;

                        if (enm != null)
                        {
                            _enumeratorStack.Add(enm);
                            _current = null;
                            _enumeratorStackChanged = true;
                            return true;
                        }

                        _currentCallAgainRequestRequest = _current as CallAgainRequest;
                            
                        if (_currentCallAgainRequestRequest != null)
                        {
                            if (_currentCallAgainRequestRequest.message != null)
                            {
                                _state = _currentCallAgainRequestRequest.message;
                            }

                            if (_currentCallAgainRequestRequest.task != null)
                            {
                                _task = _currentCallAgainRequestRequest.task;
                            }
                            return true;
                        }
                        
                        return true;
                        
                    }
                    else
                    {
                        if (_enumeratorStack.Count > 1)
                        {
                            _enumeratorStack.RemoveAt(_enumeratorStack.Count - 1);
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
                }

                return false;
            }

            private bool NeedToStopInternalYielding()
            {
                if (_enumeratorStackChanged)
                {
                    return false;
                }
                
                if (_currentCallAgainRequestRequest == null || ((TotalTimeUsedThisFrame + timer.ElapsedMilliseconds) > maxMilisecondsPerFrame))
                {
                    var el = timer.ElapsedMilliseconds;

                    TotalTimeUsedThisFrame += el;

                    if (_logUnoptimizedSections && Application.isEditor && el > maxMilisecondsPerFrame * 2)
                    {
                        Debug.Log("{0} Needs x{1} segmentation".F(_state, el / maxMilisecondsPerFrame));
                    }

                    _frames++;

                    return true;
                }
                
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

            // For Managed Yielding:
            public bool MoveNext()
            {
                ResetTimer();

                while (NextYieldInternal())
                {
                    if (NeedToStopInternalYielding())
                    {
                        return true;
                    }
                }

                OnDone();

                return false;
            }


            private bool CanStart_Internal(Action onExitAction, Action onDoneFullyAction, out int thisVersion)
            {
                thisVersion = EnumeratorVersion;
                
                if (_enumeratorStack.IsNullOrEmptyCollection())
                {
                    _state = "No enumerator";
                    Debug.LogError(_state);
                    return false;
                }

                onDoneFully = onDoneFullyAction;
                onExit = onExitAction;

                if (thisVersion == _runningVersion)
                {
                    _state = "This enumerator is already running";
                    Debug.LogError(_state);
                    return false;
                }

                _runningVersion = thisVersion;
              
                ResetTimer();
                
                _state = "Starting";
                
                return true;
            }
            
            public async Task StartTask(Action onExitAction = null, Action onDoneFullyAction = null)
            {
                
                if (!CanStart_Internal(onExitAction, onDoneFullyAction, out var thisVersion))
                   return;

                while (NextYieldInternal())
                {
                    if (NeedToStopInternalYielding())
                    {
                        await Task.Yield();
                        ResetTimer();
                    }

                    if (EnumeratorVersion != thisVersion)
                    {
                        return;
                    }
                }
                OnDone();
            }
            
            //To be used inside a Coroutine:
            public IEnumerator GetCoroutine(Action onExitAction = null, Action onDoneFullyAction = null)
            {
                if (!CanStart_Internal(onExitAction, onDoneFullyAction, out var thisVersion))
                    yield break;

                while (NextYieldInternal())
                {
                    if (NeedToStopInternalYielding())
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

            public TimedEnumeration Reset(IEnumerator enumerator, Action onExitAction = null, Action onDoneFullyAction = null, string nameForInspector = "")
            {
                ResetInternal(enumerator);
                _state = "Resetting: " + enumerator;
                onDoneFully = onExitAction;
                onExit = onDoneFullyAction;
                NameForPEGI = nameForInspector.IsNullOrEmpty() ? enumerator.ToString() : nameForInspector;

                return this;
            }

            public TimedEnumeration(bool logUnoptimizedSections = false, string nameForInspector = "")
            {
                _logUnoptimizedSections = logUnoptimizedSections;
                NameForPEGI = nameForInspector;
            }

            public TimedEnumeration(IEnumerator enumerator, bool logUnoptimizedSections = false, string nameForInspector = "")
            {
                _logUnoptimizedSections = logUnoptimizedSections;
                Reset(enumerator, nameForInspector: nameForInspector);
            }

            #region Inspector
            
            protected bool _logUnoptimizedSections;
            private string _state = "";
            private int _yields;
            private int _frames;

            public string NameForPEGI { get; set; }

            public void InspectInList(IList list, int ind, ref int edited)
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
                
            }

            public void Inspect()
            {
                if (!Exited && !_stopAndCancel && "Stop & Cancel".Click().nl())
                    _stopAndCancel = true;

                if (!Exited && "Yield".Click().nl())
                    MoveNext();

                _state.writeBig();

            }
            #endregion
        }

    }
}