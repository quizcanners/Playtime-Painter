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



namespace StoryTriggerData {

   
    [ExecuteInEditMode]
    public class Book : ComponentSTD {

        public static Book inst { get {

                if (_inst == null)
                    _inst = FindObjectOfType<Book>();

                if (_inst == null) {
                    _inst = (new GameObject().AddComponent<Book>());
                }

                return _inst;
            }
        }

        static Book _inst;

        public STD_Values stdValues = new STD_Values();

        public const string storyTag = "HOME";


        public override string getDefaultTagName() {
            return storyTag;
        }


        public static List<Page> HOMEpages;

        public List<String> OtherBooks;


        // *********************** SAVING/LOADING  MGMT
        [NonSerialized]
        bool Loaded;
      

        public override void Decode(string tag, string data) {

            //UnityEngine.Debug.Log("Decoding: "+data);

            switch (tag) {
                case "name": gameObject.name = data; break;
                case "spos": UniversePosition.playerPosition.Reboot(data); break;
                case "pages":
                    HOMEpages = data.ToListOfStoryPoolablesOfTag<Page>(Page.storyTag);
                    break;
            }

        }

        public override void Reboot() {
            HOMEpages = new List<Page>();
            if (!Application.isPlaying)
                STD_Pool.DestroyAll();
            Loaded = false;
        }

        public override iSTD Reboot(string data) {
            Reboot();

            var cody = new stdDecoder(data);

            while (cody.gotData)
                Decode(cody.getTag(), cody.getData());
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
            Reboot(ResourceLoader.LoadStoryFromResource(TriggerGroups.StoriesFolderName, gameObject.name, storyTag));
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

      

        public void OnEnable() {

            _inst = this;

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

            if (browsedPage >= pool.Max)
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
                    sp.Reboot(null);
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

                UniversePosition.playerPosition.PEGI();

            } else {
                if (pegi.Click("< Pages"))
                    browsedPage = -1;
                else
                pool.scripts[browsedPage].PEGI();
            }

            return changed;

        }


        // *********************** TIMED EVENTS MONItORING

        bool waiting;
        float timeToWait = -1;

        public void AddTimeListener(float seconds) {
            seconds += 0.5f;
            if (!waiting) timeToWait = seconds;
            else timeToWait = Mathf.Min(timeToWait, seconds);
            waiting = true;
        }


        public Page lerpTarget;
        [NonSerialized]
        public UniversePosition lerpPosition;

        void CombinedUpdate() {

            if (lerpTarget != null)
            {
                //   "Lerpong".Log();

                if ((lerpTarget.enabled == false) || (SpaceValues.playerPosition.LerpTo(lerpTarget.sPOS, lerpTarget.uReach, Application.isPlaying ? Time.deltaTime : 0.5f) < 1)) lerpTarget = null;


            }
            else if (lerpPosition != null)
            {
                if (SpaceValues.playerPosition.LerpTo(lerpPosition, UniverseLength.one, Application.isPlaying ? Time.deltaTime : 0.5f) < 1) lerpPosition = null;
            }
            

            if (waiting) {
                timeToWait -= Time.deltaTime;
                if (timeToWait < 0) {
                    waiting = false;
                    STD_Values.AddQuestVersion();
                }
            }
        }

        void Update() {

            if (StoryGodMode.inst != null)
                StoryGodMode.inst.DistantUpdate();

            if (Application.isPlaying)
                CombinedUpdate();
            foreach (var p in HOMEpages)
                p.PostPositionUpdate();
        }

    

        public static int RealTimeOnStartUp = 0;
        public void Awake() {
            RealTimeOnStartUp = (int)((DateTime.Now.Ticks - 733000 * TimeSpan.TicksPerDay) / TimeSpan.TicksPerSecond);
        }

        public static int GetRealTime() {
            return RealTimeOnStartUp + (int)Time.realtimeSinceStartup;
        }

    }
}