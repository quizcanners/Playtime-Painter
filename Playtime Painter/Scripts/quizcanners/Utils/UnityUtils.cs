#if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IOS) 
#define QC_USE_NETWORKING // To Prevent unwanted permissions in the manifest
#endif

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using PlayerAndEditorGUI;

using Object = UnityEngine.Object;
using System.Reflection;
using System.Text;

#if QC_USE_NETWORKING 
using UnityEngine.Networking;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCannersUtilities {
    
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    public static class UnityUtils
    {

        #region External Communications

        public static void SendEmail(string to) => Application.OpenURL("mailto:{0}".F(to));

        public static void SendEmail(string email, string subject, string body) =>
            Application.OpenURL("mailto:{0}?subject={1}&body={2}".F(email, subject.MyEscapeUrl(), body.MyEscapeUrl()));

        static string MyEscapeUrl(this string url) =>
#if QC_USE_NETWORKING
            UnityWebRequest.EscapeURL(url).Replace("+", "%20");
#else
        url.Replace("+", "%20");
#endif

        public static void OpenBrowser(string address) => Application.OpenURL(address);

        #endregion

        #region Timing

        public static double TimeSinceStartup() =>
#if UNITY_EDITOR
            (!Application.isPlaying)
                ? EditorApplication.timeSinceStartup
                :
#endif
                Time.realtimeSinceStartup;

        public static bool TimePassedAbove(this double value, float interval) =>
            (TimeSinceStartup() - value) > interval;
        
        #endregion

        #region Raycasts

        public static bool RayCastGotHit(this Vector3 from, Vector3 vPos)
        {
            var ray = from - vPos;
            return Physics.Raycast(new Ray(vPos, ray), ray.magnitude);
        }

        public static bool RayCastGotHit(this Vector3 from, Vector3 vPos, float safeGap)
        {
            var ray = vPos - from;

            var magnitude = ray.magnitude - safeGap;

            return (!(magnitude <= 0)) && Physics.Raycast(new Ray(@from, ray), magnitude);
        }

        public static bool RayCastHit(this Vector3 from, Vector3 to, out RaycastHit hit)
        {
            var ray = to - from;
            return Physics.Raycast(new Ray(from, ray), out hit);
        }

        #endregion

        #region Gizmos

        public static void LineTo(this Vector3 v3A, Vector3 v3B, Color col)
        {
            Gizmos.color = col;
            Gizmos.DrawLine(v3A, v3B);
        }

        #endregion

        #region Transformations 

        public static void TrySetLocalScale<T>(this List<T> graphics, float size) where T : Graphic
        {

            foreach (var g in graphics)
                if (g)
                    g.rectTransform.localScale = Vector3.one * size;

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

        #region Rect Transform

        public static void SetPivotTryKeepPosition(this RectTransform rectTransform, float pivotX, float pivotY) =>
            rectTransform.SetPivotTryKeepPosition(new Vector2(pivotX, pivotY));

        public static void SetPivotTryKeepPosition(this RectTransform rectTransform, Vector2 pivot)
        {
            if (!rectTransform) return;
            var size = rectTransform.rect.size;
            var deltaPivot = rectTransform.pivot - pivot;
            var deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y) * rectTransform.localScale.x;
            rectTransform.pivot = pivot;
            rectTransform.localPosition -= deltaPosition;
        }

        #endregion

        #region Components & GameObjects

        public static void SetActive<T>(this List<T> list, bool to) where T : Component {
            if (list != null)
                foreach (var e in list)
                    if (e) e.gameObject.SetActive(to);
        }

        public static void SetActive(this List<GameObject> list, bool to)
        {
            if (list != null)
                foreach (var go in list)
                    if (go)
                        go.SetActive(to);
        }

        public static T Instantiate<T>(string name = null) where T : MonoBehaviour
        {
            var go = new GameObject(name.IsNullOrEmpty() ? typeof(T).ToPegiStringType() : name);
            return go.AddComponent<T>();
        }

        public static GameObject TryGetGameObjectFromObj(this object obj)
        {
            var go = obj as GameObject;

            if (go) return go;

            var cmp = obj as Component;
            if (cmp)
                go = cmp.gameObject;

            return go;
        }

        public static T TryGet_fromObj<T>(object obj) where T : class
        {

            if (obj.IsNullOrDestroyed_Obj())
                return null;

            var pgi = obj as T;

            if (pgi != null)
                return pgi;

            var go = obj.TryGetGameObjectFromObj();

            return go ? go.TryGet<T>() : null;
        }

        public static T TryGet<T>(this GameObject go) where T : class =>
            go ? go.GetComponents<Component>().OfType<T>().FirstOrDefault() : null;

        public static bool IsNullOrDestroyed_Obj(this object obj)
        {
            if (obj as UnityEngine.Object)
                return false;

            return obj == null;
        }

        public static bool TrySetAlpha_DisableIfZero(this Graphic graphic, float alpha)
        {
            if (!graphic) return false;

            var ret = graphic.TrySetAlpha(alpha);

            graphic.gameObject.SetActive(alpha > 0.01f);

            return ret;

        }

        public static bool TrySetAlpha(this Graphic graphic, float alpha)
        {
            if (!graphic) return false;

            var col = graphic.color;

            if (Math.Abs(col.a - alpha) < float.Epsilon) return false;

            col.a = alpha;
            graphic.color = col;
            return true;

        }

        public static void TrySetAlpha_DisableIfZero<T>(this List<T> graphics, float alpha) where T : Graphic
        {
            if (graphics.IsNullOrEmpty()) return;

            foreach (var g in graphics)
                g.TrySetAlpha_DisableIfZero(alpha);
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

        public static void TrySetColor_RGB<T>(this List<T> graphics, Color color) where T : Graphic
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

#if !NO_PEGI
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

        public static GameObject GetFocusedGameObject()
        {

#if UNITY_EDITOR
            var tmp = Selection.objects;
            return !tmp.IsNullOrEmpty() ? tmp[0].TryGetGameObjectFromObj() : null;
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

            var colliders = go.GetComponents<Collider>();

            foreach (var c in colliders)
                if (c.GetType() != typeof(MeshCollider))
                    c.enabled = false;

            var mc = go.GetComponent<MeshCollider>();

            if (!mc)
                mc = go.AddComponent<MeshCollider>();

            return mc;

        }

        public static Transform TryGetCameraTransform(this GameObject go, Camera cam = null)
        {

            if (Application.isPlaying)
            {
                if (!cam)
                    cam = Camera.main;
            }

#if UNITY_EDITOR
            else if (SceneView.lastActiveSceneView != null)
                cam = SceneView.lastActiveSceneView.camera;
#endif

            if (cam)
                return cam.transform;

            cam = UnityEngine.Object.FindObjectOfType<Camera>();

            return cam ? cam.transform : go.transform;
        }

        public static void SetLayerRecursively(GameObject go, int layerNumber)
        {
            foreach (var trans in go.GetComponentsInChildren<Transform>(true))
                trans.gameObject.layer = layerNumber;

        }

        public static bool IsFocused(this Object obj)
        {

#if UNITY_EDITOR
            var tmp = Selection.objects;
            if (tmp.IsNullOrEmpty() || !tmp[0])
                return false;

            return tmp[0] == obj;
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

        public static void DestroyWhateverUnityObject(this UnityEngine.Object obj)
        {
            if (!obj) return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
        }

        public static void DestroyWhatever(this Texture tex) => tex.DestroyWhateverUnityObject();

        public static void DestroyWhatever(this GameObject go) => go.DestroyWhateverUnityObject();

        public static void DestroyWhateverComponent(this Component cmp) => cmp.DestroyWhateverUnityObject();

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

        public static bool HasParameter(this Animator animator, string paramName) =>
            animator && animator.parameters.Any(param => param.name.SameAs(paramName));

        public static bool
            HasParameter(this Animator animator, string paramName, AnimatorControllerParameterType type) =>
            animator && animator.parameters.Any(param => param.name.SameAs(paramName) && param.type == type);

        #endregion

        #region Audio 

        private static Type audioUtilClass;

#if UNITY_EDITOR
        private static Type AudioUtilClass
        {
            get
            {
                if (audioUtilClass == null)
                    audioUtilClass = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

                return audioUtilClass;
            }
        }
#endif

        private static MethodInfo playClipMethod;

        private static MethodInfo setClipSamplePositionMethod;

        public static EditorAudioPlayRequest Play(this AudioClip clip, float volume = 1) =>
            Play(clip, Vector3.zero, volume);

        public static EditorAudioPlayRequest Play(this AudioClip clip, Vector3 position, float volume = 1)
        {

            var rqst = new EditorAudioPlayRequest(clip);

            if (!clip) return rqst;

#if UNITY_EDITOR
            if (playClipMethod == null)
            {
                playClipMethod = AudioUtilClass.GetMethod("PlayClip",
                    BindingFlags.Static | BindingFlags.Public,
                    null, new Type[] { typeof(AudioClip) }, null
                );
            }

            playClipMethod.Invoke(null, new object[] { clip });

#else

            AudioSource.PlayClipAtPoint(clip, position, volume);

#endif



            return rqst;
        }


        /// The clip cut function group below is my addaptation of code originally wrote by DeadlyFingers (GitHub link below)
        /// https://github.com/deadlyfingers/UnityWav

        public static AudioClip Cut(AudioClip clip, float _cutPoint)
        {
            if (!clip)
                return clip;

            return Cut(clip, _cutPoint, clip.length - _cutPoint);
        }

        public static AudioClip Cut(AudioClip sourceClip, float _cutPoint, float duration)
        {

            int targetCutPoint = Mathf.RoundToInt(_cutPoint * sourceClip.frequency) * sourceClip.channels;

            int newSampleCount = sourceClip.samples - targetCutPoint;
            float[] newSamples = new float[newSampleCount];
            sourceClip.GetData(newSamples, targetCutPoint);

            int croppedSampleCount = Mathf.Min(newSampleCount,
                Mathf.RoundToInt(duration * sourceClip.frequency) * sourceClip.channels);
            float[] croppedSamples = new float[croppedSampleCount];

            Array.Copy(newSamples, croppedSamples, croppedSampleCount);

            AudioClip newClip = AudioClip.Create(sourceClip.name, croppedSampleCount, sourceClip.channels,
                sourceClip.frequency, false);

            newClip.SetData(croppedSamples, 0);

            return newClip;
        }

        public static AudioClip Override(AudioClip newClip, AudioClip oldClip)
        {


#if UNITY_EDITOR

            const int headerSize = 44;
            UInt16 bitDepth = 16;

            MemoryStream stream = new MemoryStream();

            Write(ref stream, Encoding.ASCII.GetBytes("RIFF")); //, "ID");


            const int BlockSize_16Bit = 2; // BlockSize (bitDepth)
            int chunkSize = newClip.samples * BlockSize_16Bit + headerSize - 8;
            Write(ref stream, chunkSize); //, "CHUNK_SIZE");

            Write(ref stream, Encoding.ASCII.GetBytes("WAVE")); //, "FORMAT");

            byte[] id = Encoding.ASCII.GetBytes("fmt ");
            Write(ref stream, id); //, "FMT_ID");

            int subchunk1Size = 16; // 24 - 8
            Write(ref stream, subchunk1Size); //, "SUBCHUNK_SIZE");

            UInt16 audioFormat = 1;
            Write(ref stream, audioFormat); //, "AUDIO_FORMAT");

            var channels = newClip.channels;
            Write(ref stream, Convert.ToUInt16(channels)); //, "CHANNELS");

            var sampleRate = newClip.frequency;
            Write(ref stream, sampleRate); //, "SAMPLE_RATE");

            Write(ref stream, sampleRate * channels * bitDepth / 8); //, "BYTE_RATE");

            UInt16 blockAlign = Convert.ToUInt16(channels * bitDepth / 8);
            Write(ref stream, blockAlign); //, "BLOCK_ALIGN");

            Write(ref stream, bitDepth); //, "BITS_PER_SAMPLE");

            Write(ref stream, Encoding.ASCII.GetBytes("data")); //, "DATA_ID");

            Write(ref stream, Convert.ToInt32(newClip.samples * BlockSize_16Bit)); //, "SAMPLES");

            float[] data = new float[newClip.samples * newClip.channels];
            newClip.GetData(data, 0);

            MemoryStream dataStream = new MemoryStream();
            int x = sizeof(Int16);
            Int16 maxValue = Int16.MaxValue;
            int i = 0;
            while (i < data.Length)
            {
                dataStream.Write(BitConverter.GetBytes(Convert.ToInt16(data[i] * maxValue)), 0, x);
                ++i;
            }

            Write(ref stream, dataStream.ToArray()); //, "DATA");

            dataStream.Dispose();


            var path = AssetDatabase.GetAssetPath(oldClip);

            File.WriteAllBytes(path, stream.ToArray());

            stream.Dispose();

            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
#else

            return newClip;
#endif

        }

        private static int Write(ref MemoryStream stream, short val) => Write(ref stream, BitConverter.GetBytes(val));

        private static int Write(ref MemoryStream stream, int val) => Write(ref stream, BitConverter.GetBytes(val));

        private static int Write(ref MemoryStream stream, ushort val) => Write(ref stream, BitConverter.GetBytes(val));

        private static int Write(ref MemoryStream stream, byte[] bytes)
        {
            int count = bytes.Length;
            stream.Write(bytes, 0, count);
            return count;
        }

        public class EditorAudioPlayRequest
        {

            public AudioClip clip;

            public void FromTimeOffset(float timeOff)
            {

                if (!clip)
                    return;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (setClipSamplePositionMethod == null)
                        setClipSamplePositionMethod = AudioUtilClass.GetMethod("SetClipSamplePosition",
                            BindingFlags.Static | BindingFlags.Public);

                    int pos = (int)(clip.samples * Mathf.Clamp01(timeOff / clip.length));

                    setClipSamplePositionMethod.Invoke(null, new object[] { clip, pos });
                }
#endif
            }

            public EditorAudioPlayRequest(AudioClip clip)
            {
                this.clip = clip;
            }
        }

        public static float GetLoudestPointInSeconds(this AudioClip clip)
            => clip.GetFirstLoudPointInSeconds(1);

        public static float GetFirstLoudPointInSeconds(this AudioClip clip, float increase = 3f)
        {
            if (!clip)
                return 0;

            int length = clip.samples;
            float[] data = new float[length];
            clip.GetData(data, 0);

            int maxSample = 0;
            float maxVolume = 0;

            for (int i = 0; i < length; i++)
            {

                var volume = Mathf.Abs(data[i]);

                if (volume > maxVolume)
                {

                    maxVolume = volume * increase;
                    maxSample = i;
                }
            }

            return ((float)maxSample) / ((float)(clip.frequency * clip.channels));
        }

        #endregion

        #region Unity Editor MGMT

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

        public static void SetDefine(this string val, bool to)
        {

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

        public static object SetToDirty_Obj(this object obj)
        {

#if UNITY_EDITOR
            SetToDirty(obj as UnityEngine.Object);
#endif

            return obj;
        }

        public static void FocusOn(Object go)
        {
#if UNITY_EDITOR
            var tmp = new Object[1];
            tmp[0] = go;
            Selection.objects = tmp;
#endif
        }
        
        public static void FocusOnGame()
        {
#if UNITY_EDITOR
            var assembly = typeof(EditorWindow).Assembly;
            var type = assembly.GetType("UnityEditor.GameView");
            var gameView = EditorWindow.GetWindow(type);
            gameView.Focus();
#endif

        }

        public static void RenamingLayer(int index, string name)
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;

            var tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                Debug.LogWarning(
                    "Can't set up the layers.  It's possible the format of the layers and tags data has changed in this version of Unity.");
                Debug.LogWarning("Layers is null: " + (layers == null));
                return;
            }

            var layerSp = layers.GetArrayElementAtIndex(index);

            if (layerSp.stringValue.IsNullOrEmpty() || !layerSp.stringValue.SameAs(name))
            {
                Debug.Log("Changing layer name.  " + layerSp.stringValue + " to " + name);
                layerSp.stringValue = name;
            }

            tagManager.ApplyModifiedProperties();
#endif
        }

        #endregion

        #region Assets Management

        public static bool FocusOnAsset<T>() where T: Object
        {
            #if UNITY_EDITOR

            var ass = AssetDatabase.FindAssets("t:{0}".F(typeof(T).ToString()));
            if (ass.Length > 0) {

                var all = new Object[ass.Length];

                for (int i = 0; i < ass.Length; i++)
                    all[i] = GuidToAsset<T>(ass[i]);

                Selection.objects = all;

                return true;
            }
            #endif
            return false;
        }

        public static void RefreshAssetDatabase()
        {
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public static Object GetPrefab(Object obj) =>

#if UNITY_EDITOR

#if UNITY_2018_2_OR_NEWER
            PrefabUtility.GetCorrespondingObjectFromSource(obj);
            #else
                 PrefabUtility.GetPrefabParent(obj);
            #endif
            #else
                 null;
            #endif


        public static void UpdatePrefab(GameObject gameObject)
        {
#if !NO_PEGI && UNITY_EDITOR

#if UNITY_2018_3_OR_NEWER
            var pf = UnityUtils.IsPrefab(gameObject) ? gameObject : PrefabUtility.GetPrefabInstanceHandle(gameObject);
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
                    var path = AssetDatabase
                        .GetAssetPath(pf); //PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pf);

                    if (path.IsNullOrEmpty())
                        "Path is null, Update prefab manually".showNotificationIn3D_Views();
                    else
                        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path, InteractionMode.AutomatedAction);
                }
#else
                PrefabUtility.ReplacePrefab(gameObject, GetPrefab(gameObject), ReplacePrefabOptions.ConnectToPrefab);
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

        public static bool IsPrefab(GameObject go) => go.scene.name == null;

        public static string SetUniqueObjectName(Object obj, string folderName, string extension)
        {

            folderName = Path.Combine("Assets", folderName); //.AddPreSlashIfNotEmpty());
            var name = obj.name;
            var fullPath =
#if UNITY_EDITOR
                AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderName, name) + extension);
#else
            Path.Combine(folderName,  name) + extension;
#endif
            name = fullPath.Substring(folderName.Length);
            name = name.Substring(0, name.Length - extension.Length);
            obj.name = name;

            return fullPath;
        }

        public static string GetAssetFolder(Object obj)
        {
#if UNITY_EDITOR

            var parentObject = GetPrefab(obj);
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

        public static bool SavedAsAsset(Object obj) =>
#if UNITY_EDITOR
            obj && (!AssetDatabase.GetAssetPath(obj).IsNullOrEmpty());
        #else
            obj;
        #endif

        public static string GetGuid(this Object obj, string current)
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

        public static T GuidToAsset<T>(string guid) where T : Object
#if UNITY_EDITOR
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return path.IsNullOrEmpty() ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }
        #else
               => null;
            #endif

        public static string GetGuid(this Object obj) => obj.GetGuid(null);

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

        #endregion

        #region Scriptable Objects

        private const string ScrObjExt = ".asset";

        public static T CreateScriptableObjectInTheSameFolder<T>(this ScriptableObject el, string name, bool refreshDatabase = true) where T : ScriptableObject
        {

            T added;

            #if UNITY_EDITOR
            
            var path = AssetDatabase.GetAssetPath(el);

            if (path.IsNullOrEmpty()) return null;

            added = ScriptableObject.CreateInstance<T>();

            var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path.Replace(Path.GetFileName(path), name + ScrObjExt));

            AssetDatabase.CreateAsset(added, assetPathAndName);

            added.name = name;

            if (!refreshDatabase) {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#else
            added = ScriptableObject.CreateInstance<T>();
#endif

            return added;
        }
        
        public static T DuplicateScriptableObject<T>(this T el, bool refreshDatabase = true) where T : ScriptableObject
        {
            T added;

            #if UNITY_EDITOR

            var path = AssetDatabase.GetAssetPath(el);

            if (path.IsNullOrEmpty()) return null;

            added = ScriptableObject.CreateInstance(el.GetType()) as T;

            var oldName = Path.GetFileName(path);

            if (oldName.IsNullOrEmpty()) return added;

            int len = oldName.Length;
    
            var assetPathAndName =
                AssetDatabase.GenerateUniqueAssetPath(
                    Path.Combine(
                        path.Substring(0, path.Length - len) , 
                        oldName.Substring(0, len - ScrObjExt.Length) + ScrObjExt));

            AssetDatabase.CreateAsset(added, assetPathAndName);

            var newName = Path.GetFileName(assetPathAndName);

            added.name = newName.Substring(0, newName.Length - ScrObjExt.Length);

            if (refreshDatabase) {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

#else
            added = ScriptableObject.CreateInstance(el.GetType()) as T;
#endif

            return added;
        }

        public static T CreateAndAddScriptableObjectAsset<T>(this List<T> objs, string path, string name)
            where T : ScriptableObject => CreateScriptableObjectAsset<T, T>(path, name, objs);

        public static T CreateScriptableObjectAsset<T>(this List<T> list, string path, string name, Type t) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance(t) as T;

            SaveScriptableObjectAsAsset(asset, path, name, list);

            return asset;
        }

        public static T CreateScriptableObjectAsset<T>(string path, string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();

            SaveScriptableObjectAsAsset<T, T>(asset, path, name);

            return asset;
        }

        public static T CreateScriptableObjectAsset<T, TG>(string path, string name, List<TG> optionalList = null) where T : TG where TG : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();

            SaveScriptableObjectAsAsset(asset, path, name, optionalList);

            return asset;
        }

        static void SaveScriptableObjectAsAsset<T, TG>(T asset, string path, string name, List<TG> optionalList = null)
            where T : TG where TG : ScriptableObject {

#if !NO_PEGI
            var nm = asset as IGotName;
            if (nm != null)
                nm.NameForPEGI = name;

            if (optionalList != null) {

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

            var fullPath = Path.Combine(FileSaveUtils.OutsideOfAssetsFolder, path);

            try {
                Directory.CreateDirectory(fullPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("Couldn't create Directory {0} : {1}".F(fullPath, ex.ToString()));
                return;
            }

            var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, name + ".asset"));

            try {
                AssetDatabase.CreateAsset(asset, assetPathAndName);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError("Couldn't create Scriptable Object {0} : {1}".F(assetPathAndName, ex.ToString()));
            }
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

            if (Application.isPlaying && (!Input.anyKeyDown)) return -1;

            if (!Application.isPlaying && (e.type != EventType.KeyDown)) return -1;

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
            var down = false;
#if UNITY_EDITOR
            down |= (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyDown &&
                     Event.current.keyCode == k);
            if (Application.isPlaying)
#endif
                down |= Input.GetKeyDown(k);

            return down;
        }

        public static bool IsUp(this KeyCode k)
        {

            var up = false;
#if UNITY_EDITOR
            up |= (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyUp &&
                   Event.current.keyCode == k);
            if (Application.isPlaying)
#endif
                up |= Input.GetKeyUp(k);

            return up;
        }

        #endregion

        #region Spin Around

        private static Vector2 _camOrbit;
        private static Vector3 _spinningAround;
        private static float _orbitDistance = 0;
        private static bool _orbitingFocused;

        private static float _spinStartTime = 0;
        
        public static void SpinAround(Vector3 pos, Transform cameraman)
        {
            if (Input.GetMouseButtonDown(2))
            {
                var before = cameraman.rotation; //cam.transform.rotation;
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
                cameraman.transform.rotation = cameraman.rotation.LerpBySpeed(rot, 300);
                if (Quaternion.Angle(cameraman.rotation, rot) < 1)
                    _orbitingFocused = true;
            }
            else cameraman.rotation = rot;
        }

        #endregion

        #region Textures

        #region Material MGMT

        public static bool HasTag(this Material mat, string tag, bool searchFallbacks = false, string defaultValue = "") =>
            mat && !mat.GetTag(tag, searchFallbacks, defaultValue).IsNullOrEmpty();

        public static Material MaterialWhatever(this Renderer renderer) =>
            !renderer ? null : (Application.isPlaying ? renderer.material : renderer.sharedMaterial);

        public static List<string> GetColorProperties(this Material m) =>
