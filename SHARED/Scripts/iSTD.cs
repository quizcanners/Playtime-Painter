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

namespace StoryTriggerData {

    public interface iSTD : iPEGI{
        stdEncoder Encode();
        iSTD Decode(string data);
        bool Decode(string tag, string data);
        string getDefaultTagName();
    }

    // This class can be used for some backwards compatibility. 
    public interface iKeepUnrecognizedSTD : iSTD
    {
        // include as Default option in decode function:
        //Example: default: Unrecognized(tag, data); 
        void Unrecognized(string tag, string data);

        // Use cody.AddUnrecognized(this) in Encode, it will use this Function:
        void SaveUnrecognized(stdEncoder cody);
    }

    [Serializable]
    public abstract class abstractKeepUnrecognized_STD : abstract_STD, iKeepUnrecognizedSTD {
     
        protected List<string> unrecognizedTags = new List<string>();
        protected List<string> unrecognizedData = new List<string>();
        
        public void Unrecognized(string tag, string data) {
            unrecognizedTags.Add(tag);
            unrecognizedData.Add(data);
        }
        
        public void SaveUnrecognized(stdEncoder cody) {
            for (int i = 0; i < unrecognizedTags.Count; i++)
                cody.AddText(unrecognizedTags[i], unrecognizedData[i]);
        }

        public static int inspectedUnrecognized = -1;
        public override bool PEGI() {
            bool changed = false;
            if (unrecognizedTags != null) {
                "Unrecognized Tags".nl();
                for (int i=0; i<unrecognizedTags.Count; i++) {
                    if (icon.Delete.Click()) {
                        changed = true;
                        unrecognizedTags.RemoveAt(i);
                        unrecognizedData.RemoveAt(i);
                        i--;
                    }
                    else if (unrecognizedTags[i].foldout(ref inspectedUnrecognized, i).nl())
                        unrecognizedData[i].nl();
                }
            }

            return changed;
        }
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
        public virtual bool PEGI() { pegi.nl(); (GetType()+" class has no PEGI() function.").nl();
            return false; }
        public abstract bool Decode(string tag, string data);
        public abstract string getDefaultTagName();
    }

    [Serializable]
    public abstract class ComponentSTD : MonoBehaviour, iKeepUnrecognizedSTD
    {

        public abstract stdEncoder Encode();
        public abstract void Reboot();
        public virtual iSTD Decode(string data) {
            Reboot();
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }
        public abstract bool Decode(string tag, string data);
        public abstract string getDefaultTagName();

        protected List<string> unrecognizedTags = new List<string>();
        protected List<string> unrecognizedData = new List<string>();

        public void Unrecognized(string tag, string data)
        {
            unrecognizedTags.Add(tag);
            unrecognizedData.Add(data);
        }

        public void SaveUnrecognized(stdEncoder cody)
        {
            for (int i = 0; i < unrecognizedTags.Count; i++)
                cody.AddText(unrecognizedTags[i], unrecognizedData[i]);
        }

        public static int inspectedUnrecognized = -1;
        public virtual bool PEGI()
        {
            bool changed = false;
            if (unrecognizedTags != null)
            {
                "Unrecognized Tags".nl();
                for (int i = 0; i < unrecognizedTags.Count; i++)
                {
                    if (icon.Delete.Click())
                    {
                        changed = true;
                        unrecognizedTags.RemoveAt(i);
                        unrecognizedData.RemoveAt(i);
                        i--;
                    }
                    else if (unrecognizedTags[i].foldout(ref inspectedUnrecognized, i).nl())
                        unrecognizedData[i].nl();
                }
            }

            return changed;
        }

    }

    public static class STDExtensions {

        public static iSTD RefreshAssetDatabase(this iSTD s) {
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            return s;
        }

        public static iSTD SaveToResources(this iSTD s, string ResFolderPath, string InsideResPath, string filename) {
            ResourceSaver.SaveToResources(ResFolderPath, InsideResPath, filename, s.Encode().ToString());
            return s;
        }

        public static iSTD SaveToAssets(this iSTD s, string Path, string filename) {
            ResourceSaver.Save(Application.dataPath + Path.AddPreSlashIfNotEmpty() + "/", filename, s.Encode().ToString());
            return s;
        }

        public static iSTD SaveProgress(this iSTD s, string Path, string filename) {
            ResourceSaver.Save(Application.persistentDataPath + Path.AddPreSlashIfNotEmpty() + "/", filename, s.Encode().ToString());
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
            s.Decode(ResourceLoader.Load(Application.persistentDataPath + Folder.AddPreSlashIfNotEmpty() + "/" + fileName + ResourceSaver.fileType));
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

    }
}