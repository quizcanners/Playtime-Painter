

using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;
using PlayerAndEditorGUI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SharedTools_Stuff
{

    public static class UnityHelperFunctions {

        public static Material MaterialWhaever (this Renderer rendy) {

            if (rendy == null) return null;

            return Application.isPlaying ? rendy.material : rendy.sharedMaterial;

        }

        static List<Vector3> randomNormalized = new List<Vector3>();
        static int currentNormalized = 0;

        public static Vector3 GetRandomPointWithin( this Vector3 v3) {

            const int maxRands = 512;

            if (randomNormalized.Count < maxRands)
            {
                var newOne = Vector3.one.OnSpherePosition();
                randomNormalized.Add(newOne);
                v3.Scale(newOne);
            }
            else
            {
                currentNormalized = (currentNormalized + 1) % maxRands;
                v3.Scale(randomNormalized[currentNormalized]);
            }

            return v3;
        } 
        
        public static string GetMeaningfulHierarchyName (this GameObject go, int maxLook, int maxLength)
        {
          
            string name = go.name;

#if PEGI
            Transform parent = go.transform.parent;

            while (parent != null && maxLook > 0 && maxLength > 0)
            {
                string n = parent.name;

                if (!n.SameAs("Text") && !n.SameAs("Button") && !n.SameAs("Image"))
                {
                    name += ">" + n;
                    maxLength--;
                }

                parent = parent.parent;
                maxLook--;
            }
#endif
            return name;
        }
        
        public static void RefreshAssetDatabase()
        {
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public static void SetActive(this List<GameObject> goList, bool to)
        {
            if (goList != null)
                foreach (var go in goList)
                    if (go) go.SetActive(to);
        }

        public static bool IsUnityObject (this Type t) => typeof(UnityEngine.Object).IsAssignableFrom(t);

        #region Transformation

        public static Color ToOpaque (this Color col) {
            if (col!= null)
                col.a = 1;
            return col;
        }

        #endregion

        #region Timing



        public static double TimeSinceStartup()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return EditorApplication.timeSinceStartup;
            else
#endif
                return Time.realtimeSinceStartup;
        }

        public static bool TimePassedAbove(this double value, float interval)
        {
            return (TimeSinceStartup() - value) > interval;
        }

        #endregion

        #region Editor Updates


        public static bool MouseToPlane(this Plane _plane, out Vector3 hitPos)
        {
            Ray ray = EditorInputManager.GetScreenRay();
            float rayDistance;
            if (_plane.Raycast(ray, out rayDistance))
            {
                hitPos = ray.GetPoint(rayDistance);
                return true;
            }

            hitPos = Vector3.zero;

            return false;
        }


        public static void Log(this string text)
        {

#if UNITY_EDITOR
            UnityEngine.Debug.Log(text);
#endif
        }


        public static bool GetDefine(this string define)
        {

#if UNITY_EDITOR
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            return defines.Contains(define);
#else
        return true;
#endif
        }

        public static void SetDefine(this string val, bool to)
        {

#if UNITY_EDITOR

            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            if (defines.Contains(val) == to) return;

            if (to)
                defines += " ; " + val;
            else
                defines = defines.Replace(val, "");

            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
#endif
        }


        public static bool ApplicationIsAboutToEnterPlayMode(this MonoBehaviour mb)
        {
#if UNITY_EDITOR
            return (((EditorApplication.isPlayingOrWillChangePlaymode) && (Application.isPlaying == false)));
#else
        return false;
#endif
        }


        public static void RepaintViews()
        {
#if UNITY_EDITOR
//            if (SceneView.lastActiveSceneView != null)
  //              SceneView.lastActiveSceneView.Repaint();
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
        }
        
        public static void SetToDirty(this UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(obj);
#endif
        }

        public static void SetToDirty(this object obj)
        {
#if UNITY_EDITOR
            var uobj = obj as UnityEngine.Object;
            if (uobj)
                UnityHelperFunctions.SetToDirty(uobj);
#endif
        }
        #endregion

        #region Prefabs

        public static UnityEngine.Object GetPrefab(this UnityEngine.Object obj)
        {
#if UNITY_EDITOR

#if UNITY_2018_2_OR_NEWER
            return PrefabUtility.GetCorrespondingObjectFromSource(obj);
#else
               return PrefabUtility.GetPrefabParent(obj);
#endif
#else
    return null;
#endif


        }

        public static void UpdatePrefab(this GameObject gameObject)
        {
#if PEGI && UNITY_EDITOR

#if UNITY_2018_3_OR_NEWER
            var pf = gameObject.IsPrefab() ? gameObject :   
                 PrefabUtility.GetPrefabInstanceHandle(gameObject);
#else
            var pf = PrefabUtility.GetPrefabObject(gameObject);
#endif
            if (pf != null)
            {
                // SavePrefabAsset, SaveAsPrefabAsset, SaveAsPrefabAssetAndConnect'
#if UNITY_2018_3_OR_NEWER
                if (pf == null)
                    Debug.LogError("Handle is null");
                else
                {
                    var path = AssetDatabase.GetAssetPath(pf);//PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pf);

                    if (path == null || path.Length == 0)
                        "Path is null, Update prefab manually".showNotification();
                    else
                        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path, InteractionMode.AutomatedAction);
                }
#else
                PrefabUtility.ReplacePrefab(gameObject, gameObject.GetPrefab(), ReplacePrefabOptions.ConnectToPrefab);
                   (gameObject.name + " prefab Updated").showNotification();
#endif

            }
            else {
                (gameObject.name + " Not a prefab").showNotification();
            }
            gameObject.SetToDirty();
#endif
            }


        public static bool IsPrefab(this GameObject go) => go.scene.name == null;


        #endregion

        #region Raycasts

        public static bool RaycastGotHit(this Vector3 from, Vector3 vpos)
        {
            Vector3 ray = from - vpos;
            return Physics.Raycast(new Ray(vpos, ray), ray.magnitude);
        }

        public static bool RaycastGotHit(this Vector3 from, Vector3 vpos, float safeGap)
        {
            Vector3 ray = vpos - from;

            float magnitude = ray.magnitude - safeGap;

            if (magnitude < 0) return false;

            return Physics.Raycast(new Ray(from, ray), magnitude);
        }

        public static bool RaycastHit(this Vector3 from, Vector3 to, out RaycastHit hit)  {
            Vector3 ray = to - from;
            return Physics.Raycast(new Ray(from, ray), out hit);
        }

        #endregion

        public static string GetUniqueName<T>(this string s, List<T> list)
        {

            bool match = true;
            int index = 1;
            string mod = s;


            while (match)
            {
                match = false;

                foreach (var l in list)
                    if (l.ToString().SameAs(mod))
                    {
                        match = true;
                        break;
                    }

                if (match)
                {
                    mod = s + index.ToString();
                    index++;
                }
            }

            return mod;
        }

        public static GameObject SetFlagsOnItAndChildren(this GameObject go, HideFlags flags)
        {

            foreach (Transform child in go.transform)
            {
                child.gameObject.hideFlags = flags;
                child.gameObject.AddFlagsOnItAndChildren(flags);
            }

            return go;
        }

        public static GameObject AddFlagsOnItAndChildren(this GameObject go, HideFlags flags)
        {

            foreach (Transform child in go.transform)
            {
                child.gameObject.hideFlags |= flags;
                child.gameObject.AddFlagsOnItAndChildren(flags);
            }

            return go;
        }

        public static Transform Clear(this Transform transform)
        {


            if (Application.isPlaying)
            {
                foreach (Transform child in transform)
                {
                    Debug.Log("Destroying " + child.name);
                    GameObject.Destroy(child.gameObject);
                }
            }
            return transform;
        }

        #region Gizmos

        public static void LineTo(this Vector3 v3a, Vector3 v3b, Color col)
        {
            Gizmos.color = col;
            Gizmos.DrawLine(v3a, v3b);
        }

        #endregion

        #region Components & GameObjects

        public static GameObject GetFocused()
        {
#if UNITY_EDITOR
            UnityEngine.Object[] tmp = Selection.objects;
            return (((tmp != null) && (tmp.Length > 0)) ? (GameObject)tmp[0] : null);
#else 
            return null;
#endif

        }


        public static MeshCollider ForceMeshCollider(GameObject go)
        {

            Collider[] collis = go.GetComponents<Collider>();

            foreach (Collider c in collis)
                if (c.GetType() != typeof(MeshCollider)) c.enabled = false;

            MeshCollider mc = go.GetComponent<MeshCollider>();

            if (mc == null)
                mc = go.AddComponent<MeshCollider>();

            return mc;

        }


        public static Transform TryGetCameraTransform(this GameObject go)
        {
            Camera c = null;
            if (Application.isPlaying)
            {

                c = Camera.main;
            }
#if UNITY_EDITOR
            else
            {
                if (SceneView.lastActiveSceneView != null)
                    c = SceneView.lastActiveSceneView.camera;

            }
#endif

            if (c != null)
                return c.transform;

            c = GameObject.FindObjectOfType<Camera>();
            if (c != null) return c.transform;


            return go.transform;
        }


        public static void SetLayerRecursively(GameObject go, int layerNumber)
        {
            foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layerNumber;
            }
        }

        public static bool IsFocused(this GameObject go)
        {

#if UNITY_EDITOR
            UnityEngine.Object[] tmp = Selection.objects;
            if ((tmp == null) || (tmp.Length == 0) || tmp[0] == null)
                return false;

            return (tmp[0].GetType() == typeof(GameObject)) && ((GameObject)tmp[0] == go);
#else
        return false;
#endif
        }


        public static T ForceComponent<T>(this GameObject go, ref T co) where T : Component
        {
            if (co == null)
            {
                co = go.GetComponent<T>();
                if (co == null)
                    co = go.AddComponent<T>();
            }

            return co;
        }

        public static void DestroyWhatever(this UnityEngine.Object go)
        {
            if (go != null)
            {
                if (Application.isPlaying)
                {
                    var clean = go as IManageDestroyOnPlay;
                    if (clean != null)
                        clean.DestroyYourself();
                    else
                        UnityEngine.Object.Destroy(go);
                }
                else
                    UnityEngine.Object.DestroyImmediate(go);
            }
        }


