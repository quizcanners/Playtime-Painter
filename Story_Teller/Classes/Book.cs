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
using LogicTree;

namespace StoryTriggerData {

   
    [ExecuteInEditMode]
    public class Book : LogicMGMT {

       public static Book instBook { get { return (Book)inst; } }

        public InteractionTarget stdValues = new InteractionTarget();

        public const string storyTag = "HOME";

        public override string getDefaultTagName() {
            return storyTag;
        }
        
        public static List<Page> HOMEpages;

        public List<String> OtherBooks;
        

        // *********************** SAVING/LOADING  MGMT
        [NonSerialized]
        bool Loaded;
      
        public override bool Decode(string tag, string data) {

            switch (tag) {
                case "name": gameObject.name = data; break;
                case "spos": UniversePosition.playerPosition.Decode(data); AfterPlayerSpacePosUpdate();  break;
                case "pages":HOMEpages = data.ToListOfStoryPoolablesOfTag<Page>(Page.storyTag); break;
                default: return false;
            }
            return true;
        }

        public override void Reboot() {
            HOMEpages = new List<Page>();
            if (!Application.isPlaying)
                STD_Pool.DestroyAll();
            Loaded = false;
        }

        public override iSTD Decode(string data) {
            Reboot();
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }

        public override stdEncoder Encode() {
            stdEncoder cody = new stdEncoder();
            cody.AddText("name", gameObject.name);

            cody.Add("spos", UniversePosition.playerPosition);

            cody.AddIfNotEmpty("pages", HOMEpages);

            return cody;
        }

        public static string PrefabsResourceFolder = "stdPrefabs";

        public void LoadOrInit() {
            Decode(ResourceLoader.LoadStoryFromResource(TriggerGroups.StoriesFolderName, gameObject.name, storyTag));
            Loaded = true;
        }

        public void SaveChanges() {
#if UNITY_EDITOR
            if (Loaded) {
                TriggerGroups.Save();

                foreach (Page p in Page.myPoolController.scripts)
                    if ((p != null) && (p.gameObject.activeSelf))
                        p.SavePageContent();

                inst.SaveToResources(TriggerGroups.StoriesFolderName, gameObject.name, storyTag);
                AssetDatabase.Refresh();
            }
#endif
        }
        
        public void OnDisable() {
                UnityHelperFunctions.FocusOn(this.gameObject);
            if (!Application.isPlaying)
                SaveChanges();
                Reboot();
            
        }

        public override void OnEnable() {

            base.OnEnable();

            STD_Pool.InitStoryPoolsIfNull();

            LoadOrInit();

#if UNITY_EDITOR

            EditorApplication.update -= CombinedUpdate;
            if (!Application.isPlaying)
                EditorApplication.update += CombinedUpdate;

            try {
                DirectoryInfo levelDirectoryPath = new DirectoryInfo(Application.dataPath + TriggerGroups.StoriesFolderName.AddPreSlashIfNotEmpty() + "/Resources");

                FileInfo[] fileInfo = levelDirectoryPath.GetFiles("*.*", SearchOption.TopDirectoryOnly);

                foreach (FileInfo file in fileInfo) {
                    // file name check
                    //if (file.Name == "something") {

                    //}
                    // file extension check
                    if ((file.Extension == ResourceSaver.fileType) && (!file.Name.Substring(0, file.Name.Length-ResourceSaver.fileType.Length).Equals(gameObject.name))) {
                        UnityEngine.Debug.Log("Found another book" + file.Name);
                    }
                    // etc.
                }
            } catch(Exception ex) {
                UnityEngine.Debug.Log(ex.ToString());
            }

#else


#endif

        }

        // *********************** COMPONENT MGMT

    int browsedPage = -1;
        string nameHold;

    public void RenameBook(string newName) {
#if UNITY_EDITOR

            string path = "Assets" + TriggerGroups.StoriesFolderName.AddPreSlashIfNotEmpty() + "/Resources/";

            foreach (Page p in HOMEpages)
                if (p.OriginBook == this.gameObject.name)
                    p.OriginBook = newName;

            AssetDatabase.RenameAsset(path + gameObject.name,  newName);
            AssetDatabase.RenameAsset(path + gameObject.name+ ResourceSaver.fileType, newName + ResourceSaver.fileType);
            gameObject.name = newName;
            OnDisable();
            LoadOrInit();
#endif
        }


