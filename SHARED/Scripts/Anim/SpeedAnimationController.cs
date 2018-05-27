using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if PEGI
using PlayerAndEditorGUI;
#endif
using SharedTools_Stuff;
using STD_Logic;

    namespace STD_Animations
{

    public enum SpeedSource { position, scale, shaderVal, rotation }
    
    public static class STD_AnimationExtensions
    {
        public static void DecodeFrame (this Animated_STD_PEGI obj, string data) {
            if (obj != null) {
                var cody = new stdDecoder(data);
                foreach (var tag in cody)
                    obj.DecodeFrame(tag, cody.getData());
            }
        }
    } 

    public interface Animated_STD_PEGI {
        stdEncoder EncodeFrame();
        bool DecodeFrame(string tag, string data);
#if PEGI
        bool Frame_PEGI();
#endif
    }

    public class SpeedAnimationFrame : abstractKeepUnrecognized_STD {

        public SpeedAnimationController mgmt { get { return SpeedAnimationController.inspectedAnimationController; } }

        public AnimatedElement el { get { return AnimatedElement.inspectedAnimatedObject; } }
        
        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.Add("lpos", localPos.Encode());
            cody.Add("lsize", LocalScale.Encode());
            cody.Add("lrot", localRotation.Encode());
            cody.Add("encData", customData.Encode());
            cody.Add("shadeVal", shaderValue.Encode());
            if (isOverrideSpeed)
            {
                cody.Add("src", (int)frameSpeedSource);
                cody.Add("speed", frameSpeed);
            }

            return cody;
        }

        public Countless<Vector3> localPos = new Countless<Vector3>();
        public Countless<Vector3> LocalScale = new Countless<Vector3>();
        public Countless<Quaternion> localRotation = new Countless<Quaternion>();
        public Countless<float> shaderValue = new Countless<float>();
        public Countless<string> customData = new Countless<string>();

        public float frameSpeed = 0.1f;
        public SpeedSource frameSpeedSource = SpeedSource.position;
        public bool isOverrideSpeed;

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "lpos": data.DecodeInto(out localPos); break;
                case "lsize": data.DecodeInto(out LocalScale); break;
                case "lrot": data.DecodeInto(out localRotation); break;
                case "encData": data.DecodeInto(out customData); break;
                case "shadeVal": data.DecodeInto(out shaderValue); break;
                case "speed": frameSpeed = data.ToFloat(); isOverrideSpeed = true; break;
                case "src": frameSpeedSource = (SpeedSource)data.ToInt(); break;
                default: return false;
            }

            return true;
        }

#if PEGI
        public override bool PEGI() {

            pegi.nl();


            if (mgmt.keyElement == el)
            {
                "Override Speed".toggle(90, ref isOverrideSpeed).nl();

                if (isOverrideSpeed)
                {

                    "Frame Speed".edit(ref frameSpeed).nl();

                    if (frameSpeed == 0)
                        "Frame speed is 0, next frame is unreachable ".writeWarning();

                    "Frame Speed Source".editEnum(ref frameSpeedSource).nl();

                }
            }
            return false;
        }
#endif
        public float GetDelta() {

            float speed = isOverrideSpeed ? frameSpeed : mgmt.frameSpeed;

            var speedSource = isOverrideSpeed ? frameSpeedSource : mgmt.speedSource;

            if (speed == 0)
                return 0;

            var key = mgmt.keyElement;

            if (key == null)
                return 0;
            
            float distance = 0;
            switch (speedSource) {
                case SpeedSource.scale: distance = key.deltaLocalScale.magnitude; break;
                case SpeedSource.shaderVal: distance = Mathf.Abs(key.deltaShaderValue); break;
                case SpeedSource.rotation: distance = Mathf.Abs(key.angleOfDeltaLocalRotation); break;
                default: distance = key.deltaLocalPos.magnitude; break;
            }

            return distance > 0 ? Mathf.Clamp01(speed*Time.deltaTime / distance) : 1;
        }
        
        public SpeedAnimationFrame(SpeedAnimationFrame other) {
            if (other != null) {
                other.localPos.Encode().ToString().DecodeInto(out localPos);
                other.LocalScale.Encode().ToString().DecodeInto(out LocalScale);
                other.localRotation.Encode().ToString().DecodeInto(out localRotation);
                other.shaderValue.Encode().ToString().DecodeInto(out shaderValue);
                isOverrideSpeed = other.isOverrideSpeed;
                frameSpeed = other.frameSpeed;
                frameSpeedSource = other.frameSpeedSource;
            }
        }

        public SpeedAnimationFrame() {

        }
    }
    
    [Serializable]
    public class AnimatedElement : abstractKeepUnrecognized_STD
