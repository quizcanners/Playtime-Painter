using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using PlayerAndEditorGUI;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SharedTools_Stuff
{

    public static class StuffExplorer
    {

        public static void OpenPersistantFolder()
        {
#if UNITY_EDITOR
            EditorUtility.RevealInFinder(Application.persistentDataPath);
#endif
        }

        public static void OpenPersistantFolder(string folder)
        {
#if UNITY_EDITOR
            EditorUtility.RevealInFinder(Path.Combine(Application.persistentDataPath, folder));
#endif
        }
    }

    public static class StuffDeleter
    {

        public static bool DeleteFile_PersistantFolder(string subPath, string fileName)
            => DeleteFile(Path.Combine(Application.persistentDataPath, subPath, Path.Combine(Application.persistentDataPath, subPath, "{0}{1}".F(fileName, StuffSaver.jsonFileType))));
        
        public static bool DeleteFile(string path)  {
            if (File.Exists(path))
            {
#if PEGI  && UNITY_EDITOR
                "Deleting {0}".F(path).showNotification();
#endif

                File.Delete(path);
                return true;
            }
            else
                return false;
        }
    }

    public static class StuffLoader
    {

        public static string Load(string fullPath)
        {
            string data = null;
            if (File.Exists(fullPath))
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    FileStream file = File.Open(fullPath, FileMode.Open);
                    data = (string)bf.Deserialize(file);
                    file.Close();
                }
                catch (Exception ex)
                {
                    //#if UNITY_EDITOR
                    Debug.Log(fullPath + " not loaded " + ex.ToString());
                    //#endif
                }
            }
            return data;
        }
        
        public static string LoadStoryFromResource(string resourceFolderLocation, string insideResourceFolder, string name)
        {


            string data = null;

#if UNITY_EDITOR

            string resourceName = insideResourceFolder.AddPreSlashIfNotEmpty() + "/" + name;
            string path = Application.dataPath + resourceFolderLocation.AddPreSlashIfNotEmpty() + "/Resources" + resourceName + StuffSaver.fileType;
            
            if (File.Exists(path))
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
   
                    FileStream file = File.Open(path, FileMode.Open);
                    data = (string)bf.Deserialize(file);
                    file.Close();
                }
                catch (Exception ex)
                {
                    Debug.Log(path + "is Busted !" + ex.ToString());

                }

            }


#endif
#if !UNITY_EDITOR
        data = LoadStoryFromResource( insideResourceFolder,  name);
       /* {
        TextAsset asset = Resources.Load(resourceName) as TextAsset;

            if (asset != null) {
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(asset.bytes);
                data = (string)bf.Deserialize(ms);
                Resources.UnloadAsset(asset);
                //Debug.Log("Loaded " + pathNdName);
               
            }
            else
            {
                //   Debug.Log("Failed liading "+pathNdName);
              
            }
        }*/
#endif

            return data;
        }
        
        public static string LoadStoryFromResource(string insideResourceFolder, string name)
        {
            string resourceName = insideResourceFolder + (insideResourceFolder.Length > 0 ? "/" : "") + name;
            string data = null;



            TextAsset asset = Resources.Load(resourceName) as TextAsset;

            if (asset != null)
            {
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(asset.bytes);
                data = (string)bf.Deserialize(ms);
                Resources.UnloadAsset(asset);
                //Debug.Log("Loaded " + pathNdName);

            }
            else
            {
                //   Debug.Log("Failed liading "+pathNdName);

            }



            return data;
        }
        
        public static string LoadTextAsset(UnityEngine.Object o)
        {
            string data = null;
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(o);
            if (path != null)
            {
                string subpath = Application.dataPath;
                path = subpath.Substring(0, subpath.Length - 6) + path;
                return Load(path);
            }

#endif
            return data;
        }

        public static string LoadStoryFromAssets(string Folder, string name)
        {


            string data = null;

#if UNITY_EDITOR
            string path = Application.dataPath + Folder.AddPreSlashIfNotEmpty() + "/" + name + StuffSaver.fileType;

            if (File.Exists(path))
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();

                    FileStream file = File.Open(path, FileMode.Open);
                    data = (string)bf.Deserialize(file);
                    file.Close();
                }
                catch (Exception ex)
                {
                    Debug.Log(path + "is Busted !" + ex.ToString());

                }

            }
#endif

            return data;
        }

        public static string LoadFromPersistantPath(string subPath, string filename)
        {
            var filePath = Path.Combine(Application.persistentDataPath, subPath, "{0}{1}".F(filename, StuffSaver.jsonFileType));

            if (File.Exists(filePath))
                return File.ReadAllText(filePath);

            return null;
        }

        public static List<String> ListFileNamesFromPersistantFolder(string subPath) {
           var lst = new List<string> ( Directory.GetFiles(Path.Combine(Application.persistentDataPath, subPath)));

            for (int i=0; i<lst.Count; i++)
            {
                var txt = lst[i].Replace(@"\", @"/");
                txt = txt.Substring(txt.LastIndexOf("/")+1);
                txt = txt.Substring(0, txt.Length - StuffSaver.jsonFileType.Length);
                lst[i] = txt;
            }

            return lst;
        }

        public static bool LoadResource<T>(string pathNdName, ref T Arrangement)
        {
#if UNITY_EDITOR
            string path = Application.dataPath + "/Resources/" + pathNdName + StuffSaver.fileType;

            if (File.Exists(path))
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    FileStream file = File.Open(path, FileMode.Open);
                    Arrangement = (T)bf.Deserialize(file);
                    file.Close();
                }
                catch (Exception ex)
                {
                    Debug.Log(path + "is Busted !" + ex.ToString());
                    return false;
                }
                return true;
            }
            Debug.Log(path + " not found");
            return false;
