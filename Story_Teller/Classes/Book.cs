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
using STD_Logic;

namespace StoryTriggerData
{
    
    [ExecuteInEditMode]
    public class Book : LogicMGMT
    {

        public static Book Inst { get { return (Book)inst; } }

        public InteractionTarget stdValues = new InteractionTarget();
        
        public const string StoriesFolderName = "Stories";

        public const string PagesFolderName = "Pages";

        public const string BookSOname = "Book";

        public const string PrefabsResourceFolder = "stdPrefabs";

        public const string TriggersFolderName = "Triggers";

        [NonSerialized]
        public List<Page> HOMEpages;

        // *********************** SAVING/LOADING  MGMT
 
        public void SaveChanges()
        {

        }

        public void OnDisable()
        {
            UnityHelperFunctions.FocusOn(this);
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                TriggerGroup.SaveAll(StoriesFolderName + "/" + TriggersFolderName);

                foreach (Page p in Page.myPoolController.scripts)
                    if ((p != null) && (p.gameObject.activeSelf))
                        p.SavePageContent();

                AssetDatabase.Refresh();
#endif
            }

        }

        public override void OnEnable()
        {

            base.OnEnable();

            STD_Pool.InitStoryPoolsIfNull();

            HOMEpages = new List<Page>();
            if (!Application.isPlaying)
                STD_Pool.DestroyAll();
            
#if UNITY_EDITOR

            EditorApplication.update -= CombinedUpdate;
            if (!Application.isPlaying)
                EditorApplication.update += CombinedUpdate;

            try
            {
              //  DirectoryInfo levelDirectoryPath = new DirectoryInfo(Application.dataPath + StoriesFolderName.AddPreSlashIfNotEmpty() + "/Resources");

             //   FileInfo[] fileInfo = levelDirectoryPath.GetFiles("*.*", SearchOption.TopDirectoryOnly);

                //foreach (FileInfo file in fileInfo)
                //{
                    //if ((file.Extension == ResourceSaver.fileType) && (!file.Name.Substring(0, file.Name.Length - ResourceSaver.fileType.Length).Equals(gameObject.name)))

                //}
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex.ToString());
            }

#else


#endif

        }

        // *********************** COMPONENT MGMT
        
       
#if PEGI
        int browsedPage = -1;
        bool unfoldTriggerGroup = false;
        public override bool PEGI()
        {
            bool changed = base.PEGI();

            if (!showDebug)
            {

                Page pg = null;

                pegi.nl();

                "Test".select(ref pg, HOMEpages).nl();
                
                PoolController<Page> pool = Page.myPoolController;

                if (browsedPage >= pool.initializedCount)
                    browsedPage = -1;

                if (browsedPage == -1)
                {

                    pegi.writeOneTimeHint("You need to press Enter in the end to rename Books, Pages and Triggers", "bookRename");
                    pegi.newLine();
                    "Trigger groups: ".nl();

                    foreach (TriggerGroup s in TriggerGroup.all.GetAllObjsNoOrder())
                    {
                        pegi.write(s.ToString(), 80);
                        pegi.write(s.GetIndex().ToString(), 30);

                        if (unfoldTriggerGroup && (TriggerGroup.Browsed == s))
                        {
                            if (icon.Close.Click(20))
                                unfoldTriggerGroup = false;
                            pegi.newLine();
                            s.PEGI();
                        }
                        else if (icon.Edit.Click(20))
                        {
                            TriggerGroup.Browsed = s;
                            unfoldTriggerGroup = true;
                        }

                        pegi.newLine();
                    }

                    pegi.write("Pages :");

                    if (icon.Add.Click(25).nl())
                    {
                        Page sp = pool.getOne();
                        sp.Decode(null);
                        HOMEpages.Add(sp);
                        sp.OriginBook = this.name;
                    }

                    int Delete = -1;

                    for (int i = 0; i < HOMEpages.Count; i++)
                    {
                        Page p = HOMEpages[i];
                        if (p == null) HOMEpages.RemoveAt(i);
                        else
                        {
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

                    if ("Save And Clear".Click()) OnDisable();

                    pegi.newLine();

                    if (SpaceValues.playerPosition.PEGI())
                        AfterPlayerSpacePosUpdate();




                }
                else
                {
                    if (pegi.Click("< Pages"))
                        browsedPage = -1;
                    else
                        pool.scripts[browsedPage].PEGI();
                }
            }
            return changed;

        }

        [MenuItem("Tools/"+ StoriesFolderName+ "/Instantiate Book..")]
        public static void CreateBook() => UnityHelperFunctions.CreateAsset_SO_DONT_RENAME<Book>(StoriesFolderName, BookSOname);
        
        public static bool InstantiateBookPEGI()
        {
            if (Inst == null && "Add Book".Click())
                CreateBook();

            return false;
        }

#endif
        public Page lerpTarget;
        [NonSerialized]
        public UniversePosition lerpPosition;

        public void AfterPlayerSpacePosUpdate()
        {
            Vector3 v3 = SpaceValues.playerPosition.Meters;
            Shader.SetGlobalVector("_wrldOffset", new Vector4(v3.x, v3.y, v3.z, 0));
        }

        void CombinedUpdate()
        {

            if (lerpTarget != null)
            {

                if ((lerpTarget.enabled == false) || (SpaceValues.playerPosition.LerpTo(lerpTarget.sPOS, lerpTarget.uReach, Application.isPlaying ? Time.deltaTime : 0.5f) < 1)) lerpTarget = null;
                AfterPlayerSpacePosUpdate();

            }
            else if (lerpPosition != null)
            {
                if (SpaceValues.playerPosition.LerpTo(lerpPosition, UniverseLength.one, Application.isPlaying ? Time.deltaTime : 0.5f) < 1) lerpPosition = null;
                AfterPlayerSpacePosUpdate();
            }



        }

        override public void Update()
        {

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