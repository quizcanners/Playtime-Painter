using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using STD_Logic;

namespace STD_Animations
{

    [ExecuteInEditMode]
    public class SpeedAnimationController : ComponentSTD, IManageFading, ICallAfterIFinish, IKeepMySTD {

        List<AnimatedElement> elementsUnsorted = new List<AnimatedElement>();

        [NonSerialized] public Countless<AnimatedElement> elements = new Countless<AnimatedElement>();
        [SerializeField] int keyElementIndex;
        public int indexForNewObject;
        public AnimatedElement KeyElement {
            get { return elements?[keyElementIndex]; }
            set { keyElementIndex = value.IndexForPEGI; }
        }

        public bool decodeOnEnable = false;

        // Frames
        [SerializeField] string std_Data;
        [NonSerialized] List<SpeedAnimationFrame> frames = new List<SpeedAnimationFrame>();
        [SerializeField] int frameIndex;
        public bool SetFrameIndex(int newIndex)
        {
            if (frameIndex != newIndex && newIndex < frames.Count && newIndex >= 0) {

                if (Application.isPlaying)
                    Set();

                frameIndex = newIndex;

                OnFrameChange();

                if (!Application.isPlaying)
                    Set();

                return true;
            }
            return false;
        }
        public SpeedAnimationFrame CurrentFrame
        {
            get
            {

                if (frames == null || frames.Count == 0)
                    return null;
                frameIndex = frameIndex.ClampZeroTo(frames.Count);

                return frames[frameIndex];
            }
        }
        public SpeedAnimationFrame NextFrame { get { if (frames.Count > frameIndex + 1) return frames[frameIndex + 1]; else return null; } }
        public SpeedAnimationFrame PreviousFrame { get { if (frameIndex > 0) return frames[frameIndex - 1]; else return null; } }

        // Speed
        public SpeedSource speedSource;
        public float _timer = 0;
        public float TimerToPortion() {
            float portion = 1;

            var f = CurrentFrame;
            if (f == null)
                return portion;

            float currentTime = _timer + Time.deltaTime;

            if (currentTime < f.frameDuration)
                portion = Time.deltaTime / (f.frameDuration - _timer);

            _timer = currentTime;

            return portion;
        }

        [SerializeField] float maxSpeed = 1;
        [SerializeField] AnimationCurve speedCurve = new AnimationCurve();
        [SerializeField] bool curveSpeed = false;
        public float FrameSpeed
        {
            get
            {

                float portion = 0.5f;

                if (frames.Count > 0)
                    portion = Mathf.Clamp01(((float)portion / (float)frames.Count) * (frameIndex * 2 + 1));

                return curveSpeed ? maxSpeed * speedCurve.Evaluate(portion) : maxSpeed;

            }
        }

        public string Config_STD { get {  return std_Data;  } set  { std_Data = value; } }

        // Management
        bool setFirstFrame;
        [NonSerialized] bool isPaused = false;
        [NonSerialized] bool playInEditor = true;

        public Action onFinish;
        public void SetCallback(Action OnFinish)
        {
            onFinish += OnFinish;
        }

        #region Encode/Decode

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add("frames", frames)
            .Add("elm", elementsUnsorted, this)
            .Add_Bool("curve", curveSpeed)
            .Add("MaxSpeed", maxSpeed)
            .Add("src", (int)speedSource)
            .Add("Nextind", indexForNewObject)
            .Add("KeyElement", keyElementIndex)
            .Add_Bool("first", setFirstFrame);

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "curve": curveSpeed = data.ToBool(); break;
                case "MaxSpeed": maxSpeed = data.ToFloat(); break;
                case "src": speedSource = (SpeedSource)data.ToInt(); break;
                case "KeyElement": keyElementIndex = data.ToInt(); break;
                case "frames": data.Decode_List(out frames); break;
                case "Nextind": indexForNewObject = data.ToInt(); break;
                case "first": setFirstFrame = data.ToBool(); break;

                case "elm":
                    List<AnimatedElement> tmp;
                    data.Decode_List(out tmp, this);
                    foreach (var v in tmp)
                    {
                        if (elements[v.IndexForPEGI] == null)
                        {
                            elementsUnsorted.Add(v);
                            elements[v.IndexForPEGI] = v;
                        }
                        else
                            elements[v.IndexForPEGI].Decode(v.Encode().ToString());
                    }
                    break;