#endregion

#region Text Editing
        public static string ToStringShort(this Vector3 v)
        {
            StringBuilder sb = new StringBuilder();

            if (v.x != 0) sb.Append("x:" + ((int)v.x));
            if (v.y != 0) sb.Append(" y:" + ((int)v.y));
            if (v.z != 0) sb.Append(" z:" + ((int)v.z));

            return sb.ToString();
        }

        public static Color[] GetPixels(this Texture2D tex, int width, int height)
        {

            if ((tex.width == width) && (tex.height == height))
                return tex.GetPixels();

            Color[] dst = new Color[width * height];

            Color[] src = tex.GetPixels();

            float dX = (float)tex.width / (float)width;
            float dY = (float)tex.height / (float)height;

            for (int y = 0; y < height; y++)
            {
                int dstIndex = y * width;
                int srcIndex = ((int)(y * dY)) * tex.width;
                for (int x = 0; x < width; x++)
                    dst[dstIndex + x] = src[srcIndex + (int)(x * dX)];

            }


            return dst;
        }

        public static bool SameAs(this string s, string other) =>
            (((s == null || s.Length == 0) && (other == null || other.Length == 0)) || (String.Compare(s, other) == 0));

        public static bool SearchCompare(this string search, string name)
        {
            if ((search.Length == 0) || Regex.IsMatch(name, search, RegexOptions.IgnoreCase)) return true;

            if (search.Contains(" "))
            {
                string[] sgmnts = search.Split(' ');
                for (int i = 0; i < sgmnts.Length; i++)
                    if (!Regex.IsMatch(name, sgmnts[i], RegexOptions.IgnoreCase)) return false;

                return true;
            }
            return false;
        }
        
        public static string RemoveAssetsPart(this string s)
        {
            var ind = s.IndexOf("Assets");
            if (ind == 0 || ind == 1) return s.Substring(6+ind);
            if (ind > 1) return s.Substring(0, ind);
            return s;
        }

        public static string AddPreSlashIfNotEmpty(this string s)
        {
            return (s.Length == 0 || (s[0] == '/')) ? s : "/" + s;
        }

        public static string AddPostSlashIfNotEmpty(this string s)
        {
            return (s.Length == 0 || (s[s.Length - 1] == '/')) ? s : s + "/";
        }

        public static string AddPreSlashIfNone(this string s)
        {
            return (s.Length == 0 || (s[0] != '/')) ? "/" + s : s;
        }

        public static string AddPostSlashIfNone(this string s)
        {
            return (s.Length == 0 || (s[s.Length - 1] != '/')) ? s+ "/" : s;
        }

