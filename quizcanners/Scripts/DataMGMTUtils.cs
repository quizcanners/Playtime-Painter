using UnityEngine;
using PlayerAndEditorGUI;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#else 
using System.Diagnostics;
#endif

namespace QuizCannersUtilities
{
    public static class FileExplorerUtils
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

    public static class FileDeleteUtils
    {

        public static void DeleteResource(string assetFolder, string insideAssetFolderAndName)
        {
#if UNITY_EDITOR
            try
            {
                var path = Path.Combine("Assets", Path.Combine(assetFolder, Path.Combine("Resources", insideAssetFolderAndName))) + FileSaveUtils.bytesFileType;
                AssetDatabase.DeleteAsset(path);
            }
            catch (Exception e)
            {
                Debug.Log("Oh No " + e);
            }
#endif
        }


        public static bool DeleteFile_PersistentFolder(string subPath, string fileName)
            => DeleteFile(Path.Combine(Application.persistentDataPath, subPath, Path.Combine(Application.persistentDataPath, subPath, "{0}{1}".F(fileName, FileSaveUtils.JsonFileType))));

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

    public static class FileLoadUtils
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
            var path = Path.Combine(Application.dataPath, Path.Combine(resourceFolderLocation, Path.Combine("Resources",  resourceName + FileSaveUtils.bytesFileType)));

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
            var path = Path.Combine(Application.dataPath, folder, name + FileSaveUtils.bytesFileType);

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
                "{0}{1}".F(filename, FileSaveUtils.JsonFileType));

            return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
        }

        public static List<string> ListFileNamesFromPersistentFolder(string subPath)
        {
            var lst = new List<string>(Directory.GetFiles(Path.Combine(Application.persistentDataPath, subPath)));

            for (var i = 0; i < lst.Count; i++)
            {
                var txt = lst[i].Replace(@"\", @"/");
                txt = txt.Substring(txt.LastIndexOf("/") + 1);
                txt = txt.Substring(0, txt.Length - FileSaveUtils.JsonFileType.Length);
                lst[i] = txt;
            }

            return lst;
        }

        public static bool LoadResource<T>(string pathNdName, ref T arrangement)
        {
#if UNITY_EDITOR
            var path = Application.dataPath + "/Resources/" + pathNdName + FileSaveUtils.bytesFileType;

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
            var fullPath = Path.Combine(path, name + FileSaveUtils.bytesFileType);
            
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
            var filePath = Path.Combine(Application.streamingAssetsPath, fileName + ".json");

            if (!File.Exists(filePath)) return false;
            
            dta = JsonUtility.FromJson<T>(File.ReadAllText(filePath));
            return dta != null;
        }
    }
    
    public class ResourceLoaderAssync<T>
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

    public static class FileSaveUtils
    {
   

        private static readonly BinaryFormatter Formatter = new BinaryFormatter();
        
        public const string bytesFileType = ".bytes";

        public const string JsonFileType = ".json";


        #region Assets
        public static void SaveAsset(this UnityEngine.Object obj, string folder, string extension, bool refreshAfter = false)
        {
            #if UNITY_EDITOR
            var fullPath = Path.Combine(Application.dataPath, folder);
            Directory.CreateDirectory(fullPath);

            AssetDatabase.CreateAsset(obj, obj.SetUniqueObjectName(folder, extension));

            if (refreshAfter)
                AssetDatabase.Refresh();

            #endif
        }


        #endregion

        #region Bytes

        public static void SaveBytesToAssetsByRelativePath(string path, string filename, string data) =>
            SaveBytesByFullPath(Path.Combine(Application.dataPath, path), filename, data);

        public static void SaveBytesByFullPath(string fullDirectoryPath, string filename, string data){

            var full = CreateDirectoryPathBytes(fullDirectoryPath, filename);
            using (var file = File.Create(full))
            {
#if PEGI
                if (!Application.isPlaying)
                    ("Saved To " + full).showNotificationIn3D_Views();
#endif
                Formatter.Serialize(file, data);
            }

        }

        public static void SaveBytesToResources(string resFolderPath, string insideResPath, string filename, string data) =>
            SaveBytesToAssetsByRelativePath(Path.Combine(resFolderPath, "Resources", insideResPath), filename, data);

        private static string CreateDirectoryPathBytes(string fullDirectoryPath, string filename) =>
            CreateDirectoryPath(fullDirectoryPath, filename, bytesFileType);

        #endregion

        #region Json

        public static void SaveJsonToStreamingAsset<TG>(string fileName, TG dta)
        {
            if (dta == null) return;
            File.WriteAllText(CreateDirectoryPathJson(Application.streamingAssetsPath, fileName), JsonUtility.ToJson(dta));
        }

        public static void SaveJsonToStreamingAsset<TG>(string folderName, string fileName, TG dta)
        {
            if (dta == null) return;
            File.WriteAllText(CreateDirectoryPathJson(Application.streamingAssetsPath, folderName, fileName), JsonUtility.ToJson(dta));
        }
        
        public static void SaveJsonToPersistentPath(string subPath, string filename, string data) =>
            File.WriteAllText(CreateDirectoryPathJson(Application.persistentDataPath, subPath, filename), data);
        
        private static string CreateDirectoryPathJson(string fullDirectoryPath, string filename) => CreateDirectoryPath(fullDirectoryPath, filename, JsonFileType);

        private static string CreateDirectoryPathJson(string path1, string path2, string filename) => CreateDirectoryPath(path1, path2, filename, JsonFileType);


        #endregion

        public static void SaveTextureToAssetsFolder(string subFolder, string fileName, string extension, Texture2D texture) =>
            File.WriteAllBytes(CreateDirectoryPath(Application.dataPath, subFolder, fileName, extension), texture.EncodeToPNG());

        public static void SaveTextureOutsideAssetsFolder(string subFolder, string fileName, string extension, Texture2D texture) =>
            File.WriteAllBytes(CreateDirectoryPath(UnityUtils.OutsideOfAssetsFolder, subFolder, fileName, extension), texture.EncodeToPNG());

        ///Assets

        public static void SaveToAssetsFolder(string subFolder, string fileName , string extension, byte[] data) =>
            File.WriteAllBytes(CreateDirectoryPath(Application.dataPath, subFolder, fileName, extension), data);
        
        public static string CreateDirectoryPath(string path1, string path2, string filename, string extension)
        {
            var fullDirectoryPath = Path.Combine(path1, path2);
            return CreateDirectoryPath(fullDirectoryPath, filename, extension);
        }

        public static string CreateDirectoryPath(string fullDirectoryPath, string filename, string extension) {
            Directory.CreateDirectory(fullDirectoryPath);
            return Path.Combine(fullDirectoryPath, filename + extension);
        }

    }
}