                default: return false;
            }
            return true;
        }

        public override ISTD Decode(string data) {

            processedAnimationController = this;

            Reboot();

            UpdateCountless();

            base.Decode(data);

            if (setFirstFrame && CurrentFrame != null) {
                OnFrameChange();
                Set();
            }

            processedAnimationController = null;

            _timer = 0;

            return this;

        }

        #endregion
        
        #region Inspector

        public static SpeedAnimationController processedAnimationController;

        float editor_FramePortion = 0;
#if PEGI
        public int inspectedElement = -1;
        public bool inspectElements = false;

        public override bool Inspect()  {

            if (gameObject.IsPrefab())
                return false;

            bool changed = base.Inspect();

            if (inspectedStuff == -1) {
                
                if (icon.Save.Click()) {
                    this.Save_STDdata();
                    this.UpdatePrefab(gameObject);
                }

                if (icon.Load.Click().nl())
                    this.Load_STDdata();

                "Auto Load".toggle("Object will load it's own data OnEnable", 70, ref decodeOnEnable).nl();

                processedAnimationController = this;
                
                pegi.write("Speed From:", 70);
                if (CurrentFrame == null || !CurrentFrame.isOverrideSpeed)
                    pegi.editEnum(ref speedSource);
                else
                    pegi.write(CurrentFrame.frameSpeedSource.ToString(), 50);

                if (speedSource != SpeedSource.time)  {

                    if (KeyElement != null)
                        ("of " + KeyElement.NameForPEGI).nl();
                    else {
                        pegi.nl();
                        "No Key element".writeWarning();
                    }
                }

                pegi.newLine();

                if (frameIndex == 0)
                    "Set First Frame".toggle("Will first frame be set instead of transitioned to", 90, ref setFirstFrame).nl();

                if (frameIndex > 0)
                {
                    if (icon.Undo.ClickUnfocus(ref changed)) {
                        SetFrameIndex(frameIndex - 1);
                        editor_FramePortion = 1;
                    }
                }
                else if (icon.UndoDisabled.Click())
                    "First Frame".showNotificationIn3D_Views();

                if (frameIndex < frames.Count - 1)
                {
                    if (icon.Redo.ClickUnfocus(ref changed))
                    {
                        SetFrameIndex(frameIndex + 1);
                        editor_FramePortion = 0;
                    }

                }
                else if (icon.RedoDisabled.Click())
                    "Last Frame".showNotificationIn3D_Views();

                if (Application.isPlaying)
                {
                    if (isPaused && frames.Count > 0 && icon.Play.Click())
                    {
                        isPaused = false;
                        SetFrameIndex(0);
                    }

                    if (!isPaused && icon.Pause.Click())
                        isPaused = true;
                }
                else
                {
                    if ((playInEditor ? icon.Pause : icon.Play).Click())
                    {
                        playInEditor = !playInEditor;
                        Set();
                    }
                }

                if (icon.Add.Click())
                {
                    if (frames.Count == 0)
                        frames.Add(new SpeedAnimationFrame(null));
                    else
                    {
                        frames.Insert(frameIndex + 1, new SpeedAnimationFrame(CurrentFrame));
                        SetFrameIndex(frameIndex + 1);
                    }
                }

                if (icon.Search.Click())
                {
                    
                    foreach (var e in elementsUnsorted)
                    {

                        if (e.NameForPEGI.SameAs(transform.name))
                        {
                            var ren = transform.GetComponent<Renderer>();
                            if (ren) e.rendy = ren;
                            e.transform = transform;
                            break;
                        }

                        foreach (Transform t in transform)
                        {
                            if (t.name.SameAs(e.NameForPEGI))
                            {
                                var ren = t.GetComponent<Renderer>();
                                if (ren) e.rendy = ren;
                                e.transform = t;
                                break;
                            }
                        }
                    }
                }

                if (CurrentFrame != null)
                {
                    if (icon.Delete.Click().nl())
                    {
                        frames.RemoveAt(frameIndex);
                        SetFrameIndex(frameIndex - 1);
                    }
                    
                    if (speedSource != SpeedSource.time) {
                        if (inspectedElement == -1 && (CurrentFrame == null || CurrentFrame.isOverrideSpeed == false)) {
                            "Speed".foldout(ref curveSpeed);
                            changed |= pegi.edit(ref maxSpeed).nl();
                            if (curveSpeed)
                                changed |= "Curve:".edit(ref speedCurve).nl();
                        }
                    }
                    else
                        if (CurrentFrame != null)
                        "Time (sec) ".edit(60, ref CurrentFrame.frameDuration).nl();

                    if (Application.isPlaying || !playInEditor) {
                        var added = pegi.edit_List(ref elementsUnsorted, ref inspectedElement,  ref changed);
                        if (added != null)
                        {
                            added.NameForPEGI = "";
                            added.propertyName = "";
                            added.IndexForPEGI = indexForNewObject;
                            indexForNewObject += 1;
                            elements[added.IndexForPEGI] = added;
                        }
                        else if (changed) UpdateCountless();

                    }
                    else
                        "Playback in progress".nl();
                }

                pegi.newLine();

                if (playInEditor && !Application.isPlaying && CurrentFrame != null)
                    changed |= "Frame".edit(50, ref editor_FramePortion, 0f, 1f).nl();
            }


            if (!Application.isPlaying && playInEditor && changed)
                AnimateToPortion(editor_FramePortion);


            processedAnimationController = null;

            return changed;
        }
