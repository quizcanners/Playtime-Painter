using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using Playtime_Painter;
using PlayerAndEditorGUI;

namespace StoryTriggerData {

    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(CubeWorldSpace))]
    public class CubeWorldSpaceDrawer : Editor {
        public override void OnInspectorGUI() {
            ef.start(serializedObject);
            ((CubeWorldSpace)target).PEGI();
            ef.end();
        }
    }
# endif

    [ExecuteInEditMode]
    [TagName(CubeWorldSpace.tagName)]
    public class CubeWorldSpace : STD_Poolable {


        string strokeData;
        PlaytimePainter painter;


        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.AddText("name", gameObject.name);
            cody.AddIfNotZero("pos", transform.localPosition);
            if (strokeData != null) 
                cody.AddText("playVectors", strokeData );

            if ((painter.savedEditableMesh != null) && (painter.savedEditableMesh.Length > 0))
                cody.AddText("mesh", painter.savedEditableMesh);

            cody.AddIfNotOne("scale", transform.localScale);
            cody.AddIfNotZero("rot", transform.localRotation.eulerAngles);
            cody.Add(stdValues);
            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "name": gameObject.name = data; break;
                case "pos": transform.localPosition = data.ToVector3(); break;
                case "playVectors": strokeData = data; painter.PlayStrokeData(data); break;
                case "mesh": painter.TryLoadMesh(data); break;
                case "scale": transform.localScale = data.ToVector3();  break;
                case "rot": transform.localRotation = Quaternion.Euler(data.ToVector3()); break;
                case STD_Values.storyTag: stdValues.Decode(data); break;
                default: return false;
            }
            return true;
        }

        public override void Reboot() {
            if (painter == null) 
                painter = GetComponent<PlaytimePainter>();

            stdValues = new STD_Values();
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(Vector3.zero);
        }

     
        public override bool PEGI() {
            bool changed = false;
            base.PEGI();

            if (!stdValues.browsing_interactions) {

                pegi.ClickToEditScript();

                PainterConfig pcfg = PainterConfig.inst;

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