using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// This interface works for simple data and for complex classes
// Usually the base class for comples classes will have 

namespace SharedTools_Stuff
{

    public interface iSTD
        #if PEGI
        : iPEGI
#endif 
        {
        stdEncoder Encode(); 
        iSTD Decode(string data);
        bool Decode(string tag, string data);
    }

  /* 
   * Implementation Example:
   * 
   * public bool Decode(string tag, string data)
    {
        switch (tag)
        {
            case "tf": data.DecodeInto(transform); break;
            case "n": transform.name = data; break;

            default: return false;
        }

        return true;
    }

    public stdEncoder Encode()
    {
        var cody = new stdEncoder();
        cody.Add("tf", transform);
        cody.AddText("n", name);
        return cody;
    }
    */


    // This class can be used for some backwards compatibility. 
    public interface iKeepUnrecognizedSTD : iSTD {
        void Unrecognized(string tag, string data);
        
        stdEncoder SaveUnrecognized(stdEncoder cody);
    }

    [Serializable]
    public abstract class abstractKeepUnrecognized_STD : abstract_STD, iKeepUnrecognizedSTD {
     
        protected List<string> unrecognizedTags = new List<string>();
        protected List<string> unrecognizedData = new List<string>();
        
        public void Unrecognized(string tag, string data) {
            this.Unrecognized(tag, data, ref unrecognizedTags, ref unrecognizedData);
        }
        
        public stdEncoder SaveUnrecognized(stdEncoder cody) {
            for (int i = 0; i < unrecognizedTags.Count; i++)
                cody.Add_String(unrecognizedTags[i], unrecognizedData[i]);
            return cody;
        }


#if PEGI
           bool showUnrecognized = false;
        public static int inspectedUnrecognized = -1;
        public bool PEGI_Unrecognized()
        {

            bool changed = false;

            pegi.nl();

            var cnt = unrecognizedTags.Count;

            if (cnt > 0 && ("Unrecognized for " + ToString() + "[" + cnt + "]").foldout(ref showUnrecognized).nl())
                changed |= this.PEGI(ref unrecognizedTags, ref unrecognizedData, ref inspectedUnrecognized);

            return changed;
        }
       
        public override bool PEGI() {

            bool changed = PEGI_Unrecognized();

        
            return changed;
        }
#endif
    }


    [Serializable]
    public abstract class abstract_STD : iSTD {

        public abstract stdEncoder Encode();
        public virtual iSTD Decode(string data) {
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }
        public virtual iSTD Decode(stdEncoder cody) {
           
            new stdDecoder(cody.ToString()).DecodeTagsFor(this);
            return this;
        }
        #if PEGI
        public virtual bool PEGI() { pegi.nl(); (GetType()+" class has no PEGI() function.").nl();
            return false; }
#endif
        public abstract bool Decode(string tag, string data);
    }

    public abstract class ComponentSTD : MonoBehaviour, iKeepUnrecognizedSTD {

        public override string ToString()
        {
            return gameObject.name;
        }

        public virtual void Reboot() {

        }

        public virtual iSTD Decode(string data) {
            Reboot();
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }

        protected List<string> unrecognizedTags = new List<string>();
        protected List<string> unrecognizedData = new List<string>();

        public void Unrecognized(string tag, string data) {
            this.Unrecognized(tag, data, ref unrecognizedTags, ref unrecognizedData);
        }
        public stdEncoder SaveUnrecognized(stdEncoder cody)
        {
            for (int i = 0; i < unrecognizedTags.Count; i++)
                cody.Add_String(unrecognizedTags[i], unrecognizedData[i]);
            return cody;
        }

        public iSTD_ExplorerData explorer = new iSTD_ExplorerData();
        public bool showDebug;

        #if PEGI
        [NonSerialized] public int inspectedUnrecognized = -1;
        public virtual bool PEGI() {

            bool changed = false;

            if (!showDebug && icon.Config.Click())
                showDebug = true;

           

            if (showDebug)
            {
                if (icon.Exit.Click("Back to element inspection").nl())
                    showDebug = false;
                
                explorer.PEGI(this);
                
            }
            return changed;
        }
#endif

        public virtual bool Decode(string tag, string data)
        {
            switch (tag){
                case "tf": data.DecodeInto(transform); break;
                case "n": transform.name = data; break;

                default: return false;
            }

            return true;
        }

        public virtual stdEncoder Encode() {
            var cody = new stdEncoder();
            cody.Add("tf", transform);
            cody.Add_String("n", name);
            return cody;
        }
    }

    public static class STDExtensions {

        public static string copyBufferValue;

