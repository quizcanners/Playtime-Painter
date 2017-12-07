using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using PlayerAndEditorGUI;


namespace StoryTriggerData {

    [ExecuteInEditMode]
    public abstract class STD_Object : PoolableBase, iSTD {

        public STD_Values stdValues;

        public Page parentPage;

        public abstract void Reboot();

        public virtual void Reboot(string data) {

            gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            gameObject.name = "new " + getDefaultTagName();

            Reboot();

            new stdDecoder(data).DecodeTagsFor(this);
        }

        public override void Deactivate() {
            if (stdValues != null)
                stdValues.removeAllTags();

            UnLink();

            base.Deactivate();
        }

        public abstract string getDefaultTagName();

        public override string ToString() {
            return getDefaultTagName();
        }

        public abstract void Decode(string tag, string data);

        public abstract stdEncoder Encode();

        public STD_Object LinkTo(Page p) {
            p.Link(this);
            return this;
        }

        public void UnLink() {
            if (parentPage != null)
                parentPage.Unlink(this);
        }


        public abstract void SetStaticPoolController(STD_Pool inst);

        static string customTag = "";
        static string customData = "";

        public virtual bool Call_PEGI() {
            bool dataSet = false;

            pegi.newLine();
            "Override Call_PEGI and return some Decodable data, like this one:".nl();
            pegi.writeOneTimeHint("This example shows how to set object's position, and set it as call data.", "callEditHint");


            "POS".edit(() => transform.position);


            if (icon.Done.Click(20).nl()) {
                STD_Call.returnTag = "pos";
                STD_Call.returnData = transform.position.Encode(3);
                dataSet = true;
            }

            pegi.writeOneTimeHint("If the decode function has some tag like Jump, Open etc, you could use this to set a call to it", "customCalls");

            "custom tag:".edit(70, ref customTag).nl();

            "custom data:".edit(70, ref customData).nl();

            if ((customTag.Length > 0) && "Return custom data".Click().nl()) {
                STD_Call.returnTag = customTag;
                STD_Call.returnData = customData;
                dataSet = true;
            }

            return dataSet;
        }

        public static STD_Object browsed;
        public override void PEGI() {
            browsed = this;

            if ((stdValues == null) || (!stdValues.browsing_interactions)) {
                pegi.newLine();

                if ((stdValues == null) || (Dialogue.browsedObj != stdValues)) {

                    pegi.write(parentPage == null ? "UnLinked" : "Ownership: " + parentPage.gameObject.name);

                    pegi.ClickToEditScript();

                    pegi.newLine();

                    if ((parentPage != null) && (pegi.Click("Test Conversion"))) {
                        Debug.Log("Debug Testing conversion");
                        var encode = Encode();
                        Page pg = parentPage;
                        Deactivate();
                        pg.Decode(getDefaultTagName(), encode.ToString());
                    }
                }

                if (stdValues!= null)
                    Dialogue.PEGI(stdValues);

                pegi.newLine();

            }

            if (stdValues!= null)
                stdValues.PEGI();
        }
    }
}