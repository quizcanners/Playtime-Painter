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
using QuizCannersUtilities;


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

        public virtual void DerivedUpdate() { }

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

            DerivedUpdate();
        }

        public void Awake() => RealTimeNow();

        #region Inspector
        #if PEGI

        protected override void ResetInspector() {
            inspectedTriggerGroup = -1;
            base.ResetInspector();
        }

        protected virtual void InspectionTabs() {
            icon.Condition.toggle("Trigger groups", ref inspectedStuff, 1);
            icon.Close.toggle("Close All", ref inspectedStuff, -1);
        }


        [SerializeField] protected int inspectedTriggerGroup = -1;
        [SerializeField] protected int tmpIndex = -1;
        [NonSerialized] TriggerGroup replaceRecieved = null;
        [NonSerialized] bool inspectReplacementOption = false;
        public override bool Inspect()
        {
            var changed = false;

            InspectionTabs();

            changed |= base.Inspect().nl();

            if (inspectedStuff == 1) {

                if (inspectedTriggerGroup == -1) {

                    #region Paste Options

                    if (replaceRecieved != null) {

                        var current = TriggerGroup.all.GetIfExists(replaceRecieved.IndexForPEGI);
                        string hint = (current != null) ? "{0} [ Old: {1} => New: {2} triggers ] ".F(replaceRecieved.NameForPEGI, current.Count, replaceRecieved.Count) : replaceRecieved.NameForPEGI;
                        
                        if (hint.enter(ref inspectReplacementOption))
                            replaceRecieved.Nested_Inspect();
                        else
                        {
                            if (icon.Done.ClickUnfocus())
                            {
                                TriggerGroup.all[replaceRecieved.IndexForPEGI] = replaceRecieved;
                                replaceRecieved = null;
                            }
                            if (icon.Close.ClickUnfocus())
                                replaceRecieved = null;
                        }
                    }
                    else
                    {

                        string tmp = "";
                        if ("Paste Messaged STD data".edit(140, ref tmp) || STDExtensions.LoadOnDrop(out tmp)) {

                            var group = new TriggerGroup();
                            group.DecodeFromExternal(tmp);

                            var current = TriggerGroup.all.GetIfExists(group.IndexForPEGI);
                           
                            if (current == null)
                                TriggerGroup.all[group.IndexForPEGI] = group;
                            else {
                                replaceRecieved = group;
                                if (!replaceRecieved.NameForPEGI.SameAs(current.NameForPEGI))
                                    replaceRecieved.NameForPEGI += " replaces {0}".F(current.NameForPEGI);
                            }
                        }



                    }
                    pegi.nl();

                    #endregion

                }

                "Trigger Groups".write(PEGI_Styles.ListLabel); 
                pegi.nl();

                changed |= TriggerGroup.all.Inspect<UnnullableSTD<TriggerGroup>, TriggerGroup>(ref inspectedTriggerGroup);

                if (inspectedTriggerGroup == -1) {
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
#endif
        #endregion
    }
}