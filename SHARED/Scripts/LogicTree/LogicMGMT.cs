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
    public class LogicMGMT : ScriptableSTD  {

        public static LogicMGMT inst;

        public static int currentLogicVersion = 0;
        public static void AddLogicVersion()
        {
            currentLogicVersion++;
        }

        public static int RealTimeOnStartUp = 0;

        public static int RealTimeNow()
        {
            if (RealTimeOnStartUp == 0)
                RealTimeOnStartUp = (int)((DateTime.Now.Ticks - 733000 * TimeSpan.TicksPerDay) / TimeSpan.TicksPerSecond);

            return RealTimeOnStartUp + (int)Time.realtimeSinceStartup;
        }

        public virtual void OnEnable() {

            inst = this;

        }

        bool waiting;
        float timeToWait = -1;

        public virtual Values InspectedValues()
        {
            return null;
        }

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
                    AddLogicVersion();
                }
            }
        }

        public void Awake()
        {
            RealTimeNow();
        }

        public override StdEncoder Encode() {
            var cody = new StdEncoder();
            
            return cody;
        }

        public override bool Decode(string tag, string data)
        {

            return true;
        }
        
    }
}