#if PEGI
        ,iPEGI, iGotName, iGotIndex
#endif

    {
        [NonSerialized] MaterialPropertyBlock props;
        public SpeedAnimationController mgmt { get { return SpeedAnimationController.inspectedAnimationController; } }
        public SpeedAnimationFrame frame { get { return SpeedAnimationController.inspectedAnimationController.currentFrame; } }
        public static AnimatedElement inspectedAnimatedObject;

        [SerializeField] int index;
       public int GetIndex()
       {
            return index;
        }

        public void SetIndex(int val)
        {
            index = val;
        }

        [SerializeField] string _name;
        public string NameForPEGI { get { return _name; } set { _name = value; } }
        public Transform transform;
        public Renderer rendy;
        public MonoBehaviour script;
        public string propertyName;
        public float currentShaderValue;

        public override stdEncoder Encode()
        {
            var cody = new stdEncoder();

            cody.Add("i", index);
            cody.AddText("n", _name);
            cody.AddText("prop", propertyName);

            if (script) {
                var asp = script as iSTD;
                if (asp != null)
                    cody.Add("stdDTA", asp.Encode());
            }

            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag)  {
                case "i": index = data.ToInt(); break;
                case "n": _name = data; break;
                case "prop": propertyName = data; break;
                case "stdDTA":
                    if (script) {
                        var asp = script as iSTD;
                        if (asp != null)
                            data.DecodeInto(asp); 
                    } break;
                default: return false;
            }

            return true;
        }

        public Vector3 localPos { get {
                return frame.localPos[index];
            } set { frame.localPos[index] = value; } }
        public Vector3 LocalScale { get { return frame.LocalScale[GetIndex()]; } set { frame.LocalScale[index] = value; } }
        public string customData { get { return frame.customData[GetIndex()]; } set { frame.customData[index] = value; } }
        public Quaternion localRotation { get { return frame.localRotation[GetIndex()]; } set { frame.localRotation[GetIndex()] = value; } }
        public float shaderValue { get {
                return frame.shaderValue[index];
            } set {
                frame.shaderValue[index] = value;
            }
        }

        public Vector3 deltaLocalPos { get { if (!transform) return Vector3.zero; return transform.localPosition - localPos; } }
        public Vector3 deltaLocalScale { get { if (!transform) return Vector3.zero; return transform.localScale - LocalScale; } }
        public float angleOfDeltaLocalRotation { get { if (!transform) return 0; return  Quaternion.Angle(transform.localRotation, localRotation); } }
        public float deltaShaderValue { get { return currentShaderValue - shaderValue; } }
        public Animated_STD_PEGI animSTD { get { if (script) return (script as Animated_STD_PEGI); return null; } }


        public void SetCurrentShaderValue(float value) {
            
            currentShaderValue = value;
            if (rendy) {
                if (props == null) props = new MaterialPropertyBlock();
                props.SetFloat(propertyName, value);
                rendy.SetPropertyBlock(props);
            }
        }
        
        public void Animate(float portion) {
            if (portion == 1)
                Set();
            else {
                if (transform)
                {
                    transform.localPosition -= deltaLocalPos * portion;
                    transform.localScale -= deltaLocalScale * portion;
                    transform.localRotation = Quaternion.Lerp(transform.localRotation, localRotation, portion);
                }

                if (rendy)
                    SetCurrentShaderValue(currentShaderValue - deltaShaderValue * portion);
            }
        }

        public void Set()
        {
            if (transform) {
                transform.localPosition = localPos;
                transform.localScale = LocalScale;
                transform.localRotation = localRotation;
            }
            if (rendy)
                SetCurrentShaderValue(shaderValue);

            animSTD.DecodeFrame(customData);
        }
        
        public void Record() {
            if (mgmt.currentFrame == null)
                return;

            if (transform != null) {
                localPos = transform.localPosition;
                LocalScale = transform.localScale;
                localRotation = transform.localRotation;
            }
            if (rendy)
                shaderValue = currentShaderValue;

            
            var asp = animSTD;
            if (asp != null) 
                    customData = asp.EncodeFrame().ToString();
            
        }

            #if PEGI
        [SerializeField] bool transformInLocalSpace = true;
        [SerializeField] bool showDependencies = true;
        public override bool PEGI() {
            inspectedAnimatedObject = this;

            var key = mgmt.keyElement;
            
            if (this.PEGI_Name().nl() && transform)
                transform.name = NameForPEGI;

            if ("Dependencies".foldout(ref showDependencies).nl())
            {

                if ((key == null || key != this) && "Set As Key".Click())
                    mgmt.keyElement = this;

                var ind = index;
                if ("Index:".edit(50, ref ind).nl())
                    index = ind;
                if ("Transform".edit(80, ref transform).nl() && transform)
                    NameForPEGI = transform.name;

                "STD Script".edit("Use Anumated PEGI interface to add custom data.", 80, ref script).nl();

                "Renderer".edit(80, ref rendy).nl();

                mgmt.currentFrame.PEGI();
            }

            if (rendy)
            {
                "On Material".edit(90, ref propertyName).nl();

                if (pegi.edit(ref currentShaderValue, 0, 1).nl())
                    SetCurrentShaderValue(currentShaderValue);
            }

          

            if (script)
            {
                var asp = script as Animated_STD_PEGI;

                if (asp != null) 
                    asp.Frame_PEGI().nl();
            }

            if (transform)
            {
                transform.PEGI_CopyPaste(ref transformInLocalSpace);

                if (mgmt.transform != transform)
                    transform.PEGI(transformInLocalSpace); //"TF:".edit(() => transform).nl();
            }
   
            inspectedAnimatedObject = null;

            return false;
        }
