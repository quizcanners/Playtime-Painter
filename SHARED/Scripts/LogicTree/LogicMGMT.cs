using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Linq.Expressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using PlayerAndEditorGUI;
using SharedTools_Stuff;


namespace STD_Logic
{
    public class LogicMGMT : ComponentSTD   {

        public static LogicMGMT inst;

        bool waiting;
        float timeToWait = -1;
        public static int currentLogicVersion = 0;
        public static void AddLogicVersion() => currentLogicVersion++;
        
        public static int RealTimeOnStartUp = 0;

        public static int RealTimeNow()
        {
            if (RealTimeOnStartUp == 0)
                RealTimeOnStartUp = (int)((DateTime.Now.Ticks - 733000 * TimeSpan.TicksPerDay) / TimeSpan.TicksPerSecond);

            return RealTimeOnStartUp + (int)Time.realtimeSinceStartup;
        }
        
        public override StdEncoder Encode() =>this.EncodeUnrecognized();

        public override bool Decode(string tag, string data) => false;

        public virtual void OnEnable()  =>  inst = this;
        
        public void AddTimeListener(float seconds)
        {
            seconds += 0.5f;
            if (!waiting) timeToWait = seconds;
            else timeToWait = Mathf.Min(timeToWait, seconds);
            waiting = true;
        }

        public virtual void DerrivedUpdate() { }

        public void Update()
        {
            if (waiting)
            {
                timeToWait -= Time.deltaTime;
                if (timeToWait < 0)
                {
                    waiting = false;
                    AddLogicVersion();
                }
            }

            DerrivedUpdate();
        }

        public void Awake() => RealTimeNow();

#if PEGI
        [SerializeField] protected int inspectedLogicBranchStuff = -1;
        [SerializeField] protected int inspectedTriggerGroup = -1;
        [SerializeField] protected int tmpIndex = -1;
        public override bool Inspect()
        {
            var changed = false;

            if (inspectedLogicBranchStuff == -1)
                changed |= base.Inspect();
            else
                showDebug = false;

            pegi.nl();

            if (!showDebug && icon.Condition.fold_enter_exit("Trigger Groups", ref inspectedLogicBranchStuff, 0))
            {

                pegi.nl();
                "Trigger Groups".nl();

                changed |= TriggerGroup.all.Inspect<UnnullableSTD<TriggerGroup>, TriggerGroup>(ref inspectedTriggerGroup);

                if (inspectedTriggerGroup == -1)
                {
                    "At Index: ".edit(60, ref tmpIndex);
                    if (tmpIndex >= 0 && ExtensionsForGenericCountless.TryGet(TriggerGroup.all, tmpIndex) == null && icon.Add.ClickUnfocus("Create New Group"))
                    {
                        TriggerGroup.all[tmpIndex].NameForPEGI = "Group " + tmpIndex.ToString();//.GetIndex();
                        tmpIndex++;
                    }
                    pegi.nl();
                }
            }

            pegi.nl();

            return changed;
        }
# endif
    }
}