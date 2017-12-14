using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using PlayerAndEditorGUI;



namespace StoryTriggerData
{

    public static class DialogueOptionExtensionFunctions {

        public static string ToStringSafe(this List<DialogueChoice> o, bool detail) {
            bool AnyOptions = ((o != null) && (o.Count > 0));
            return  (AnyOptions ? "[" + o.Count + "]: " + (detail ? "..." : o[0].text.ToString()) : "NONE");
        }

        public static string ToStringSafe(this List<Interaction> i) {
            bool any = (i != null) && (i.Count>0);
            return any ? i.Count + "_"+ i[0].Texts.ToStringSafe(true) : "NONE";
        }
    }

    [Serializable]
    public class DialogueChoice: abstract_STD  {
        public VariablesWeb conditions;
        public Sentance text;
        public List<Sentance> texts2;
        public List<Result> results;
        public List<STD_Call> calls;
        public string goToReference;


        public static bool unfoldPegi;

        public const string storyTag = "talkOpt";

        public override string getDefaultTagName() {
            return storyTag;
        }

        public override stdEncoder Encode() {
            var cody = new stdEncoder(); //EncodeData();
            cody.AddIfNotEmpty("goto", goToReference);
            cody.Add("web", conditions);
            cody.Add("t",text);
            cody.AddIfNotEmpty("t2", texts2);
            cody.AddIfNotEmpty("res", results);

            return cody;
        }

        public override void Decode(string tag, string data) {
          
           switch (tag) {
                case "goto": goToReference = data; break;
                case "web": conditions = new VariablesWeb(data); break;
                case "t": text = new Sentance(data); break;
                case "t2": texts2 = data.ToListOf_STD<Sentance>(); break;
                case "res": results = data.ToListOf_STD<Result>(); break;
           }

        }

        public DialogueChoice() {
            Clear();
        }

        public DialogueChoice(string data) {

            Clear();

            if (data != null)
                Reboot(data);
          
        }

       

        void Clear() {
            conditions = new VariablesWeb(null);
            texts2 = new List<Sentance>();
            text = new Sentance(null);
            results = new List<Result>();
            calls = new List<STD_Call>();
        }

        public void PEGI(STD_Values so) {
            
            text.PEGI();

            pegi.newLine();
            if (pegi.foldout("___Conditions:", ref Condition.unfoldPegi)) {
                Result.showOnExit = false;

                conditions.PEGI(so);
            }

            pegi.newLine();

            if (pegi.foldout("___Results:", ref Result.showOnExit)){
                Condition.unfoldPegi = false;

               results.PEGI(so);
            }

            pegi.newLine();

            pegi.write("After choice Calls:");

            calls.PEGI();

            pegi.newLine();

            pegi.write("After choice text:");

            pegi.newLine();

            texts2.PEGI();

           

            pegi.newLine();
            pegi.write("Go To:", 40);
            pegi.edit(ref goToReference);

            int dummy = -1;

            if (pegi.select(ref dummy, so.interactionReferences))
                goToReference = so.interactionReferences[dummy];
            
            pegi.newLine();

        }
    }

    [Serializable]
    public enum QOoptionType { Dialogue, PassiveLogic, Secret }

    [Serializable]
    public class Interaction : abstract_STD {

        public string reference="";
        public VariablesWeb conditions;
        public List<Sentance> Texts;
        public List<DialogueChoice> options;
        public List<Result> FinalResults;
        public int editedOption;
  


        public override stdEncoder Encode() {
            var cody = new stdEncoder(); //EncodeData();
            cody.AddIfNotEmpty("ref", reference);
            cody.Add("Conds", conditions);
            cody.AddIfNotEmpty("txt",Texts);
            cody.AddIfNotEmpty("opt", options);
            cody.AddIfNotEmpty("fin", FinalResults);
      
            return cody;
        }

        public override void Decode(string tag, string data) {
            //Debug.Log("Decoding " + tag + " with " + data);

            switch (tag) {
                case "ref": reference = data; break;
                case "Conds": conditions = new VariablesWeb(data); break;
                case "txt": Texts = data.ToListOf_STD<Sentance>(); break;
                case "opt": options = data.ToListOf_STD<DialogueChoice>(); break;
                case "fin": FinalResults = data.ToListOf_STD<Result>(); break;
            }
        }



        public const string storyTag_intrct = "intrct";

