using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace SharedTools_Stuff {

    public enum StatLogging_ExampleEnum { Started, DataUpdated, DataProcessed, Ended }
    
    public class StatLogger: IPEGI {

        UnnullableSTD<LogStat> allStats = new UnnullableSTD<LogStat>();

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

                processedStat.addedToList = true;
            }
            else
            {
                int ind = executionOrder.IndexOf(processedStat);
                if (ind != 0)
                    executionOrder.Move(ind, 0);
            }
            
            return this;
        }

        public void AddOne() => processedStat.count += 1;

        public void Add(float value) => processedStat.value += value;

        public void Rename(string name) => processedStat.name = name;

#if PEGI
        public bool PEGI()
        {
            bool changed = "Logs".edit_List(executionOrder, true);


            return changed;
        }
#endif
    }

    public class LogStat : AbstractKeepUnrecognized_STD, IPEGI, IGotDisplayName
    {
        public bool addedToList = false;
        public string name;
        public int count = 0;
        public float value = 0;

        public int starts = 0;
        public int ends = 0;
        public int doubleStarts = 0;
        public int doubleEnds = 0;

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
#endif
    }



    public static class StatLogger_ExampleExtensions
    {
        public static StatLogger exampleLogger = new StatLogger();

        public static StatLogger Log(this StatLogging_ExampleEnum log) => exampleLogger.Stat((int)log, log.ToString());

    }

}
