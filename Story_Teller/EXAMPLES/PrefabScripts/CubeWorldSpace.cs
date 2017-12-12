using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using TextureEditor;
using PlayerAndEditorGUI;

namespace StoryTriggerData {

    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(CubeWorldSpace))]
    public class CubeWorldSpaceDrawer : Editor {
        public override void OnInspectorGUI() {
            ef.start(serializedObject);
            ((CubeWorldSpace)target).PEGI();
            pegi.newLine();
        }
    }
# endif

    [ExecuteInEditMode]
    [TagName(CubeWorldSpace.tagName)]
    public class CubeWorldSpace : STD_Object {


        string strokeData;
        PlaytimePainter painter;


        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.AddText("name", gameObject.name);
            cody.AddIfNotZero("pos", transform.position);
            if (strokeData != null) 
                cody.AddText("playVectors", strokeData );
            cody.AddIfNotOne("scale", transform.localScale);
            cody.AddIfNotNull(stdValues);
            return cody;
        }

        public override void Decode(string tag, string data) {
            switch (tag) {
                case "name": gameObject.name = data; break;
                case "pos": transform.position = data.ToVector3(); break;
                case "playVectors": strokeData = data; painter.PlayStrokeData(data); break;
                case "scale": transform.localScale = data.ToVector3(); break;
                case STD_Values.storyTag: stdValues.Reboot(data); break;
            }
        }

        public override void Reboot() {
            if (painter == null) 
                painter = GetComponent<PlaytimePainter>();

            stdValues = new STD_Values();
            transform.localScale = Vector3.one;
            transform.position = Vector3.zero;
        }

     
        public override bool PEGI() {
            bool changed = false;
            base.PEGI();

            if (!stdValues.browsing_interactions) {

                pegi.ClickToEditScript();

                painterConfig pcfg = painterConfig.inst();

                string recordName = pcfg.recordingNames.Count > 0 ? pcfg.recordingNames[pcfg.browsedRecord] : null;

                if ((recordName != null) && (pegi.Click("Copy recording " + recordName))) {
                    strokeData = String.Copy(pcfg.GetRecordingData(recordName));
                    painter.PlayStrokeData(strokeData);
                }

                if ((strokeData != null) && (icon.Delete.Click("Delete Recording",20))) 
                    strokeData = null;

                pegi.newLine();
            }

            return changed;
        }

 

        public const string tagName = "cube";

        public override string getDefaultTagName() {
            return tagName;
        }

        public static STD_Pool StoryPoolController;
        public override void SetStaticPoolController(STD_Pool inst) {
            StoryPoolController = inst;
        }
    }


}