        public override string getDefaultTagName() {
            return storyTag_intrct;
        }


        public Interaction() {
            Clear();
        }
     
        public Interaction(string data) {
            Clear();
            if (data == null)
                Texts.Add(new Sentance("Edit this", Languages.en));
            Reboot(data);
        }

        void Clear() {
            conditions = new VariablesWeb(null);
            options = new List<DialogueChoice>();
            FinalResults = new List<Result>();
            editedOption = -1;
            Texts = new List<Sentance>();
            reference = "";
        }

        public void Execute( STD_Values so) {
            for (int j = 0; j < options.Count; j++)
                if (options[j].conditions.TestConditions(so)) { options[j].results.apply(so); break; }
            FinalResults.apply(so);
        }


        public static bool unfoldPegi;
        public void PEGI( bool OneClickAction, STD_Values st) {
            
            pegi.write("Reference name:",80);

            string modified = reference;
            if (pegi.editDelayed(ref modified)) {
                var lst = new List<Interaction>();
                st.iGroup.getAllInteractions(ref lst);
                Debug.Log("Looping interactions "+lst.Count);

                if (st.interactionReferences.Contains(modified)) {
                    pegi.resetOneTimeHint("uniRef");
                } else {

                    if ((reference.Length > 0)!= (modified.Length>0)) {
                        foreach (var s in lst)
                            s.SwapReferencesInOptions(reference, modified);
                    }

                    if (reference.Length>0)
                        st.interactionReferences.Remove(reference);

                    if (modified.Length>0)
                        st.interactionReferences.Add(modified);
                    
                    reference = modified;
                }
                    
            }

            pegi.writeOneTimeHint("References should be unique for Story Object", "uniRef");
            pegi.writeOneTimeHint("Reference name can be used to jump to another dialogue option. It is optional.", "refJump");
            pegi.writeOneTimeHint("You need to press Enter in the end to finalize changes.", "refFinalize");

            pegi.newLine();

            pegi.ClickTab(ref Condition.unfoldPegi, "Conditions");
            pegi.ClickTab(ref Sentance.showTexts, "text");
            pegi.ClickTab(ref DialogueChoice.unfoldPegi, "Choices");
            pegi.ClickTab(ref Result.showFinal, "Results");

            Condition.unfoldPegi = pegi.selectedTab == 0;
            Sentance.showTexts = pegi.selectedTab == 1;
            DialogueChoice.unfoldPegi = pegi.selectedTab == 2;
            Result.showFinal = pegi.selectedTab == 3;

            pegi.newLine();
                pegi.Space();
            pegi.newLine();

            if (Condition.unfoldPegi) 
                conditions.PEGI(st);

            if (Sentance.showTexts) 
                        Texts.PEGI();

            if ( DialogueChoice.unfoldPegi) {

                if (OneClickAction) pegi.write("One Will Be excecuted");
                pegi.newLine();

                if (editedOption == -1) {
                    if (options != null) {
                        for (int i = 0; i < options.Count; i++) {

                            if (icon.Delete.Click(20)) options.RemoveAt(i);
                            else {

                                DialogueChoice tmp = options[i];

                                //string pre = tmp.text.ToString();

                                if (pegi.edit(tmp.text))
                                    //tmp.text.setTranslation(pre);

                                if (icon.Edit.Click(20)) {
                                    editedOption = i;
                                    pegi.FocusControl("none");
                                }
                            }
                            pegi.newLine();
                         
                        }
                    }

                    if (pegi.Click("Add choice"))
                        options.Add(new DialogueChoice(null));
                    if ((options.Count > 0) && pegi.Click("Copy choice"))
                        options.Add(new DialogueChoice(options[options.Count - 1].Encode().ToString()));

                    pegi.newLine();
                } else {

                    if (pegi.Click("Collapse"))
                        editedOption = -1;
                    else
                        options[editedOption].PEGI(st);

                }
            }

            if (Result.showFinal) 
                FinalResults.PEGI(st);
        }


        void SwapReferencesInOptions(string from, string to) {
            foreach (DialogueChoice d in options)
                if (d.goToReference == from)
                    d.goToReference = to;
        }

    }

    [Serializable]
	public class InteractionBranch : abstract_STD{
        public const string storyTag = "qoEvent";
        public string name = "no name";

        public VariablesWeb conds = new VariablesWeb(null);

