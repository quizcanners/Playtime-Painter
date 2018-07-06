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
#if PEGI
using PlayerAndEditorGUI;
#endif
using SharedTools_Stuff;

namespace StoryTriggerData
{

    [ExecuteInEditMode]
    public abstract class STD_Poolable : PoolableBase, IKeepUnrecognizedSTD
#if PEGI
      , IPEGI, IGotDisplayName
#endif
    {

        public Page parentPage;

        UnrecognizedTags_List uTags = new UnrecognizedTags_List();
        public UnrecognizedTags_List UnrecognizedSTD => uTags;

        public abstract void Reboot();

        public abstract string GetObjectTag();

        public virtual ISTD Decode(string data)
        {

            gameObject.SetFlagsOnItAndChildren(HideFlags.DontSave);

            gameObject.name = "new " + GetObjectTag();

            Reboot();

            new StdDecoder(data).DecodeTagsFor(this);

            return this;
        }

        public override void Deactivate()
        {
            UnLink();
            base.Deactivate();
        }

        public abstract bool Decode(string tag, string data);

        public abstract StdEncoder Encode();

        public STD_Poolable LinkTo(Page p)
        {
            p.Link(this);
            return this;
        }

        public void UnLink()
        {
            if (parentPage != null)
                parentPage.Unlink(this);
        }

        public abstract void SetStaticPoolController(STD_Pool inst);



#if PEGI
        static string customTag = "";
        static string customData = "";

        public virtual bool Call_PEGI()
        {
            bool dataSet = false;

            pegi.newLine();
            "Override Call_PEGI and return some Decodable data, like this one:".nl();
            pegi.writeOneTimeHint("This example shows how to set object's position, and set it as call data.", "callEditHint");


            "POS".edit(() => transform.position, this);


            if (icon.Done.Click(20).nl())
            {
                STD_Call.returnTag = "pos";
                STD_Call.returnData = transform.position.Encode(3).ToString();
                dataSet = true;
            }

            pegi.writeOneTimeHint("If the decode function has some tag like Jump, Open etc, you could use this to set a call to it", "customCalls");

            "custom tag:".edit(70, ref customTag).nl();

            "custom data:".edit(70, ref customData).nl();

            if ((customTag.Length > 0) && "Return custom data".Click().nl())
            {
                STD_Call.returnTag = customTag;
                STD_Call.returnData = customData;
                dataSet = true;
            }

            return dataSet;
        }

        public static STD_Poolable browsed;
        public override bool PEGI()
        {
            browsed = this;

            bool changed = false;

            changed |= base.PEGI();



            pegi.write(parentPage == null ? "UnLinked" : "Ownership: " + parentPage.gameObject.name);

            pegi.ClickToEditScript();

            pegi.newLine();

#if UNITY_EDITOR

            changed |= uTags.Nested_Inspect();

            pegi.newLine();
#endif

            if ((parentPage != null) && (pegi.Click("Test Conversion")))
            {
                Debug.Log("Debug Testing conversion");
                var encode = Encode();

                Page pg = parentPage;
                Deactivate();

                pg.Decode(GetObjectTag(), encode.ToString());
            }

            pegi.newLine();

            return changed;
        }
#endif

        public virtual void PostPositionUpdate()
        {

        }

        public string NameForPEGIdisplay() => GetObjectTag();
    }
}
