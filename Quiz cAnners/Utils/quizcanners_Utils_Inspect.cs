﻿using System;
using System.Collections.Generic;
using QuizCanners.Migration;
using QuizCanners.Inspect;
using UnityEngine;

using Profiler = UnityEngine.Profiling.Profiler;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
    using UnityEditor.Sprites;
#endif

namespace QuizCanners.Utils
{
#pragma warning disable IDE0018 // Inline variable declaration

    public static class QcUtils
    {
       
        #region Various Managers Classes
        /*
        public class PerformanceTimer : IPEGI_ListInspect, IGotReadOnlyName
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

            public string GetNameForInspector() => "Avg {0}: {1}/{2}sec [{3} - {4}] ({5}) ".F(_name,
                ((float)_averageYieldsPerInterval).ToString("0.00"),
                (Math.Abs(_intervalInSeconds - 1d) > float.Epsilon) ? _intervalInSeconds.ToString("0") : "", (int)_minYieldsPerInterval,
                (int)_maxYieldsPerInterval, (int)_totalIntervalsProcessed);

            public void InspectInList(ref int edited, int ind)
            {
                if (icon.Refresh.Click("Reset Stats"))
                    ResetStats();
                
                GetNameForInspector().write();
            }

            #endregion

            public PerformanceTimer(string name = "Speed", float interval = 1f)
            {
                _name = name;
                _intervalInSeconds = interval;
            }
        }
        */

        [Serializable]
        public class ScreenShootTaker : IPEGI
        {
            [SerializeField] public string folderName = "ScreenShoots";

            private bool _showAdditionalOptions;

            public void Inspect()
            {
                pegi.nl();

                "Camera ".selectInScene(ref cameraToTakeScreenShotFrom);

                pegi.nl();

                "Transparent Background".toggleIcon(ref AlphaBackground);

                if (!AlphaBackground && cameraToTakeScreenShotFrom)
                {
                    if (cameraToTakeScreenShotFrom.clearFlags == CameraClearFlags.Color &&
                        cameraToTakeScreenShotFrom.clearFlags == CameraClearFlags.SolidColor) {
                        var col = cameraToTakeScreenShotFrom.backgroundColor;
                        if (pegi.edit(ref col))
                            cameraToTakeScreenShotFrom.backgroundColor = col;
                    }
                }

                pegi.nl();

                "Img Name".edit(90, ref screenShotName);
                var path = System.IO.Path.Combine(QcFile.OutsideOfAssetsFolder, folderName);
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
                        if ("Take Very large ScreenShot".ClickConfirm("tbss", "This will try to take a very large screen shot. Are we sure?"))
                            RenderToTextureManually();
                    }
                    else if (icon.ScreenGrab.Click("Render Screenshoot from camera").nl())
                        RenderToTextureManually();
                }

                pegi.FullWindow.DocumentationClickOpen("To Capture UI with this method, use Canvas-> Render Mode-> Screen Space - Camera. " +
                                                              "You probably also want Transparent Background turned on. Or not, depending on your situation. " +
                                                              "Who am I to tell you what to do, I'm just a hint.");

                pegi.nl();

                if ("Other Options".isFoldout(ref _showAdditionalOptions).nl())
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

                    if ("ScreenCapture.CaptureScreenshot".Click())
                        CaptureScreenShot();


                    if (icon.Folder.Click())
                        QcFile.Explorer.OpenPath(QcFile.OutsideOfAssetsFolder);

                    pegi.FullWindow.DocumentationClickOpen("Game View Needs to be open for this to work");

                }