#endif

        #endregion

        void Update()
        {
            processedAnimationController = this;

            if (!Application.isPlaying)
            {
                if (!playInEditor)
                {
                    if (CurrentFrame != null)
                        foreach (var el in elementsUnsorted)
                            el.Record();
                }
                else
                    AnimateToPortion(editor_FramePortion);

            }
            else
                if (!isPaused)
            {

                bool isLastFrame = NextFrame == null;

                if (CurrentFrame != null)
                {

                    float delta;

                    //if (frameIndex == 0 && setFirstFrame)
                    //  delta = 1;
                    //else
                    delta = CurrentFrame.GetDelta();

                    foreach (var el in elementsUnsorted)
                        el.Animate(delta);

                    if (delta == 1)
                    {
                        SetFrameIndex(frameIndex + 1);

                        if (isLastFrame)
                        {
                            onFinish?.Invoke();
                            Destroy(gameObject);
                        }

                    }
                }
            }
        }
        
        void AnimateToPortion(float portion) {

            if (PreviousFrame != null) {
                frameIndex -= 1;
                OnFrameChange();
                Set();
                frameIndex += 1;
                OnFrameChange();
            }

            foreach (var el in elementsUnsorted)
                el.Animate(portion);

        }

        void OnFrameChange()
        {
            foreach (var el in elementsUnsorted)
                el.OnFrameChanged();

            _timer = 0;
        }

        void Set() {
            foreach (var el in elementsUnsorted)
                el.Set();

            _timer = 0;
        }

        void UpdateCountless()
        {
            elements.Clear();

            foreach (var el in elementsUnsorted)
                elements[el.IndexForPEGI] = el;
        }

        public void OnEnable()
        {
            if (decodeOnEnable)
                this.Load_STDdata();
            
        }
        
        public void Reboot()
        {
            if (Application.isPlaying)
                frameIndex = 0;

            if (elements == null)
                elements = new Countless<AnimatedElement>();

            if (frames == null)
                frames = new List<SpeedAnimationFrame>();
        }

        public void FadeAway()
        {

        }

        public bool TryFadeIn()
        {
            return false;
        }

    }

    public interface ICallAfterIFinish {
        void SetCallback(Action OnFinish);
    }

    public enum SpeedSource { position, scale, shaderVal, rotation, time }

    public static class STD_AnimationExtensions
    {
        public static void DecodeFrame(this IAnimated_STD_PEGI obj, string data)
        {
            if (obj != null)
            {
                var cody = new StdDecoder(data);
                foreach (var tag in cody)
                    obj.DecodeFrame(tag, cody.GetData());
            }
        }
    }

    public interface IAnimated_STD_PEGI
    {
        StdEncoder EncodeFrame();
        bool DecodeFrame(string tag, string data);
        void AnimatePortion(float portion);
        void SetFrame();
#if PEGI
        bool Frame_PEGI();
#endif
    }


    //[Serializable]
    public class AnimatedElement : AbstractKeepUnrecognized_STD, IPEGI, IGotName, IGotIndex, IPEGI_ListInspect {

        [NonSerialized] MaterialPropertyBlock props;
        public SpeedAnimationController Mgmt { get { return SpeedAnimationController.processedAnimationController; } }
        public SpeedAnimationFrame Frame { get { return SpeedAnimationController.processedAnimationController.CurrentFrame; } }
        public static AnimatedElement inspectedAnimatedObject;

        [SerializeField] int index;

        public int IndexForPEGI { get { return index; } set { index = value; } }

        [SerializeField] string _name;
        public string NameForPEGI { get { return _name; } set { _name = value; } }
        public Transform transform;
        public Renderer rendy;
        public MonoBehaviour script;
        public ParticleSystem particles; 

        public string propertyName;
        public float currentShaderValue;

        public override StdEncoder Encode()
        {
            var cody = this.EncodeUnrecognized()
            .Add("i", index)
            .Add_String("n", _name)
            .Add_String("prop", propertyName)
            .Add_Reference("tf", transform)
            .Add_Reference("ren", rendy)
            .Add_Reference("scrpt", script)
            .Add_Reference("ps", particles);
            if (script)  {
                var asp = script as ISTD;
                if (asp != null)
                    cody.Add("stdDTA", asp.Encode());
            }

            return cody;
        }

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "i": index = data.ToInt(); break;
                case "n": _name = data; break;
                case "prop": propertyName = data; break;
                case "stdDTA": data.TryDecodeInto(script); break;
                case "tf": data.Decode_Reference(ref transform); break;
                case "ren": data.Decode_Reference(ref rendy); break;
                case "scrpt": data.Decode_Reference(ref script); break;
                case "ps": data.Decode_Reference(ref particles); break;
                default: return false;
            }

            return true;
        }

        public Vector3 LocalPos
        {
            get
            {
                return Frame.localPos[index];
            }
            set { Frame.localPos[index] = value; }
        }
        public Vector3 LocalScale { get { return Frame.LocalScale[IndexForPEGI]; } set { Frame.LocalScale[index] = value; } }
        public string CustomData { get { return Frame.customData[IndexForPEGI]; } set { Frame.customData[index] = value; } }
        public Quaternion LocalRotation { get { return Frame.localRotation[IndexForPEGI]; } set { Frame.localRotation[IndexForPEGI] = value; } }
        public bool Emit { get { return Frame.emit[IndexForPEGI]; } set { Frame.emit[IndexForPEGI] = value; } }
        public float ShaderValue
        {
            get
            {
                return Frame.shaderValue[index];
            }
            set
            {
                Frame.shaderValue[index] = value;
            }
        }

        public Vector3 DeltaLocalPos { get { if (!transform) return Vector3.zero; return transform.localPosition - LocalPos; } }
        public Vector3 DeltaLocalScale { get { if (!transform) return Vector3.zero; return transform.localScale - LocalScale; } }
        public float AngleOfDeltaLocalRotation { get { if (!transform) return 0; return Quaternion.Angle(transform.localRotation, LocalRotation); } }
        public float DeltaShaderValue { get { return currentShaderValue - ShaderValue; } }
        public IAnimated_STD_PEGI AnimSTD => script as IAnimated_STD_PEGI;
        
        public void SetCurrentShaderValue(float value)
        {

            currentShaderValue = value;
            if (rendy) {
                if (props == null)
                    props = new MaterialPropertyBlock();

                    rendy.GetPropertyBlock(props);
                  
                props.SetFloat(propertyName, value);
                rendy.SetPropertyBlock(props);
            }
        }

        public void Animate(float portion)
        {
            if (particles)  {
                if (Emit)
                    particles.Play();
                else
                    particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }

            if (portion == 1)
                Set();
            else
            {
                if (transform)
                {
                    transform.localPosition -= DeltaLocalPos * portion;
                    transform.localScale -= DeltaLocalScale * portion;
                    transform.localRotation = Quaternion.Lerp(transform.localRotation, LocalRotation, portion);
                }

                if (rendy)
                    SetCurrentShaderValue(currentShaderValue - DeltaShaderValue * portion);

                var astd = AnimSTD;
                if (astd != null)
                    astd.AnimatePortion(portion);
            }

        
        }

        public void OnFrameChanged() {
            var astd = AnimSTD;
            if (astd != null)
                astd.DecodeFrame(CustomData);
        }

        public void Set() {
            if (transform) {
                transform.localPosition = LocalPos;
                transform.localScale = LocalScale;
                transform.localRotation = LocalRotation;
            }

            if (rendy)
                SetCurrentShaderValue(ShaderValue);

            var astd = AnimSTD;
            if (astd != null)
                astd.SetFrame();
        }

        public void Record()
        {
            if (Mgmt.CurrentFrame == null)
                return;

            if (transform != null)
            {
                LocalPos = transform.localPosition;
                LocalScale = transform.localScale;
                LocalRotation = transform.localRotation;
            }
            if (rendy)
                ShaderValue = currentShaderValue;


            var asp = AnimSTD;
            if (asp != null)
                CustomData = asp.EncodeFrame().ToString();

        }

