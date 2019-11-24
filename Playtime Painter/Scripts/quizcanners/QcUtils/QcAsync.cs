using PlayerAndEditorGUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.XR;
using Debug = UnityEngine.Debug;

namespace QuizCannersUtilities
{

    public static class QcAsync {

        public static TimedEnumeration.CallAgain CallAgain_StoreReturnData(object returnData) => new TimedEnumeration.CallAgain(returnData: returnData);

        public static TimedEnumeration.CallAgain CallAgain() => new TimedEnumeration.CallAgain();

        public static TimedEnumeration.CallAgain CallAgain(string message) => new TimedEnumeration.CallAgain(message: message);

        public static TimedEnumeration.CallAgain CallAfter(float seconds) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(TimeSpan.FromMilliseconds(QcMath.Seconds_To_Miliseconds(seconds))));

        public static TimedEnumeration.CallAgain CallAfter(float seconds, string message) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(TimeSpan.FromMilliseconds(QcMath.Seconds_To_Miliseconds(seconds))), message: message);
        
        public static TimedEnumeration.CallAgain CallAfter_Thread(Action afterThisTask) =>
            new TimedEnumeration.CallAgain(task: Task.Run(afterThisTask));

        public static TimedEnumeration.CallAgain CallAfter_Thread(Action afterThisTask, string message) =>
            new TimedEnumeration.CallAgain(task: Task.Run(afterThisTask), message: message);
        
        private static bool _debugPauseCoroutines;
        
        private static List<TimedEnumeration> enumerators = new List<TimedEnumeration>();
        
        public static TimedEnumeration StartManagedCoroutine(IEnumerator enumerator, Action onDone = null) {

            var enm = new TimedEnumeration(enumerator, onDone);
            enumerators.Insert(0, enm);
            return enm;
        }

        public static void UpdateManagedCoroutines() {

            if (!_debugPauseCoroutines)
                for (int i = enumerators.Count - 1; i >= 0; i--)
                {
                    if (!enumerators[i].MoveNext())
                        enumerators.RemoveAt(i);
                }
        }

        #region Inspector
        
        private static string CalculatePi(int digits) {

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


                r[xlen - 1] = x[xlen - 1] % 10; ;

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

        private static IEnumerator NestedCoroutine()
        {

            Debug.Log("Starting nested coroutine. Frame: {0}".F(Time.frameCount));

            int sum = 0;

            for (int i = 0; i < 10000; i++) {

                var pi = CalculatePi(50);

              //  Debug.Log("{0}  {1}".F(i, pi));

                yield return CallAgain();
            }

            Debug.Log("Done with nested coroutine. Frame: {0}; Result: {1}".F(Time.frameCount, sum));

        }

        private static IEnumerator TestCoroutine() {

          /*  for (int i = 0; i < 5; i++) { 
                Debug.Log("{0}: Frame: {1}".F(i, Time.frameCount));
                yield return CallAgain("Asking to execute this function again if we have enough time this frame");
            }

            for (int i = 0; i < 5; i++) {
                Debug.Log("With wait {0}. Frame: {1}".F(i, Time.frameCount));
                yield return CallAfter(0.5f);
            }*/

            for (var e = NestedCoroutine(); e.MoveNext();)
                yield return e.Current;
            
            string pi = "";

           /* yield return CallAfter_Thread(()=>
            {
                pi = CalculatePi(100000);
            },"Now we are calculating Pi in a task");
            */
          //  yield return CallAgain_StoreReturnData(pi);

            Debug.Log("Done calculating Pi");

        }

        public static int GetActiveCoroutinesCount => enumerators.Count;

        private static readonly ListMetaData coroutinesListMeta = new ListMetaData("Managed Coroutines", allowCreating: false, allowDeleting: true);

        public static bool InspectManagedCoroutines() {

            var changed = false;


            if (!coroutinesListMeta.Inspecting)
            {

                if ("Run an Example Managed  Coroutine".Click().nl()) {
                    TimedEnumeration mngd = null;

                    mngd = StartManagedCoroutine(TestCoroutine(),
                        () => { Debug.Log("Finished Managed Coroutine. The Pi is: {0}".F(mngd.returnedData)); });
                }
            }

            coroutinesListMeta.edit_List(ref enumerators).nl();

            "Pause Coroutines".toggleIcon(ref _debugPauseCoroutines).nl();

            if (!coroutinesListMeta.Inspecting) {
                
                ("Managed Timed coroutines can run in Editor, but need an object to send an update call to them every frame: QcAsync.UpdateManagedCoroutines()." +
                 " Alternatively a TimedEnumerator can be started with Unity's " +
                    "StartCoroutine(new TimedEnumeration(enumerator)). It will in turn call yield on it multiple times with care for performance.").writeHint();
                
                ("Examples are in QcAsync.cs class").writeHint();
            }

            

            

            return changed;
        }

        #endregion

        public class TimedEnumeration : IPEGI_ListInspect
        {

            public class CallAgain
            {

                public object returnData;

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

                public CallAgain(object returnData)
                {
                    this.returnData = returnData;
                }
            }

          

            private Stopwatch timer = new Stopwatch();

            private const float maxMilisecondsPerFrame = 1000f * 0.5f / 60f;

            public int Yields { get; private set; }

            public int Frames { get; private set; }

            public bool DoneFully { get; private set; }

            public bool Done { get; private set; }

            private bool logUnoptimizedSections;

            public int EnumeratorVersion { get; private set; }

            public bool stopAndCancel;

            private string state = "";

            private IEnumerator _enumerator;

            private Task _task;

            public object returnedData;

            public Action onDoneFully;

            private int runningVersion;

            private CallAgain _currentCallAgainRequest;

            private object _current;

            private void OnDone()
            {

                Done = true;

                if (!stopAndCancel)
                {

                    DoneFully = true;
                    onDoneFully?.Invoke();
                }
                else
                    state = "Stopped and cancelled after " + state;
            }


            private bool NextYieldInternal()
            {

                if (_task != null)
                {
                    if (!_task.IsCompleted)
                    {
                        _currentCallAgainRequest = null;
                        return true;
                    }
                    else
                        _task = null;

                }

                if (_enumerator.MoveNext())
                {

                    Yields++;

                    _current = _enumerator.Current;

                    _currentCallAgainRequest = _current as CallAgain;

                    if (_currentCallAgainRequest != null) {

                        if (_currentCallAgainRequest.message != null)
                            state = _currentCallAgainRequest.message;

                        if (_currentCallAgainRequest.task != null)
                            _task = _currentCallAgainRequest.task;

                        if (_currentCallAgainRequest.returnData != null)
                            returnedData = _currentCallAgainRequest.returnData;
                    }

                    return true;
                }

                return false;
            }

            private bool InternalNeedToStopYielding()
            {

                var el = timer.ElapsedMilliseconds;

                if ((TotalTimeUsedThisFrame > maxMilisecondsPerFrame) || (el > (maxMilisecondsPerFrame - TotalTimeUsedThisFrame)) || _currentCallAgainRequest == null)
                {

                    TotalTimeUsedThisFrame += el;

                    if (logUnoptimizedSections && Application.isEditor && el > maxMilisecondsPerFrame * 2)
                        Debug.Log("{0} Needs x{1} segmentation".F(state, el / maxMilisecondsPerFrame));

                    Frames++;

                    return true;
                }

                TotalTimeUsedThisFrame += el;

                return false;
            }

            private static float TotalTimeUsedThisFrame = 0;

            private static int FrameIndex = -1;

            private void ResetTimer()
            {
                timer.Restart();

                if (FrameIndex != Time.frameCount) {
                    TotalTimeUsedThisFrame = 0;
                    FrameIndex = Time.frameCount;
                }

            }

            public IEnumerator Start(Action onDoneFully = null)
            {

                if (_enumerator == null)
                {
                    Debug.LogError("No enumerator");
                    yield break;
                }

                this.onDoneFully = onDoneFully;

                int thisVersion = EnumeratorVersion;

                if (thisVersion == runningVersion)
                {
                    Debug.LogError("This enumerator is already running");
                    yield break;

                }

                runningVersion = thisVersion;

                stopAndCancel = false;

                ResetTimer();

                while (!stopAndCancel && NextYieldInternal())
                {

                    if (InternalNeedToStopYielding())
                    {

                        yield return _current;

                        ResetTimer();

                    }

                    if (EnumeratorVersion != thisVersion)
                        yield break;

                }

                OnDone();

            }

            public bool MoveNext()
            {

                ResetTimer();

                if (!stopAndCancel)
                    while (NextYieldInternal())
                        if (InternalNeedToStopYielding())
                            return true;

                OnDone();

                return false;
            }

            #region Inspector

            public bool InspectInList(IList list, int ind, ref int edited)
            {

                if (!Done && !stopAndCancel && icon.Close.Click())
                    stopAndCancel = true;

                "{2} {3} {4} [{0} YLDS / {1} FRMS]".F(Yields, Frames, state,
                    EnumeratorVersion > 1 ? ("V: " + EnumeratorVersion.ToString()) : "", _task == null ? "[yield]" : "[TASK]").write();

                if (Done)
                    (DoneFully ? icon.Done : icon.Empty).write();
                else if (icon.Next.Click())
                    MoveNext();

                return false;
            }

            #endregion


            public TimedEnumeration(bool logUnoptimizedSections = false)
            {
                this.logUnoptimizedSections = logUnoptimizedSections;
            }

            public TimedEnumeration(IEnumerator enumerator, bool logUnoptimizedSections = false)
            {
                this.logUnoptimizedSections = logUnoptimizedSections;
                Reset(enumerator);
            }

            public TimedEnumeration(IEnumerator enumerator, Action onDoneFully, bool logUnoptimizedSections = false)
            {
                this.logUnoptimizedSections = logUnoptimizedSections;
                this.onDoneFully = onDoneFully;
                Reset(enumerator);
            }

            public void Reset(IEnumerator enumerator)
            {
                EnumeratorVersion += 1;
                _enumerator = enumerator;
            }
        }

    }
}