#endregion


        public static void ToLinear (this Color[] list)
        {
            for (int i = 0; i < list.Length; i++)
                list[i] = list[i].linear;
        }

        public static void ToGamma(this Color[] list)
        {
            for (int i = 0; i < list.Length; i++)
                list[i] = list[i].gamma;
        }


#region Assets Management

        public static string SetUniqueObjectName(this UnityEngine.Object obj, string folderName, string extension)
        {

            folderName = "Assets" + folderName.AddPreSlashIfNotEmpty();
            string name = obj.name;
            string fullpath =
#if UNITY_EDITOR

                AssetDatabase.GenerateUniqueAssetPath(folderName + "/" + name + extension);
#else
            folderName + "/" + name + extension;
#endif
            name = fullpath.Substring(folderName.Length);
            name = name.Substring(0, name.Length - extension.Length);
            obj.name = name;

            return fullpath;
        }

        public static string GetAssetFolder(this UnityEngine.Object obj)
        {
#if UNITY_EDITOR

            UnityEngine.Object parentObject = obj.GetPrefab();
            if (parentObject != null)
                obj = parentObject;

            string path = AssetDatabase.GetAssetPath(obj);

            if (path != null && path.Length > 0)
            {

                int ind = path.LastIndexOf("/");

                if (ind > 0)
                    path = path.Substring(0, ind);

                return path;
            }
            return "";
#else
            return "";
#endif
        }

        public static bool SavedAsAsset(this UnityEngine.Object go)
        {
#if UNITY_EDITOR
            return (!String.IsNullOrEmpty(AssetDatabase.GetAssetPath(go)));
#else
        return true;
#endif

        }

        public static string GetGUID(this UnityEngine.Object obj, string current)
        {
            if (obj == null)
                return current;

#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(obj);
            if (path != null && path.Length > 0)
                current = AssetDatabase.AssetPathToGUID(path);
#endif
            return current;
        }

        public static T GUIDtoAsset<T>(string guid) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path != null && path.Length > 0)
                return AssetDatabase.LoadAssetAtPath<T>(path);
#endif

            return null;
        }

        public static string GetGUID(this UnityEngine.Object obj) => obj.GetGUID(null);

        public static void AddResourceIfNew(this List<string> l, string assetFolder, string insideAssetsFolder)
        {

#if UNITY_EDITOR

            try
            {
                string path = Application.dataPath + "/" + assetFolder
                                                                     + "/Resources" + insideAssetsFolder.AddPreSlashIfNotEmpty();

                if (!Directory.Exists(path)) return;

                DirectoryInfo dirInfo = new DirectoryInfo(path);

                if (dirInfo == null) return;

                FileInfo[] fileInfo = dirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);

                l = new List<string>();

                foreach (FileInfo file in fileInfo)
                {
                    string name = file.Name.Substring(0, file.Name.Length - StuffSaver.fileType.Length);
                    if ((file.Extension == StuffSaver.fileType) && (!l.Contains(name)))
                    {
                        l.Add(name);
                    }
                }

            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex.ToString());
            }