#if PEGI
        [SerializeField] bool transformInLocalSpace = true;
        [SerializeField] bool showDependencies = true;
        List<string> materialProperties = new List<string>();
        public override bool Inspect()
        {
            inspectedAnimatedObject = this;

            var key = Mgmt.KeyElement;

            if (this.inspect_Name().nl() && transform)
                transform.name = NameForPEGI;

            if ("Dependencies".foldout(ref showDependencies).nl())
            {

                if ((key == null || key != this) && "Set As Key".Click())
                    Mgmt.KeyElement = this;

                var ind = index;
                if ("Index:".edit(50, ref ind).nl())
                    index = ind;
                if ("Transform".edit(80, ref transform).nl() && transform)
                    NameForPEGI = transform.name;

                if ("Renderer".edit(80, ref rendy).nl() && rendy)
                    materialProperties = rendy.sharedMaterial.GetFloatFields();

                "Particles".edit(80, ref particles);
                if (particles) {
                    bool emit = Emit;
                    if (pegi.toggle(ref emit))
                        Emit = emit;
                }

                pegi.nl();
                if ("STD Script".edit("Use Anumated PEGI interface to add custom data.", 80, ref script).nl()) {
                    if (script != null) {

                        if (script as IAnimated_STD_PEGI == null) {
                            var cmpnts = script.gameObject.GetComponent<IAnimated_STD_PEGI>();
                            if (cmpnts != null)
                                script = cmpnts as MonoBehaviour;
                        }

                    }
                }

            }

            Mgmt.CurrentFrame.Inspect();

            if (rendy)  {
                if (propertyName.Length > 0 && icon.Delete.Click())
                    propertyName = "";

                "Mat Property".select(90, ref propertyName, materialProperties);

                if (icon.Refresh.Click().nl()) 
                    materialProperties = rendy.sharedMaterial.GetFloatFields();
                
                if (pegi.edit(ref currentShaderValue, 0, 1).nl())
                    SetCurrentShaderValue(currentShaderValue);
            }

            (script as IAnimated_STD_PEGI)?.Frame_PEGI().nl();

            if (transform) {
                transform.PEGI_CopyPaste(ref transformInLocalSpace);
                transform.inspect(transformInLocalSpace); //"TF:".edit(() => transform).nl();
            }

            inspectedAnimatedObject = null;

            return false;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited) {
            var astd = AnimSTD;
            if (astd!= null) {
                var ilp = astd as IPEGI_ListInspect;
                if (ilp != null)
                    return (ilp.PEGI_inList(list, ind, ref edited));
            }

            if (particles != null) {
                bool emit = Emit;
                if (particles.name.toggle(ref emit))
                    Emit = emit;
            }
            else {
                if (rendy)
                {
                    if (propertyName.Length == 0) {

                        if (materialProperties.Count == 0)
                            materialProperties = rendy.sharedMaterial.GetFloatFields();

                        "Mat Property".select(90, ref propertyName, materialProperties);
                    }
                    else
                    if (propertyName.edit(ref currentShaderValue, 0, 1))
                        SetCurrentShaderValue(currentShaderValue);
                }
                else if (transform != null)
                {
                    transform.name.write();
                    transform.clickHighlight();
                }
                else
                {
                    if (pegi.edit(ref transform) && transform)
                        NameForPEGI = transform.name;
                    if (pegi.edit(ref rendy) && rendy && rendy.sharedMaterial)
                        NameForPEGI = rendy.sharedMaterial.name;
                }

            }

            if (icon.Enter.Click(NameForPEGI))
                edited = ind;

            return false;
        }