#if UNITY_EDITOR
            m.GetProperties(MaterialProperty.PropType.Color);
            #else
            new List<String>();
            #endif

        public static List<string> MyGetTexturePropertiesNames(this Material m) =>
            #if UNITY_EDITOR
             m.GetProperties(MaterialProperty.PropType.Texture);
            #else
            new List<String>();
            #endif
 
        public static List<string> GetFloatProperties(this Material m)
        {
            #if UNITY_EDITOR
            var l = m.GetProperties(MaterialProperty.PropType.Float);
            l.AddRange(m.GetProperties(MaterialProperty.PropType.Range));
            return l;
            #else
            return new List<string>();
            #endif
        }
        
        public static List<ShaderProperty.TextureValue> MyGetTextureProperties(this Material m)
        {
#if UNITY_EDITOR
            {
                var lst = new List<ShaderProperty.TextureValue>();
                foreach (var n in m.GetProperties(MaterialProperty.PropType.Texture))
                    lst.Add(new ShaderProperty.TextureValue(n));

                return lst;
            }
#else
            return new List<ShaderProperty.TextureValue>();
#endif
        }

#if UNITY_EDITOR
        private static List<string> GetProperties(this Material m, MaterialProperty.PropType type)
        {
            var fNames = new List<string>();


            #if UNITY_EDITOR
            if (!m)
                return fNames;

            var mat = new Material[1];
            mat[0] = m;
            MaterialProperty[] props;

            try {
                props = MaterialEditor.GetMaterialProperties(mat);
            }
            catch {
                return fNames = new List<string>();
            }

            if (props == null) return fNames;
            fNames.AddRange(from p in props where p.type == type select p.name);
#endif

            return fNames;
        }