        public List<InteractionBranch> interactionBranches = new List<InteractionBranch>();

        public List<Interaction> interactions = new List<Interaction>();

        public bool isOneClickAction;
        public int EditorSelectedInteraction = -1;


        public override stdEncoder Encode (){
			var cody= new stdEncoder ();

            cody.AddText("name",name);
            cody.Add("cond", conds);
            cody.AddIfNotEmpty("igr", interactionBranches);
            cody.AddIfNotEmpty(interactions);

            return cody;
		}

        public override void Decode(string subtag, string data) {
            switch (subtag) {
                case "name": name = data; break;
                case "cond": conds = new VariablesWeb(data); break;
                case "igr": interactionBranches = data.ToListOf_STD<InteractionBranch>(); break; //new List<InteractionGroup>(data); break;
                case Interaction.storyTag_intrct:
                    interactions = data.ToListOf_STD<Interaction>(); break; 
            }
        }

        public override string getDefaultTagName(){return storyTag;}

        public void getAllInteractions(ref List<Interaction> lst) {
            lst.AddRange(interactions);
            foreach (InteractionBranch ig in interactionBranches)
                ig.getAllInteractions(ref lst);
        }

        public override string ToString()
        {
            return interactions.ToStringSafe();
        }

        public InteractionBranch() {
        }

        public InteractionBranch(string data) {
            Reboot(data);
        }

        public string getShortDescription() {
            return "[" + interactions.Count + "],[" + interactionBranches.Count + "]";
        }

        int browsedBranch = -1;
        public bool PEGI(STD_Values so) {

            bool changed = false;

            EditorSelectedInteraction = Mathf.Min(EditorSelectedInteraction, interactions.Count - 1);

            if (EditorSelectedInteraction != -1) {
                pegi.newLine();
                if (icon.Close.Click(20)) {
                    EditorSelectedInteraction = -1; pegi.FocusControl("none");//EditorGUI.FocusTextInControl("none");
                } else
                interactions[EditorSelectedInteraction].PEGI(isOneClickAction, so);
                
            } else {

                browsedBranch = Mathf.Clamp(browsedBranch, -1, interactionBranches.Count);

                if (browsedBranch == -1) {
                    pegi.newLine();
                    pegi.Space();

                    (" Branch:  " + name).nl();
                    
                    if (pegi.foldout("Condition Tree: ", ref Condition.unfoldPegi)) {
                        Interaction.unfoldPegi = false;
                        pegi.newLine();
                        conds.PEGI(so);
                    }
                    pegi.newLine();
                    pegi.Space();
                    pegi.newLine();

                    if (pegi.foldout("Interaction Tree", ref Interaction.unfoldPegi)) {

                        pegi.newLine();

                        Condition.unfoldPegi = false;

                        pegi.write("Interactions: ");

                        List<Interaction> Idata = interactions;

                        if (icon.Add.Click(25))
                            Idata.Add(new Interaction(null));

                        pegi.newLine();

                        for (int i = 0; i < Idata.Count; i++) {
                            if (icon.Delete.Click(20))
                                interactions.RemoveAt(i);
                            else
                                Idata[i].Texts[0].PEGI();

                            if (icon.Edit.Click(20).nl())
                                EditorSelectedInteraction = i;
                            
                        }

                        pegi.newLine();
                        pegi.Space();
                        pegi.newLine();

                        pegi.write("Sub Branches:");

                        if (icon.Add.Click(25))
                            interactionBranches.Add(new InteractionBranch(null));

                        pegi.newLine();

                        int delete = -1;
                        for (int j = 0; j < interactionBranches.Count; j++) {

                            InteractionBranch ig = interactionBranches[j];

                            if (icon.Delete.Click("Delete Interaction Sub Branch", 20))
                                delete = j;

                            pegi.edit(ref ig.name);

                            pegi.write(ig.getShortDescription(), 35);

                            if (icon.Edit.Click("Edit Interaction Sub Branch", 20).nl())
                                browsedBranch = j;
                            
                        }

                        if (delete != -1)
                            interactionBranches.RemoveAt(delete);
                    }

                    pegi.newLine();
                   
                } else {
                    if (("<"+name).Click())
                        browsedBranch = -1;
                    else
                        interactionBranches[browsedBranch].PEGI(so);
                }
                
            }
            return changed;
        }

    }

}