#endif
        }


        public static void RenameAsset<T>(this T obj, string newName) where T: UnityEngine.Object
        {

            if (newName.Length > 0)
            {

#if UNITY_EDITOR
                var path = AssetDatabase.GetAssetPath(obj);
                if (path != null && path.Length > 0)
                {
                    AssetDatabase.RenameAsset(path, newName);
                }
#endif
                obj.name = newName;
            }

        }

        public static T CreateScriptableObjectSameFolder<T>(this ScriptableObject el) where T : ScriptableObject
        {
            T added = null;


#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(el);
            if (path != null && path.Length > 0)
            {

                added = ScriptableObject.CreateInstance(typeof(T)) as T;

                string oldName = Path.GetFileName(path);

                path = path.Replace(oldName, "");

                string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + oldName.Substring(0, oldName.Length - 6) + ".asset");

                AssetDatabase.CreateAsset(added, assetPathAndName);

                added.name = assetPathAndName.Substring(path.Length, assetPathAndName.Length - path.Length - 6);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#else
            added = ScriptableObject.CreateInstance(typeof(T)) as T;
#endif

            return added;
        }
        
        public static T DuplicateScriptableObject<T>(this T el) where T : ScriptableObject
        {
            T added = null;


#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(el);
            if (path != null && path.Length>0) {

                added = ScriptableObject.CreateInstance(el.GetType()) as T;

                string oldName = Path.GetFileName(path);

                path = path.Replace(oldName, "");

        

                string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + oldName.Substring(0, oldName.Length - 6) + ".asset");

                AssetDatabase.CreateAsset(added, assetPathAndName);



                added.name = assetPathAndName.Substring(path.Length, assetPathAndName.Length - path.Length - 6);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#else
            added = ScriptableObject.CreateInstance(el.GetType()) as T;
#endif

            return added;
        }
        
        public static bool TryAdd_UObj_ifNew<T>(this List<T> list, UnityEngine.Object ass) where T : UnityEngine.Object
        {
            if (ass == null)
                return false;

            if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
            {
                var go = ass as GameObject;
                if (go)
                {
                    var cmp = go.GetComponent<T>();
                    if (cmp != null && !list.Contains(cmp))
                    {

                        list.Add(cmp);
                        // Debug.Log("Added " + cmp.ToString() + " into " + typeof(T).ToString());
                        return true;
                    }
                    //  else    Debug.Log("Failed on null "+(cmp == null));
                } //else  Debug.Log("Not a GO");
                return false;
            }

            if (ass.GetType() == typeof(T) || ass.GetType().IsSubclassOf(typeof(T)))
            {
               // Debug.Log("is other " + ass.ToString());
                T cst = ass as T;
                if (!list.Contains(cst))
                {
                    list.Add(cst);
                   // Debug.Log("Added " + cst.ToString() + " into " + typeof(T).ToString());
                }
                return true;
            }
            // else Debug.Log("Wrong path");
            return false;

        }

        public static T CreateAsset_SO<T>(this List<T> objs, string path, string name) where T : ScriptableObject
        {
            return CreateAsset_SO<T, T>(path, name, objs);


        }

#if UNITY_EDITOR
        public static void DuplicateResource(string assetFolder, string insideAssetFolder, string oldName, string newName)
        {
            string path = "Assets" + assetFolder.AddPreSlashIfNotEmpty() + "/Resources" + insideAssetFolder.AddPreSlashIfNotEmpty() + "/";
            AssetDatabase.CopyAsset(path + oldName + StuffSaver.fileType, path + newName + StuffSaver.fileType);
        }

#endif
        // The function below uses this function's name
        public static T CreateAsset_SO_DONT_RENAME<T>(string path, string name) where T : ScriptableObject
        {
            return CreateAsset_SO<T, T>(path, name, null);
        }

        public static T CreateAsset_SO<T>(this List<T> list, string path, string name, Type t) where T : ScriptableObject
        {
            var obj = typeof(UnityHelperFunctions)
                .GetMethod("CreateAsset_SO_DONT_RENAME")
                .MakeGenericMethod(t)
                .Invoke(null, new object[] { path, name }) as T;

            list.Add(obj);

            return obj;
        }

        public static T CreateAsset_SO<T, G>(string path, string name, List<G> optionalList) where T : G where G : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

#if PEGI

            var nm = asset as IGotName;
            if (nm != null)
                nm.NameForPEGI = name;
            
            if (optionalList != null)
            {

                var ind = asset as IGotIndex;

                if (ind != null)
                {
                    int maxInd = 0;
                    foreach (var o in optionalList)
                    {
                        var io = o as IGotIndex;
                        if (io != null)
                            maxInd = Mathf.Max(io.IndexForPEGI + 1, maxInd);
                    }
                    ind.IndexForPEGI = maxInd;
                }
                
                optionalList.Add(asset);
                
            }
#endif
#if UNITY_EDITOR

            if (!path.Contains("Assets"))
                path = "Assets" + path.AddPreSlashIfNotEmpty();

            string fullPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + path;
            try
            {
                Directory.CreateDirectory(fullPath);
            } catch (Exception ex)
            {
                Debug.LogError("Couldn't create Directory {0} : {1}".F(fullPath, ex.ToString()));
                return null;
            }

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath("{0}{1}.asset".F(path.AddPostSlashIfNone(), name));

            try
            {

                AssetDatabase.CreateAsset(asset, assetPathAndName);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            } catch (Exception ex)
            {
                Debug.LogError("Couldn't create Scriptable Object {0} : {1}".F(assetPathAndName, ex.ToString()));
            }
