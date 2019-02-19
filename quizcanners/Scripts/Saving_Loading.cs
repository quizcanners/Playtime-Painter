using UnityEngine;
using PlayerAndEditorGUI;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCannersUtilities
{
    public static class StuffExplorer
    {
        public static void OpenPersistentFolder() => OpenPath(Application.persistentDataPath);

        public static void OpenPersistentFolder(string folder) =>
            OpenPath(Path.Combine(Application.persistentDataPath, folder));

        public static void OpenPath(string path)
        {
#if UNITY_EDITOR
            EditorUtility.RevealInFinder(path);
#else
            Process.Start(path.TrimEnd(new[]{'\\', '/'}));
#endif
        }
    }

    public static class StuffDeleter
    {
        public static bool DeleteFile_PersistentFolder(string subPath, string fileName)
            => DeleteFile(Path.Combine(Application.persistentDataPath, subPath,
                Path.Combine(Application.persistentDataPath, subPath, "{0}{1}".F(fileName, StuffSaver.JsonFileType))));

        public static bool DeleteFile(string path)
        {
            if (File.Exists(path))
            {
#if PEGI && UNITY_EDITOR
                "Deleting {0}".F(path).showNotificationIn3D_Views();
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
        private static readonly BinaryFormatter Formatter = new BinaryFormatter();

        public static string Load(string fullPath)
        {
            if (!File.Exists(fullPath)) return null;

            string data = null;

            try
            {
                using (var file = File.Open(fullPath, FileMode.Open))
                    data = (string) Formatter.Deserialize(file);
            }
            catch (Exception ex)
            {
                Debug.Log(fullPath + " not loaded " + ex);
            }

            return data;
        }

        public static string LoadStoryFromResource(string resourceFolderLocation, string insideResourceFolder,
            string name)
        {
#if UNITY_EDITOR

            var resourceName = Path.Combine(insideResourceFolder, name);
            var path = Path.Combine(Application.dataPath, resourceFolderLocation, "Resources",
                       resourceName + StuffSaver.FileType);

            if (!File.Exists(path)) return null;

            try
            {
                using (var file = File.Open(path, FileMode.Open))
                    return (string) (Formatter.Deserialize(file));
            }
            catch (Exception ex)
            {
                Debug.Log(path + "is Busted !" + ex);
            }


            return null;

#else
        return LoadStoryFromResource( insideResourceFolder,  name);
#endif
        }

        public static string LoadStoryFromResource(string insideResourceFolder, string name)
        {
            var resourceName = insideResourceFolder + (insideResourceFolder.Length > 0 ? "/" : "") + name;

            var asset = Resources.Load(resourceName) as TextAsset;

            try
            {
                if (asset == null) return null;

                using (var ms = new MemoryStream(asset.bytes))
                {
                    return (string) Formatter.Deserialize(ms);
                }
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }
        }

        public static string LoadTextAsset(UnityEngine.Object o)
        {
#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(o);

            if (path.IsNullOrEmpty()) return null;

            var subpath = Application.dataPath;
            path = subpath.Substring(0, subpath.Length - 6) + path;

            return Load(path);


#else
            return null;
#endif
        }

        public static string LoadStoryFromAssets(string folder, string name)
        {
#if UNITY_EDITOR
            var path = Path.Combine(Application.dataPath, folder, name + StuffSaver.FileType);

            if (!File.Exists(path)) return null;

            try
            {
                using (var file = File.Open(path, FileMode.Open))
                    return (string) Formatter.Deserialize(file);
            }
            catch (Exception ex)
            {
                Debug.Log(path + "is Busted !" + ex);
            }

#endif

            return null;
        }

        public static string LoadFromPersistentPath(string subPath, string filename)
        {
            var filePath = Path.Combine(Application.persistentDataPath, subPath,
                "{0}{1}".F(filename, StuffSaver.JsonFileType));

            return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
        }

        public static List<string> ListFileNamesFromPersistentFolder(string subPath)
        {
            var lst = new List<string>(Directory.GetFiles(Path.Combine(Application.persistentDataPath, subPath)));

            for (var i = 0; i < lst.Count; i++)
            {
                var txt = lst[i].Replace(@"\", @"/");
                txt = txt.Substring(txt.LastIndexOf("/") + 1);
                txt = txt.Substring(0, txt.Length - StuffSaver.JsonFileType.Length);
                lst[i] = txt;
            }

            return lst;
        }

        public static bool LoadResource<T>(string pathNdName, ref T arrangement)
        {
#if UNITY_EDITOR
            var path = Application.dataPath + "/Resources/" + pathNdName + StuffSaver.FileType;

            if (File.Exists(path))
            {
                try
                {
                    using (var file = File.Open(path, FileMode.Open))
                        arrangement = (T) Formatter.Deserialize(file);
                }
                catch (Exception ex)
                {
                    Debug.Log(path + "is Busted !" + ex);
                    return false;
                }

                return true;
            }

            Debug.Log(path + " not found");
            return false;
#else
        {
            var asset = Resources.Load(pathNdName) as TextAsset;

        try {
                if (asset != null) {
                   
                    using (var ms = new MemoryStream(asset.bytes)) 
                        arrangement = (T)Formatter.Deserialize(ms);
                    
                    return true;
                }
                
                return false;
                
            } finally{
             Resources.UnloadAsset(asset);
            }
        }
#endif

           
        }

        public static bool LoadFrom<T>(string path, string name, ref T dta)
        {
            var fullPath = Path.Combine(path, name + StuffSaver.FileType);
            
            if (!File.Exists(fullPath)) return false;
            
            try
            {

                using (var file = File.Open(fullPath, FileMode.Open))
                    dta = (T) Formatter.Deserialize(file);
                

            }
            catch (Exception ex)
            {
                Debug.Log(path + " not loaded " + ex);

                return false;
            }

            return true;
            


        }

        public static bool LoadStreamingAssets<T>(string fileName, ref T dta)
        {
            var filePath = Application.streamingAssetsPath + "/" + fileName + ".json";

            if (!File.Exists(filePath)) return false;
            
            dta = JsonUtility.FromJson<T>(File.ReadAllText(filePath));
            return dta != null;
        }
    }


    public class StuffLoaderAssync<T>
    {
        ResourceRequest _rqst;

        public bool TryUnpackAsset(ref T arrangement)
        {
            if (!_rqst.isDone) return false;
            
            var asset = _rqst.asset as TextAsset;

            if (asset == null)
                return true;
                
            try
            {
                using (var ms = new MemoryStream(asset.bytes))
                    arrangement = (T) ((new BinaryFormatter()).Deserialize(ms));
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }

            return true;

        }

        public bool RequestLoad(string pathNdName)
        {
            _rqst = Resources.LoadAsync(pathNdName);
            return _rqst != null;
        }
    }

    public static class StuffSaver
    {
        private static readonly BinaryFormatter Formatter = new BinaryFormatter();

        
        public const string FileType = ".bytes";

        public const string JsonFileType = ".json";

        public static void SaveStreamingAsset<TG>(string fileName, TG dta)
        {
            if (dta == null) return;
            Directory.CreateDirectory(Application.streamingAssetsPath);
            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, fileName + JsonFileType), JsonUtility.ToJson(dta));
        }

        public static void SaveStreamingAsset<TG>(string folderName, string fileName, TG dta)
        {
            if (dta == null) return;
            var path = Path.Combine(Application.streamingAssetsPath, folderName);
            Directory.CreateDirectory(path);
            var filePath = Path.Combine(path, fileName + JsonFileType);
            File.WriteAllText(filePath, JsonUtility.ToJson(dta));
        }

        public static void Save<TG>(string fullPath, string fileName, TG dta)
        {
            if (dta == null) return;
            
            Directory.CreateDirectory(fullPath);

            using (var file = File.Create(Path.Combine(fullPath, fileName + FileType)))
                Formatter.Serialize(file, dta);
            
        }

        public static void Save_ToAssets_ByRelativePath(string path, string filename, string data) =>
            Save_ByFullPath(Path.Combine(Application.dataPath, path.RemoveAssetsPart()),
                filename, data);

        public static void Save_ByFullPath(string fullDirectoryPath, string filename, string data)
        {
            var fullPath = fullDirectoryPath;
            Directory.CreateDirectory(fullPath);

            var full = Path.Combine(fullPath, filename + FileType);
            using (var file = File.Create(full))
            {
#if PEGI
                if (!Application.isPlaying)
                    ("Saved To " + full).showNotificationIn3D_Views();
#endif
                Formatter.Serialize(file, data);
            }

        }

        public static void SaveToResources(string resFolderPath, string insideResPath, string filename, string data) =>
            Save_ToAssets_ByRelativePath(Path.Combine(resFolderPath, "Resources", insideResPath), filename, data);

        public static void SaveToPersistentPath(string subPath, string filename, string data)
        {
            var filePath = Path.Combine(Application.persistentDataPath, subPath);
            Directory.CreateDirectory(filePath);
            filePath = Path.Combine(filePath, "{0}{1}".F(filename, JsonFileType));
            File.WriteAllText(filePath, data);
        }
    }
}