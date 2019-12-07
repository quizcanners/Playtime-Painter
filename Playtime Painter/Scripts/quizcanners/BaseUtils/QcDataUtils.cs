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
        
        public class ExplorerUtils
        {
            public static List<string> GetFileNamesFromPersistentFolder(string subPath)
                => GetFileNamesFrom(Path.Combine(Application.persistentDataPath, subPath));

            private static List<string> GetFileNamesFrom(string fullPath)
            {

                var lst = new List<string>(Directory.GetFiles(fullPath));

                for (var i = 0; i < lst.Count; i++)
                {

                    var txt = lst[i].Replace(@"\", @"/");
                    txt = txt.Substring(txt.LastIndexOf("/") + 1);
                    txt = txt.Substring(0, txt.Length - bytesFileType.Length);
                    lst[i] = txt;
                }

                return lst;
            }

            public static List<string> GetFolderNamesFromPersistentFolder(string subPath)
                => GetFolderNamesFrom(Path.Combine(Application.persistentDataPath, subPath));
            
            private static List<string> GetFolderNamesFrom(string fullPath)
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

            public static bool DeleteFromPersistentFolder(string subPath, string fileName)
                => DeleteFile(Path.Combine(Application.persistentDataPath, subPath,
                    Path.Combine(Application.persistentDataPath, subPath, fileName + bytesFileType)));

            private static bool DeleteFile(string fullPath)
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

            public static string LoadStringFromResource(string resourceFolderLocation, string insideResourceFolder,
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

            public static string LoadStringFromResource(string insideResourceFolder, string name)
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

            public static string LoadFromAssets(string folder, string name)
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

            public static string LoadFromPersistentPath(string subPath, string filename)
            {

                var filePath = PersistantPath(subPath, filename);
                return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
            }

            private static string PersistantPath(string subPath, string fileName)
                => Path.Combine(Application.persistentDataPath, subPath, fileName + bytesFileType);

            private static string Load(string fullPath)
            {
                if (!File.Exists(fullPath))
                    return null;

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

        public class SaveUtils {
            
            private static readonly BinaryFormatter Formatter = new BinaryFormatter();

            #region Create Asset

            public static void SaveAsset(UnityEngine.Object obj, string folder, bool refreshAfter = false) =>
                SaveAsset(obj, folder, ".mat", refreshAfter);
            
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
            
            #region Write All Bytes

            public static void SaveTextureToAssetsFolder(string subFolder, string fileName, Texture2D texture) =>
                File.WriteAllBytes(CreateDirectoryPath(Application.dataPath, subFolder, fileName, ".png"), texture.EncodeToPNG());

            public static void SaveTextureOutsideAssetsFolder(string subFolder, string fileName, string extension, Texture2D texture) =>
                File.WriteAllBytes(CreateDirectoryPath(OutsideOfAssetsFolder, subFolder, fileName, extension), texture.EncodeToPNG());

            public static void SaveToAssetsFolder(string subFolder, string fileName, string extension, byte[] data) =>
                File.WriteAllBytes(CreateDirectoryPath(Application.dataPath, subFolder, fileName, extension), data);

            #endregion

            #region Formatter Serialize

            public static void SaveToResources(string resFolderPath, string insideResPath, string filename, string data) =>
                SaveToAssets(Path.Combine(resFolderPath, "Resources", insideResPath), filename, data);

            public static void SaveToAssets(string path, string filename, string data) =>
                SaveByFullPath(Path.Combine(Application.dataPath, path), filename, data);


            private static void SaveByFullPath(string fullDirectoryPath, string filename, string data)
            {
                using (var file = File.Create(FullPath(fullDirectoryPath, filename, bytesFileType)))
                    Formatter.Serialize(file, data);
            }

            #endregion

            #region Write All Text
            public static void SaveToPersistentPath(string subPath, string filename, string data) =>
                File.WriteAllText(CreateDirectoryPath(Application.persistentDataPath, subPath, filename, bytesFileType), data);

            #endregion

            #region Create Directory
            private static string CreateDirectoryPath(string path1, string path2, string filename, string extension)
            {
                var fullDirectoryPath = Path.Combine(path1, path2);
                return FullPath(fullDirectoryPath, filename, extension);
            }

            private static string FullPath(string fullDirectoryPath, string filename, string extension)
            {
                Directory.CreateDirectory(fullDirectoryPath);
                return Path.Combine(fullDirectoryPath, filename + extension);
            }
            #endregion
        }

    }





}