#endif



            return asset;
        }


        public static void DeleteResource(string assetFolder, string insideAssetFolderAndName)
        {



#if UNITY_EDITOR
            try
            {
                string path = "Assets" + assetFolder.AddPreSlashIfNotEmpty() + "/Resources/" + insideAssetFolderAndName + StuffSaver.fileType;
                //Debug.Log("Deleting " +path);
                //Application.dataPath + "/"+assetFolder + "/Resources/" + insideAssetFolderAndName);
                AssetDatabase.DeleteAsset(path);
            }
            catch (Exception e)
            {
                Debug.Log("Oh No " + e.ToString());
            }
#endif
        }


#endregion

        public static int TotalCount(this List<int>[] lists)
        {
            int total = 0;

            foreach (var e in lists)
                total += e.Count;

            return total;
        }



#region Texture Properties

#if UNITY_EDITOR
        public static List<string> GetFields(this Material m, MaterialProperty.PropType type) {
            List<string> fNames = new List<string>();

            if (m == null) return fNames;

            Material[] mat = new Material[1];
            mat[0] = m;
            MaterialProperty[] props = null;

            try
            {
                props = MaterialEditor.GetMaterialProperties(mat);
            }
            catch
            {
                return fNames = new List<string>();
            }
            
            if (props != null)
                foreach (MaterialProperty p in props)
                    if (p.type == type)
                        fNames.Add(p.name);

            return fNames;
        }
#endif

        public static List<string> GetFloatFields(this Material m)
        {
#if UNITY_EDITOR
            var l = m.GetFields(MaterialProperty.PropType.Float);
            l.AddRange(m.GetFields(MaterialProperty.PropType.Range));
            return l;
#else
            return new List<string>();
#endif
        }

        public static List<string> MyGetTextureProperties(this Material m)
        {
#if UNITY_2018_2_OR_NEWER
            if (!m) return new List<string>();
            else
            return new List<string>(m.GetTexturePropertyNames());
#else

#if UNITY_EDITOR
            return m.GetFields(MaterialProperty.PropType.Texture);
#else
            return new List<string>();
#endif
#endif
        }

        public static List<string> GetColorProperties(this Material m) {

#if UNITY_EDITOR
            return m.GetFields(MaterialProperty.PropType.Color);
#else
            return new List<string>();
#endif

        }

