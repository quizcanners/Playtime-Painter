using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

// This interface works for simple data and for complex classes
// Usually the base class for comples classes will have 

namespace StoryTriggerData {

    public interface iSTD {
        stdEncoder Encode();
        iSTD Reboot(string data);
        void Decode(string tag, string data);
        bool PEGI();
        string getDefaultTagName();
    }

    [Serializable]
    public abstract class abstract_STD : iSTD {

        public abstract stdEncoder Encode();
        public virtual iSTD Reboot(string data) {
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }
        public virtual bool PEGI() { pegi.nl(); (GetType()+" class has no PEGI() function.").nl(); return false; }
        public abstract void Decode(string tag, string data);
        public abstract string getDefaultTagName();

    }

    [Serializable]
    public abstract class ComponentSTD : MonoBehaviour, iSTD {

        public abstract stdEncoder Encode();
        public abstract void Reboot();
        public virtual bool PEGI() { return false; }
        public virtual iSTD Reboot(string data) {
            Reboot();
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }
        public abstract void Decode(string tag, string data);
        public abstract string getDefaultTagName();

    }

    public static class STDExtensions {

        public static void SaveToResources(this iSTD s, string ResFolderPath, string InsideResPath, string filename) {
            ResourceSaver.SaveToResources(ResFolderPath, InsideResPath, filename, s.Encode().ToString());
        }

        public static void SaveToAssets(this iSTD s, string Path, string filename) {
            ResourceSaver.Save(Application.dataPath + Path.AddPreSlashIfNotEmpty() + "/", filename, s.Encode().ToString());
        }

        public static void SaveProgress(this iSTD s, string Path, string filename) {
            ResourceSaver.Save(Application.persistentDataPath + Path.AddPreSlashIfNotEmpty() + "/", filename, s.Encode().ToString());
        }

		public static T LoadFromAssets<T>(this T s, string fullPath, string name) where T:iSTD, new() {
			if (s == null)
				s = new T ();
            s.Reboot(ResourceLoader.LoadStoryFromAssets(fullPath, name));
			return s;
        }

		public static T LoadSavedProgress<T>(this T s, string Folder, string fileName)where T:iSTD, new() {
			if (s == null)
				s = new T ();
            s.Reboot(ResourceLoader.Load(Application.persistentDataPath + Folder.AddPreSlashIfNotEmpty() + "/" + fileName + ResourceSaver.fileType));
			return s;
		}

		public static T LoadFromResources<T>(this T s, string resFolderLocation, string subFolder, string file)where T:iSTD, new() {
			if (s == null)
				s = new T ();
			s.Reboot(ResourceLoader.LoadStoryFromResource(resFolderLocation, subFolder, file));
			return s;
		}

		public static T LoadFromResources<T>(this T s, string subFolder, string file)where T:iSTD, new() {
			if (s == null)
				s = new T ();
			s.Reboot(ResourceLoader.LoadStoryFromResource(subFolder, file));
			return s;
		}

    }
}