                pegi.nl();

            }

            private bool grab;

            [SerializeField] private Camera cameraToTakeScreenShotFrom;
            [SerializeField] private int UpScale = 1;
            [SerializeField] private bool AlphaBackground;

            [NonSerialized] private RenderTexture forScreenRenderTexture;
            [NonSerialized] private Texture2D screenShotTexture2D;

            public void CaptureScreenShot()
            {
                ScreenCapture.CaptureScreenshot("{0}".F(System.IO.Path.Combine(folderName, GetScreenShotName()) + ".png"), UpScale);
            }

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
                    MakeOpaque(screenShotTexture2D);

                screenShotTexture2D.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                cam.clearFlags = clearFlags;

                QcFile.Save.TextureOutsideAssetsFolder(folderName, GetScreenShotName(), ".png", screenShotTexture2D);
            }

            private void MakeOpaque(Texture2D tex)
            {
                var pixels = tex.GetPixels32();

                for (int i = 0; i < pixels.Length; i++)
                {
                    var col = pixels[i];
                    col.a = 255;
                    pixels[i] = col;
                }

                tex.SetPixels32(pixels);
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
            [SerializeField] public List<UnityEngine.UI.Graphic> materialUsers = new List<UnityEngine.UI.Graphic>();
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
        public struct DynamicRangeFloat : ICfgCustom, IPEGI
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

            public void Inspect()
            {
                var rangeChanged = false;

                if ("><".Click())
                    UpdateRange(0.3f);

                pegi.edit(ref _value, dynamicMin, dynamicMax);
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


                    if (pegi.editDelayed(ref min, 40))
                    {
                        rangeChanged = true;

                        if (min >= max)
                            max = min + (max - before);
                    }

                    "-".write(10);

                    if (pegi.editDelayed(ref max, 40))
                    {
                        rangeChanged = true;
                        min = Mathf.Min(min, max);
                    }

                    "]".write(10);

                    pegi.FullWindow.DocumentationClickOpen("Use >< to shrink range around current value for more precision. And <> to expand range.", "About <> & ><");

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
            }

            #endregion

            #region Encode & Decode

            public CfgEncoder Encode() => new CfgEncoder()
                .Add_IfNotEpsilon("m", min)
                .Add_IfNotEpsilon("v", Value)
                .Add_IfNotEpsilon("x", max);

            public void Decode(CfgData data)
            {
              
                new CfgDecoder(data).DecodeTagsFor(ref this);
                dynamicMin = min;
                dynamicMax = max;
            }

            public void Decode(string key, CfgData data)
            {
                switch (key)
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
                }
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

        #region Inspect Debug Options 
        private static int inspectedSection = -1;
        private static int inspectedData = -1;

        public static void InspectDebug()
        {
            pegi.nl();

            int sectionIndex = -1;

             if ("Data".isEntered(ref inspectedSection, ++sectionIndex).nl())
            {
                if (inspectedData == -1)
                {
                    if ("Player Data Folder".Click().nl())
                    {
                        QcFile.Explorer.OpenPersistentFolder();
                        pegi.SetCopyPasteBuffer(Application.persistentDataPath, sendNotificationIn3Dview: true);
                    }

                    if (Application.isEditor && "Editor Data Folder".Click().nl())
                        QcFile.Explorer.OpenPath(
                            "C:/Users/{0}/AppData/Local/Unity/Editor/Editor.log".F(Environment.UserName));
                }

                if ("Cache".isEntered(ref inspectedData, 1).nl())
                {

                    if ("Caching.ClearCache() [{0}]".F(Caching.cacheCount).ClickConfirm("clCach").nl())
                    {
                        if (Caching.ClearCache())
                            pegi.GameView.ShowNotification("Bundles were cleared");
                        else
                            pegi.GameView.ShowNotification("ERROR: Bundles are being used");
                    }

                    List<string> lst = new List<string>();

                    Caching.GetAllCachePaths(lst);

                    "Caches".edit_List(lst, path =>
                    {
                        var c = Caching.GetCacheByPath(path);

                        if (icon.Delete.Click())
                        {

                            if (Caching.RemoveCache(c))
                                pegi.GameView.ShowNotification("Bundle was cleared");
                            else
                                pegi.GameView.ShowNotification("ERROR: Bundle is being used");
                        }

                        if (icon.Folder.Click())
                            QcFile.Explorer.OpenPath(path);

                        if (icon.Copy.Click())
                            pegi.SetCopyPasteBuffer(path);

                        path.write();

                        return path;
                    });
                }
            }

            if ("Logs".isEntered(ref inspectedSection, ++sectionIndex).nl())
                QcLog.LogHandler.Nested_Inspect();
            
            if ("Profiler".isEntered(ref inspectedSection, ++sectionIndex).nl())
            {
                "Mono Heap Size Long {0}".F(Profiler.GetMonoHeapSizeLong().ToMegabytes()).nl();

                "Mono Used Size Long {0}".F(Profiler.GetMonoUsedSizeLong().ToMegabytes()).nl();

                "Temp Allocated Size {0}".F(ToMegabytes(Profiler.GetTempAllocatorSize())).nl();

                "Total Allocated Memmory Long {0}".F(Profiler.GetTotalAllocatedMemoryLong().ToMegabytes()).nl();

                "Total Unused Reserved Memmory Long {0}".F(Profiler.GetTotalUnusedReservedMemoryLong().ToMegabytes()).nl();

                if ("Unload Unused Assets".Click().nl())
                {
                    Resources.UnloadUnusedAssets();
                }


            }

            if ("Time & Audio".isEntered(ref inspectedSection, ++sectionIndex).nl())
            {
                "Time.time: {0}".F(QcSharp.SecondsToReadableString(Time.time)).nl();

                "AudioSettings.dspTime: {0}".F(QcSharp.SecondsToReadableString(AudioSettings.dspTime)).nl();

                "Use it to schedule Audio Clips: audioSource.PlayScheduled(AudioSettings.dspTime + 0.5);".writeHint();

                "Clip Duration: double duration = (double)AudioClip.samples / AudioClip.frequency;".writeHint();

                "Time.unscaled time: {0}".F(QcSharp.SecondsToReadableString(Time.unscaledTime)).nl();

                "Time.frameCount: {0}".F(Time.frameCount).nl();

                var tScale = Time.timeScale;
                if ("Time.timescale".edit(ref tScale, 0f, 4f))
                    Time.timeScale = tScale;

                if (Mathf.Approximately(tScale, 1) == false && icon.Refresh.Click())
                    Time.timeScale = 1;

                pegi.nl();

                "Time.deltaTime: {0}".F(QcSharp.SecondsToReadableString(Time.deltaTime)).nl();

                "Time.realtimeSinceStartup {0}".F(QcSharp.SecondsToReadableString(Time.realtimeSinceStartup)).nl();

                var fr = Application.targetFrameRate;
                if ("Frame-Rate".edit(ref fr).nl() && fr > 0)
                {
                    Application.targetFrameRate = fr;
                }
            }

            if ("Services".isEntered(ref inspectedSection, ++sectionIndex).nl())
                Service.Locator.Inspect();

            "Screen Shots".enter_Inspect(screenShots, ref inspectedSection, ++sectionIndex).nl();

            "Json Inspector".enter_Inspect(jsonInspector, ref inspectedSection, ++sectionIndex).nl();

            if ("ICfg Inspector".isEntered(ref inspectedSection, ++sectionIndex).nl())
                iCfgExplorer.Inspect(null).nl();

            if ("Gui Styles".isEntered(ref inspectedSection, ++sectionIndex).nl())
            {
                PEGI_Styles.Inspect().nl();
            }

            if ("Texture Utils".isEntered(ref inspectedSection, ++sectionIndex).nl())
            {

#if UNITY_EDITOR
                Sprite sa = null;

                "To extract a Texture from Sprite, Set Read/Write Enabled to True and make sure it's format is Uncompressed (RGBA32 should do it)".writeHint();

                if ("Extract Sprite Atlas Texture".edit(ref sa) && sa)
                {
                    string atlasName;
                    Texture2D atlasTexture;

                    Packer.GetAtlasDataForSprite(sa, out atlasName, out atlasTexture);

                    //var atlas = SpriteUtility.GetSpriteTexture(sa, getAtlasData: true);
                    if (atlasTexture)
                    {
                        atlasTexture.Reimport_IfNotReadale();

                        string name = atlasName;//"From {0}".F(sa.name);
                        atlasTexture.SaveTextureAsAsset("Atlas Textures", ref name, saveAsNew: true);
                    }
                }
#else
                "Only in Editor".writeWarning();
#endif

            }

            if ("Managed Coroutines [{0}]".F(QcAsync.DefaultCoroutineManager.GetActiveCoroutinesCount).isEntered(ref inspectedSection, ++sectionIndex).nl())
                QcAsync.DefaultCoroutineManager.Nested_Inspect();


            pegi.IsEnteredCheckLast(ref inspectedSection, sectionIndex);
        }

        public static string ToMegabytes(uint bytes)
        {
            bytes >>= 10;
            bytes /= 1024; // On new line to workaround IL2CPP bug
            return "{0} Mb".F(bytes.ToString());
        }
        
        internal static string ToMegabytes(this long bytes)
        {
            bytes >>= 10;
            bytes /= 1024; // On new line to workaround IL2CPP bug
            return "{0} Mb".F(bytes.ToString());
        }

        #endregion
    }
}