#endif

    }

    public class SpeedAnimationFrame : AbstractKeepUnrecognized_STD, IPEGI  {

        public SpeedAnimationController Mgmt { get { return SpeedAnimationController.processedAnimationController; } }

        public AnimatedElement El { get { return AnimatedElement.inspectedAnimatedObject; } }


        public Countless<Vector3> localPos = new Countless<Vector3>();
        public Countless<Vector3> LocalScale = new Countless<Vector3>();
        public Countless<Quaternion> localRotation = new Countless<Quaternion>();
        public Countless<float> shaderValue = new Countless<float>();
        public Countless<string> customData = new Countless<string>();
        public CountlessBool emit = new CountlessBool();


        public float frameSpeed = 0.1f;
        public float frameDuration = 0.1f;

        public SpeedSource frameSpeedSource = SpeedSource.position;
        public bool isOverrideSpeed;
        public bool allowGapToEndFrame; // Means a frame can be ended before elements reach it's destination state
        public float gap;

        #region Ecoding_Decoding

        public override StdEncoder Encode()
        {
            var cody = this.EncodeUnrecognized()
            .Add("lpos", localPos.Encode())
            .Add("lsize", LocalScale.Encode())
            .Add("lrot", localRotation.Encode())
            .Add("encData", customData.Encode())
            .Add("shadeVal", shaderValue.Encode())
            .Add("emt", emit)
            .Add("time", frameDuration)
            .Add_Bool("isGap", allowGapToEndFrame);

            if (allowGapToEndFrame)
                cody.Add("gap", gap);

            if (isOverrideSpeed) {
                cody.Add("src", (int)frameSpeedSource)
                .Add("speed", frameSpeed);
            }

            return cody;
        }


        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "lpos": data.DecodeInto_Countless(out localPos); break;
                case "lsize": data.DecodeInto_Countless(out LocalScale); break;
                case "lrot": data.DecodeInto_Countless(out localRotation); break;
                case "encData": data.DecodeInto_Countless(out customData); break;
                case "shadeVal": data.DecodeInto_Countless(out shaderValue); break;
                case "speed": frameSpeed = data.ToFloat(); isOverrideSpeed = true; break;
                case "src": frameSpeedSource = (SpeedSource)data.ToInt(); break;
                case "emt": data.DecodeInto(out emit); break;
                case "time": frameDuration = data.ToFloat(); break;
                case "isGap": allowGapToEndFrame = data.ToBool();
                    if (!allowGapToEndFrame) gap = 1; break;
                case "gap": gap = data.ToFloat(); break; 
                default: return false;
            }

            return true;
        }

        #endregion

