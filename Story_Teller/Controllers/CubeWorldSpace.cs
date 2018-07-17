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

namespace StoryTriggerData {


    [ExecuteInEditMode]
    [StoryTagName(CubeWorldSpace.tagName)]
    public class CubeWorldSpace : STD_Poolable, IPEGI
    {


        string strokeData;
        PlaytimePainter painter;


        public override StdEncoder Encode() {
            var cody =this.EncodeUnrecognized(); //new stdEncoder();

            cody.Add_String("name", gameObject.name);
            cody.Add_IfNotZero("pos", transform.localPosition);
            if (strokeData != null) 
                cody.Add_String("playVectors", strokeData );

            if ((painter.SavedEditableMesh != null) && (painter.SavedEditableMesh.Length > 0))
                cody.Add_String("mesh", painter.SavedEditableMesh);

            cody.Add_IfNotOne("scale", transform.localScale);
            cody.Add_IfNotZero("rot", transform.localRotation.eulerAngles);
           // cody.Add(InteractionTarget.storyTag, stdValues);
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
               // case InteractionTarget.storyTag: data.DecodeInto(out stdValues); break;
                default: return false;
            }
            return true;
        }

        public override void Reboot() {
            if (painter == null) 
                painter = GetComponent<PlaytimePainter>();

          //  stdValues = new InteractionTarget();
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(Vector3.zero);
        }

#if PEGI
        public override bool PEGI() {
            bool changed = false;
            base.PEGI();

           // if (!stdValues.browsing_interactions) {

                pegi.ClickToEditScript();

                PainterConfig pcfg = PainterConfig.Inst;

                string recordName = pcfg.recordingNames.Count > 0 ? pcfg.recordingNames[pcfg.browsedRecord] : null;

                if ((recordName != null) && (pegi.Click("Copy recording " + recordName))) {
                    strokeData = String.Copy(pcfg.GetRecordingData(recordName));
                    painter.PlayStrokeData(strokeData);
                }

                if ((strokeData != null) && (icon.Delete.Click("Delete Recording",20))) 
                    strokeData = null;

                pegi.newLine();
         //   }

            return changed;
        }
#endif


        public const string tagName = "cube";

        public override string GetObjectTag() {
            return tagName;
        }

        public static STD_Pool StoryPoolController;
        public override void SetStaticPoolController(STD_Pool inst) {
            StoryPoolController = inst;
        }
    }


}