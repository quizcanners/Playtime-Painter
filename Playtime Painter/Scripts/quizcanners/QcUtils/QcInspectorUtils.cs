using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PlayerAndEditorGUI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuizCannersUtilities
{
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    public static class QcUtils
    {
        
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

        #endregion

        public static bool CanAdd<T>(this List<T> list, ref object obj, out T conv, bool onlyIfNew = true)
        {
            conv = default;

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

                var tc = TaggedTypesCfg.TryGetOrCreate(typeof(T));

                if (tc != null && !tc.Types.Contains(objType))
                    return false;
            }

            return !onlyIfNew || !list.Contains(conv);
        }

        private static void AssignUniqueIndex<T>(IList<T> list, T el)
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

        public static T AddWithUniqueNameAndIndex<T>(IList<T> list) => AddWithUniqueNameAndIndex(list, "New " + typeof(T).ToPegiStringType());

        public static T AddWithUniqueNameAndIndex<T>(IList<T> list, string name) =>
            AddWithUniqueNameAndIndex(list, (T)Activator.CreateInstance(typeof(T)), name);

        public static T AddWithUniqueNameAndIndex<T>(IList<T> list, T e, string name)
        {
            AssignUniqueIndex(list, e);
            list.Add(e);
            var named = e as IGotName;
            if (named != null)
                named.NameForPEGI = name;
            e.AssignUniqueNameIn(list);
            return e;
        }

        private static void AssignUniqueNameIn<T>(this T el, IList<T> list)
        {

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

        #region Various Managers Classes

        public class PerformanceTimer : IPEGI_ListInspect, IGotDisplayName
        {
            private readonly string _name;
            private float _timer;
            private double _yieldsCounter;
            private double _maxYieldsPerInterval;
            private double _minYieldsPerInterval = float.PositiveInfinity;
            private double _averageYieldsPerInterval;
            private double _totalIntervalsProcessed;
            private readonly float _intervalInSeconds;

            public void Update(float add = 0)
            {
                _timer += Time.deltaTime;
                if (Math.Abs(add) > float.Epsilon)
                    AddYield(add);

                if (_timer <= _intervalInSeconds) return;
                
                _timer -= _intervalInSeconds;

                _maxYieldsPerInterval = Mathf.Max((float)_yieldsCounter, (float)_maxYieldsPerInterval);
                _minYieldsPerInterval = Mathf.Min((float)_yieldsCounter, (float)_minYieldsPerInterval);

                _totalIntervalsProcessed += 1;

                var portion = 1d / _totalIntervalsProcessed;
                _averageYieldsPerInterval = _averageYieldsPerInterval * (1d - portion) + _yieldsCounter * portion;

                _yieldsCounter = 0;

            }

            public void AddYield(float result = 1) => _yieldsCounter += result;

            public void ResetStats()
            {
                _timer = 0;
                _yieldsCounter = 0;
                _maxYieldsPerInterval = 0;
                _minYieldsPerInterval = float.PositiveInfinity;
                _averageYieldsPerInterval = 0;
                _totalIntervalsProcessed = 0;
            }

            #region Inspector

            public string NameForDisplayPEGI() => "Avg {0}: {1}/{2}sec [{3} - {4}] ({5}) ".F(_name,
                ((float)_averageYieldsPerInterval).ToString("0.00"),
                (Math.Abs(_intervalInSeconds - 1d) > float.Epsilon) ? _intervalInSeconds.ToString("0") : "", (int)_minYieldsPerInterval,
                (int)_maxYieldsPerInterval, (int)_totalIntervalsProcessed);

            public bool InspectInList(IList list, int ind, ref int edited)
            {
                if (icon.Refresh.Click("Reset Stats"))
                    ResetStats();
                
                NameForDisplayPEGI().write();
                
                return false;
            }

            #endregion

            public PerformanceTimer(string name = "Speed", float interval = 1f)
            {
                _name = name;
                _intervalInSeconds = interval;
            }
        }

        public class ChillLogger : IGotDisplayName
        {
            private bool _logged;
            private readonly bool _disabled;
            private double _lastLogged;
            private int _calls;
            private readonly string message = "error";

            public string NameForDisplayPEGI() => message + (_disabled ? " Disabled" : " Enabled");

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

            public void Log_Now(string msg, bool asError, Object obj = null)
            {

                //  if (disabled)
                //  return;

                if (msg == null)
                    msg = message;

                if (_calls > 0)
                    msg += " [+ {0} calls]".F(_calls);

                if (_lastLogged > 0)
                    msg += " [{0} s. later]".F(QcUnity.TimeSinceStartup() - _lastLogged);
                else
                    msg += " [at {0}]".F(QcUnity.TimeSinceStartup());

                if (asError)
                    Debug.LogError(msg, obj);
                else
                    Debug.Log(msg, obj);

                _lastLogged = QcUnity.TimeSinceStartup();
                _calls = 0;
                _logged = true;
            }

            public void Log_Once(string msg = null, bool asError = true, Object obj = null)
            {

                if (!_logged)
                    Log_Now(msg, asError, obj);
                else
                    _calls++;
            }

            public void Log_Interval(float seconds, string msg = null, bool asError = true, Object obj = null)
            {

                if (!_logged || (QcUnity.TimeSinceStartup() - _lastLogged > seconds))
                    Log_Now(msg, asError, obj);
                else
                    _calls++;
            }

            public void Log_Every(int callCount, string msg = null, bool asError = true, Object obj = null)
            {

                if (!_logged || (_calls > callCount))
                    Log_Now(msg, asError, obj);
                else
                    _calls++;
            }

            private static List<string> loggedErrors = new List<string>();
            public static void LogErrorOnce(string key, string msg, Object target = null)
            {
                if (loggedErrors.Contains(key))
                    return;

                loggedErrors.Add(key);

                if (target)
                    Debug.LogError(msg, target);
                else 
                    Debug.LogError(msg);
            }

            public static void LogErrorOnce(string key, Func<string> action, Object target = null)
            {
                if (loggedErrors.Contains(key))
                    return;

                loggedErrors.Add(key);

                if (target)
                    Debug.LogError(action(), target);
                else
                    Debug.LogError(action());
            }

            private static List<string> loggedWarnings = new List<string>();
            public static void LogWarningOnce(string key, string msg, Object target = null)
            {
                if (loggedWarnings.Contains(key))
                    return;

                loggedWarnings.Add(key);

                if (target)
                    Debug.LogWarning(msg, target);
                else
                    Debug.LogWarning(msg);
            }

        }

        [Serializable]
        public class ScreenShootTaker : IPEGI
        {

            [SerializeField] public string folderName = "ScreenShoots";

            private bool _showAdditionalOptions;

            public bool Inspect()
            {
                pegi.nl();

                "Camera ".selectInScene(ref cameraToTakeScreenShotFrom);

                pegi.nl();

                "Transparent Background".toggleIcon(ref AlphaBackground).nl();

                "Img Name".edit(90, ref screenShotName);
                var path = Path.Combine(QcFile.OutsideOfAssetsFolder, folderName);
                if (icon.Folder.Click("Open Screen Shots Folder : {0}".F(path)))
                    QcFile.Explorer.OpenPath(path);

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

                pegi.nl();

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
                        QcFile.Explorer.OpenPath(QcFile.OutsideOfAssetsFolder);

                    "Game View Needs to be open for this to work".fullWindowDocumentationClickOpen();

                }

                pegi.nl();

                return false;
            }

            private bool grab;

            public Camera cameraToTakeScreenShotFrom;
            public int UpScale = 4;
            public bool AlphaBackground;

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

                if (!AlphaBackground)
                {
                    var pixels = screenShotTexture2D.GetPixels32();

                    for (int i=0; i<pixels.Length; i++)
                    {
                        var col = pixels[i];
                        col.a = 255;
                        pixels[i] = col;
                    }

                    screenShotTexture2D.SetPixels32(pixels);
                }

                screenShotTexture2D.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;

                cam.clearFlags = clearFlags;

                QcFile.Save.TextureOutsideAssetsFolder("ScreenShoots", GetScreenShotName(), ".png", screenShotTexture2D);
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

                    QcFile.Save.TextureOutsideAssetsFolder("ScreenShoots", GetScreenShotName(), ".png",
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
        public class MaterialPlaytimeInstancer {
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
        }

        [Serializable]
        public class MeshMaterialPlaytimeInstancer
        {

            [SerializeField] public bool instantiateInEditor;
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
        public struct DynamicRangeFloat : ICfg, IPEGI
        {

            [SerializeField] public float min;
            [SerializeField] public float max;

            [SerializeField] private float _value;

            public float Value
            {
                get { return _value; }

                set
                {
                    _value = value;
                    min = Mathf.Min(min, value);
                    max = Mathf.Max(max, value);
                    UpdateRange();
                }
            }

            #region Inspector

            private float dynamicMin;
            private float dynamicMax;

            private void UpdateRange(float by = 1)
            {

                float width = dynamicMax - dynamicMin;

                width *= by * 0.5f;

                dynamicMin = Mathf.Max(min, _value - width);
                dynamicMax = Mathf.Min(max, _value + width);
            }

            private bool _showRange;

            public bool Inspect()
            {
                var changed = false;
                var rangeChanged = false;

                if ("><".Click())
                    UpdateRange(0.3f);

                pegi.edit(ref _value, dynamicMin, dynamicMax).changes(ref changed);
                //    Value = _value;

                if ("<>".Click())
                    UpdateRange(3f);
             

                if (!_showRange && icon.Edit.ClickUnFocus("Edit Range", 20))
                    _showRange = true;

                if (_showRange)
                {
                  

                    if (icon.FoldedOut.ClickUnFocus("Hide Range"))
                        _showRange = false;

                    pegi.nl();

                    "[{0} : {1}] - {2}".F(dynamicMin, dynamicMax, "Focused Range").nl();

                    "Range: [".write(60);

                    var before = min;


                    if (pegi.editDelayed(ref min, 40).changes(ref rangeChanged))
                    {
                        if (min >= max)
                            max = min + (max - before);


                    }

                    "-".write(10);

                    if (pegi.editDelayed(ref max, 40).changes(ref rangeChanged))
                    {
                        min = Mathf.Min(min, max);
                    }

                    "]".write(10);

                    "Use >< to shrink range around current value for more precision. And <> to expand range."
                        .fullWindowDocumentationClickOpen("About <> & ><");

                    if (icon.Refresh.Click())
                    {
                        dynamicMin = min;
                        dynamicMax = max;

                    }

                    pegi.nl();

                    "Tap Enter to apply Range change in the field (will Clamp current value)".writeHint();



                    pegi.nl();

                    if (rangeChanged)
                    {
                        Value = Mathf.Clamp(_value, min, max);

                        if (Mathf.Abs(dynamicMin - dynamicMax) < (float.Epsilon * 10))
                        {
                            dynamicMin = Mathf.Clamp(dynamicMin - float.Epsilon * 10, min, max);
                            dynamicMax = Mathf.Clamp(dynamicMax + float.Epsilon * 10, min, max);
                        }
                    }


                }


                return changed | rangeChanged;
            }

            #endregion

            #region Encode & Decode

            public CfgEncoder Encode() => new CfgEncoder()
                .Add_IfNotEpsilon("m", min)
                .Add_IfNotEpsilon("v", Value)
                .Add_IfNotEpsilon("x", max);

            public void Decode(string data)
            {
              
                new CfgDecoder(data).DecodeTagsFor(ref this);
                dynamicMin = min;
                dynamicMax = max;
            }

            public bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "m":
                        min = data.ToFloat();
                        break;
                    case "v":
                        Value = data.ToFloat();
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
                dynamicMin = min;
                dynamicMax = max;
                _value = value;

                _showRange = false;

            }
        }

        private static readonly ScreenShootTaker screenShots = new ScreenShootTaker();

        private static readonly ICfgObjectExplorer iCfgExplorer = new ICfgObjectExplorer();

        private static readonly EncodedJsonInspector jsonInspector = new EncodedJsonInspector();

        #endregion

        #region Inspect Inspector 
        private static int inspectedSection = -1;

        public static bool InspectInspector()
        {
            var changed = false;

            if ("Coroutines [{0}]".F(QcAsync.GetActiveCoroutinesCount).enter(ref inspectedSection, 0).nl())
                QcAsync.InspectManagedCoroutines().nl(ref changed);

            "Screen Shots".enter_Inspect(screenShots, ref inspectedSection, 1).nl(ref changed);

            "Json Inspector".enter_Inspect(jsonInspector, ref inspectedSection, 2).nl();

            if ("ICfg Inspector".enter(ref inspectedSection, 3).nl())
                iCfgExplorer.Inspect(null).nl(ref changed);

            if ("Gui Styles".enter(ref inspectedSection, 4).nl())
            {
                PEGI_Styles.Inspect().nl();
            }

            if (inspectedSection == -1)
            {
                if ("Player Data Folder".Click().nl())
                    QcFile.Explorer.OpenPersistentFolder();

                if (Application.isEditor && "Editor Data Folder".Click().nl())
                    QcFile.Explorer.OpenPath("C:/Users/{0}/AppData/Local/Unity/Editor/Editor.log".F(Environment.UserName));

            }

            return changed;
        }

        #endregion


    }

#pragma warning restore IDE0034 // Simplify 'default' expression
#pragma warning restore IDE0019 // Use pattern matching
#pragma warning restore IDE0018 // Inline variable declaration
}