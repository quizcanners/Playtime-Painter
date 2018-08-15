using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace SharedTools_Stuff
{

    public enum StatLogging_ExampleEnum { Started, DataUpdated, DataProcessed, Ended }

    public class StatLogger : IPEGI
    {

#if PEGI
        readonly UnnullableSTD<LogStat> allStats = new UnnullableSTD<LogStat>();

        List<LogStat> executionOrder = new List<LogStat>();

        public LogStat processedStat = null;
#endif

        void Create(int index, string name)
        {

#if PEGI
            lock (executionOrder) {
                if (executionOrder.Count == 0)
                    executionOrder.Add(processedStat);
                else
                    executionOrder.Insert(0, processedStat);

                processedStat.name = name;

                processedStat.myIndex = index;

                processedStat.addedToList = true;
            }
#endif
        }

        public StatLogger Get(int index, string name)
        {
#if PEGI
            if (loopLock.Unlocked)
                using (loopLock.Lock())
                {
                    processedStat = allStats[index];

                    if (!processedStat.addedToList)
                        Create(index, name);

                }
#endif
            return this;
        }
        
        volatile LoopLock loopLock = new LoopLock();
        public StatLogger StatMoveToFirst(int index, string name)
        {
#if PEGI

            lock (executionOrder)
            {
                if (loopLock.Unlocked)
                    using (loopLock.Lock())
                    {

                        processedStat = allStats[index];

                        if (!processedStat.addedToList)
                            Create(index, name);
                        else
                        {
                            if (executionOrder.Contains(processedStat))
                            {

                                int ind = executionOrder.IndexOf(processedStat);
                                if (ind > 0)
                                {
                                    if (ind >= executionOrder.Count)
                                        Debug.Log("Debug element is " + ind + " while length is " + executionOrder.Count);
                                    else
                                        executionOrder.Move(ind, 0);
                                }
                            }
                            else
                                Debug.Log("List doesn't contain elemnt " + name);
                        }

                        if (processedStat.outputToLog)
                            Debug.Log(processedStat.ToPEGIstring());

                    }
            }
#endif
            return this;
        }

        public void AddOne()
#if PEGI
            => processedStat.count += 1;
#else
        {}
#endif

        public void Add(float value)
#if PEGI
            => processedStat.value += value;
#else
        {}
#endif

        public void Rename(string name)
#if PEGI
            => processedStat.name = name;
#else
        {}
#endif


#if PEGI

        int inspectedStat = -1;
        public bool PEGI()
        {
            bool changed = false;

            if (inspectedStat == -1)
                "Logs".edit_List(executionOrder, ref inspectedStat, true);
            else
            {
                if (icon.Back.Click())
                    inspectedStat = -1;
                else
                    allStats[inspectedStat].PEGI();
            }
            return changed;
        }
#endif
    }

    public class LogStat : AbstractKeepUnrecognized_STD, IPEGI, IGotDisplayName, IPEGI_ListInspect
    {
        public int myIndex = 0;
        public bool addedToList = false;
        public string name;
        public int count = 0;
        public float value = 0;

        Dictionary<string, string> events = new Dictionary<string, string>();

        public string this[string key]
        {
            get
            {
                string retVal;
                if (events.TryGetValue(key, out retVal))
                    return retVal;
                else return "Not Set";
            }
            set
            {
                events[key] = value;
                if (outputToLog)
                    Debug.Log(name + " " + key + value);
            }
        }

        public bool outputToLog = false;

        void Reset()
        {
            count = 0;
            value = 0;
        }

#if PEGI
        public string NameForPEGIdisplay() => name + (count > 0 ? "[" + count + "]" : "") + (value > 0 ? " = " + value : "");

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            this.ToPEGIstring().write();
            if (icon.Edit.Click())
                edited = myIndex;
            return false;
        }

        int editedEvent = -1;

        public override bool PEGI()
        {
            bool changed = false;

            changed |= "Name".edit(ref name).nl();

            changed |= "Output To Log".toggle(ref outputToLog).nl();

            if (count > 0 && ("Reset Count " + count).Click().nl())
                count = 0;

            if (value > 0 && ("Reset Value " + value).Click().nl())
                value = 0;

            changed |= events.edit(ref editedEvent, true).nl();

            return changed;
        }

#endif
    }



    public static class StatLogger_ExampleExtensions
    {
        public static StatLogger exampleLogger = new StatLogger();

        public static StatLogger Log(this StatLogging_ExampleEnum log) => exampleLogger.StatMoveToFirst((int)log, log.ToString());

    }

}
