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

    public static class ResourceLoader
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
            string path = Application.dataPath + resourceFolderLocation.AddPreSlashIfNotEmpty() + "/Resources" + resourceName + ResourceSaver.fileType;

            //Debug.Log("Trying to load " + path);
            if (File.Exists(path))
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    //Debug.Log("Loading "+path);

                    FileStream file = File.Open(path, FileMode.Open);
                    data = (string)bf.Deserialize(file);
                    //Debug.Log("Loaded: "+data);
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


        public static string LoadStory(UnityEngine.Object o)
        {
            string data = null;
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(o);
            if (path != null)
            {
                string subpath = Application.dataPath;
                path = subpath.Substring(0, subpath.Length - 6) + path;
                // Debug.Log("Loading "+path);
                return Load(path);
            }

#endif
            return data;
        }

        public static string LoadStoryFromAssets(string Folder, string name)
        {


            string data = null;

#if UNITY_EDITOR
            string path = Application.dataPath + Folder.AddPreSlashIfNotEmpty() + "/" + name + ResourceSaver.fileType;

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


    }


    public class ResourceLoader<T>
    {
        ResourceRequest rqst;

        public bool TryUnpackAsset(ref T Arrangement)
        {
            if (rqst.isDone)
            {
                TextAsset asset = rqst.asset as TextAsset;
                BinaryFormatter bf = new BinaryFormatter();
                using (MemoryStream ms = new MemoryStream(asset.bytes))
                {
                    Arrangement = (T)bf.Deserialize(ms);
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

        public bool LoadResource(string pathNdName, ref T Arrangement)
        {
#if UNITY_EDITOR
            string path = Application.dataPath + "/Resources/" + pathNdName + ResourceSaver.fileType;

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



        public bool LoadFrom(string path, string name, ref T _dta)
        {

            string fullPath = path + "/" + name + ResourceSaver.fileType;
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

        public bool LoadStreamingAssets(string fileName, ref T dta)
        {

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

    public static class ResourceSaver
    {

        public const string fileType = ".bytes";


        public static void SaveStreamingAsset<G>(string fileName, G dta)
        {
            if (dta == null) return;
            Directory.CreateDirectory(Application.streamingAssetsPath);
            string dataAsJson = JsonUtility.ToJson(dta);
            string filePath = Application.streamingAssetsPath + "/" + fileName + ".json";
            //Debug.Log("Saving to "+ filePath);
            File.WriteAllText(filePath, dataAsJson);

        }

        public static void SaveStreamingAsset<G>(string folderName, string fileName, G dta)
        {
            if (dta == null) return;
            Directory.CreateDirectory(Application.streamingAssetsPath + "/" + folderName);
            string dataAsJson = JsonUtility.ToJson(dta);
            string filePath = Application.streamingAssetsPath + "/" + folderName + "/" + fileName + ".json";
            //Debug.Log("Saving to " + filePath);
            File.WriteAllText(filePath, dataAsJson);

        }

        public static void Save<G>(string fullPath, string fileName, G dta)
        {
            if (dta == null) return;
            Directory.CreateDirectory(fullPath);
            BinaryFormatter bf = new BinaryFormatter();

            FileStream file = File.Create(fullPath + "/" + fileName + ResourceSaver.fileType);
            bf.Serialize(file, dta);
            file.Close();
        }

        public static void SaveToResources<G>(string path, string filename, G dta)
        {
            if (dta == null) return;
            string fullPath = Application.dataPath + "/Resources/" + path;
            Directory.CreateDirectory(fullPath);
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(fullPath + "/" + filename + ResourceSaver.fileType);
            bf.Serialize(file, dta);
            file.Close();
        }



        public static void SaveToResources<G>(string fileName, G dta)
        {
            if (dta == null) return;
            Directory.CreateDirectory(Application.dataPath + "/Resources");
            string fullPath = Application.dataPath + "/Resources/" + fileName;
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(fullPath + fileType);
            bf.Serialize(file, dta);
            file.Close();
        }




        public static void Save(string directory, string filename, string data)
        {
            string fullPath = directory;
            Directory.CreateDirectory(fullPath);
            BinaryFormatter bf = new BinaryFormatter();
            string full = fullPath + filename + fileType;
            FileStream file = File.Create(full);
#if !NO_PEGI
            if (Application.isPlaying == false)
                ("Saved To " + full).showNotification();
#endif
            bf.Serialize(file, data);
            file.Close();
        }



        public static void SaveToResources(string ResFolderPath, string InsideResPath, string filename, string data)
        {
            Save(Application.dataPath + ResFolderPath.AddPreSlashIfNotEmpty() + "/Resources" + InsideResPath.AddPreSlashIfNotEmpty() + "/", filename, data);
        }

    }

}