#endif
#if !UNITY_EDITOR
        {
            TextAsset asset = Resources.Load(pathNdName) as TextAsset;

            if (asset != null) {
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(asset.bytes);
                Arrangement = (T)bf.Deserialize(ms);
                Resources.UnloadAsset(asset);
                //Debug.Log("Loaded " + pathNdName);
                return true;
            }
            else
            {
                //   Debug.Log("Failed liading "+pathNdName);
                return false;
            }
        }
#endif
        }

        public static bool LoadFrom<T>(string path, string name, ref T _dta)
        {

            string fullPath = path + "/" + name + StuffSaver.fileType;
            if (File.Exists(fullPath))
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    FileStream file = File.Open(fullPath, FileMode.Open);
                    _dta = (T)bf.Deserialize(file);
                    file.Close();
                }
                catch (Exception ex)
                {
                    //#if UNITY_EDITOR
                    Debug.Log(path + " not loaded " + ex.ToString());
                    //#endif
                    return false;
                }
                return true;
            }


            return false;
        }

        public static bool LoadStreamingAssets<T>(string fileName, ref T dta) {

            string filePath = Application.streamingAssetsPath + "/" + fileName + ".json";

            if (File.Exists(filePath))
            {
                string dataAsJson = File.ReadAllText(filePath);
                dta = JsonUtility.FromJson<T>(dataAsJson);
                return dta != null;
            }

            return false;
        }

    }


    public class StuffLoaderAssync<T>
    {
        ResourceRequest rqst;

        public bool TryUnpackAsset(ref T Arrangement)
        {
            if (rqst.isDone)
            {
                TextAsset asset = rqst.asset as TextAsset;
                using (MemoryStream ms = new MemoryStream(asset.bytes))
                {
                    Arrangement = (T)((new BinaryFormatter()).Deserialize(ms));
                }
                Resources.UnloadAsset(asset);
                return true;
            }
            else
                return false;
        }

        public bool RequestLoad(string pathNdName)
        {
            rqst = Resources.LoadAsync(pathNdName);
            return (rqst == null) ? false : true;
        }

    }

    public static class StuffSaver
    {

        public const string fileType = ".bytes";

        public const string jsonFileType = ".json";

        public static void SaveStreamingAsset<G>(string fileName, G dta)
        {
            if (dta == null) return;
            Directory.CreateDirectory(Application.streamingAssetsPath);
            string dataAsJson = JsonUtility.ToJson(dta);
            string filePath = Path.Combine(Application.streamingAssetsPath, fileName + jsonFileType);
            File.WriteAllText(filePath, dataAsJson);

        }

        public static void SaveStreamingAsset<G>(string folderName, string fileName, G dta)
        {
            if (dta == null) return;
            var path = Path.Combine(Application.streamingAssetsPath, folderName);
            Directory.CreateDirectory(path);
            string filePath = Path.Combine(path, fileName + jsonFileType);
            File.WriteAllText(filePath, JsonUtility.ToJson(dta));

        }

        public static void Save<G>(string fullPath, string fileName, G dta)
        {
            if (dta == null) return;
            Directory.CreateDirectory(fullPath);
            BinaryFormatter bf = new BinaryFormatter();

            FileStream file = File.Create(fullPath + "/" + fileName + fileType);
            bf.Serialize(file, dta);
            file.Close();
        }
 
        public static void Save_ToAssets_ByRelativePath(string Path, string filename, string data) =>
            Save_ByFullPath(Application.dataPath + Path.RemoveAssetsPart().AddPreSlashIfNotEmpty().AddPostSlashIfNone(), filename, data);
        
        public static void Save_ByFullPath(string fullDirectoryPath, string filename, string data)
        {
            string fullPath = fullDirectoryPath;
            Directory.CreateDirectory(fullPath);
            BinaryFormatter bf = new BinaryFormatter();
            string full = fullPath + filename + fileType;
            FileStream file = File.Create(full);
#if PEGI
            if (Application.isPlaying == false)
                ("Saved To " + full).showNotification();
#endif
            bf.Serialize(file, data);
            file.Close();
        }
        
        public static void SaveToResources(string ResFolderPath, string InsideResPath, string filename, string data) =>
        Save_ToAssets_ByRelativePath(ResFolderPath + "/Resources" + InsideResPath.AddPreSlashIfNotEmpty(), filename, data);

       public static void SaveToPersistantPath(string subPath, string filename, string data) {
            var filePath = Path.Combine (Application.persistentDataPath, subPath);
            Directory.CreateDirectory(filePath);
            filePath = Path.Combine(filePath, "{0}{1}".F(filename, jsonFileType));
            File.WriteAllText(filePath, data);
        }
      
}

}