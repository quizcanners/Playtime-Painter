using UnityEngine;
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

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    public class QcFile
    {

        public static string OutsideOfAssetsFolder =
            Application.dataPath.Substring(0, Application.dataPath.Length - 6);

        public const string bytesFileType = ".bytes";

        public const string JsonFileType = ".json";

        public class ExplorerUtils
        {
            public static List<string> GetFileNamesFromPersistentFolder(string subPath)
                => GetFileNamesFrom(Path.Combine(Application.persistentDataPath, subPath));

            public static List<string> GetFileNamesFrom(string fullPath)
            {

                var lst = new List<string>(Directory.GetFiles(fullPath));

                for (var i = 0; i < lst.Count; i++)
                {

                    var txt = lst[i].Replace(@"\", @"/");
                    txt = txt.Substring(txt.LastIndexOf("/") + 1);
                    txt = txt.Substring(0, txt.Length - JsonFileType.Length);
                    lst[i] = txt;
                }

                return lst;
            }

            public static List<string> GetFolderNamesFromPersistentFolder(string subPath)
                => GetFolderNamesFrom(Path.Combine(Application.persistentDataPath, subPath));


            public static List<string> GetFolderNamesFrom(string fullPath)
            {

                var lst = new List<string>(Directory.GetDirectories(fullPath));

                for (var i = 0; i < lst.Count; i++)
                {
                    var txt = lst[i].Replace(@"\", @"/");
                    txt = txt.Substring(txt.LastIndexOf("/") + 1);
                    lst[i] = txt;
                }

                return lst;
            }


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

        public class DeleteUtils
        {

            public static void DeleteResource_Bytes(string assetFolder, string insideAssetFolderAndName)
            {
#if UNITY_EDITOR
                try
                {
                    var path = Path.Combine("Assets",
                                   Path.Combine(assetFolder, Path.Combine("Resources", insideAssetFolderAndName))) +
                               bytesFileType;
                    AssetDatabase.DeleteAsset(path);
                }
                catch (Exception e)
                {
                    Debug.Log("Oh No " + e);
                }
#endif
            }

            public static bool Delete_PersistentFolder_Json(string subPath, string fileName)
                => DeleteFile(Path.Combine(Application.persistentDataPath, subPath,
                    Path.Combine(Application.persistentDataPath, subPath, fileName + JsonFileType)));

            public static bool DeleteFile(string fullPath)
            {
                if (File.Exists(fullPath)) {

                    #if UNITY_EDITOR
                    Debug.Log("Deleting" + fullPath);
                    #endif

                    File.Delete(fullPath);
                    return true;
                }
                else
                    return false;
            }
        }

        public class LoadUtils
        {
            private static readonly BinaryFormatter Formatter = new BinaryFormatter();

            public static string LoadBytesFromResource(string resourceFolderLocation, string insideResourceFolder,
                string name)
            {

#if UNITY_EDITOR

                var resourceName = Path.Combine(insideResourceFolder, name);
                var path = Path.Combine(Application.dataPath,
                    Path.Combine(resourceFolderLocation,
                        Path.Combine("Resources", resourceName + bytesFileType)));

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
        return LoadBytesFromResource( insideResourceFolder,  name);
#endif
            }

            public static string LoadBytesFromResource(string insideResourceFolder, string name)
            {
                var resourcePathAndName = insideResourceFolder + (insideResourceFolder.Length > 0 ? "/" : "") + name;

                var asset = Resources.Load(resourcePathAndName) as TextAsset;

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

            public static string LoadBytesFromAssets(string folder, string name)
            {
#if UNITY_EDITOR
                var path = Path.Combine(Application.dataPath, folder, name + bytesFileType);

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

            public static string TryLoadAsTextAsset(UnityEngine.Object o)
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

            #region Json

            public static bool LoadJsonFromUnityObjectOverride<T, G>(T target, G jsonFile) where G : UnityEngine.Object
            {

#if UNITY_EDITOR
                var filePath = AssetDatabase.GetAssetPath(jsonFile);

                if (!filePath.IsNullOrEmpty())
                {

                    JsonUtility.FromJsonOverwrite(File.ReadAllText(filePath), target);
                    return true;
                }
#endif
                return false;
            }

            public static bool LoadJsonFromPersistentPathOverride<T>(T target, string filename, params string[] folders)
            {

                var filePath = JsonPersistantPath(Path.Combine(folders), filename);

                if (File.Exists(filePath))
                {
                    try
                    {
                        JsonUtility.FromJsonOverwrite(File.ReadAllText(filePath), target);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Could load Json: " + ex.ToString());
                        return false;
                    }

                    return true;
                }

                return false;
            }

            public static T LoadJsonFromPersistentPath<T>(string filename, params string[] subFolders)
            {

                var filePath = JsonPersistantPath(subFolders.Length > 0 ? Path.Combine(subFolders) : "", filename);
                return File.Exists(filePath) ? JsonUtility.FromJson<T>(File.ReadAllText(filePath)) : default(T);
            }

            public static string LoadJsonFromPersistentPath(string subPath, string filename)
            {

                var filePath = JsonPersistantPath(subPath, filename);
                return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
            }

            private static string JsonPersistantPath(string subPath, string fileName)
                => Path.Combine(Application.persistentDataPath, subPath, fileName + JsonFileType);

            public static bool TryLoadJsonFromStreamingAssets<T>(string fileName, ref T dta)
            {
                var filePath = Path.Combine(Application.streamingAssetsPath, fileName + JsonFileType);

                if (!File.Exists(filePath)) return false;

                dta = JsonUtility.FromJson<T>(File.ReadAllText(filePath));
                return dta != null;
            }

            #endregion

            public static bool LoadBytesFrom<T>(string path, string name, ref T dta)
            {
                var fullPath = Path.Combine(path, name + bytesFileType);

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

            private static string Load(string fullPath)
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

        public class SaveUtils {
            
            private static readonly BinaryFormatter Formatter = new BinaryFormatter();
            
            #region Assets
            public static void SaveAsset(UnityEngine.Object obj, string folder, string extension, bool refreshAfter = false)
            {
#if UNITY_EDITOR
                var fullPath = Path.Combine(Application.dataPath, folder);
                Directory.CreateDirectory(fullPath);

                AssetDatabase.CreateAsset(obj, QcUnity.SetUniqueObjectName(obj, folder, extension));

                if (refreshAfter)
                    AssetDatabase.Refresh();

#endif
            }


            #endregion

            #region Bytes

            public static void SaveBytesToAssetsByRelativePath(string path, string filename, string data) =>
                SaveBytesByFullPath(Path.Combine(Application.dataPath, path), filename, data);

            public static void SaveBytesByFullPath(string fullDirectoryPath, string filename, string data)
            {
                using (var file = File.Create(CreateDirectoryPathBytes(fullDirectoryPath, filename)))
                    Formatter.Serialize(file, data);


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

            public static void SaveJsonToPersistantPath<T>(T obj, string fileName, params string[] subFolders) =>
              File.WriteAllText(CreateDirectoryPathJson(Application.persistentDataPath, Path.Combine(subFolders), fileName), JsonUtility.ToJson(obj));

            public static void SaveJsonToPersistentPath(string subPath, string filename, string data) =>
                File.WriteAllText(CreateDirectoryPathJson(Application.persistentDataPath, subPath, filename), data);

            private static string CreateDirectoryPathJson(string fullDirectoryPath, string filename)
                => CreateDirectoryPath(fullDirectoryPath, filename, JsonFileType);

            private static string CreateDirectoryPathJson(string path1, string path2, string filename)
                => CreateDirectoryPath(path1, path2, filename, JsonFileType);


            #endregion

            public static void SaveTextureToAssetsFolder(string subFolder, string fileName, Texture2D texture) =>
                File.WriteAllBytes(CreateDirectoryPath(Application.dataPath, subFolder, fileName, ".png"), texture.EncodeToPNG());

            public static void SaveTextureOutsideAssetsFolder(string subFolder, string fileName, string extension, Texture2D texture) =>
                File.WriteAllBytes(CreateDirectoryPath(OutsideOfAssetsFolder, subFolder, fileName, extension), texture.EncodeToPNG());

            public static void SaveToAssetsFolder(string subFolder, string fileName, string extension, byte[] data) =>
                File.WriteAllBytes(CreateDirectoryPath(Application.dataPath, subFolder, fileName, extension), data);

            public static string CreateDirectoryPath(string path1, string path2, string filename, string extension)
            {
                var fullDirectoryPath = Path.Combine(path1, path2);
                return CreateDirectoryPath(fullDirectoryPath, filename, extension);
            }

            public static string CreateDirectoryPath(string fullDirectoryPath, string filename, string extension)
            {
                Directory.CreateDirectory(fullDirectoryPath);
                return Path.Combine(fullDirectoryPath, filename + extension);
            }

        }

    }





}