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


namespace LogicTree
{
    public class LogicMGMT : ComponentSTD
    {

        public static LogicMGMT inst
        {
            get
            {

                if (_inst == null)
                    _inst = FindObjectOfType<LogicMGMT>();

                if (_inst == null)
                {
                    _inst = (new GameObject().AddComponent<LogicMGMT>());
                }

                return _inst;
            }
        }

        protected static LogicMGMT _inst;

        public static int questVersion = 0;
        public static void AddQuestVersion()
        {
            questVersion++;
        }

        public static int RealTimeOnStartUp = 0;

        public static int RealTimeNow()
        {
            if (RealTimeOnStartUp == 0)
                RealTimeOnStartUp = (int)((DateTime.Now.Ticks - 733000 * TimeSpan.TicksPerDay) / TimeSpan.TicksPerSecond);

            return RealTimeOnStartUp + (int)Time.realtimeSinceStartup;
        }

        public virtual void OnEnable()
        {

            _inst = this;

        }

        bool waiting;
        float timeToWait = -1;

        public void AddTimeListener(float seconds)
        {
            seconds += 0.5f;
            if (!waiting) timeToWait = seconds;
            else timeToWait = Mathf.Min(timeToWait, seconds);
            waiting = true;
        }

        public virtual void Update()
        {
            if (waiting)
            {
                timeToWait -= Time.deltaTime;
                if (timeToWait < 0)
                {
                    waiting = false;
                    AddQuestVersion();
                }
            }
        }

        public void Awake()
        {
            RealTimeNow();
        }

        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            return cody;
        }

        public override void Reboot()
        {

        }

        public override bool Decode(string tag, string data)
        {
            return false;
        }

        public override string getDefaultTagName()
        {
            return "LogicMGMT";
        }
    }
}