#endregion

        public static bool DisplayNameContains(this Material m, string propertyName, string tag)
        {
            /*
#if UNITY_EDITOR
                    try
                    {
                        var p = MaterialEditor.GetMaterialProperty(new Material[] { m }, propertyName);
                        if (p!= null) 
                            return p.displayName.Contains(tag);

                    } catch (Exception ex) {
                        Debug.Log("Materail "+m.name +" has no "+ propertyName+ " "+ex.ToString());
                    }
#endif
            */
            return propertyName.Contains(tag);
        }

        public static void SetActiveTo(this GameObject go, bool setTo)
        {
            if (go.activeSelf != setTo)
                go.SetActive(setTo);
        }

        public static void EnabledUpdate(this Renderer c, bool setTo)
        {
            //There were some update when enabled state is changed
            if (c != null && c.enabled != setTo)
                c.enabled = setTo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns>Return -1 if no numeric key was pressed</returns>
        public static int NumericKeyDown(this Event e)
        {

            if ((Application.isPlaying) && (!Input.anyKeyDown)) return -1;

            if ((!Application.isPlaying) && (e.type != EventType.KeyDown)) return -1;

            if (KeyCode.Alpha0.IsDown()) return 0;
            if (KeyCode.Alpha1.IsDown()) return 1;
            if (KeyCode.Alpha2.IsDown()) return 2;
            if (KeyCode.Alpha3.IsDown()) return 3;
            if (KeyCode.Alpha4.IsDown()) return 4;
            if (KeyCode.Alpha5.IsDown()) return 5;
            if (KeyCode.Alpha6.IsDown()) return 6;
            if (KeyCode.Alpha7.IsDown()) return 7;
            if (KeyCode.Alpha8.IsDown()) return 8;
            if (KeyCode.Alpha9.IsDown()) return 9;

            return -1;
        }

        public static bool IsDown(this KeyCode k)
        {
            bool down = false;
#if UNITY_EDITOR
            down |= (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode == k);
            if (Application.isPlaying)
#endif
                down |= Input.GetKeyDown(k);

            return down;
        }

        public static bool IsUp(this KeyCode k)
        {

            bool up = false;
#if UNITY_EDITOR
            up |= (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyUp && Event.current.keyCode == k);
            if (Application.isPlaying)
#endif
                up |= Input.GetKeyUp(k);

            return up;
        }

        public static void Focus(this GameObject go)
        {
#if UNITY_EDITOR
            GameObject[] tmp = new GameObject[1];
            tmp[0] = go;
            Selection.objects = tmp;
#endif
        }

        public static void FocusOn(UnityEngine.Object go)
        {
#if UNITY_EDITOR
            UnityEngine.Object[] tmp = new UnityEngine.Object[1];
            tmp[0] = go;
            Selection.objects = tmp;
#endif
        }

        public static void CopyFrom(this Texture2D tex, RenderTexture rt)
        {

          

            if (rt == null || tex == null)
            {
#if UNITY_EDITOR
                Debug.Log("Texture is null");
#endif
                return;
            }

            RenderTexture curRT = RenderTexture.active;

            RenderTexture.active = rt;

            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

            RenderTexture.active = curRT;

        }



#if UNITY_EDITOR

#region Texture Saving

        public static void SaveTexture(this Texture2D tex)
        {

            byte[] bytes = tex.EncodeToPNG();
            //Debug.Log("Format " + tex.format); 

            string dest = AssetDatabase.GetAssetPath(tex).Replace("Assets", "");

            File.WriteAllBytes(Application.dataPath + dest, bytes);

            AssetDatabase.Refresh();
        }

        public static string GetAssetPath(this Texture2D tex)
        {
            return AssetDatabase.GetAssetPath(tex);
        }

        public static string GetPathWithout_Assets_Word(this Texture2D tex)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            if (String.IsNullOrEmpty(path)) return null;
            return path.Replace("Assets", "");
        }


        public static Texture2D RewriteOriginalTexture_NewName(this Texture2D tex, string name)
        {
            if (name == tex.name)
                return tex.RewriteOriginalTexture();

            //Debug.Log("Rewriting original texture");

            byte[] bytes = tex.EncodeToPNG();

            string dest = tex.GetPathWithout_Assets_Word();
            dest = dest.ReplaceLastOccurrence(tex.name, name);
            if (String.IsNullOrEmpty(dest)) return tex;

            File.WriteAllBytes(Application.dataPath + dest, bytes);

            //Debug.Log ("Writing to "+dest);
            //AssetDatabase.Refresh();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);

            Texture2D result = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + dest, typeof(Texture2D));

            result.ReimportToMatchImportConfigOf(tex);

            AssetDatabase.DeleteAsset(tex.GetAssetPath());

            AssetDatabase.Refresh();
            return result;
        }

        public static Texture2D RewriteOriginalTexture(this Texture2D tex)
        {
            //Debug.Log("Rewriting original texture");

            byte[] bytes = tex.EncodeToPNG();

            string dest = tex.GetPathWithout_Assets_Word();
            if (String.IsNullOrEmpty(dest)) return tex;

            File.WriteAllBytes(Application.dataPath + dest, bytes);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);
            //AssetDatabase.Refresh();

            Texture2D result = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + dest, typeof(Texture2D));

            result.ReimportToMatchImportConfigOf(tex);

            return result;
        }

        public static Texture2D SaveTextureAsAsset(this Texture2D tex, string folderName, ref string textureName, bool saveAsNew)
        {

            byte[] bytes = tex.EncodeToPNG();

            string lastPart = folderName.AddPreSlashIfNotEmpty() + "/";
            string folderPath = Application.dataPath + lastPart;
            Directory.CreateDirectory(folderPath);

            string fileName = textureName + ".png";

            string relativePath = "Assets" + lastPart + fileName;

            if (saveAsNew)
                relativePath = AssetDatabase.GenerateUniqueAssetPath(relativePath);

            string fullPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + relativePath;

            File.WriteAllBytes(fullPath, bytes);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);
            //AssetDatabase.Refresh(); 

            Texture2D result = (Texture2D)AssetDatabase.LoadAssetAtPath(relativePath, typeof(Texture2D));

            textureName = result.name;

            result.ReimportToMatchImportConfigOf(tex);

            return result;
        }

        public static Texture2D CreatePngSameDirectory(this Texture2D diffuse, string newName)
        {
            return CreatePngSameDirectory(diffuse, newName, diffuse.width, diffuse.height);
        }

        public static Texture2D CreatePngSameDirectory(this Texture2D diffuse, string newName, int width, int height)
        {

            Texture2D Result = new Texture2D(width, height, TextureFormat.RGBA32, true, false);

            diffuse.Reimport_IfNotReadale();

            var pxls = diffuse.GetPixels(width, height);
            pxls[0].a = 0.5f;

            Result.SetPixels(pxls);

            byte[] bytes = Result.EncodeToPNG();

            string dest = AssetDatabase.GetAssetPath(diffuse).Replace("Assets", "");

            var extension = dest.Substring(dest.LastIndexOf(".") + 1);

            dest = dest.Substring(0, dest.Length - extension.Length) + "png";

            dest = dest.ReplaceLastOccurrence(diffuse.name, newName);

            File.WriteAllBytes(Application.dataPath + dest, bytes);

            AssetDatabase.Refresh();

            var tex = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + dest, typeof(Texture2D));

            var imp = tex.GetTextureImporter();
            bool needReimport = imp.WasNotReadable();
            needReimport |= imp.WasClamped();
            needReimport |= imp.WasWrongIsColor(diffuse.IsColorTexture());
            if (needReimport)
                imp.SaveAndReimport();

            return tex;

        }
#endregion

      
        public static void FocusOnGame()
        {

            System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            System.Type type = assembly.GetType("UnityEditor.GameView");
            EditorWindow gameview = EditorWindow.GetWindow(type);
            gameview.Focus();


        }

        public static void RenamingLayer(int index, string name)
        {
            if (Application.isPlaying) return;

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                Debug.LogWarning("Can't set up the layers.  It's possible the format of the layers and tags data has changed in this version of Unity.");
                Debug.LogWarning("Layers is null: " + (layers == null));
                return;
            }


            SerializedProperty layerSP = layers.GetArrayElementAtIndex(index);
            if ((layerSP.stringValue != name) && ((layerSP.stringValue == null) || (layerSP.stringValue.Length == 0)))
            {
                Debug.Log("Changing layer name.  " + layerSP.stringValue + " to " + name);
                layerSP.stringValue = name;
            }


            tagManager.ApplyModifiedProperties();
        }

