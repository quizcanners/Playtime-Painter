using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using PlayerAndEditorGUI;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCannersUtilities {

    public static class UnityHelperFunctions {

        #region External Communications
        
        public static void SendEmail(string to) => Application.OpenURL("mailto:{0}".F(to));

        public static void SendEmail(string email, string subject, string body) =>
        Application.OpenURL("mailto:{0}?subject={1}&body={2}".F(email, subject.MyEscapeUrl(), body.MyEscapeUrl()));

        static string MyEscapeUrl(this string url) =>
            #if UNITY_2018_1_OR_NEWER
            UnityWebRequest
            #else
            WWW
            #endif
            .EscapeURL(url).Replace("+", "%20");

        public static void OpenBrowser(string address) => Application.OpenURL(address);

        #endregion

        #region Timing

        public static double TimeSinceStartup() =>
            #if UNITY_EDITOR
            (!Application.isPlaying)
                ? EditorApplication.timeSinceStartup :
            #endif
                Time.realtimeSinceStartup;

        public static bool TimePassedAbove(this double value, float interval) => (TimeSinceStartup() - value) > interval;
        

        #endregion

        #region Raycasts

        public static bool RaycastGotHit(this Vector3 from, Vector3 vpos)
        {
            var ray = from - vpos;
            return Physics.Raycast(new Ray(vpos, ray), ray.magnitude);
        }

        public static bool RaycastGotHit(this Vector3 from, Vector3 vpos, float safeGap)
        {
            var ray = vpos - from;

            var magnitude = ray.magnitude - safeGap;

            return (magnitude <= 0) ? false : Physics.Raycast(new Ray(from, ray), magnitude);
        }

        public static bool RaycastHit(this Vector3 from, Vector3 to, out RaycastHit hit)
        {
            var ray = to - from;
            return Physics.Raycast(new Ray(from, ray), out hit);
        }

        #endregion

        #region Gizmos

        public static void LineTo(this Vector3 v3a, Vector3 v3b, Color col)
        {
            Gizmos.color = col;
            Gizmos.DrawLine(v3a, v3b);
        }

        #endregion

        #region Transformations 

        public static void TrySetLocalScale<T>(this List<T> graphics, float size) where T: Graphic {

            foreach (var g in graphics)
                if (g) g.rectTransform.localScale = Vector3.one * size;

        }

        public static Color ToOpaque(this Color col)
        {
            col.a = 1;
            return col;
        }

        public static Color ToTransparent(this Color col)
        {
            col.a = 0;
            return col;
        }

        #endregion

        #region Components & GameObjects

        public static GameObject TryGetGameObject_Obj(this object obj) {
            var go = obj as GameObject;

            if (go) return go; 
            
            var cmp = obj as Component;
            if (cmp)
                go = cmp.gameObject;
            
            return go;
        }

        public static T TryGet_fromObj<T>(this object obj) where T : class {

            if (obj.IsNullOrDestroyed_Obj())
                return null;

            var pgi = obj as T;

            if (pgi != null)
                return pgi;

            var go = obj.TryGetGameObject_Obj();

             return go ? go.TryGet<T>() : null;
        }

        public static T TryGet_fromMb<T>(this MonoBehaviour mb) where T : class => mb ? mb.gameObject.TryGet<T>() : null;

        public static T TryGet_fromTf<T>(this Transform tf) where T:class => tf ? tf.gameObject.TryGet<T>() : null;

        public static T TryGet<T>(this GameObject go) where T:class {

            if (!go)
                return null;

            foreach (var m in go.GetComponents<Component>())
            {
                var p = m as T;
                if (p != null)
                    return p;
            }
            return null;
        }

        public static bool IsNullOrDestroyed_Obj(this object obj) {
            if (obj as UnityEngine.Object)
                return false;
                
             return obj == null;
        }
        
        public static T NullIfDestroyed<T>(this T obj) => obj.IsNullOrDestroyed_Obj() ? default(T) : obj;
  
        public static bool TrySetAlpha(this Graphic graphic, float alpha)
        {
            if (!graphic) return false;
            
            var col = graphic.color;
            
            if (Math.Abs(col.a - alpha) < float.Epsilon) return false;
                
            col.a = alpha;
            graphic.color = col;
            return true;
               
        }

        public static void TrySetAlpha<T>(this List<T> graphics, float alpha) where T : Graphic
        {
            if (graphics.IsNullOrEmpty()) return;
            
            foreach (var g in graphics)
                g.TrySetAlpha(alpha);
        }

        public static bool TrySetColor_RGB(this Graphic graphic, Color color)
        {
            if (!graphic) return false;
            
            color.a = graphic.color.a;
            graphic.color = color;
            return true;
        }

        public static void TrySetColor_RGB<T>(this List<T> graphics, Color color) where T: Graphic
        {
            if (graphics.IsNullOrEmpty()) return;
            
            foreach (var g in graphics)
                g.TrySetColor_RGB(color);
        }

        public static bool TrySetColor_RGBA(this Graphic graphic, Color color)
        {
            if (!graphic) return false;
            graphic.color = color;
            return true;
        }

        public static void TrySetColor_RGBA<T>(this List<T> graphics, Color color) where T : Graphic
        {
            if (graphics.IsNullOrEmpty()) return;

            foreach (var g in graphics)
                g.TrySetColor_RGBA(color);
        }


        public static string GetMeaningfulHierarchyName(this GameObject go, int maxLook, int maxLength)
        {

            var name = go.name;

#if PEGI
            var parent = go.transform.parent;

            while (parent && maxLook > 0 && maxLength > 0)
            {
                var n = parent.name;

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

        public static bool IsUnityObject(this Type t) => typeof(UnityEngine.Object).IsAssignableFrom(t);

        public static void SetActive(this List<GameObject> goList, bool to)
        {
            if (goList == null) return;
            
            foreach (var go in goList)
                if (go) go.SetActive(to);
        }

        public static GameObject GetFocusedGameObject() {

            #if UNITY_EDITOR
            var tmp = Selection.objects;
            return !tmp.IsNullOrEmpty()  ? tmp[0].TryGetGameObject_Obj() : null;
            #else 
            return null;
            #endif

        }

        public static GameObject SetFlagsOnItAndChildren(this GameObject go, HideFlags flags)
        {

            foreach (Transform child in go.transform)
            {
                child.gameObject.hideFlags = flags;
                child.gameObject.AddFlagsOnThisAndChildren(flags);
            }

            return go;
        }

        public static GameObject AddFlagsOnThisAndChildren(this GameObject go, HideFlags flags)
        {

            foreach (Transform child in go.transform)
            {
                child.gameObject.hideFlags |= flags;
                child.gameObject.AddFlagsOnThisAndChildren(flags);
            }

            return go;
        }

        public static MeshCollider ForceMeshCollider(GameObject go)
        {

            var collis = go.GetComponents<Collider>();

            foreach (var c in collis)
                if (c.GetType() != typeof(MeshCollider)) c.enabled = false;

            var mc = go.GetComponent<MeshCollider>();

            if (!mc)
                mc = go.AddComponent<MeshCollider>();

            return mc;

        }

        public static Transform TryGetCameraTransform(this GameObject go)
        {
            Camera c = null;
            if (Application.isPlaying)
                c = Camera.main;
            #if UNITY_EDITOR
            else
                if (SceneView.lastActiveSceneView != null)
                    c = SceneView.lastActiveSceneView.camera;
            #endif

            if (c)
                return c.transform;

            c = UnityEngine.Object.FindObjectOfType<Camera>();
            
            return c ? c.transform : go.transform;
        }

        public static void SetLayerRecursively(GameObject go, int layerNumber)
        {
            foreach (var trans in go.GetComponentsInChildren<Transform>(true))
                trans.gameObject.layer = layerNumber;
            
        }

        public static bool IsFocused(this GameObject go)
        {

            #if UNITY_EDITOR
            var tmp = Selection.objects;
            if (tmp.IsNullOrEmpty() || !tmp[0])
                return false;

            return (tmp[0] is GameObject) && (GameObject)tmp[0] == go;
            #else
            return false;
            #endif
        }

        public static T ForceComponent<T>(this GameObject go, ref T co) where T : Component
        {
            if (co)
                return co;
            
            co = go.GetComponent<T>();
            if (!co)
                co = go.AddComponent<T>();
            
            return co;
        }

        public static void DestroyWhatever_UObj(this UnityEngine.Object obj)
        {
            if (!obj) return;
            
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
        }

        public static void DestroyWhatever(this Texture tex) => tex.DestroyWhatever_UObj();

        public static void DestroyWhatever(this GameObject go) => go.DestroyWhatever_UObj();

        public static void DestroyWhatever_Component(this Component cmp) => cmp.DestroyWhatever_UObj();
        
        public static void SetActiveTo(this GameObject go, bool setTo)
        {
            if (go.activeSelf != setTo)
                go.SetActive(setTo);
        }

        public static void EnabledUpdate(this Renderer c, bool setTo)
        {
            //There were some update when enabled state is changed
            if (c && c.enabled != setTo)
                c.enabled = setTo;
        }

        public static bool HasParameter(this Animator animator, string paramName)
        {
            if (!animator) return false;
            
            foreach (var param in animator.parameters) 
                if (param.name.SameAs(paramName))
                    return true;
            
            return false;
        }

        public static bool HasParameter(this Animator animator, string paramName, AnimatorControllerParameterType type)
        {
            if (!animator) return false;
            
            foreach (var param in animator.parameters)
                if (param.name.SameAs(paramName) && param.type == type)
                    return true;
                
            return false;
        }

        #endregion

        #region Unity Editor MGMT

        public static bool MouseToPlane(this Plane plane, out Vector3 hitPos)
        {
            var ray = EditorInputManager.GetScreenRay();
            float rayDistance;
            if (plane.Raycast(ray, out rayDistance))
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
            Debug.Log(text);
            #endif
        }

        public static bool GetDefine(this string define)
        {

#if UNITY_EDITOR
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            return defines.Contains(define);
#else
        return true;
#endif
        }

        public static void SetDefine(this string val, bool to) {

            #if UNITY_EDITOR
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            if (defines.Contains(val) == to) return;

            if (to)
                defines += " ; " + val;
            else
                defines = defines.Replace(val, "");

            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
            #endif
        }

        public static bool ApplicationIsAboutToEnterPlayMode()
        {
#if UNITY_EDITOR
            return EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying;
#else
        return false;
#endif
        }
        
        public static void RepaintViews()
        {
#if UNITY_EDITOR
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
        }

        public static List<UnityEngine.Object> SetToDirty(this List<UnityEngine.Object> objs)
        {
#if UNITY_EDITOR
            if (objs.IsNullOrEmpty()) return objs;
            
            foreach (var o in objs)
                o.SetToDirty();
#endif
            return objs;

        }

        public static UnityEngine.Object SetToDirty(this UnityEngine.Object obj)
        {
            #if UNITY_EDITOR
            if (!obj) return obj;
            
            EditorUtility.SetDirty(obj);


#if UNITY_2018_3_OR_NEWER
            if (PrefabUtility.IsPartOfAnyPrefab(obj))
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
#endif

        #endif
            return obj;
        }

        public static object SetToDirty_Obj(this object obj) {

            #if UNITY_EDITOR
            SetToDirty(obj as UnityEngine.Object);
            #endif

            return obj;
        }

        public static void FocusOn(UnityEngine.Object go)
        {
        #if UNITY_EDITOR
            var tmp = new UnityEngine.Object[1];
            tmp[0] = go;
            Selection.objects = tmp;
        #endif
        }

#if UNITY_EDITOR
        private static Tool _previousEditorTool = Tool.None;
#endif

        public static void RestoreUnityTool() {
            #if UNITY_EDITOR
            if (_previousEditorTool != Tool.None && Tools.current == Tool.None)
                Tools.current = _previousEditorTool;
            #endif
        }

        public static void HideUnityTool() {
            #if UNITY_EDITOR
            if (Tools.current == Tool.None) return;
            
            _previousEditorTool = Tools.current;
            Tools.current = Tool.None;
            
            #endif
        }
        
        #if UNITY_EDITOR
        public static void FocusOnGame()
        {

            var assembly = typeof(EditorWindow).Assembly;
            var type = assembly.GetType("UnityEditor.GameView");
            var gameview = EditorWindow.GetWindow(type);
            gameview.Focus();


        }

        public static void RenamingLayer(int index, string name)
        {
            if (Application.isPlaying) return;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                Debug.LogWarning("Can't set up the layers.  It's possible the format of the layers and tags data has changed in this version of Unity.");
                Debug.LogWarning("Layers is null: " + (layers == null));
                return;
            }


            var layerSp = layers.GetArrayElementAtIndex(index);
            
            if (layerSp.stringValue.IsNullOrEmpty() || !layerSp.stringValue.SameAs(name)) {
                Debug.Log("Changing layer name.  " + layerSp.stringValue + " to " + name);
                layerSp.stringValue = name;
            }

            tagManager.ApplyModifiedProperties();
        }
        #endif
        #endregion

        #region Assets Management

        public static void RefreshAssetDatabase()
        {
        #if UNITY_EDITOR
            AssetDatabase.Refresh();
        #endif
        }
        
        public static UnityEngine.Object GetPrefab(this UnityEngine.Object obj) =>
        
            #if UNITY_EDITOR
            
            #if UNITY_2018_2_OR_NEWER
                 PrefabUtility.GetCorrespondingObjectFromSource(obj);
            #else
                 PrefabUtility.GetPrefabParent(obj);
            #endif
            #else
                 null;
            #endif
        

        public static void UpdatePrefab(this GameObject gameObject)
        {
#if PEGI && UNITY_EDITOR

#if UNITY_2018_3_OR_NEWER
            var pf = gameObject.IsPrefab() ? gameObject :
                 PrefabUtility.GetPrefabInstanceHandle(gameObject);
#else
            var pf = PrefabUtility.GetPrefabObject(gameObject);
#endif
            if (pf)
            {
                // SavePrefabAsset, SaveAsPrefabAsset, SaveAsPrefabAssetAndConnect'
#if UNITY_2018_3_OR_NEWER
                if (!pf)
                    Debug.LogError("Handle is null");
                else
                {
                    var path = AssetDatabase.GetAssetPath(pf);//PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pf);

                    if (path.IsNullOrEmpty())
                        "Path is null, Update prefab manually".showNotificationIn3D_Views();
                    else
                        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path, InteractionMode.AutomatedAction);
                }
#else
                PrefabUtility.ReplacePrefab(gameObject, gameObject.GetPrefab(), ReplacePrefabOptions.ConnectToPrefab);
                   (gameObject.name + " prefab Updated").showNotificationIn3D_Views();
#endif

            }
            else
            {
                (gameObject.name + " Not a prefab").showNotificationIn3D_Views();
            }
            gameObject.SetToDirty();
#endif
        }

        public static bool IsPrefab(this GameObject go) => go.scene.name == null;

        public static string SetUniqueObjectName(this UnityEngine.Object obj, string folderName, string extension)
        {

            folderName = Path.Combine("Assets",folderName); //.AddPreSlashIfNotEmpty());
            var name = obj.name;
            var fullpath =
            #if UNITY_EDITOR
            AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderName, name) + extension);
            #else
            Path.Combine(folderName,  name) + extension;
            #endif
            name = fullpath.Substring(folderName.Length);
            name = name.Substring(0, name.Length - extension.Length);
            obj.name = name;

            return fullpath;
        }

        public static string GetAssetFolder(this UnityEngine.Object obj)
        {
        #if UNITY_EDITOR

            var parentObject = obj.GetPrefab();
            if (parentObject)
                obj = parentObject;

            var path = AssetDatabase.GetAssetPath(obj);

            if (path.IsNullOrEmpty()) return "";
            
            var ind = path.LastIndexOf("/", StringComparison.Ordinal);

            if (ind > 0)
                path = path.Substring(0, ind);

            return path;
            
        #else
            return "";
        #endif
        }

        public static bool SavedAsAsset(this UnityEngine.Object go) =>
        #if UNITY_EDITOR
            !AssetDatabase.GetAssetPath(go).IsNullOrEmpty();
        #else
            true;
        #endif

        public static string GetGuid(this UnityEngine.Object obj, string current)
        {
            if (!obj)
                return current;

        #if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(obj);
            if (!path.IsNullOrEmpty())
                current = AssetDatabase.AssetPathToGUID(path);
        #endif
            return current;
        }

        public static T GuidToAsset<T>(string guid) where T : UnityEngine.Object
        #if UNITY_EDITOR
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return path.IsNullOrEmpty() ? null : AssetDatabase.LoadAssetAtPath<T>(path);
            }
        #else
               => null;
        #endif

        public static string GetGuid(this UnityEngine.Object obj) =>   obj.GetGuid(null);

        public static void AddResourceIfNew(this List<string> l, string assetFolder, string insideAssetsFolder)
        {

            #if UNITY_EDITOR

            try
            {
                var path = Path.Combine(Application.dataPath, assetFolder, "Resources" , insideAssetsFolder);

                if (!Directory.Exists(path)) return;

                var dirInfo = new DirectoryInfo(path);

                var fileInfo = dirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);

                l = new List<string>();

                foreach (var file in fileInfo)
                {
                    var name = file.Name.Substring(0, file.Name.Length - StuffSaver.FileType.Length);
                    if (file.Extension == StuffSaver.FileType && !l.Contains(name))
                        l.Add(name);
                }

            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }

            #endif
        }

        public static void RenameAsset<T>(this T obj, string newName) where T : UnityEngine.Object
        {

            if (newName.IsNullOrEmpty() || !obj) return;
            
        #if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(obj);
            if (!path.IsNullOrEmpty())
                AssetDatabase.RenameAsset(path, newName);
        #endif
            
            obj.name = newName;

        }

        public static T DuplicateScriptableObject<T>(this T el) where T : ScriptableObject
        {
            T added = null;


        #if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(el);
            
            if (path.IsNullOrEmpty()) return null;
            
            added = ScriptableObject.CreateInstance(el.GetType()) as T;
    
            var oldName = Path.GetFileName(path);

            if (oldName.IsNullOrEmpty()) return added;
            
            path = path.Replace(oldName, "");

            var assetPathAndName =
                AssetDatabase.GenerateUniqueAssetPath(path + oldName.Substring(0, oldName.Length - 6) + ".asset");

            AssetDatabase.CreateAsset(added, assetPathAndName);

            added.name = assetPathAndName.Substring(path.Length, assetPathAndName.Length - path.Length - 6);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#else
            added = ScriptableObject.CreateInstance(el.GetType()) as T;
        #endif
        
            return added;
        }

        public static T CreateAsset_SO<T>(this List<T> objs, string path, string name) where T : ScriptableObject => CreateAsset_SO<T, T>(path, name, objs);

        #if UNITY_EDITOR
        public static void DuplicateResource(string assetFolder, string insideAssetFolder, string oldName, string newName)
        {
            var path = Path.Combine("Assets", assetFolder, "Resources", insideAssetFolder);
            AssetDatabase.CopyAsset(Path.Combine(path, oldName) + StuffSaver.FileType, Path.Combine(path, newName) + StuffSaver.FileType);
        }
        #endif

        public static T CreateAsset_SO<T>(this List<T> list, string path, string name, Type t) where T : ScriptableObject {

            var obj = CreateAsset_SO<T,T>(path, name);

            list.Add(obj);

            return obj;
        }

        public static T CreateAsset_SO<T, TG>(string path, string name, List<TG> optionalList = null) where T : TG where TG : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();

            #if PEGI

            var nm = asset as IGotName;
            if (nm != null)
                nm.NameForPEGI = name;

            if (optionalList != null)
            {

                var ind = asset as IGotIndex;

                if (ind != null)
                {
                    var maxInd = 0;
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
                path = Path.Combine("Assets", path);

            var fullPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + path;
            try
            {
                Directory.CreateDirectory(fullPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("Couldn't create Directory {0} : {1}".F(fullPath, ex.ToString()));
                return null;
            }

            var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, name+ ".asset"));

            try
            {

                AssetDatabase.CreateAsset(asset, assetPathAndName);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
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
                var path = Path.Combine("Assets", assetFolder, "Resources", insideAssetFolderAndName) + StuffSaver.FileType;
                AssetDatabase.DeleteAsset(path);
            }
            catch (Exception e)
            {
                Debug.Log("Oh No " + e.ToString());
            }
        #endif
        }

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

        #region Input MGMT
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
        #endregion

        #region Spin Around

        private static Vector2 _camOrbit;
        private static Vector3 _spinningAround;
        private static float _orbitDistance = 0;
        private static bool _orbitingFocused;

        private static float _spinStartTime = 0;
                // Use this for initialization
                public static void SpinAround(Vector3 pos, Transform cameraman)
                {
                    if (Input.GetMouseButtonDown(2))
                    {
                        var before = cameraman.rotation;//cam.transform.rotation;
                        cameraman.transform.LookAt(pos);
                        var rotE = cameraman.rotation.eulerAngles;
                        _camOrbit.x = rotE.y;
                        _camOrbit.y = rotE.x;
                        _orbitDistance = (pos - cameraman.position).magnitude;
                        _spinningAround = pos;
                        cameraman.rotation = before;
                        _orbitingFocused = false;
                        _spinStartTime = Time.time;
                    }

                    if (Input.GetMouseButtonUp(2))
                        _orbitDistance = 0;

                    if ((!(Math.Abs(_orbitDistance) > float.Epsilon)) || !Input.GetMouseButton(2)) return;
                    
                    _camOrbit.x += Input.GetAxis("Mouse X") * 5;
                    _camOrbit.y -= Input.GetAxis("Mouse Y") * 5;

                    if (_camOrbit.y <= -360)
                        _camOrbit.y += 360;
                    if (_camOrbit.y >= 360)
                        _camOrbit.y -= 360;

                    var rot = Quaternion.Euler(_camOrbit.y, _camOrbit.x, 0);
                    var campos = rot *
                                 (new Vector3(0.0f, 0.0f, -_orbitDistance)) +
                                 _spinningAround;

                    cameraman.position = campos;
                    if ((Time.time - _spinStartTime) < 0.2f) return;
                        
                    if (!_orbitingFocused)
                    {
                        cameraman.transform.rotation = cameraman.rotation.Lerp_bySpeed(rot, 300);
                        if (Quaternion.Angle(cameraman.rotation, rot) < 1)
                            _orbitingFocused = true;
                    }
                    else cameraman.rotation = rot;
                }

        #endregion

        #region Textures
        #region Material MGMT
        public static bool HasTag(this Material mat, string tag) => mat && !mat.GetTag(tag, false, null).IsNullOrEmpty();
        
        public static string TagValue(this Material mat, string tag) => mat ? mat.GetTag(tag, false, null) : null;
        
        public static Material MaterialWhatever(this Renderer renderer) =>
                !renderer ? null : (Application.isPlaying ? renderer.material : renderer.sharedMaterial);
        
        #if UNITY_EDITOR
        private static List<string> GetFields(this Material m, MaterialProperty.PropType type)
                {
                    var fNames = new List<string>();

                    if (!m) return fNames;

                    var mat = new Material[1];
                    mat[0] = m;
                    MaterialProperty[] props;

                    try
                    {
                        props = MaterialEditor.GetMaterialProperties(mat);
                    }
                    catch
                    {
                        return fNames = new List<string>();
                    }

                    if (props == null) return fNames;
                    fNames.AddRange(from p in props where p.type == type select p.name);

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

        public static List<string> MyGetTexturePropertiesNames(this Material m)
        {
#if UNITY_EDITOR
            
             return m.GetFields(MaterialProperty.PropType.Texture);
       
#else
            return new List<string>();
#endif
        }
        
        public static List<ShaderProperty.TextureValue> MyGetTextureProperties(this Material m)
        {
        #if UNITY_EDITOR
            {
                var lst = new List<ShaderProperty.TextureValue>();
                foreach (var n in m.GetFields(MaterialProperty.PropType.Texture))
                    lst.Add(new ShaderProperty.TextureValue(n));

                return lst;
            }
        #else
            return new List<ShaderProperty.TextureValue>();
        #endif
        }

                public static List<string> GetColorProperties(this Material m)
                {

        #if UNITY_EDITOR
                    return m.GetFields(MaterialProperty.PropType.Color);
        #else
                    return new List<string>();
        #endif

                }

   
                
        #endregion

        #region Texture MGMT
        public static Color[] GetPixels(this Texture2D tex, int width, int height)
        {

            if ((tex.width == width) && (tex.height == height))
                return tex.GetPixels();

            var dst = new Color[width * height];

            var src = tex.GetPixels();

            var dX = (float)tex.width / (float)width;
            var dY = (float)tex.height / (float)height;

            for (var y = 0; y < height; y++)
            {
                var dstIndex = y * width;
                var srcIndex = ((int)(y * dY)) * tex.width;
                for (var x = 0; x < width; x++)
                    dst[dstIndex + x] = src[srcIndex + (int)(x * dX)];

            }


            return dst;
        }


        public static void CopyFrom(this Texture2D tex, RenderTexture rt) {
            if (!rt || !tex){
#if UNITY_EDITOR
                Debug.Log("Texture is null");
#endif
                return;
            }

            var curRT = RenderTexture.active;

            RenderTexture.active = rt;

            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

            RenderTexture.active = curRT;

        }

        public static bool TextureHasAlpha (this Texture2D tex)
        {
            
                if (!tex) return false;

                // May not cover all cases

                switch (tex.format)
                {
                    case TextureFormat.ARGB32: return true;
                    case TextureFormat.RGBA32: return true;
                    case TextureFormat.ARGB4444: return true;
                    case TextureFormat.BGRA32: return true;
                    case TextureFormat.PVRTC_RGBA4: return true;
                    case TextureFormat.RGBAFloat: return true;
                    case TextureFormat.RGBAHalf: return true;
                }
                return false;
            
        }

        #endregion

        #region Texture Import Settings

                public static bool IsColorTexture(this Texture2D tex)
                {
        #if UNITY_EDITOR
                    if (!tex) return true;

                    TextureImporter importer = tex.GetTextureImporter();

                    if (importer != null)
                        return importer.sRGBTexture;
        #endif
                    return true;
                }

                public static Texture2D CopyImportSettingFrom(this Texture2D dest, Texture2D original)
                {
        #if UNITY_EDITOR
                    var dst = dest.GetTextureImporter();
                    var org = original.GetTextureImporter();

                    if (!dst || !org) return dest;

                    var maxSize = Mathf.Max(original.width, org.maxTextureSize);

                    var needReimport = (dst.wrapMode != org.wrapMode) ||
                                        (dst.sRGBTexture != org.sRGBTexture) ||
                                        (dst.textureType != org.textureType) ||
                                        (dst.alphaSource != org.alphaSource) ||
                                        (dst.maxTextureSize < maxSize) ||
                                        (dst.isReadable != org.isReadable) ||
                                        (dst.textureCompression != org.textureCompression) ||
                                        (dst.alphaIsTransparency != org.alphaIsTransparency);

                    if (!needReimport)
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
        #endif

                    return dest;
                }
        

        #if UNITY_EDITOR

                public static TextureImporter GetTextureImporter(this Texture2D tex) => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;
        
                public static bool HadNoMipmaps(this TextureImporter importer)
                {

                    var needsReimport = false;

                    if (importer.mipmapEnabled == false)
                    {
                        importer.mipmapEnabled = true;
                        needsReimport = true;
                    }

                    return needsReimport;

                }

                public static void Reimport_IfMarkedAsNOrmal(this Texture2D tex) {
                    if (!tex) return;

                    var importer = tex.GetTextureImporter();

                    if ((importer != null) && (importer.WasMarkedAsNormal()))
                        importer.SaveAndReimport();
                }

                public static bool WasMarkedAsNormal(this TextureImporter importer, bool convertToNormal = false)
                {

                    var needsReimport = false;

                    if ((importer.textureType == TextureImporterType.NormalMap) != convertToNormal)
                    {
                        importer.textureType = convertToNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                        needsReimport = true;
                    }

                    return needsReimport;

                }
        
                public static void Reimport_IfClamped(this Texture2D tex)
                {
                    if (!tex) return;

                    var importer = tex.GetTextureImporter();

                    if ((importer != null) && (importer.WasClamped()))
                        importer.SaveAndReimport();
                }
                public static bool WasClamped(this TextureImporter importer)
                {

                    var needsReimport = false;


                    if (importer.wrapMode != TextureWrapMode.Repeat)
                    {
                        importer.wrapMode = TextureWrapMode.Repeat;
                        needsReimport = true;
                    }

                    return needsReimport;

                }

                public static void Reimport_IfNotReadale(this Texture2D tex)
                {
                    if (!tex) return;

                    var importer = tex.GetTextureImporter();

                    if (importer != null && importer.WasNotReadable())
                    {
                        importer.SaveAndReimport();
                        Debug.Log("Reimporting to make readable");
                    }
                }
                public static bool WasNotReadable(this TextureImporter importer)
                {

                    var needsReimport = false;



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
                    if (!tex) return;

                    var importer = tex.GetTextureImporter();

                    if ((importer != null) && (importer.WasWrongIsColor(value)))
                        importer.SaveAndReimport();
                }
                public static bool WasWrongIsColor(this TextureImporter importer, bool isColor)
                {

                    var needsReimport = false;

                    if (importer.sRGBTexture != isColor)
                    {
                        importer.sRGBTexture = isColor;
                        needsReimport = true;
                    }

                    return needsReimport;
                }

                public static void Reimport_IfNotSingleChanel(this Texture2D tex)
                {
                    if (!tex) return;
            
                    var importer = tex.GetTextureImporter();

                    if ((importer != null) && (importer.WasNotSingleChanel()))
                        importer.SaveAndReimport();
            
                }
        
                public static bool WasNotSingleChanel(this TextureImporter importer)
                {

                    var needsReimport = false;


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

                    if (!tex) return;

                    var importer = tex.GetTextureImporter();

                    if ((importer != null) && (importer.WasAlphaNotTransparency()))
                        importer.SaveAndReimport();
            

                }
                public static bool WasAlphaNotTransparency(this TextureImporter importer)
                {

                    var needsReimport = false;

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
                    if (!tex) return;

                    var importer = tex.GetTextureImporter();

                    if ((importer != null) && (importer.WasWrongMaxSize(width)))
                        importer.SaveAndReimport();
            
                }
                public static bool WasWrongMaxSize(this TextureImporter importer, int width)
                {

                    var needsReimport = false;

                    if (importer.maxTextureSize < width)
                    {
                        importer.maxTextureSize = width;
                        needsReimport = true;
                    }

                    return needsReimport;

                }


        #endif
        #endregion

        #region Texture Saving

                public static string GetPathWithout_Assets_Word(this Texture2D tex)
                {
        #if UNITY_EDITOR
                    var path = AssetDatabase.GetAssetPath(tex);
                    return string.IsNullOrEmpty(path) ? null : path.Replace("Assets", "");
        #else
                    return null;
        #endif
                }
        
        #if UNITY_EDITOR
                public static void SaveTexture(this Texture2D tex)
                {

                    var bytes = tex.EncodeToPNG();

                    var dest = AssetDatabase.GetAssetPath(tex).Replace("Assets", "");

                    File.WriteAllBytes(Application.dataPath + dest, bytes);

                    AssetDatabase.Refresh();
                }

                public static string GetAssetPath(this Texture2D tex) => AssetDatabase.GetAssetPath(tex);
        
                public static Texture2D RewriteOriginalTexture_NewName(this Texture2D tex, string name)
                {
                    if (name == tex.name)
                        return tex.RewriteOriginalTexture();

                    var bytes = tex.EncodeToPNG();

                    var dest = tex.GetPathWithout_Assets_Word();
                    dest = dest.ReplaceLastOccurrence(tex.name, name);
                    if (string.IsNullOrEmpty(dest)) return tex;

                    File.WriteAllBytes(Application.dataPath + dest, bytes);

                    AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);

                    var result = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + dest, typeof(Texture2D));

                    result.CopyImportSettingFrom(tex);

                    AssetDatabase.DeleteAsset(tex.GetAssetPath());

                    AssetDatabase.Refresh();
            
                    return result;
                }

                public static Texture2D RewriteOriginalTexture(this Texture2D tex) {
  
                    var dest = tex.GetPathWithout_Assets_Word();
                    if (dest.IsNullOrEmpty())
                        return tex;

                    var bytes = tex.EncodeToPNG();

                    File.WriteAllBytes(Application.dataPath + dest, bytes);

                    AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);

                    var result = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets" + dest, typeof(Texture2D));

                    result.CopyImportSettingFrom(tex);

                    return result;
                }

                public static Texture2D SaveTextureAsAsset(this Texture2D tex, string folderName, ref string textureName, bool saveAsNew)
                {

                    var bytes = tex.EncodeToPNG();


                    var folderPath = Path.Combine(Application.dataPath, folderName);
                    Directory.CreateDirectory(folderPath);

                    var fileName = textureName + ".png";

                    var relativePath = Path.Combine("Assets", folderName, fileName);

                    if (saveAsNew)
                        relativePath = AssetDatabase.GenerateUniqueAssetPath(relativePath);

                    var fullPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + relativePath;

                    File.WriteAllBytes(fullPath, bytes);

                    AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);
               
                    var result = (Texture2D)AssetDatabase.LoadAssetAtPath(relativePath, typeof(Texture2D));

                    textureName = result.name;

                    result.CopyImportSettingFrom(tex);

                    return result;
                }

                public static Texture2D CreatePngSameDirectory(this Texture2D diffuse, string newName) =>
                     CreatePngSameDirectory(diffuse, newName, diffuse.width, diffuse.height);
        
                public static Texture2D CreatePngSameDirectory(this Texture2D diffuse, string newName, int width, int height)
                {

                    if (!diffuse) return null;
                    
                    var result = new Texture2D(width, height, TextureFormat.RGBA32, true, false);

                    diffuse.Reimport_IfNotReadale();

                    var pixels = diffuse.GetPixels(width, height);
                    pixels[0].a = 0.5f;

                    result.SetPixels(pixels);

                    var bytes = result.EncodeToPNG();

                    var dest = AssetDatabase.GetAssetPath(diffuse).Replace("Assets", "");

                    var extension = dest.Substring(dest.LastIndexOf(".", StringComparison.Ordinal) + 1);

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
        #endif
        #endregion

        #region Terrain Layers
        public static void SetSplashPrototypeTexture(this Terrain terrain, Texture2D tex, int index)
        {

            if (!terrain) return;

        #if UNITY_2018_3_OR_NEWER
            var l = terrain.terrainData.terrainLayers;

            if (l.Length > index)
                l[index].diffuseTexture = tex;
        #else

            SplatPrototype[] newProtos = terrain.GetCopyOfSplashPrototypes();

            if (newProtos.Length <= index)
            {
                CsharpUtils.AddAndInit(ref newProtos, index + 1 - newProtos.Length);
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
                    {
                        var sp = l[ind];
                        return sp != null ? l[ind].diffuseTexture : null;
                    }
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

                    if (!terrain) return null;

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
        #endregion

        #region Shaders

        public static void ToggleShaderKeywords(this Material mat, bool value, string ifTrue, string iFalse)
        {
            if (mat)
            {
                mat.DisableKeyword(value ? iFalse : ifTrue);
                mat.EnableKeyword(value ? ifTrue : iFalse);
            }
        }

        public static void SetShaderKeyword(this Material mat, string keyword, bool isTrue)
        {
            if (!keyword.IsNullOrEmpty() && mat)
            {
                if (isTrue)
                    mat.EnableKeyword(keyword);
                else
                    mat.DisableKeyword(keyword);
            }
        }

        public static void ToggleShaderKeywords(bool value, string ifTrue, string iFalse)
        {
            Shader.DisableKeyword(value ? iFalse : ifTrue);
            Shader.EnableKeyword(value ? ifTrue : iFalse);
        }

        public static void SetShaderKeyword(string keyword, bool isTrue)
        {
            if (!keyword.IsNullOrEmpty()) {
                if (isTrue)
                    Shader.EnableKeyword(keyword);
                else
                    Shader.DisableKeyword(keyword);
            }
        }

        public static bool GetKeyword(this Material mat, string keyword) => Array.IndexOf(mat.shaderKeywords, keyword) != -1;
        

        #endregion

        #region Meshes

        public static void SetColor(this MeshFilter mf, Color col) {

            if (mf) {

                var m = mf.mesh;

                var cols = new Color[m.vertexCount]; 

                for (int i = 0; i < m.vertexCount; i++)
                    cols[i] = col;

                mf.mesh.colors = cols;

            }
        }

        public static void SetAlpha(this MeshFilter mf, float alpha)
        {
            if (!mf) return;

            var mesh = mf.mesh;
            
            var m = mesh;

            var cols = mesh.colors;
            
            if (cols.IsNullOrEmpty())
                cols = new Color[m.vertexCount];

            for (var i = 0; i < m.vertexCount; i++)
                cols[i].a = alpha;

            mf.mesh.colors = cols;
        }
        
        public static int GetSubMeshNumber(this Mesh m, int triangleIndex)
        {
            if (!m) return 0;
            
            if (m.subMeshCount == 1)
                return 0;

            if (!m.isReadable)
            {
                Debug.Log("Mesh {0} is not readable. Enable for submesh material editing.".F(m.name));
                return 0;
            }

            var triangles = new int[] {
                m.triangles[triangleIndex * 3],
                m.triangles[triangleIndex * 3 + 1],
                m.triangles[triangleIndex * 3 + 2] };

            for (var i = 0; i < m.subMeshCount; i++)
            {

                if (i == m.subMeshCount - 1)
                    return i;

                var subMeshTris = m.GetTriangles(i);
                for (var j = 0; j < subMeshTris.Length; j += 3)
                    if (subMeshTris[j] == triangles[0] &&
                        subMeshTris[j + 1] == triangles[1] &&
                        subMeshTris[j + 2] == triangles[2])
                        return i;
            }

            return 0;
        }

        public static void AssignMeshAsCollider(this MeshCollider c, Mesh mesh)
        {
            c.sharedMesh = null;
            c.sharedMesh = mesh;
        }

        #endregion
    }

    public class PerformanceTimer : IPEGI_ListInspect, IGotDisplayName
    {
        private readonly string _name;
        private float _timer = 0;
        private double _perIntervalCount = 0;
        private double _max = 0;
        private double _min = float.PositiveInfinity;
        private double _average = 0;
        private double _totalCount = 0;
        private readonly float _intervalLength = 1f;
        
        public void Update(float add = 0)
        {
            _timer += Time.deltaTime;
            if (Math.Abs(add) > float.Epsilon)
                Add(add);

            if (_timer <= _intervalLength) return;
            

            _timer -= _intervalLength;

            _max = Mathf.Max((float)_perIntervalCount, (float)_max);
            _min = Mathf.Min((float)_perIntervalCount, (float)_min);

            _totalCount += 1;

            var portion = 1d / _totalCount;
            _average = _average * (1d - portion) + _perIntervalCount * portion;

            _perIntervalCount = 0;

        }

        public void Add(float result = 1) => _perIntervalCount += result;
        
        public void ResetStats()
        {
            _timer = 0;
            _perIntervalCount = 0;
            _max = 0;
            _min = float.PositiveInfinity;
            _average = 0;
            _totalCount = 0;
        }

#region Inspector

        public string NameForDisplayPEGI => "Avg: {0}/{1}sec [{2} - {3}] ({4}) ".F(((float)_average).ToString("0.00"),  (Math.Abs(_intervalLength - 1d) > float.Epsilon) ? _intervalLength.ToString("0") : "", (int)_min, (int)_max, (int)_totalCount);

#if PEGI
        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            if (icon.Refresh.Click("Reset Stats"))
                ResetStats();

         //   "_name interval".edit(80, ref intervalLength);

            NameForDisplayPEGI.write();

          
            return false;
        }
#endif
#endregion

        public PerformanceTimer(string name = "Timer", float interval = 1f)
        {
            _name = name;
            _intervalLength = interval;
        }
    }
    
    public class ChillLogger : IGotDisplayName
    {
        private bool _logged = false;
        private bool _disabled = false;
        private float _lastLogged = 0;
        private int _calls;
        private readonly string message = "error";

        public string NameForDisplayPEGI => message + (_disabled ? " Disabled" : " Enabled");

        public ChillLogger(string msg, bool logInBuild = false)
        {
            message = msg;
#if !UNITY_EDITOR
            _disabled = (!logInBuild);
#else
            _disabled = false;
#endif
        }

        public ChillLogger()
        {

        }

        public void Log_Now(string msg, bool asError, UnityEngine.Object obj = null)
        {

          //  if (disabled)
              //  return;

            if (msg == null)
                msg = message;

            if (_calls > 0)
                msg += " [+ {0} calls]".F(_calls);

            if (_lastLogged > 0)
                msg += " [{0} s. later]".F(Time.time - _lastLogged);
            else
                msg += " [at {0}]".F(Time.time);

            if (asError)
                Debug.LogError(msg, obj);
            else
                Debug.Log(msg, obj);

            _lastLogged = Time.time;
            _calls = 0;
            _logged = true;
        }

        public void Log_Once(string msg = null, bool asError = true, UnityEngine.Object obj = null)
        {

            if (!_logged)
                Log_Now(msg, asError, obj);
            else
                _calls++;
        }

        public void Log_Interval(float seconds, string msg = null, bool asError = true, UnityEngine.Object obj = null)
        {

            if (!_logged || (Time.time - _lastLogged > seconds))
                Log_Now(msg, asError, obj);
            else
                _calls++;
        }

        public void Log_Every(int callCount, string msg = null, bool asError = true, UnityEngine.Object obj = null)
        {

            if (!_logged || (_calls > callCount))
                Log_Now(msg, asError, obj);
            else
                _calls++;
        }

    }

    public class TextureDownloadManager : IPEGI {
        readonly List<WebRequestMeta> _loadedTextures = new List<WebRequestMeta>();

        class WebRequestMeta : IGotName, IPEGI_ListInspect, IPEGI {
            private UnityWebRequest _request;
            private string _address;
            public string URL => _address;
            private Texture _texture;
            private bool _failed = false;

            public string NameForPEGI { get { return _address; } set { _address = value; } }

            private Texture Take() {
                var tmp = _texture;
                _texture = null;
                _failed = false;
                DisposeRequest();
                return tmp;
            }

            public bool TryGetTexture(out Texture tex, bool remove = false) {
                tex = _texture;

                if (remove && _texture) Take();

                if (_failed) return true;

                if (_request != null) {
                    if (_request.isNetworkError || _request.isHttpError) {

                        _failed = true;

#if UNITY_EDITOR
                        Debug.Log(_request.error);
#endif
                        DisposeRequest();
                        return true;
                    }

                    if (_request.isDone) {
                        if (_texture)
                            _texture.DestroyWhatever();
                        _texture = ((DownloadHandlerTexture)_request.downloadHandler).texture;
                        DisposeRequest();
                        tex = _texture;

                        if (remove && _texture)
                            Take();
                    }
                    else return false;
                }
                else if (!_texture) Start();

                return true;
            }

            void Start() {
                _request?.Dispose();
                _request = UnityWebRequestTexture.GetTexture(_address);
                _request.SendWebRequest();
                _failed = false;
                Debug.Log("Loading {0}".F(_address));
            }

            public WebRequestMeta(string URL) {
                _address = URL;
                Start();
            }

            private void DisposeRequest() {
                _request?.Dispose();
                _request = null;
            }

            public void Dispose() {
                if (_texture)
                    _texture.DestroyWhatever();

                DisposeRequest();
            }

#region Inspector
#if PEGI
            public bool PEGI_inList(IList list, int ind, ref int edited)
            {
                var changed = false;
                Texture tex;
                TryGetTexture(out tex);

                if (_request != null)
                    "Loading".write(60);
                if (_failed)
                    "Failed".write(50);

                if (_texture) {
                    if (icon.Refresh.Click())
                        Start();

                    if (_texture.Click())
                        edited = ind;

                } else {

                    if (_failed) {
                        if (icon.Refresh.Click("Failed"))
                            Start();
                        "Failed ".F(_address).write(40);
                    }
                    else {
                        icon.Active.write();
                        "Loading ".write(40);
                    }

                }
                _address.write();
                return changed;
            }

            public bool Inspect()
            {
                Texture tex;
                TryGetTexture(out tex);

                if (_texture)
                    pegi.write(_texture, 200);

                return false;
            }
#endif
#endregion
        }

        public string GetURL(int ind) {
            var el = _loadedTextures.TryGet(ind);
            return (el == null) ? "" : el.URL;
        }

        public bool TryGetTexture(int ind, out Texture tex, bool remove = false) {
            tex = null;
            var el = _loadedTextures.TryGet(ind);
            return (el != null) ? el.TryGetTexture(out tex, remove) : true;
        }

        public int StartDownload(string address) {
            var el = _loadedTextures.GetByIGotName(address);

            if (el == null) {
                el = new WebRequestMeta(address);
                _loadedTextures.Add(el);
            }

            return _loadedTextures.IndexOf(el);
        }

        public void Dispose() {
            foreach (var t in _loadedTextures)
                t.Dispose();

            _loadedTextures.Clear();
        }

#region Inspector
#if PEGI
        int inspected = -1;
        string tmp = "";
        public bool Inspect()
        {

            var changed = "Textures and Requests".write_List(_loadedTextures, ref inspected);

            "URL".edit(30, ref tmp);
            if (tmp.Length > 0 && icon.Add.Click().nl())
                StartDownload(tmp);

            return changed;
        }
#endif
#endregion
    }

    public static class ShaderProperty
    {
        
        public abstract class BaseIndexHolder : AbstractStd, IGotDisplayName, IPEGI_ListInspect {
            protected int id;
            private string _name;


            public override int GetHashCode() => id;

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                var bi = obj as BaseIndexHolder;

                return bi != null ? bi.id == id : _name.SameAs(obj.ToString());
            }
            private void UpdateIndex() =>  id = Shader.PropertyToID(_name);
            
            protected BaseIndexHolder() { }
            protected BaseIndexHolder(string name) {
                _name = name;
                UpdateIndex();
            }

            public string NameForDisplayPEGI => _name;

            public override string ToString() => _name;
            
            
            #if PEGI
            public bool PEGI_inList(IList list, int ind, ref int edited)
            {
                "[{0}] {1}".F(id, _name).write();
                if (icon.Refresh.Click("Update Index (Shouldn't be necessary)"))
                    UpdateIndex();

                return false;
            }
            #endif

            public override StdEncoder Encode() => new StdEncoder().Add_String("n", _name);
            
            
            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "n": _name = data; UpdateIndex(); break;
                    default: return false;
                }

                return true;
            }
        }

        public static bool Has<T>(this Material mat, T property) where T : BaseIndexHolder =>
            mat.HasProperty(property.GetHashCode());
        
        
        #region Float
        public class FloatValue : BaseIndexHolder
        {
            
            public float lastValue = 0;
            
            public Material SetOn(Material material, float value)
            {
                lastValue = value;

                if (material)
                    material.SetFloat(id, value);

                return material;
            }

            public Material SetOn(Material material)
            {
                material.SetFloat(id, lastValue);
                return material;
            }

            public void SetGlobal(float value) => GlobalValue = value;

            public float Get(Material mat) => mat.GetFloat(id);

            public float GlobalValue {
                get { return  Shader.GetGlobalFloat(id); }
                set { Shader.SetGlobalFloat(id, value); lastValue = value; }
            }

            public FloatValue(string name) : base(name) { }
            
            public FloatValue() { }
        }

        public static Material Set(this Material mat, FloatValue property, float value) => property.SetOn(mat, value);
        
        public static Material Set(this Material mat, FloatValue property) => property.SetOn(mat);

        public static float Get(this Material mat, FloatValue property) => property.Get(mat);
        #endregion

        #region Color
        public class ColorValue : BaseIndexHolder
        {

            public Color lastValue = Color.gray;
            
            public void SetOn(Material material, Color value) {
                lastValue = value;
                if (material)
                    material.SetColor(id, value);
            } 
            
            public void SetOn(Material material) => material.SetColor(id, lastValue);

            public void SetGlobal(Color value) => GlobalValue = value;
            
            public Color Get(Material mat) => mat.GetColor(id);

            public Color GlobalValue {
                get { return  Shader.GetGlobalColor(id); }
                set { Shader.SetGlobalColor(id, value); }
            }

            public ColorValue(string name) : base(name) { }
            
            public ColorValue() { }
        }

        public static Color Get(this Material mat, ColorValue property) => property.Get(mat);

        public static void Set(this Material mat, ColorValue property, Color value) => property.SetOn(mat, value);
        
        public static void Set(this Material mat, ColorValue property) => property.SetOn(mat);
        #endregion

        #region Vector
        public class VectorValue : BaseIndexHolder
        {

            private Vector4 _lastValue = Vector4.zero;
            
            public void SetOn(Material material, Vector4 value) {
                _lastValue = value;
                if (material)
                    material.SetVector(id, value);
            } 
            
            public void SetOn(Material material) => material.SetColor(id, _lastValue);

            public void SetGlobal(Vector4 value) => GlobalValue = value;

            public Vector4 Get(Material mat) => mat.GetVector(id);

            public Vector4 GlobalValue {
                get { return  Shader.GetGlobalVector(id); }
                set { Shader.SetGlobalVector(id, value); }
            }

            public VectorValue(string name) : base(name) { }
            
            public VectorValue()  { }
        }

        public static Vector4 Get(this Material mat, VectorValue property) => property.Get(mat);
        
        public static void Set(this Material mat, VectorValue property, Vector4 value) => property.SetOn(mat, value);
        
        public static void Set(this Material mat, VectorValue property) => property.SetOn(mat);
        #endregion

        #region Texture
        public class TextureValue : BaseIndexHolder
        {

            private Texture _lastValue = null;

            private List<string> _usageTags = new List<string>();
            
            public TextureValue AddUsageTag(string value) {
                 if (!_usageTags.Contains(value)) _usageTags.Add(value);
                return this;
            }

            public bool HasUsageTag(string tag) => _usageTags.Contains(tag);
            

            public override StdEncoder Encode() => new StdEncoder()
                .Add("b", base.Encode())
                .Add("tgs", _usageTags);

            public override bool Decode(string tg, string data) {
                switch (tg) {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "tgs":  data.Decode_List(out _usageTags); break;
                    default: return false;
                }
                return true;
            }


            public void SetOn(Material material, Texture value) {
                _lastValue = value;
                if (material)
                    material.SetTexture(id, value);
            } 
            
            public void SetOn(Material material) => material.SetTexture(id, _lastValue);

            public void SetGlobal(Texture value) => GlobalValue = value;

            public Texture GlobalValue {
                get { return  Shader.GetGlobalTexture(id); }
                set { Shader.SetGlobalTexture(id, value); }
            }

            public Texture GetFrom( Material mat) => mat.GetTexture(id);

            public Vector2 GetOffset (Material mat) => mat ? mat.GetTextureOffset(id) : Vector2.zero;
            
            public Vector2 GetTiling(Material mat) => mat ? mat.GetTextureScale(id) : Vector2.one;

            public void SetOffset(Material mat, Vector2 value)
            {
                if (mat)
                     mat.SetTextureOffset(id, value);
            }

            public void SetTiling(Material mat, Vector2 value)
            {
                if (mat)
                     mat.SetTextureScale(id, value);
            }

            public TextureValue(string name, string tag) : base(name) { AddUsageTag(tag);  }

            public TextureValue(string name) : base(name) { }
            
            public TextureValue()  { }
        }

        public static Texture Get(this Material mat, TextureValue property) => property.GetFrom(mat);

        public static void Set(this Material mat, TextureValue property, Texture value) => property.SetOn(mat, value);
        
        public static void Set(this Material mat, TextureValue property) => property.SetOn(mat);

        public static Vector2 GetOffset(this Material mat, TextureValue property) => property.GetOffset(mat);

        public static Vector2 GetTiling(this Material mat, TextureValue property) => property.GetTiling(mat);

        public static void SetOffset(this Material mat, TextureValue property, Vector2 value) => property.SetOffset(mat, value);

        public static void SetTiling(this Material mat, TextureValue property, Vector2 value) => property.SetTiling(mat, value);
        #endregion
    } 

}





