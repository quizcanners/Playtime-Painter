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
using SharedTools_Stuff;

namespace StoryTriggerData {

    [ExecuteInEditMode]
    public abstract class STD_Poolable : PoolableBase, iKeepUnrecognizedSTD {

        public InteractionTarget stdValues;

        public Page parentPage;

        protected List<string> unrecognizedTags = new List<string>();
        protected List<string> unrecognizedData = new List<string>();

        public void Unrecognized(string tag, string data){
            this.Unrecognized(tag, data, ref unrecognizedTags, ref unrecognizedData); 
        }
        
         public stdEncoder SaveUnrecognized(stdEncoder cody){
            for (int i=0; i<unrecognizedTags.Count; i++)
                cody.AddText(unrecognizedTags[i], unrecognizedData[i]);
            return cody;
         }
 
        public abstract void Reboot();

        public abstract string getDefaultTagName();

        public virtual iSTD Decode(string data) {

            //gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            //gameObject.hideFlags &= ~(HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild);
            gameObject.SetFlagsOnItAndChildren(HideFlags.DontSave);//AddFlagsOnItAndChildren(HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild);
            //colors &= ~(Blah.BLUE | Blah.RED)

            gameObject.name = "new " + getDefaultTagName();
            
            Reboot();
                 
            unrecognizedTags = new List<string>();
            unrecognizedData = new List<string>();
 
            new stdDecoder(data).DecodeTagsFor(this);

            return this;
        }

        public override void Deactivate() {
            if (stdValues != null)
                stdValues.removeAllTags();

            UnLink();

            base.Deactivate();
        }

        public override string ToString() {
            return getDefaultTagName();
        }

        public abstract bool Decode(string tag, string data);

        public abstract stdEncoder Encode();

        public STD_Poolable LinkTo(Page p) {
            p.Link(this);
            return this;
        }

        public void UnLink() {
            if (parentPage != null)
                parentPage.Unlink(this);
        }
        
        public abstract void SetStaticPoolController(STD_Pool inst);



#if PEGI
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

        public static STD_Poolable browsed;
        public override bool PEGI() {
            browsed = this;

            bool changed = false;

            changed |= base.PEGI();

            if ((stdValues == null) || (!stdValues.browsing_interactions)) {
                pegi.newLine();

                if ((stdValues == null) || (Dialogue.browsedObj != stdValues)) {

                    pegi.write(parentPage == null ? "UnLinked" : "Ownership: " + parentPage.gameObject.name);

                    pegi.ClickToEditScript();

                    pegi.newLine();

#if UNITY_EDITOR
                    if (unrecognizedTags.Count>0){
                        "Unrecognized Tags:".nl();
                        for (int i=0; i<unrecognizedTags.Count; i++){
                            if (icon.Delete.Click(20)) {
                                    unrecognizedTags.RemoveAt(i);
                                    unrecognizedData.RemoveAt(i);
                            } else 
                                    (unrecognizedTags[i] + " | " + unrecognizedData[i]).nl();
                        }
                    }
                    
                    pegi.newLine();
#endif

                    if ((parentPage != null) && (pegi.Click("Test Conversion"))) {
                        Debug.Log("Debug Testing conversion");
                        var encode = Encode();
                        SaveUnrecognized(encode);

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

            return changed;
        }
#endif
        /* public override void OnDestroy()
         {
             base.OnDestroy();
             this.transform.Clear();

         }*/


        public virtual void PostPositionUpdate() {

        }
    }
}