#endif
        
    
        
        public static void SetKeyword(string name, bool value)
        {

            if (value) Shader.EnableKeyword(name);
            else
                Shader.DisableKeyword(name);

        }

#region Terrain Layers
        public static void SetSplashPrototypeTexture(this Terrain terrain, Texture2D tex, int index)
        {

            if (terrain == null) return;



#if UNITY_2018_3_OR_NEWER
            var l = terrain.terrainData.terrainLayers;

            if (l.Length > index)
                l[index].diffuseTexture = tex;
#else

            SplatPrototype[] newProtos = terrain.GetCopyOfSplashPrototypes();

            if (newProtos.Length <= index)
            {
                ArrayManager<SplatPrototype> arrman = new ArrayManager<SplatPrototype>();
                arrman.AddAndInit(ref newProtos, index + 1 - newProtos.Length);
            }

            newProtos[index].texture = tex;

       
            terrain.terrainData.splatPrototypes = newProtos;
#endif



        }

        public static Texture GetSplashPrototypeTexture(this Terrain terrain, int ind)
        {

#if UNITY_2018_3_OR_NEWER
            var l = terrain.terrainData.terrainLayers;

            if (l.Length > ind)
                return l[ind].diffuseTexture;
            else
                return null;
#else

            SplatPrototype[] prots = terrain.terrainData.splatPrototypes;

            if (prots.Length <= ind) return null;


            return prots[ind].texture;
#endif
        }

#if !UNITY_2018_3_OR_NEWER
        public static SplatPrototype[] GetCopyOfSplashPrototypes(this Terrain terrain)
        {

            if (terrain == null) return null;

            SplatPrototype[] oldProtos = terrain.terrainData.splatPrototypes;
            SplatPrototype[] newProtos = new SplatPrototype[oldProtos.Length];
            for (int i = 0; i < oldProtos.Length; i++)
            {
                SplatPrototype oldProto = oldProtos[i];
                SplatPrototype newProto = new SplatPrototype();
                newProtos[i] = newProto;

                newProto.texture = oldProto.texture;
                newProto.tileSize = oldProto.tileSize;
                newProto.tileOffset = oldProto.tileOffset;
                newProto.normalMap = oldProto.normalMap;
            }

            return newProtos;
        }
#endif
#endregion

#region Spin Around

        public static Vector2 camOrbit = new Vector2();
        public static Vector3 SpinningAround;
        public static float OrbitDistance = 0;
        public static bool OrbitingFocused;
        public static float SpinStartTime = 0;
        // Use this for initialization
        public static void SpinAround(Vector3 pos, Transform cameraman)
        {
            if (Input.GetMouseButtonDown(2))
            {
                Quaternion before = cameraman.rotation;//cam.transform.rotation;
                cameraman.transform.LookAt(pos);
                Vector3 rot = cameraman.rotation.eulerAngles;
                camOrbit.x = rot.y;
                camOrbit.y = rot.x;
                OrbitDistance = (pos - cameraman.position).magnitude;
                SpinningAround = pos;
                cameraman.rotation = before;
                OrbitingFocused = false;
                SpinStartTime = Time.time;
            }

            if (Input.GetMouseButtonUp(2))
                OrbitDistance = 0;

            if ((OrbitDistance != 0) && (Input.GetMouseButton(2)))
            {

                camOrbit.x += Input.GetAxis("Mouse X") * 5;
                camOrbit.y -= Input.GetAxis("Mouse Y") * 5;

                if (camOrbit.y <= -360)
                    camOrbit.y += 360;
                if (camOrbit.y >= 360)
                    camOrbit.y -= 360;
                //y = Mathf.Clamp (y, min, max);




                Quaternion rot = Quaternion.Euler(camOrbit.y, camOrbit.x, 0);
                Vector3 campos = rot *
                    (new Vector3(0.0f, 0.0f, -OrbitDistance)) +
                    SpinningAround;

                cameraman.position = campos;
                if ((Time.time - SpinStartTime) > 0.2f)
                {
                    if (!OrbitingFocused)
                    {
                        cameraman.transform.rotation = MyMath.Lerp(cameraman.rotation, rot, 300);
                        if (Quaternion.Angle(cameraman.rotation, rot) < 1)
                            OrbitingFocused = true;
                    }
                    else cameraman.rotation = rot;
                }

            }
        }

#endregion

#region Texture Import Settings

        public static bool IsColorTexture(this Texture2D tex)
        {
#if UNITY_EDITOR
            if (tex == null) return true;

            TextureImporter importer = tex.GetTextureImporter();

            if (importer != null)
                return importer.sRGBTexture;
#endif
            return true;
        }