        public static iSTD RefreshAssetDatabase(this iSTD s) {
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            return s;
        }

        public static bool LoadOnDrop<T>(this T obj) where T: iSTD {

#if PEGI
             UnityEngine.Object myType = null;
            if (pegi.edit(ref myType)) {
                obj.Decode(ResourceLoader.LoadStory(myType));

                ("Loaded " + myType.name).showNotification();

                return true;
            }
#endif
            return false;
        }

        public static iSTD SaveToResources(this iSTD s, string ResFolderPath, string InsideResPath, string filename) {
            ResourceSaver.SaveToResources(ResFolderPath, InsideResPath, filename, s.Encode().ToString());
            return s;
        }
        
        public static iSTD SaveToAssets(this iSTD s, string Path, string filename) {
            ResourceSaver.Save(Application.dataPath + Path.AddPreSlashIfNotEmpty().AddPostSlashIfNone(), filename, s.Encode().ToString());
            return s;
        }

        public static iSTD SaveProgress(this iSTD s, string Path, string filename) {
            ResourceSaver.Save(Application.persistentDataPath + Path.AddPreSlashIfNotEmpty().AddPostSlashIfNone(), filename, s.Encode().ToString());
            return s;
        }

		public static T LoadFromAssets<T>(this T s, string fullPath, string name) where T:iSTD, new() {
			if (s == null)
				s = new T ();
            s.Decode(ResourceLoader.LoadStoryFromAssets(fullPath, name));
			return s;
        }

		public static T LoadSavedProgress<T>(this T s, string Folder, string fileName)where T:iSTD, new() {
			if (s == null)
				s = new T ();
            s.Decode(ResourceLoader.Load(Application.persistentDataPath + Folder.AddPreSlashIfNotEmpty().AddPostSlashIfNone() + fileName + ResourceSaver.fileType));
			return s;
		}

		public static T LoadFromResources<T>(this T s, string resFolderLocation, string subFolder, string file)where T:iSTD, new() {
			if (s == null)
				s = new T ();
			s.Decode(ResourceLoader.LoadStoryFromResource(resFolderLocation, subFolder, file));
			return s;
		}

		public static T LoadFromResources<T>(this T s, string subFolder, string file)where T:iSTD, new() {
			if (s == null)
				s = new T ();
			s.Decode(ResourceLoader.LoadStoryFromResource(subFolder, file));
			return s;
		}
        /*
        public static bool PEGI <T>(this T mono, ref iSTD_Explorer exp) where T:MonoBehaviour, iSTD {
            bool changed = false;
            #if PEGI
            if (!exp) {
                exp = mono.GetComponent<iSTD_Explorer>();
                if (!exp && "Add iSTD Explorer".Click())
                    exp = mono.gameObject.AddComponent<iSTD_Explorer>();

                changed |= exp != null;
            }
            else
            {
                exp.ConnectSTD = mono;
                changed |=exp.PEGI();
            }  
#endif

            return changed;
        }
        */
        #if PEGI
        public static bool PEGI(this iKeepUnrecognizedSTD el, ref List<string> tags, ref List<string> data, ref int inspected)  {
            bool changed = false;
            if (tags != null && tags.Count > 0) {
               // ("Unrecognized Tags on "+el.ToString()).nl();

                if (inspected < 0)
                {

                    for (int i = 0; i < tags.Count; i++)
                    {
                        if (icon.Delete.Click())
                        {
                            changed = true;
                            tags.RemoveAt(i);
                            data.RemoveAt(i);
                            i--;
                        }
                        else
                        {
                            pegi.write(tags[i]);
                            if (icon.Edit.Click().nl())
                                inspected = i;
                        }
                    }
                }
                else
                {
                    if (inspected >= tags.Count || icon.Back.Click())
                        inspected = -1;
                    else
                    {
                        int i = inspected;
                        var t = tags[i];
                        if ("Tag".edit(40, ref t).nl())
                            tags[i] = t;
                        var d = data[i];
                        if ("Data".edit(50, ref d).nl())
                            data[i] = d;
                    }
                }
            }

            pegi.nl();

            return changed;
        }
#endif
        public static void Unrecognized (this iKeepUnrecognizedSTD el, string tag, string data, ref List<string> unrecognizedTags, 
            ref List<string> unrecognizedData) {
          
                if (unrecognizedTags.Contains(tag))
                {
                    int ind = unrecognizedTags.IndexOf(tag);
                    unrecognizedTags[ind] = tag;
                    unrecognizedData[ind] = data;
                }
                else
                {
                    unrecognizedTags.Add(tag);
                    unrecognizedData.Add(data);
                }
            
        }

    }
}