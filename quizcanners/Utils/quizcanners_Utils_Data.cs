using System;
using System.Collections.Generic;
using System.IO;
using QuizCanners.Inspect;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.Serialization.Formatters.Binary;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCanners.Utils
{

    public static class QcFile
    {
        private const string TEXT_FILE_TYPE = ".txt";
        private const string BYTES_FILE_TYPE = ".bytes";
        private const bool DEFAULT_IS_BINARY = false;

        public enum LocationEnum { PersistantPath, Resources, Assets }

        public class RelativeLocation 
        {
            public string FolderName;
            public string FileName;
            public bool AsBytes;

            internal string Extension => AsBytes ? BYTES_FILE_TYPE : TEXT_FILE_TYPE;

            public RelativeLocation (string folderName, string fileName, bool asBytes = DEFAULT_IS_BINARY) 
            {
                FolderName = folderName;
                FileName = fileName;
                AsBytes = asBytes;
            }
        }

        private static readonly BinaryFormatter Formatter = new BinaryFormatter();

        public static readonly string OutsideOfAssetsFolder =
            Application.dataPath.Substring(0, Application.dataPath.Length - 6);

        public static class Explorer
        {
            public static List<string> GetFileNamesFromPersistentFolder(string subPath)
                => GetFileNamesFrom(Path.Combine(Application.persistentDataPath, subPath));

            private static List<string> GetFileNamesFrom(string fullPath)
            {

                var lst = new List<string>(Directory.GetFiles(fullPath));

                for (var i = 0; i < lst.Count; i++)
                {

                    var txt = lst[i].Replace(@"\", @"/");
                    txt = txt.Substring(txt.LastIndexOf("/", StringComparison.Ordinal) + 1);

                    int extension = txt.LastIndexOf(".", StringComparison.Ordinal);

                    if (extension>0)
                        txt = txt.Substring(0, extension);
                    lst[i] = txt;
                }

                return lst;
            }

            public static List<string> GetFolderNamesFromPersistentFolder(string subPath)
                => GetFolderNamesFrom(Path.Combine(Application.persistentDataPath, subPath));
            
            private static List<string> GetFolderNamesFrom(string fullPath)
            {
                if (File.Exists(fullPath) == false)
                    return new List<string>();

                var lst = new List<string>(Directory.GetDirectories(fullPath));

                for (var i = 0; i < lst.Count; i++)
                {
                    var txt = lst[i].Replace(@"\", @"/");
                    txt = txt.Substring(txt.LastIndexOf("/", StringComparison.Ordinal) + 1);
                    lst[i] = txt;
                }

                return lst;
            }
            
            public static void OpenPersistentFolder() => OpenPath(Application.persistentDataPath);

            public static void OpenPersistentFolder(string folder) =>
                OpenPath(Path.Combine(Application.persistentDataPath, folder));

            public static void OpenUrl(string url) => Application.OpenURL(url);
            
            public static void OpenPath(string path)
            {
#if UNITY_EDITOR
                EditorUtility.RevealInFinder(path);
#else
                 System.Diagnostics.Process.Start(path.TrimEnd(new[] { '\\', '/' }));
#endif
            }

            public static string TryGetFullPathToAsset(Object o)
            {
#if UNITY_EDITOR
                var dest = AssetDatabase.GetAssetPath(o).Replace("Assets", "");

                return Application.dataPath + dest;
#else
                return null;
#endif
            }
        }

        public static class Delete
        {
            public class InPersistentFolder 
            {
                public static bool FileTry(string subPath, string fileName, bool asBytes = DEFAULT_IS_BINARY)
                {
                    var location = new RelativeLocation(folderName: subPath, fileName: fileName, asBytes: asBytes);
                    return FileTry(location);
                }

                public static bool FileTry(RelativeLocation location)
                  => DeleteInternal(Path.Combine(Application.persistentDataPath, location.FolderName, location.FileName + location.Extension));

                public static void DeleteDirectory(string subPath, bool deleteSubdirectories = true, bool showNotificationInGameView = true)
                {
                    var path = Path.Combine(Application.persistentDataPath, subPath);

                    if (Directory.Exists(path))
                    {
                        Directory.Delete(Path.Combine(Application.persistentDataPath, subPath), deleteSubdirectories);
                        if (showNotificationInGameView && Application.isEditor)
                        {
                            pegi.GameView.ShowNotification("{0} removed".F(path));
                        }
                    }
                    else if (showNotificationInGameView && Application.isEditor)
                    {
                        pegi.GameView.ShowNotification("{0} not found".F(path));
                    }
                }

            }

            public static void FromResources(string assetFolder, string insideAssetFolderAndName, bool asBytes = DEFAULT_IS_BINARY)
            {
#if UNITY_EDITOR
                try
                {
                    var path = Path.Combine("Assets",
                                   Path.Combine(assetFolder, Path.Combine("Resources", insideAssetFolderAndName))) +
                               (asBytes ? BYTES_FILE_TYPE : TEXT_FILE_TYPE);
                    AssetDatabase.DeleteAsset(path);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
#endif
            }


            private static bool DeleteInternal(string fullPath, bool showNotificationIn3DView = false)
            {
                if (System.IO.File.Exists(fullPath)) {
                    
                    if (showNotificationIn3DView && Application.isEditor)
                        pegi.GameView.ShowNotification("Deleting " + fullPath);
                    
                    System.IO.File.Delete(fullPath);
                    return true;
                }

                if (showNotificationIn3DView && Application.isEditor)
                    pegi.GameView.ShowNotification("File not found: " + fullPath);

                return false;
            }
            
        }

        public static class Load
        {
            public static string FromResources(string insideResourceFolder, string name, bool asBytes = DEFAULT_IS_BINARY)
            {
                var resourcePathAndName = insideResourceFolder + (insideResourceFolder.Length > 0 ? "/" : "") + name;

                var asset = Resources.Load(resourcePathAndName) as TextAsset;

                try
                {
                    if (!asset)
                        return null;

                    if (asBytes)
                    {
                        Stream stream = new MemoryStream(asset.bytes);
                        return Formatter.Deserialize(stream) as string;
                    }

                    return asset.text;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                finally
                {
                    Resources.UnloadAsset(asset);
                }

                return null;
            }
            public static string TryLoadAsTextAsset(Object o, bool asBytes = DEFAULT_IS_BINARY)
            {
                var asset = o as TextAsset;
                if (asset)
                {
                    if (asBytes)
                    {
                        Stream stream = new MemoryStream(asset.bytes);
                        return Formatter.Deserialize(stream) as string;
                    }
                    return asset.text;
                }

            #if UNITY_EDITOR

                var path = AssetDatabase.GetAssetPath(o);
                
                if (path.IsNullOrEmpty()) return null;
                
                var subpath = Application.dataPath;
                path = subpath.Substring(0, subpath.Length - 6) + path;

                return InternalAsString(path, asBytes);

            #else
                return null;
            #endif
            }

            private static string InternalAsString(string fullPath, bool asBytes)
            {
                if (!File.Exists(fullPath))
                    return null;

                string data = null;

                try
                {
                    if (asBytes)
                    {
                        var file = File.Open(fullPath, FileMode.Open);
                        using (file)
                        {
                            data = (string)Formatter.Deserialize(file);
                        }
                    }
                    else
                    {
                        StreamReader reader = new StreamReader(fullPath);
                        data = reader.ReadToEnd();
                        reader.Close();
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log(fullPath + " not loaded " + ex);
                }

                return data;
            }

            public class FromPersistentPath 
            {
               /* public static bool BinaryTry<T> (RelativeLocation location, out T result) 
                {
                    var fullPath = FullPath(location);

                    result = default(T);

                    if (!File.Exists(fullPath))
                        return false;

                    var file = File.Open(fullPath, FileMode.Open);
                    using (file)
                    {
                        result = (T)Formatter.Deserialize(file);
                        return true;
                    }
                }
               */
                public static bool StringTry(string subPath, string filename, out string result, bool asBytes = DEFAULT_IS_BINARY)
                {
                    var location = new RelativeLocation(folderName: subPath, fileName: filename, asBytes: asBytes);
                    return StringTry(location, out result);
                }

                public static bool StringTry(RelativeLocation location, out string result)
                {
                    var fullPath = FullPath(location);

                    result = null;

                    if (!File.Exists(fullPath))
                        return false;

                    if (location.AsBytes)
                    {
                        var file = File.Open(fullPath, FileMode.Open);
                        using (file)
                        {
                            result = (string)Formatter.Deserialize(file);
                            return true;
                        }
                    }

                    result = File.ReadAllText(fullPath);
                    return true;
                }

                public static string String(RelativeLocation location)
                {
                    StringTry(location, out string result);
                    return result;
                }

                public static string String(string subPath, string filename, bool asBytes = DEFAULT_IS_BINARY)
                {
                    StringTry(subPath: subPath, filename: filename, out string result, asBytes: asBytes);
                    return result;
                }

                public static bool TryOverrideFromJson<T>(string subPath, string filename, ref T result, bool asBytes = DEFAULT_IS_BINARY) 
                {
                    var location = new RelativeLocation(folderName: subPath, fileName: filename, asBytes: asBytes);
                    return TryOverrideFromJson(location, ref result);
                }

                public static bool TryOverrideFromJson<T>(RelativeLocation location, ref T result)
                {
                    if (StringTry(location, out string data))
                    {
                        try
                        {
                            if (typeof(T).IsValueType)
                            {
                                object boxedStruct = result;
                                JsonUtility.FromJsonOverwrite(data, boxedStruct);
                                result = (T)boxedStruct;
                            }
                            else
                            {
                                JsonUtility.FromJsonOverwrite(data, result);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                        return true;
                    }

                    result = default(T);
                    return false;
                }

                public static bool JsonTry<T>(RelativeLocation location, out T result)
                {
                    if (StringTry(location, out string data))
                    {
                        result = JsonUtility.FromJson<T>(data);
                        return true;
                    }

                    result = default(T);
                    return false;
                }

                internal static string FullPath(RelativeLocation location) // string subPath, string fileName, string extension)
                    => Path.Combine(Application.persistentDataPath, location.FolderName, location.FileName + location.Extension);
            }
        }

        public static class Save 
        {
            #region Unity Assets

            public static void Asset(Object obj, string folder, string extension, bool refreshAfter = false)
            {
                #if UNITY_EDITOR
                    var fullPath = Path.Combine(Application.dataPath, folder);

                    Directory.CreateDirectory(fullPath);

                    AssetDatabase.CreateAsset(obj, QcUnity.SetUniqueObjectName(obj, folder, extension));

                    if (refreshAfter)
                        AssetDatabase.Refresh();
                #endif
            }
            
            public static void TextureToAssetsFolder(string subFolder, string fileName, Texture2D texture) =>
                File.WriteAllBytes(CreateDirectoryPath(Application.dataPath, subFolder, fileName, ".png"), texture.EncodeToPNG());

            public static void TextureOutsideAssetsFolder(string subFolder, string fileName, string extension, Texture2D texture) =>
                File.WriteAllBytes(CreateDirectoryPath(OutsideOfAssetsFolder, subFolder, fileName, extension), texture.EncodeToPNG());

            #endregion

            #region Write All Text

            public static void ToResources(string resFolderPath, string insideResPath, string filename, string data, bool asBytes = DEFAULT_IS_BINARY) =>
               ToAssets(Path.Combine(resFolderPath, "Resources", insideResPath), filename, data, asBytes: asBytes);

            public static void ToAssets(string path, string filename, string data, bool asBytes = DEFAULT_IS_BINARY) =>
                ByFullPath(Path.Combine(Application.dataPath, path), filename, data, asBytes: asBytes);

            private static void ByFullPath(string fullDirectoryPath, string fileName, string data, bool asBytes)
            {
                string extension = asBytes ? BYTES_FILE_TYPE : TEXT_FILE_TYPE;

                string fullPath = CreateDirectoryPath(fullDirectoryPath, fileName, extension);

                if (asBytes)
                {
                    using var file = File.Create(fullPath);
                    Formatter.Serialize(file, data);
                }
                else
                {
                    File.WriteAllText(fullPath, data);
                }

            }

            public class ToPersistentPath 
            {
                /*
                public static bool BinaryTry<T>(T objectToSave, RelativeLocation location)
                {
                    var path = CreateDirectoryPath(Application.persistentDataPath, location);

                    try
                    {
                        using var file = File.Create(path);
                        Formatter.Serialize(file, objectToSave);
                        return true;
                    } catch (Exception ex) 
                    {
                        Debug.LogException(ex);
                        return false;
                    }
                }
                */
                public static bool JsonTry(object objectToSerialize, string folderName, string filename, bool asBytes = DEFAULT_IS_BINARY)
                {
                    var location = new RelativeLocation(folderName: folderName, fileName: filename, asBytes: asBytes);

                    return JsonTry(objectToSerialize: objectToSerialize, location: location);
                }

                public static bool JsonTry(object objectToSerialize, RelativeLocation location)
                {
                    try
                    {
                        var data = JsonUtility.ToJson(objectToSerialize);
                        String(location, data);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        return false;
                    }
                }

                public static void String(string subPath, string fileName, string data, bool asBytes = DEFAULT_IS_BINARY)
                {
                    RelativeLocation location = new RelativeLocation(folderName: subPath, fileName: fileName, asBytes: asBytes);
                    String(location, data: data);
                }

                public static void String(RelativeLocation location, string data)
                {
                    var path = CreateDirectoryPath(Application.persistentDataPath, location);

                    if (location.AsBytes)
                    {
                        using var file = File.Create(path);
                        Formatter.Serialize(file, data);
                    }
                    else
                    {
                        File.WriteAllText(path, data);
                    }
                }
            }

            #endregion

            #region Create Directory

            private static string CreateDirectoryPath(string path1, RelativeLocation location)
            {
                var fullDirectoryPath = Path.Combine(path1, location.FolderName);
                return CreateDirectoryPath(fullDirectoryPath, location.FileName, location.Extension);
            }

            private static string CreateDirectoryPath(string path1, string path2, string filename, string extension)
            {
                var fullDirectoryPath = Path.Combine(path1, path2);
                return CreateDirectoryPath(fullDirectoryPath, filename, extension);
            }

            private static string CreateDirectoryPath(string fullDirectoryPath, string fileName, string extension)
            {
                Directory.CreateDirectory(fullDirectoryPath);
                return Path.Combine(fullDirectoryPath, fileName + extension);
            }
            #endregion
        }


        public class Location : RelativeLocation
        {
            public LocationEnum LocationEnum;

            public bool TrySaveJson(object objectToSerialize) 
            {
                switch (LocationEnum) 
                {
                    case LocationEnum.PersistantPath: return Save.ToPersistentPath.JsonTry(objectToSerialize, this);
                    default: Debug.LogError(QcLog.CaseNotImplemented(LocationEnum, context: "Try Save Json")); return false;
                }
            }

            public bool TryOverrideFromJson(ref object objectToSerialize)
            {
                switch (LocationEnum)
                {
                    case LocationEnum.PersistantPath: return Load.FromPersistentPath.TryOverrideFromJson(this, ref objectToSerialize);
                    default: Debug.LogError(QcLog.CaseNotImplemented(LocationEnum, context: "Try Load Json")); return false;
                }
            }

            public Location(LocationEnum location, string folderName, string fileName, bool asBytes = DEFAULT_IS_BINARY)
                : base(folderName: folderName, fileName: fileName, asBytes: asBytes)
            {
                LocationEnum = location;
            }
        }

    }
}