using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using Playtime_Painter;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace StoryTriggerData
{

#if PEGI && UNITY_EDITOR
    using UnityEditor;

    [ExecuteInEditMode]
    [CustomEditor(typeof(Terra))]
    public class TerraDrawer : Editor
    {
        public override void OnInspectorGUI()
        {
            ef.start(serializedObject);
            ((Terra)target).PEGI();
            ef.end();
        }
    }
# endif

    [ExecuteInEditMode]
    [TagName(Terra.tagName)]
    public class Terra : STD_Poolable {

        string strokeData;
        PlaytimePainter painter;
        WaterController water;

        public override stdEncoder Encode()
        {
            var cody = new stdEncoder();

            cody.Add_String("name", gameObject.name);
            cody.Add_IfNotZero("pos", transform.localPosition);
            if (strokeData != null)
                cody.Add_String("playVectors", strokeData);

            cody.Add_IfNotOne("scale", transform.localScale);
            cody.Add_IfNotZero("rot", transform.localRotation.eulerAngles);
            cody.Add("w_Noise", water.noise);
            cody.Add("w_Thick", water.thickness);
            cody.Add("w_Scale", water.upscale);
            cody.Add("w_Wet", water.wetAreaHeight);


            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "name": gameObject.name = data; break;
                case "pos": transform.localPosition = data.ToVector3(); break;
                case "playVectors": strokeData = data; painter.PlayStrokeData(data); break;
                case "scale": transform.localScale = data.ToVector3();  break;
                case "rot": transform.localRotation = Quaternion.Euler(data.ToVector3()); break;
                case "w_Noise": water.noise = data.ToFloat(); break;
                case "w_Thick": water.thickness = data.ToFloat(); break;
                case "w_Scale": water.upscale = data.ToFloat(); break;
                case "w_Wet": water.wetAreaHeight = data.ToFloat(); water.SetFoamDynamics(); break;
                default: return false;
            }
            return true;
        }

        public override void Reboot() {

            if (painter == null)
                painter = GetComponent<PlaytimePainter>();
            if (water == null)
                water = GetComponentInChildren<WaterController>();

            stdValues = new InteractionTarget();
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(Vector3.zero);
        }

#if PEGI
        public override bool PEGI()
        {
            bool changed = false;
            base.PEGI();

            if (!stdValues.browsing_interactions)
            {

                pegi.ClickToEditScript();

                PainterConfig pcfg = PainterConfig.inst;

                string recordName = pcfg.recordingNames.Count > 0 ? pcfg.recordingNames[pcfg.browsedRecord] : null;

                if ((recordName != null) && (pegi.Click("Copy recording " + recordName)))
                {
                    strokeData = String.Copy(pcfg.GetRecordingData(recordName));
                    painter.PlayStrokeData(strokeData);
                }

                if ((strokeData != null) && (icon.Delete.Click("Delete Recording", 20)))
                    strokeData = null;

                pegi.newLine();
            }

            return changed;
        }

#endif

        public const string tagName = "tera";

        public override string getDefaultTagName()
        {
            return tagName;
        }



        public override void PostPositionUpdate()
        {
        

            painter.UpdateTerrainPosition();
        }

        public static STD_Pool StoryPoolController;
        public override void SetStaticPoolController(STD_Pool inst)
        {
            StoryPoolController = inst;
        }
    }


}