#endif
        
    }
    
    [ExecuteInEditMode]
    public class SpeedAnimationController : ComponentSTD, iCleanMyself  {

        // Elements
        [SerializeField] List<AnimatedElement> elementsUnsorted = new List<AnimatedElement>();

        [NonSerialized] public Countless<AnimatedElement> elements = new Countless<AnimatedElement>();
        [SerializeField] int keyElementIndex;
        public int indexForNewObject;
        public AnimatedElement keyElement { get { return (elements != null) ? elements[keyElementIndex] : null;  }
            set { keyElementIndex = value.GetIndex(); } }
        
        // Frames
        [SerializeField]  string std_Data;
        [NonSerialized] List<SpeedAnimationFrame> frames = new List<SpeedAnimationFrame>();
        [SerializeField] int frameIndex;
        public bool SetFrameIndex(int newIndex)
        {
            if (frameIndex != newIndex && newIndex < frames.Count && newIndex >= 0)
            {
                if (Application.isPlaying)
                    foreach (var el in elementsUnsorted)
                    el.Set();

                frameIndex = newIndex;

                if (!Application.isPlaying)
                    foreach (var el in elementsUnsorted)
                        el.Set();

                return true;
            }
            return false;
        }
        public SpeedAnimationFrame currentFrame
        {
            get
            {

                if (frames == null || frames.Count == 0)
                    return null;
                frameIndex = frameIndex.ClampZeroTo(frames.Count);

                return frames[frameIndex];
            }
        }
        public SpeedAnimationFrame nextFrame { get { if (frames.Count > frameIndex + 1) return frames[frameIndex + 1]; else return null; } }
        public SpeedAnimationFrame previousFrame { get { if (frameIndex > 0) return frames[frameIndex - 1]; else return null; } }

        // Speed
        public SpeedSource speedSource;
        [SerializeField] float maxSpeed = 1;
        [SerializeField] AnimationCurve speedCurve = new AnimationCurve();
        [SerializeField] bool curveSpeed = false;
        public float frameSpeed { get {

                float portion = 0.5f;

                if (frames.Count > 0) 
                    portion = Mathf.Clamp01(((float)portion / (float)frames.Count) * (frameIndex * 2 + 1));
                
                return curveSpeed ? maxSpeed * speedCurve.Evaluate(portion) : maxSpeed;

            } }
        
        // Management
        [NonSerialized] bool isPaused;
        [NonSerialized] bool playInEditor;
        [NonSerialized] Action _callback;
        [NonSerialized] TestOnceCondition oneTimeCondition;

        public override stdEncoder Encode()  {
            var cody = new stdEncoder();
            cody.AddText("frames", frames.Encode());
            cody.Add("elm", elementsUnsorted);
            cody.Add_Bool("curve", curveSpeed);
            cody.Add("MaxSpeed", maxSpeed);
            cody.Add("src", (int)speedSource);
            cody.Add("Nextind", indexForNewObject);
            cody.Add("KeyElement", keyElementIndex);
            cody.Add("oneTimeCond", oneTimeCondition);
            return cody;
        }

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "curve": curveSpeed = data.ToBool(); break;
                case "MaxSpeed": maxSpeed = data.ToFloat(); break;
                case "src": speedSource = (SpeedSource)data.ToInt(); break;
                case "KeyElement": keyElementIndex = data.ToInt(); break;
                case "frames": data.DecodeInto(out frames); break;
                case "Nextind":indexForNewObject = data.ToInt(); break;
                case "elm":
                    List<AnimatedElement> tmp;
                    data.DecodeInto(out tmp);
                    foreach (var v in tmp) {
                        if (elements[v.GetIndex()] == null) {
                            elementsUnsorted.Add(v);
                            elements[v.GetIndex()] = v;
                        }
                        else
                            elements[v.GetIndex()].Decode(v.Encode());
                    }
                    break;
                case "oneTimeCond": data.DecodeInto(out oneTimeCondition); break;

                default: return false;
            }
            return true;
        }
        
        void Update()
        {
            inspectedAnimationController = this;

            if (!Application.isPlaying)
            {
                if (!playInEditor)
                {
                    if (currentFrame != null)
                        foreach (var el in elementsUnsorted)
                            el.Record();
                }
                else
                    AnimateToPortion(editor_FramePortion);

            }
            else
                if (!isPaused)
            {

                bool isLastFrame = nextFrame == null;

                if (currentFrame != null)
                {
                    float delta = currentFrame.GetDelta();

                    foreach (var el in elementsUnsorted)
                        el.Animate(delta);

                    if (delta == 1)
                    {
                        SetFrameIndex(frameIndex + 1);

                        if (isLastFrame) {
                            if (_callback != null)
                                _callback.Invoke();
                            Destroy(gameObject);
                        }
                    }
                }
            }
        }
        
        public void Init(Action callback)
        {
            _callback = callback;
        }
        
        public static SpeedAnimationController inspectedAnimationController;

        float editor_FramePortion = 0;
