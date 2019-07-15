using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using Object = UnityEngine.Object;
using UnityEngine.UI;
using System.IO;

#if QC_USE_NETWORKING
using UnityEngine.Networking;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCannersUtilities
{
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    public static class QcUtils {

        public static List<T> TryAdd<T>(this List<T> list, object ass, bool onlyIfNew = true)
        {

            T toAdd;

            if (list.CanAdd(ref ass, out toAdd, onlyIfNew))
                list.Add(toAdd);

            return list;

        }

        #region TextOperations

        private const string BadFormat = "!Bad format: ";

        public static string F(this string format, Type type)
        {
            try
            {
                return string.Format(format, type.ToPegiStringType());
            }
            catch
            {
                return BadFormat + format + " " + (type == null ? "null type" : type.ToString());
            }
        }
        public static string F(this string format, string obj)
        {
            try
            {
                return string.Format(format, obj);
            }
            catch
            {
                return BadFormat + format + " " + obj;
            }
        }
        public static string F(this string format, object obj1)
        {
            try
            {
                return string.Format(format, obj1.GetNameForInspector());
            }
            catch
            {
                return BadFormat + format + " " + obj1.GetNameForInspector();
            }
        }
        public static string F(this string format, string obj1, string obj2)
        {
            try
            {
                return string.Format(format, obj1, obj2);
            }
            catch
            {
                return BadFormat + format + " " + obj1 + " " + obj2;
            }
        }
        public static string F(this string format, object obj1, object obj2)
        {
            try
            {
                return string.Format(format, obj1.GetNameForInspector(), obj2.GetNameForInspector());
            }
            catch
            {
                return BadFormat + format;
            }
        }
        public static string F(this string format, string obj1, string obj2, string obj3)
        {
            try
            {
                return string.Format(format, obj1, obj2, obj3);
            }
            catch
            {
                return BadFormat + format;
            }
        }
        public static string F(this string format, object obj1, object obj2, object obj3)
        {
            try
            {
                return string.Format(format, obj1.GetNameForInspector(), obj2.GetNameForInspector(), obj3.GetNameForInspector());
            }
            catch
            {
                return BadFormat + format;
            }
        }
        public static string F(this string format, params object[] objs)
        {
            try
            {
                return string.Format(format, objs);
            }
            catch
            {
                return BadFormat + format;
            }
        }

        public static string ToSuccessString(this bool value) => value ? "Success" : "Failed";

        #endregion

        public static bool CanAdd<T>(this List<T> list, ref object obj, out T conv, bool onlyIfNew = true)
        {
            conv = default(T);

            if (obj == null || list == null)
                return false;

            if (!(obj is T))
            {

                GameObject go;

                if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
                    go = (obj as MonoBehaviour)?.gameObject;
                else go = obj as GameObject;

                if (go)
                    conv = go.GetComponent<T>();
            }
            else conv = (T)obj;

            if (conv == null || conv.Equals(default(T))) return false;

            var objType = obj.GetType();

            var dl = typeof(T).TryGetDerivedClasses();
            if (dl != null)
            {
                if (!dl.Contains(objType))
                    return false;

            }
            else
            {

                var tc = typeof(T).TryGetTaggedClasses();

                if (tc != null && !tc.Types.Contains(objType))
                    return false;
            }

            return !onlyIfNew || !list.Contains(conv);
        }
        
        private static void AssignUniqueIndex<T>(List<T> list, T el)
        {
            var ind = el as IGotIndex;
            if (ind == null) return;
            var maxIndex = ind.IndexForPEGI;
            foreach (var o in list)
                if (!el.Equals(o))
                {
                    var oInd = o as IGotIndex;
                    if (oInd != null)
                        maxIndex = Mathf.Max(maxIndex, oInd.IndexForPEGI + 1);
                }
            ind.IndexForPEGI = maxIndex;

        }

        public static T AddWithUniqueNameAndIndex<T>(List<T> list) => AddWithUniqueNameAndIndex(list, "New "+ typeof(T).ToPegiStringType());

        public static T AddWithUniqueNameAndIndex<T>(List<T> list, string name) =>
            AddWithUniqueNameAndIndex(list, (T)Activator.CreateInstance(typeof(T)), name);

        public static T AddWithUniqueNameAndIndex<T>(List<T> list, T e, string name)
        {
            AssignUniqueIndex(list, e);
            list.Add(e);
            var named = e as IGotName;
            if (named != null)
                named.NameForPEGI = name;
            e.AssignUniqueNameIn(list);
            return e;
        }
        
        private static void AssignUniqueNameIn<T>(this T el, IReadOnlyCollection<T> list) {

            var named = el as IGotName;
            if (named == null) return;

            var tmpName = named.NameForPEGI;
            var duplicate = true;
            var counter = 0;

            while (duplicate)
            {
                duplicate = false;

                foreach (var e in list)
                {
                    var other = e as IGotName;
                    if (other == null || e.Equals(el) || !tmpName.Equals(other.NameForPEGI))
                        continue;

                    duplicate = true;
                    counter++;
                    tmpName = named.NameForPEGI + counter;
                    break;
                }
            }

            named.NameForPEGI = tmpName;

        }
        
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

            public string NameForDisplayPEGI()=> "Avg {0}: {1}/{2}sec [{3} - {4}] ({5}) ".F(_name,
                ((float)_average).ToString("0.00"),
                (Math.Abs(_intervalLength - 1d) > float.Epsilon) ? _intervalLength.ToString("0") : "", (int)_min,
                (int)_max, (int)_totalCount);
            
            public bool InspectInList(IList list, int ind, ref int edited)
            {
                if (icon.Refresh.Click("Reset Stats"))
                    ResetStats();

                //   "_name interval".edit(80, ref intervalLength);

                NameForDisplayPEGI().write();


                return false;
            }

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

            public string NameForDisplayPEGI()=> message + (_disabled ? " Disabled" : " Enabled");

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

        public class TextureDownloadManager : IPEGI
        {

            readonly List<WebRequestMeta> _loadedTextures = new List<WebRequestMeta>();

            class WebRequestMeta : IGotName, IPEGI_ListInspect, IPEGI
            {

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
#else
                Debug.Log("Can't Load {0} : QC_USE_NETWORKING is disabled".F(url));
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

                    pegi.nl();

                    if ("Enable QC_USE_NETWORKING".Click())
                        QcUnity.SetPlatformDirective("QC_USE_NETWORKING", true);

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

            #endregion
        }

        [Serializable]
        public class ScreenShootTaker : IPEGI
        {

            [SerializeField] public string folderName = "ScreenShoots";

            private bool _showAdditionalOptions;

            public bool Inspect()
            {

                "Camera ".edit(60, ref cameraToTakeScreenShotFrom);

                pegi.nl();

                "Transparent Background".toggleIcon(ref AlphaBackground).nl();

                "Img Name".edit(90, ref screenShotName);
                var path = Path.Combine(QcUnity.GetDataPathWithout_Assets_Word(), folderName);
                if (icon.Folder.Click("Open Screen Shots Folder : {0}".F(path)))
                    QcFile.ExplorerUtils.OpenPath(path);

                pegi.nl();

                "Up Scale".edit("Resolution of the texture will be multiplied by a given value", 60, ref UpScale);

                if (UpScale <= 0)
                    "Scale value needs to be positive".writeWarning();
                else
                if (cameraToTakeScreenShotFrom)
                {

                    if (UpScale > 4)
                    {
                        if ("Take Very large ScreenShot".ClickConfirm("tbss", "This will try to take a very large screen shot. Are we sure?").nl())
                            RenderToTextureManually();
                    }
                    else if ("Take Screen Shoot".Click("Render Screenshoot from camera").nl())
                        RenderToTextureManually();
                }

                if ("Other Options".foldout(ref _showAdditionalOptions).nl())
                {

                    if (!grab)
                    {
                        if ("On Post Render()".Click())
                            grab = true;
                    }
                    else
                        ("To grab screen-shot from Post-Render, OnPostRender() of this class should be called from OnPostRender() of the script attached to a camera." +
                         " Refer to Unity documentation to learn more about OnPostRender() call")
                            .writeHint();


                    pegi.nl();

                    if ("Take Screen Shot".Click())
                        ScreenCapture.CaptureScreenshot("{0}".F(GetScreenShotName() + ".png"), UpScale);


                    if (icon.Folder.Click())
                        QcFile.ExplorerUtils.OpenPath(QcUnity.GetDataPathWithout_Assets_Word());

                    "Game View Needs to be open for this to work".fullWindowDocumentationClickOpen();

                }

                pegi.nl();

                return false;
            }

            private bool grab;

            public Camera cameraToTakeScreenShotFrom;
            public int UpScale = 4;
            public bool AlphaBackground = true;

            [NonSerialized] private RenderTexture forScreenRenderTexture;
            [NonSerialized] private Texture2D screenShotTexture2D;

            public void RenderToTextureManually()
            {

                var cam = cameraToTakeScreenShotFrom;
                var w = cam.pixelWidth * UpScale;
                var h = cam.pixelHeight * UpScale;

                CheckRenderTexture(w, h);
                CheckTexture2D(w, h);

                cam.targetTexture = forScreenRenderTexture;
                var clearFlags = cam.clearFlags;

                if (AlphaBackground)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    var col = cam.backgroundColor;
                    col.a = 0;
                    cam.backgroundColor = col;
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

                QcFile.SaveUtils.SaveTextureOutsideAssetsFolder("ScreenShoots", GetScreenShotName(), ".png", screenShotTexture2D);
            }

            public void OnPostRender()
            {
                if (grab)
                {

                    grab = false;

                    var w = Screen.width;
                    var h = Screen.height;

                    CheckTexture2D(w, h);

                    screenShotTexture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                    screenShotTexture2D.Apply();

                    QcFile.SaveUtils.SaveTextureOutsideAssetsFolder("ScreenShoots", GetScreenShotName(), ".png",
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

            public bool InspectInList(IList list, int ind, ref int edited)
            {
                "works".write();
                return false;
            }

        }

        [Serializable]
        public class MeshMaterialPlaytimeInstancer
        {

            [SerializeField] public bool instantiateInEditor = false;
            [SerializeField] public List<MeshRenderer> materialUsers = new List<MeshRenderer>();
            [NonSerialized] private Material materialInstance;

            public Material GetMaterialInstance(MeshRenderer rendy)
            {
                if (materialInstance)
                    return materialInstance;

                materialUsers.Clear();
                materialUsers.Add(rendy);

                return MaterialInstance;
            }

            public Material MaterialInstance
            {
                get
                {
                    if (materialInstance)
                        return materialInstance;

                    if (materialUsers.Count == 0)
                        return null;

                    var first = materialUsers[0];

                    if (!first)
                        return null;

                    if (!Application.isPlaying && !instantiateInEditor)
                        return first.sharedMaterial;

                    materialInstance = Object.Instantiate(first.sharedMaterial);

                    materialInstance.name = "Instanced material of {0}".F(first.name);

                    foreach (var u in materialUsers)
                        if (u)
                            u.sharedMaterial = materialInstance;

                    return materialInstance;
                }
            }

            public MeshMaterialPlaytimeInstancer()
            {

            }

            public MeshMaterialPlaytimeInstancer(bool instantiateInEditor)
            {
                this.instantiateInEditor = instantiateInEditor;
            }
        }


        [Serializable]
        public struct DynamicRangeFloat : ICfg
        {

            [SerializeField] public float min;
            [SerializeField] public float max;
            [SerializeField] public float value;

            public void SetValue(float nVal)
            {
                value = nVal;
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }

            #region Inspector
            
            private bool _showRange;

            public bool Inspect()
            {
                var changed = false;
                var rangeChanged = false;

                var tmp = value;
                if (pegi.edit(ref tmp, min, max).changes(ref changed))
                    value = tmp;

                if (!_showRange && icon.Edit.ClickUnFocus("Edit Range", 20))
                    _showRange = true;

                if (_showRange)
                {
                    pegi.nl();

                    if (icon.FoldedOut.ClickUnFocus("Hide Range"))
                        _showRange = false;

                    "Range: [".write(60);

                    var before = min;

                    tmp = min;

                    if (pegi.editDelayed(ref tmp, 40).changes(ref rangeChanged))
                    {
                        min = tmp;
                        if (min >= max)
                            max = min + (max - before);
                    }

                    "-".write(10);
                    tmp = max;
                    if (pegi.editDelayed(ref tmp, 40).changes(ref rangeChanged))
                    {
                        max = tmp;
                        min = Mathf.Min(min, max);

                    }

                    "]".write(10);

                    pegi.nl();

                    "Tap Enter to apply Range change in the field (will Clamp current value)".writeHint();

                    pegi.nl();

                    if (rangeChanged)
                        value = Mathf.Clamp(value, min, max);
                }


                return changed | rangeChanged;
            }

            #endregion

            #region Encode & Decode

            public CfgEncoder Encode() => new CfgEncoder()
                .Add_IfNotEpsilon("m", min)
                .Add_IfNotEpsilon("v", value)
                .Add_IfNotEpsilon("x", max);

            public void Decode(string data) => data.DecodeTagsFor(this);

            public bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "m":
                        min = data.ToFloat();
                        break;
                    case "v":
                        value = data.ToFloat();
                        break;
                    case "x":
                        max = data.ToFloat();
                        break;
                    default: return false;
                }

                return true;
            }

            #endregion

            public DynamicRangeFloat(float min = 0, float max = 1, float value = 0.5f)
            {
                this.min = min;
                this.max = max;
                this.value = value;

                _showRange = false;

            }
        }

        #endregion


    }

#pragma warning restore IDE0034 // Simplify 'default' expression
#pragma warning restore IDE0019 // Use pattern matching
#pragma warning restore IDE0018 // Inline variable declaration
}