#endif
        
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
        
        public static Texture2D CopyFrom(this Texture2D tex, RenderTexture rt)
        {
            if (!rt || !tex)
            {
#if UNITY_EDITOR
                Debug.Log("Texture is null");
#endif
                return tex;
            }

            var curRT = RenderTexture.active;

            RenderTexture.active = rt;

            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

            RenderTexture.active = curRT;

            return tex;
        }

        public static bool TextureHasAlpha(this Texture2D tex) {

            if (!tex) return false;

            // May not cover all cases
           
            switch (tex.format) {
                case TextureFormat.ARGB32: return true;
                case TextureFormat.RGBA32: return true;
                case TextureFormat.ARGB4444: return true;
                case TextureFormat.BGRA32: return true;
                case TextureFormat.PVRTC_RGBA4: return true;
                case TextureFormat.RGBAFloat: return true;
                case TextureFormat.RGBAHalf: return true;
                case TextureFormat.Alpha8: return true;
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

        public static TextureImporter GetTextureImporter(this Texture2D tex) =>
            AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex)) as TextureImporter;

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

        public static void Reimport_IfMarkedAsNOrmal(this Texture2D tex)
        {
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

            if (importer && (importer.WasWrongIsColor(value)))
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

            if (importer  && importer.WasNotSingleChanel())
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

        public static Texture2D RewriteOriginalTexture(this Texture2D tex)
        {

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

        public static Texture2D SaveTextureAsAsset(this Texture2D tex, string folderName, ref string textureName,
            bool saveAsNew)
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

        public static void SetShaderKeyword(this Material mat, string keyword, bool isTrue)
        {
            if (mat && !keyword.IsNullOrEmpty()) {
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
            if (keyword.IsNullOrEmpty()) return;

            if (isTrue)
                Shader.EnableKeyword(keyword);
            else
                Shader.DisableKeyword(keyword);
        }

        public static bool GetKeyword(this Material mat, string keyword) =>
            Array.IndexOf(mat.shaderKeywords, keyword) != -1;

        #endregion

        #region Meshes

        public static void SetColor(this MeshFilter mf, Color col) {

            if (!mf) return;

            var m = mf.mesh;

            var cols = new Color[m.vertexCount];

            for (int i = 0; i < m.vertexCount; i++)
                cols[i] = col;

            mf.mesh.colors = cols;
        }

        public static void SetColor_RGB(this MeshFilter mf, Color col) {

            if (!mf) return;
            
            var m = mf.mesh;

            List<Color> colors = new List<Color>();

            m.GetColors(colors);

            if (colors.Count < m.vertexCount)
                mf.SetColor(col);
            else
            {
                for (int i = 0; i < m.vertexCount; i++) {
                    col.a = colors[i].a;
                    colors[i] = col;
                }

                mf.mesh.colors = colors.ToArray();
            }
            
        }
        
        public static void SetAlpha(this MeshFilter mf, float alpha)
        {
            if (!mf) return;

            var mesh = mf.mesh;

            var m = mesh;

            var cols = mesh.colors;

            if (cols.IsNullOrEmpty())
            {
                cols = new Color[m.vertexCount];

                for (var i = 0; i < m.vertexCount; i++)
                    cols[i] = Color.white;


            } else for (var i = 0; i < m.vertexCount; i++)
                cols[i].a = alpha;

            mf.mesh.colors = cols;
        }

        public static int GetSubMeshNumber(this Mesh m, int triangleIndex)
        {
            if (!m)
                return 0;

            if (m.subMeshCount == 1)
                return 0;

            if (!m.isReadable) {
                Debug.Log("Mesh {0} is not readable. Enable for submesh material editing.".F(m.name));
                return 0;
            }

            var triangles = new int[] {
                m.triangles[triangleIndex * 3],
                m.triangles[triangleIndex * 3 + 1],
                m.triangles[triangleIndex * 3 + 2]
            };

            for (var i = 0; i < m.subMeshCount; i++) {

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

        public static void AssignMeshAsCollider(this MeshCollider c, Mesh mesh) {
            // One version of Unity had a bug so this is to counter it, may be not needed anymore
            c.sharedMesh = null;
            c.sharedMesh = mesh;
        }

        #endregion
    }

    #region Various Managers Classes

    public class PerformanceTimer : IPEGI_ListInspect, IGotDisplayName
    {
        private readonly string _name;
        private float _timer;
        private double _perIntervalCount;
        private double _max;
        private double _min = float.PositiveInfinity;
        private double _average;
        private double _totalCount;
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

        public string NameForDisplayPEGI => "Avg {0}: {1}/{2}sec [{3} - {4}] ({5}) ".F(_name,
            ((float)_average).ToString("0.00"),
            (Math.Abs(_intervalLength - 1d) > float.Epsilon) ? _intervalLength.ToString("0") : "", (int)_min,
            (int)_max, (int)_totalCount);

#if !NO_PEGI
        public bool InspectInList(IList list, int ind, ref int edited)
        {
            if (icon.Refresh.Click("Reset Stats"))
                ResetStats();

            //   "_name interval".edit(80, ref intervalLength);

            NameForDisplayPEGI.write();


            return false;
        }
#endif

#endregion

        public PerformanceTimer(string name = "Speed", float interval = 1f)
        {
            _name = name;
            _intervalLength = interval;
        }
    }

    public class ChillLogger : IGotDisplayName
    {
        private bool _logged;
        private readonly bool _disabled;
        private float _lastLogged;
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

#if QC_USE_NETWORKING
            private UnityWebRequest _request;
#endif

            private string url;
            public string URL => url;
            private Texture _texture;
            private bool _failed = false;

            public string NameForPEGI
            {
                get { return url; }
                set { url = value; }
            }

            private Texture Take()
            {
                var tmp = _texture;
                _texture = null;
                _failed = false;
                DisposeRequest();
                return tmp;
            }

            public bool TryGetTexture(out Texture tex, bool remove = false)
            {
                tex = _texture;

                if (remove && _texture) Take();

                if (_failed) return true;


#if QC_USE_NETWORKING
                if (_request != null)
                {
                    if (_request.isNetworkError || _request.isHttpError)
                    {

                        _failed = true;

#if UNITY_EDITOR
                        Debug.Log(_request.error);
#endif
                        DisposeRequest();
                        return true;
                    }

                    if (_request.isDone)
                    {
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
#endif

                return true;
            }

            void Start()
            {

#if QC_USE_NETWORKING
                _request?.Dispose();
                _request = UnityWebRequestTexture.GetTexture(url);
                _request.SendWebRequest();
                _failed = false;
                Debug.Log("Loading {0}".F(url));
#endif
            }

            public WebRequestMeta(string URL)
            {
                url = URL;
                Start();
            }

            private void DisposeRequest()
            {

#if QC_USE_NETWORKING
                _request?.Dispose();
                _request = null;
#endif
            }

            public void Dispose()
            {
                if (_texture)
                    _texture.DestroyWhatever();

                DisposeRequest();
            }

#region Inspector

#if !NO_PEGI
            public bool InspectInList(IList list, int ind, ref int edited)
            {
                var changed = false;
                Texture tex;
                TryGetTexture(out tex);


#if QC_USE_NETWORKING
                if (_request != null)
                    "Loading".write(60);
                if (_failed)
                    "Failed".write(50);

                if (_texture)
                {
                    if (icon.Refresh.Click())
                        Start();

                    if (_texture.Click())
                        edited = ind;

                }
                else
                {

                    if (_failed)
                    {
                        if (icon.Refresh.Click("Failed"))
                            Start();
                        "Failed ".F(url).write(40);
                    }
                    else
                    {
                        icon.Active.write();
                        "Loading ".write(40);
                    }

                }
#else
                "QC_USE_NETWORKING is disabled (to prevent unwanted android permissions)".writeWarning();
#endif
                url.write();
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

        public string GetURL(int ind)
        {
            var el = _loadedTextures.TryGet(ind);
            return (el == null) ? "" : el.URL;
        }

        public bool TryGetTexture(int ind, out Texture tex, bool remove = false)
        {
            tex = null;
            var el = _loadedTextures.TryGet(ind);
            return (el != null) ? el.TryGetTexture(out tex, remove) : true;
        }

        public int StartDownload(string address)
        {
            var el = _loadedTextures.GetByIGotName(address);

            if (el == null)
            {
                el = new WebRequestMeta(address);
                _loadedTextures.Add(el);
            }

            return _loadedTextures.IndexOf(el);
        }

        public void Dispose()
        {
            foreach (var t in _loadedTextures)
                t.Dispose();

            _loadedTextures.Clear();
        }

#region Inspector

#if !NO_PEGI
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

    [Serializable]
    public class ScreenShootTaker : IPEGI {

  
        #if !NO_PEGI
        public bool Inspect() {

            "Up Scale".edit(60, ref UpScale).nl();
            "Alpha".toggleIcon(ref AlphaBackground).nl();

            "Img Name".edit(90, ref screenShotName).nl();
            
            "Camera ".edit(60, ref cameraToTakeScreenShotFrom);

            // "On Post render is only called when script is attached to camera. Not finished implementation ... ".writeHint();

            if (cameraToTakeScreenShotFrom && icon.SaveAsNew.Click("From Camera"))
                RenderToTextureManually();

            pegi.nl();

            if (!grab)  {
                if ("On Post Render()".Click())
                    grab = true;
            } else 
                ("To grab screen-shot at Post-Render call OnPostRender() of this class from OnPostRender() of the script attached to camera." +
                  " Refer to Unity documentation to learn more about OnPostRender() call")
                    .writeHint();
            
            
            pegi.nl();

            if ("Take Screen Shot".Click().nl())
                ScreenCapture.CaptureScreenshot("ScreenShots/{0}".F(screenShotName), UpScale);

            if (icon.Refresh.Click("Refresh Asset Database"))
                UnityUtils.RefreshAssetDatabase();

            pegi.nl();

            return false;
        }
        #endif

        private bool grab;

        public Camera cameraToTakeScreenShotFrom;
        public int UpScale = 4;
        public bool AlphaBackground = true;

        [NonSerialized] private RenderTexture forScreenRenderTexture;
        [NonSerialized] private Texture2D screenShotTexture2D;

        public void RenderToTextureManually() {

            var cam = cameraToTakeScreenShotFrom;
            var w = cam.pixelWidth * UpScale;
            var h = cam.pixelHeight * UpScale;

            CheckRenderTexture(w, h);
            CheckTexture2D(w, h);

            cam.targetTexture = forScreenRenderTexture;
            var clearFlags = cam.clearFlags;

            if (AlphaBackground) {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0, 0, 0, 0);
            }
            else
            {
                var col = cam.backgroundColor;
                col.a = 1;
                cam.backgroundColor = col;
            }

            cam.Render();
            RenderTexture.active = forScreenRenderTexture;
            screenShotTexture2D.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            screenShotTexture2D.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;

            cam.clearFlags = clearFlags;

            FileSaveUtils.SaveTextureOutsideAssetsFolder("ScreenShoots", GetScreenShotName(), ".png",
                screenShotTexture2D);
        }
        
        public void OnPostRender() {
            if (grab)  {

                grab = false;

                var w = Screen.width;
                var h = Screen.height;

                CheckTexture2D(w, h);

                screenShotTexture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                screenShotTexture2D.Apply();

                FileSaveUtils.SaveTextureOutsideAssetsFolder("ScreenShoots", GetScreenShotName(), ".png",
                    screenShotTexture2D);

            }
        }

        private void CheckRenderTexture(int w, int h)
        {
            if (!forScreenRenderTexture || forScreenRenderTexture.width != w || forScreenRenderTexture.height != h)
            {

                if (forScreenRenderTexture)
                    forScreenRenderTexture.DestroyWhatever();

                forScreenRenderTexture = new RenderTexture(w, h, 32);
            }

        }

        private void CheckTexture2D(int w, int h)
        {
            if (!screenShotTexture2D || screenShotTexture2D.width != w || screenShotTexture2D.height != h)
            {

                if (screenShotTexture2D)
                    screenShotTexture2D.DestroyWhatever();

                screenShotTexture2D = new Texture2D(w, h, TextureFormat.ARGB32, false);
            }
        }
        
        public string screenShotName;
        
        private string GetScreenShotName()
        {
            var name = screenShotName;

            if (name.IsNullOrEmpty())
                name = "SS-" + DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss");

            return name;
        }

    }

    [Serializable]
    public class MaterialPlaytimeInstancer : IPEGI_ListInspect
    {
        [SerializeField] public List<Graphic> materialUsers = new List<Graphic>();
        [NonSerialized] private Material labelMaterialInstance;

        public Material MaterialInstance
        {
            get
            {
                if (labelMaterialInstance)
                    return labelMaterialInstance;

                if (materialUsers.Count == 0)
                    return null;

                var first = materialUsers[0];

                if (!first)
                    return null;

                if (!Application.isPlaying)
                    return first.material;

                labelMaterialInstance = Object.Instantiate(first.material);

                foreach (var u in materialUsers)
                    if (u)
                        u.material = labelMaterialInstance;

                return labelMaterialInstance;
            }
        }
        #if !NO_PEGI
        public bool InspectInList(IList list, int ind, ref int edited)
        {
            "works".write();
            return false;
        }
        #endif
    }

    /*
#if !NO_PEGI && UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MaterialPlaytimeInstancer))]
    // Work in progress...
    public class MatInstancerPropertDrawer : PEGI_PropertyDrawer<MaterialPlaytimeInstancer>
        { }
#endif*/

    [Serializable]
    public class MeshMaterialPlaytimeInstancer
    {

        [SerializeField] public List<MeshRenderer> materialUsers = new List<MeshRenderer>();
        [NonSerialized] private Material labelMaterialInstance;


        public Material MaterialInstance
        {
            get
            {
                if (labelMaterialInstance)
                    return labelMaterialInstance;

                if (materialUsers.Count == 0)
                    return null;

                var first = materialUsers[0];

                if (!first)
                    return null;

                if (!Application.isPlaying)
                    return first.sharedMaterial;

                labelMaterialInstance = Object.Instantiate(first.material);

                foreach (var u in materialUsers)
                    if (u)
                        u.sharedMaterial = labelMaterialInstance;

                return labelMaterialInstance;
            }
        }
    }

    #endregion

#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore 1692
#pragma warning restore IDE0034 // Simplify 'default' expression
#pragma warning restore IDE0019 // Use pattern matching
#pragma warning restore IDE0018 // Inline variable declaration


}