        bool unfoldTriggerGroup = false;
	public override bool PEGI(){
            bool changed = false;
            PoolController<Page> pool = Page.myPoolController;

            if (browsedPage >= pool.initializedCount)
                browsedPage = -1;

            if (browsedPage == -1) {

                pegi.write("A BOOK BY: ", 60);
                string nameHold = gameObject.name;
                if (pegi.editDelayed(ref nameHold).nl())
                    RenameBook(nameHold);

                pegi.writeOneTimeHint("You need to press Enter in the end to rename Books, Pages and Triggers", "bookRename");
                pegi.newLine();
                "Trigger groups: ".nl();

                foreach (TriggerGroups s in TriggerGroups.all.GetAllObjsNoOrder()) {
                    pegi.write(s.ToString(),80); 
                    pegi.write(s.GetHashCode().ToString(),30);

                    if (unfoldTriggerGroup && (TriggerGroups.browsed == s)) {
                        if (icon.Close.Click(20))
                            unfoldTriggerGroup = false;
                        pegi.newLine();
                        s.PEGI();
                    } else if (icon.Edit.Click(20)) {
                        TriggerGroups.browsed = s;
                        unfoldTriggerGroup = true;
                    }

                    pegi.newLine();
                }


                if (Loaded) { 
                pegi.write("Pages :");

                if (icon.Add.Click(25).nl()) {
                    Page sp = pool.getOne();
                    sp.Decode(null);
                    HOMEpages.Add(sp);
                    sp.OriginBook = this.gameObject.name;
                }

                int Delete = -1;

                for (int i = 0; i < HOMEpages.Count; i++  ) {
                    Page p = HOMEpages[i];
                    if (p == null) HOMEpages.RemoveAt(i);
                    else {
                        if (icon.Delete.Click(20)) Delete = i;

                        string holder = p.gameObject.name;
                        if (pegi.editDelayed(ref holder))
                            p.RenamePage(holder);//gameObject.name = holder;

                        if (icon.Edit.Click(20).nl())
                            browsedPage = p.poolIndex;

                    }
                    }

                if (Delete != -1)
                    HOMEpages[Delete].Deactivate();

                
                    if ("Clear All".Click()) Reboot();

                    if ("Save And Clear".Click()) OnDisable();
                } else 
                if ("Load All".Click()) LoadOrInit();

                pegi.newLine();

                if (UniversePosition.playerPosition.PEGI())
                    AfterPlayerSpacePosUpdate();

            } else {
                if (pegi.Click("< Pages"))
                    browsedPage = -1;
                else
                pool.scripts[browsedPage].PEGI();
            }

            return changed;

        }


        public Page lerpTarget;
        [NonSerialized]
        public UniversePosition lerpPosition;

        public void AfterPlayerSpacePosUpdate() {
            Vector3 v3 = SpaceValues.playerPosition.Meters;
            Shader.SetGlobalVector("_wrldOffset", new Vector4(v3.x, v3.y, v3.z,0));
        }

        void CombinedUpdate() {

            if (lerpTarget != null) {

                if ((lerpTarget.enabled == false) || (SpaceValues.playerPosition.LerpTo(lerpTarget.sPOS, lerpTarget.uReach, Application.isPlaying ? Time.deltaTime : 0.5f) < 1)) lerpTarget = null;
                AfterPlayerSpacePosUpdate();

            }
            else if (lerpPosition != null)
            {
                if (SpaceValues.playerPosition.LerpTo(lerpPosition, UniverseLength.one, Application.isPlaying ? Time.deltaTime : 0.5f) < 1) lerpPosition = null;
                AfterPlayerSpacePosUpdate();
            }
            

           
        }

        override public void Update() {

            base.Update();

            if (StoryGodMode.inst != null)
                StoryGodMode.inst.DistantUpdate();

            if (Application.isPlaying)
                CombinedUpdate();
            foreach (var p in HOMEpages)
                p.PostPositionUpdate();
        }
        

    }
}