#if UNITY_EDITOR

        public static TextureImporter GetTextureImporter(this Texture2D tex)
        {
            return AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        }


        public static void ReimportToMatchImportConfigOf(this Texture2D dest, Texture2D original)
        {
            TextureImporter dst = dest.GetTextureImporter();
            TextureImporter org = original.GetTextureImporter();

            if ((dst == null) || (org == null)) return;

            int maxSize = Mathf.Max(original.width, org.maxTextureSize);

            bool needReimport = (dst.wrapMode != org.wrapMode) ||
                                (dst.sRGBTexture != org.sRGBTexture) ||
                                (dst.textureType != org.textureType) ||
                                (dst.alphaSource != org.alphaSource) ||
                                (dst.maxTextureSize < maxSize) ||
                                (dst.isReadable != org.isReadable) ||
                                (dst.textureCompression != org.textureCompression) ||
                                (dst.alphaIsTransparency != org.alphaIsTransparency);

            if (needReimport)
            {
                dst.wrapMode = org.wrapMode;
                dst.sRGBTexture = org.sRGBTexture;
                dst.textureType = org.textureType;
                dst.alphaSource = org.alphaSource;
                dst.alphaIsTransparency = org.alphaIsTransparency;
                dst.maxTextureSize = maxSize;
                dst.isReadable = org.isReadable;
                dst.textureCompression = org.textureCompression;
                dst.SaveAndReimport();
            }

        }

        public static bool HadNoMipmaps(this TextureImporter importer)
        {

            bool needsReimport = false;

            if (importer.mipmapEnabled == false)
            {
                importer.mipmapEnabled = true;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfMarkedAsNOrmal(this Texture2D tex)
        {
            if (tex == null) return;

            TextureImporter importer = tex.GetTextureImporter();

            if ((importer != null) && (importer.WasMarkedAsNormal()))
                importer.SaveAndReimport();
        }
        public static bool WasMarkedAsNormal(this TextureImporter importer)
        {

            /*  bool needsReimport = false;

              if (importer.textureType == TextureImporterType.NormalMap) {
                  importer.textureType = TextureImporterType.Default;
                  needsReimport = true;
              }*/

            return WasMarkedAsNormal(importer, false);

        }
        public static bool WasMarkedAsNormal(this TextureImporter importer, bool convertToNormal)
        {

            bool needsReimport = false;

            if ((importer.textureType == TextureImporterType.NormalMap) != convertToNormal)
            {
                importer.textureType = convertToNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                needsReimport = true;
            }

            return needsReimport;

        }



        public static void Reimport_IfClamped(this Texture2D tex)
        {
            if (tex == null) return;

            TextureImporter importer = tex.GetTextureImporter();

            if ((importer != null) && (importer.WasClamped()))
                importer.SaveAndReimport();
        }
        public static bool WasClamped(this TextureImporter importer)
        {

            bool needsReimport = false;


            if (importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfNotReadale(this Texture2D tex)
        {
            if (tex == null) return;

            TextureImporter importer = tex.GetTextureImporter();

            if ((importer != null) && (importer.WasNotReadable()))
                importer.SaveAndReimport();
        }
        public static bool WasNotReadable(this TextureImporter importer)
        {

            bool needsReimport = false;



            if (importer.isReadable == false)
            {
                importer.isReadable = true;
                needsReimport = true;
            }

            if (importer.textureType == TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Default;
                needsReimport = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                needsReimport = true;
            }

            return needsReimport;


        }

        public static void Reimport_SetIsColorTexture(this Texture2D tex, bool value)
        {
            if (tex == null) return;

            TextureImporter importer = tex.GetTextureImporter();

            if ((importer != null) && (importer.WasWrongIsColor(value)))
                importer.SaveAndReimport();
        }
        public static bool WasWrongIsColor(this TextureImporter importer, bool isColor)
        {

            bool needsReimport = false;

            if (importer.sRGBTexture != isColor)
            {
                importer.sRGBTexture = isColor;
                needsReimport = true;
            }

            return needsReimport;
        }

        public static void Reimport_IfNotSingleChanel(this Texture2D tex)
        {
            if (tex == null) return;

            TextureImporter importer = tex.GetTextureImporter();

            if ((importer != null) && (importer.WasNotSingleChanel()))
                importer.SaveAndReimport();
        }
        public static bool WasNotSingleChanel(this TextureImporter importer)
        {

            bool needsReimport = false;


            if (importer.textureType != TextureImporterType.SingleChannel)
            {
                importer.textureType = TextureImporterType.SingleChannel;
                needsReimport = true;
            }

            if (importer.alphaSource != TextureImporterAlphaSource.FromGrayScale)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                needsReimport = true;
            }

            if (importer.alphaIsTransparency == false)
            {
                importer.alphaIsTransparency = true;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfAlphaIsNotTransparency(this Texture2D tex)
        {
            if (tex == null) return;

            TextureImporter importer = tex.GetTextureImporter();

            if ((importer != null) && (importer.WasAlphaNotTransparency()))
                importer.SaveAndReimport();

        }
        public static bool WasAlphaNotTransparency(this TextureImporter importer)
        {

            bool needsReimport = false;

            if (importer.alphaIsTransparency == false)
            {
                importer.alphaIsTransparency = true;
                needsReimport = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                needsReimport = true;
            }

            if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                needsReimport = true;
            }

            return needsReimport;

        }

        public static void Reimport_IfWrongMaxSize(this Texture2D tex, int width)
        {
            if (tex == null) return;

            TextureImporter importer = tex.GetTextureImporter();

            if ((importer != null) && (importer.WasWrongMaxSize(width)))
                importer.SaveAndReimport();

        }
        public static bool WasWrongMaxSize(this TextureImporter importer, int width)
        {

            bool needsReimport = false;

            if (importer.maxTextureSize < width)
            {
                importer.maxTextureSize = width;
                needsReimport = true;
            }

            return needsReimport;

        }


#endif
#endregion
    }

}





