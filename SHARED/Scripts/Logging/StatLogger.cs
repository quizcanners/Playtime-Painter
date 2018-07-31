using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace SharedTools_Stuff {

    public enum StatLogging_ExampleEnum { Started, DataUpdated, DataProcessed, Ended }
    
    public class StatLogger: IPEGI {
        readonly UnnullableSTD<LogStat> allStats = new UnnullableSTD<LogStat>();

        List<LogStat> executionOrder = new List<LogStat>();

        public LogStat processedStat = null;

        public StatLogger Stat(int index, string name)
        {
            processedStat = allStats[index];

            if (!processedStat.addedToList)
            {
                if (executionOrder.Count == 0)
                    executionOrder.Add(processedStat);
                else
                    executionOrder.Insert(0, processedStat);

                processedStat.name = name;

                processedStat.myIndex = index;

                processedStat.addedToList = true;
            }
            else
            {
                int ind = executionOrder.IndexOf(processedStat);
                if (ind != 0)
                    executionOrder.Move(ind, 0);
            }

            if (processedStat.outputToLog)
                Debug.Log(processedStat.ToPEGIstring());

            return this;
        }

        public void AddOne() => processedStat.count += 1;

        public void Add(float value) => processedStat.value += value;

        public void Rename(string name) => processedStat.name = name;

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

        public int starts = 0;
        public int ends = 0;
        public int doubleStarts = 0;
        public int doubleEnds = 0;
        public bool outputToLog = false;

        public void AddStart()
        {
            if (starts > ends)
                doubleStarts += 1;

            starts += 1;
        }

        public void AddEnd()
        {
            if (ends >= starts)
                doubleEnds += 1;

            ends += 1;
        }

#if PEGI
        public string NameForPEGIdisplay() => name + (count > 0 ? "[" + count + "]" : "") + (value > 0 ? " = " + value : "") + (starts > 0 ? " S:" + starts : "")
            + (ends > 0 ? "E: " + ends : " ");

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            this.ToPEGIstring().write();
            if (icon.Edit.Click())
                edited = myIndex;
            return false;
        }

        public override bool PEGI()
        {
            bool changed = false;

            "Name".edit(ref name).nl();

            "Output To Log".toggle(ref outputToLog).nl();

            return changed;
        }

#endif
    }



    public static class StatLogger_ExampleExtensions
    {
        public static StatLogger exampleLogger = new StatLogger();

        public static StatLogger Log(this StatLogging_ExampleEnum log) => exampleLogger.Stat((int)log, log.ToString());

    }

}
