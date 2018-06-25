using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using STD_Logic;
/* Story Tell Tool Requirements:
 * Many Subprojects can use the same tool and merge nicely. This means every implementation needs to have database instance - a unique static class. All the triggers will be dependable on it.
 * For each object you can select which Trigger groups are relevant to it.
 * 
 * 
 */

namespace StoryTriggerData {
    
    //   [AddComponentMenu("Logic/StoryTelll")]
    //   [ExecuteInEditMode]
    //PlaytimeToolComponent,

        // This one merges interaction and STD values

    public class InteractionTarget : Values, iSTD
#if PEGI
        , IPEGI
#endif
    {

        public const string storyTag = "story";

        public InteractionBranch interactionGroup;
        public List<string> interactionReferences;

        // This are used to modify triggers when interacting with the object
        public List<Result> OnEnterResults = new List<Result>();
        public List<Result> OnExitResults = new List<Result>();
        
        public QOoptionType type;

        public void Reboot() {
            myQuestVersion = 0;
            interactionGroup = new InteractionBranch();
            interactionGroup.name = "ROOT";
            interactionReferences = new List<string>();
        }
        
        public int myQuestVersion = -1;
        
        public void UpdateLogic()
        {
            myQuestVersion = LogicMGMT.currentLogicVersion;
        }

        public void Update()
        {
            if (myQuestVersion != LogicMGMT.currentLogicVersion)
                UpdateLogic();
        }

        public InteractionTarget() {
            Reboot();
        }

        public override StdEncoder Encode() => new StdEncoder()
            .Add("i", interactionGroup)
            .Add_ifNotEmpty("ent", OnEnterResults)
            .Add_ifNotEmpty("ext", OnExitResults)
            .Add("qoType", (int)type)
            .Add("base", base.Encode());

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "i":
                    interactionGroup = new InteractionBranch(data);

                    List<Interaction> lst = new List<Interaction>();
                    interactionGroup.getAllInteractions(ref lst);

                    foreach (Interaction si in lst)
                        if (si.reference.Length > 0)
                            interactionReferences.Add(si.reference);

                    break;//.Add(new StoryEvent(data)); break;
                case "ent": data.DecodeInto(out OnEnterResults); break;
                case "ext": data.DecodeInto(out OnExitResults); break;
                case "qoType": type = (QOoptionType)data.ToInt(); break;
                default:
                    return base.Decode(tag, data);
            }
            return true;
        }

#if PEGI
        public bool browsing_interactions = false;

        public override bool PEGI() {

            bool changed = false;  

            if (!browsing_interactions) {
                
                if (this != Dialogue.browsedObj) {

                    pegi.newLine();

                    changed |= base.PEGI().nl();

               

                    pegi.write("Interactions: ", interactionGroup.getShortDescription());

                    if (icon.Edit.Click("Edit Interaction Tree", 20))
                        browsing_interactions = true;

                    pegi.newLine();

                    if (pegi.foldout("__ON_ENTER" + OnEnterResults.ToStringSafe(Interaction.showOnEnter_Results), ref Interaction.showOnEnter_Results)) {
                        Interaction.showOnExit_Results = false;
                        Trigger.showTriggers = false;

                        OnEnterResults.Inspect(this);

                    }
                    pegi.newLine();
                    if (pegi.foldout("__ON_EXIT" + OnExitResults.ToStringSafe(Interaction.showOnExit_Results), ref Interaction.showOnExit_Results)) {
                        Interaction.showOnEnter_Results = false;
                        Trigger.showTriggers = false;

                        OnExitResults.Inspect(this);

                    }
                    pegi.newLine();
                }



            } else {

                if (("<"+STD_Poolable.browsed.gameObject.name).Click(40))
                    browsing_interactions = false;
                else {
                    pegi.newLine();
                    interactionGroup.PEGI(this);
                }
            }

            return changed;
        }
        
        public void groupFilter_PEGI() {

            List<TriggerGroup> lst = TriggerGroup.all.GetAllObjsNoOrder();

            for (int i = 0; i < lst.Count; i++) {
                TriggerGroup td = lst[i];
                pegi.write(td + "_" + td.GetIndex(), 230);
                pegi.toggle(ref td.showInInspectorBrowser);
                pegi.newLine();
            }


        }
#endif
    }
}