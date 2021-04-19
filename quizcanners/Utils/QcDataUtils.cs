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

    #pragma warning disable IDE0034 // Simplify 'default' expression
    #pragma warning disable IDE0019 // Use pattern matching
    #pragma warning disable IDE0018 // Inline variable declaration

    public static class QcFile
    {
        private static readonly BinaryFormatter Formatter = new BinaryFormatter();


        public static readonly string OutsideOfAssetsFolder =
            Application.dataPath.Substring(0, Application.dataPath.Length - 6);
        
        private const string textFileType = ".txt";

        private const string bytesFileType = ".bytes";

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

            public static void FromResources(string assetFolder, string insideAssetFolderAndName, bool asBytes)
            {
#if UNITY_EDITOR
                try
                {
                    var path = Path.Combine("Assets",
                                   Path.Combine(assetFolder, Path.Combine("Resources", insideAssetFolderAndName))) +
                               (asBytes ? bytesFileType : textFileType);
                    AssetDatabase.DeleteAsset(path);
                }
                catch (Exception e)
                {
                    Debug.Log("Oh No " + e);
                }
#endif
            }

            public static bool FromPersistentFolder(string subPath, string fileName, bool asBytes = false) =>
             FromPersistentFolder(subPath, fileName, extension: asBytes ? bytesFileType : textFileType);

            public static bool FromPersistentFolder(string subPath, string fileName, string extension)
                => File(Path.Combine(Application.persistentDataPath, subPath,
                    Path.Combine(Application.persistentDataPath, subPath, fileName + extension)));

            public static void DirectoryFromPersistentPath(string subPath, bool deleteSubdirectories = true, bool showNotificationInGameView = true)
            {
                var path = Path.Combine(Application.persistentDataPath, subPath);

                if (Directory.Exists(path))
                {
                    Directory.Delete(Path.Combine(Application.persistentDataPath, subPath), deleteSubdirectories);
                    if (showNotificationInGameView && Application.isEditor)
                    {
                        pegi.GameView.ShowNotification("{0} removed".F(path));
                    }
                } else if (showNotificationInGameView && Application.isEditor)
                {
                    pegi.GameView.ShowNotification("{0} not found".F(path));
                }
            }

            private static bool File(string fullPath, bool showNotificationIn3DView = false)
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
         
            public static string FromResources(string insideResourceFolder, string name, bool asBytes = false)
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
                    Debug.LogError(ex);
                }
                finally
                {
                    Resources.UnloadAsset(asset);
                }

                return null;
            }

            public static string TryLoadAsTextAsset(Object o, bool useBytes = false)
            {
                var asset = o as TextAsset;
                if (asset)
                {
                    if (useBytes)
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

                return InternalAsString(path, useBytes);

            #else
                return null;
            #endif
            }

            public static string FromPersistentPath(string subPath, string filename, bool asBytes = false)
            {
                string extension = asBytes ? bytesFileType : textFileType;

                var fullPath = PersistentPath(subPath: subPath, fileName: filename, extension: extension);

                if (!File.Exists(fullPath))
                    return null;

                if (asBytes)
                {
                    var file = File.Open(fullPath, FileMode.Open);
                    using (file)
                    {
                        return (string)Formatter.Deserialize(file);
                    }
                }

                return File.ReadAllText(fullPath);
            }

            private static string PersistentPath(string subPath, string fileName, string extension)
                => Path.Combine(Application.persistentDataPath, subPath, fileName + extension);
            
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
        }

        public static class Save {

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

            public static void ToResources(string resFolderPath, string insideResPath, string filename, string data, bool asBytes = false) =>
                ToAssets(Path.Combine(resFolderPath, "Resources", insideResPath), filename, data, asBytes: asBytes);

            public static void ToAssets(string path, string filename, string data, bool asBytes = false) =>
                ByFullPath(Path.Combine(Application.dataPath, path), filename, data, asBytes: asBytes);

            private static void ByFullPath(string fullDirectoryPath, string filename, string data, bool asBytes)
            {
                string extension = asBytes ? bytesFileType : textFileType;

                string fullPath = CreateDirectoryPath(fullDirectoryPath, filename, extension);

                if (asBytes)
                {
                    using (var file = File.Create(fullPath))
                        Formatter.Serialize(file, data);
                }
                else
                {
                    File.WriteAllText(fullPath, data);
                }

            }
            
            public static void ToPersistentPath(string subPath, string filename, string data, bool asBytes = false)
            {
                string extension = asBytes ? bytesFileType : textFileType;

                var path = CreateDirectoryPath(Application.persistentDataPath, subPath, filename, extension);

                if (asBytes)
                {
                    using (var file = File.Create(path))
                        Formatter.Serialize(file, data);
                }
                else
                {
                    File.WriteAllText(path, data);
                }

               
            }

            #endregion

            #region Create Directory
            private static string CreateDirectoryPath(string path1, string path2, string filename, string extension)
            {
                var fullDirectoryPath = Path.Combine(path1, path2);
                return CreateDirectoryPath(fullDirectoryPath, filename, extension);
            }

            private static string CreateDirectoryPath(string fullDirectoryPath, string filename, string extension)
            {
                Directory.CreateDirectory(fullDirectoryPath);
                return Path.Combine(fullDirectoryPath, filename + extension);
            }
            #endregion
        }

    }
}