#if PEGI
        public int inspectedElement = -1;
        public bool inspectElements = false;
       
        public override bool PEGI() {

            if (gameObject.isPrefab())
                return false;

            bool changed = base.PEGI();

            if (showDebug) {
                if (oneTimeCondition == null && "Add One Time Condition".Click().nl())
                    oneTimeCondition = new TestOnceCondition();

                if (oneTimeCondition != null) {
                    if (icon.Delete.Click())
                        oneTimeCondition = null;
                    else
                        changed |= oneTimeCondition.PEGI().nl();
                }
            }
            else
            {
                
                if (icon.Save.Click())
                {
                    OnDisable();
                    gameObject.UpdatePrefab();

                }
                if (icon.Load.Click().nl())
                    OnEnable();

                inspectedAnimationController = this;


                pegi.write("Speed From:", 70);
                if (currentFrame == null || !currentFrame.isOverrideSpeed)
                    pegi.editEnum(ref speedSource);
                else
                    pegi.write(currentFrame.frameSpeedSource.ToString(), 50); //pegi.editEnum(ref currentFrame.frameSpeedSource);


                if (keyElement != null)
                    ("of " + keyElement.NameForPEGI).nl();
                else
                {
                    pegi.nl();
                    "No Key element".writeWarning();
                }
                pegi.newLine();



                ("Current: " + (frameIndex + 1) + " of " + frames.Count).nl();

                if (frameIndex > 0)
                {
                    if (icon.Undo.ClickUnfocus())
                    {
                        changed = true;
                        SetFrameIndex(frameIndex - 1);
                    }
                }
                else if (icon.UndoDisabled.Click())
                    "First Frame".showNotification();

                if (frameIndex < frames.Count - 1)
                {
                    if (icon.Redo.ClickUnfocus())
                    {
                        changed = true;
                        SetFrameIndex(frameIndex + 1);
                    }

                }
                else if (icon.RedoDisabled.Click())
                    "Last Frame".showNotification();

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
                        foreach (var el in elementsUnsorted)
                            el.Set();
                    }
                }

                if (icon.Add.Click())
                {
                    if (frames.Count == 0)
                        frames.Add(new SpeedAnimationFrame(null));
                    else
                    {
                        frames.Insert(frameIndex + 1, new SpeedAnimationFrame(currentFrame));
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





                if (currentFrame != null)
                {
                    if (icon.Delete.Click().nl())
                    {
                        frames.RemoveAt(frameIndex);
                        SetFrameIndex(frameIndex - 1);
                    }

                    pegi.newLine();

                    if (currentFrame == null || currentFrame.isOverrideSpeed == false)
                    {
                        "Speed".foldout(ref curveSpeed);
                        changed |= pegi.edit(ref maxSpeed).nl();
                        if (curveSpeed)
                            changed |= "Curve:".edit(ref speedCurve).nl();
                    }

                    if (Application.isPlaying || !playInEditor)
                    {
                        var added = elementsUnsorted.edit_List(ref inspectedElement, true, ref changed);
                        if (added != null)
                        {
                            added.NameForPEGI = "New Element";
                            added.propertyName = "_Portion";
                            added.SetIndex(indexForNewObject);
                            indexForNewObject += 1;
                            elements[added.GetIndex()] = added;
                        }
                        else if (changed) UpdateCountless();

                    }
                    else
                        "Playback in progress".nl();
                }

                pegi.newLine();

                if (playInEditor && !Application.isPlaying && currentFrame != null)
                    changed |= "Frame".edit(50, ref editor_FramePortion, 0f, 1f).nl();
            }


            if (!Application.isPlaying && playInEditor && changed)
                AnimateToPortion(editor_FramePortion);


            inspectedAnimationController = null;

            return changed;
        }