#if PEGI
        public override bool Inspect()
        {

            pegi.nl();

            if (Mgmt.KeyElement == El && Mgmt.speedSource != SpeedSource.time)
            {

                "Override Speed".toggle(90, ref isOverrideSpeed).nl();

                if (isOverrideSpeed)
                {

                    "Frame Speed".edit(ref frameSpeed).nl();

                    if (frameSpeed == 0)
                        "Frame speed is 0, next frame is unreachable ".writeWarning();

                    var before = frameSpeedSource;

                    if ("Frame Speed Source".editEnum(ref frameSpeedSource).nl() && frameSpeedSource == SpeedSource.time)
                    {
                        frameSpeedSource = before;
                        Mgmt.speedSource = SpeedSource.time;
                    }
                }
            }
            return false;
        }
#endif

        public float GetDelta()
        {

            if (Mgmt.speedSource == SpeedSource.time)
                return Mgmt.TimerToPortion();

            float speed = isOverrideSpeed ? frameSpeed : Mgmt.FrameSpeed;

            var speedSource = isOverrideSpeed ? frameSpeedSource : Mgmt.speedSource;

            if (speed == 0)
                return 0;

            var key = Mgmt.KeyElement;

            if (key == null)
                return 0;

            float distance = 0;
            switch (speedSource)
            {
                case SpeedSource.scale: distance = key.DeltaLocalScale.magnitude; break;
                case SpeedSource.shaderVal: distance = Mathf.Abs(key.DeltaShaderValue); break;
                case SpeedSource.rotation: distance = Mathf.Abs(key.AngleOfDeltaLocalRotation); break;
                default: distance = key.DeltaLocalPos.magnitude; break;
            }

            return distance > 0 ? Mathf.Clamp01(speed * Time.deltaTime / distance) : 1;
        }

        public SpeedAnimationFrame(SpeedAnimationFrame other)
        {
            if (other != null)
            {
                other.localPos.Encode().ToString().DecodeInto_Countless(out localPos);
                other.LocalScale.Encode().ToString().DecodeInto_Countless(out LocalScale);
                other.localRotation.Encode().ToString().DecodeInto_Countless(out localRotation);
                other.shaderValue.Encode().ToString().DecodeInto_Countless(out shaderValue);
                other.emit.Encode().ToString().DecodeInto(out emit);
                isOverrideSpeed = other.isOverrideSpeed;
                frameSpeed = other.frameSpeed;
                frameSpeedSource = other.frameSpeedSource;
            }
        }

        public SpeedAnimationFrame()
        {

        }

    }
}