#endif


        void AnimateToPortion(float portion) {

            if (previousFrame != null)  {
                frameIndex -= 1;
                foreach (var el in elementsUnsorted)
                    el.Set();
                frameIndex += 1;
            }

            foreach (var el in elementsUnsorted)
                el.Animate(portion);

        }

        void UpdateCountless()
        {
            elements.Clear();

            foreach (var el in elementsUnsorted)
                elements[el.GetIndex()] = el;
        }

        public void OnEnable()
        {

            inspectedAnimationController = this;
            
            Reboot();

            UpdateCountless();

            Decode(std_Data);

            if (currentFrame != null)
                foreach (var el in elementsUnsorted)
                    el.Set();
            
            inspectedAnimationController = null;
        }

        void OnDisable()
        {
            var data = "";

            try {
                 data = Encode().ToString();
            }
            catch(Exception ex) {
                Debug.Log("Failed to Encode animation "+ex.ToString());
                return;
            }

            std_Data = data;
        }
        
        public override void Reboot() {

            if (Application.isPlaying)
                frameIndex = 0;

            if (elements == null)
                elements = new Countless<AnimatedElement>();

            if (frames == null)
                frames = new List<SpeedAnimationFrame>();

        }

        public void StartFadeAway() {
           
        }

        public bool CancelFade()
        {
            return true;
        }
    }
}