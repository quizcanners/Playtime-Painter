using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using StoryTriggerData;
using SharedTools_Stuff;
using UnityEditor;

namespace SharedTools_Stuff {

    public enum SpeedSource { position, scale, shaderVal, rotation }
    
    public class SpeedAnimationFrame : abstractKeepUnrecognized_STD {

        public SpeedAnimationController mgmt { get { return SpeedAnimationController.inspectedAnimationController; } }

        public AnimatedElement el { get { return AnimatedElement.inspectedAnimatedObject; } }
        
        public override stdEncoder Encode() {
            var cody = new stdEncoder();

            cody.Add("lpos", localPos.Encode());
            cody.Add("lsize", LocalScale.Encode());
            cody.Add("lrot", localRotation.Encode());
            cody.Add("shadeVal", shaderValue.Encode());
            if (isOverrideSpeed)
                cody.Add("speed", frameSpeed);

            return cody;
        }

        public Countless<Vector3> localPos = new Countless<Vector3>();
        public Countless<Vector3> LocalScale = new Countless<Vector3>();
        public Countless<Quaternion> localRotation = new Countless<Quaternion>();
        public Countless<float> shaderValue = new Countless<float>();

        public float frameSpeed = 0.1f;
        public bool isOverrideSpeed;

        public override bool Decode(string tag, string data) {
            switch (tag) {
                case "lpos": data.DecodeInto(out localPos); break;
                case "lsize": data.DecodeInto(out LocalScale); break;
                case "lrot": data.DecodeInto(out localRotation); break;
                case "shadeVal": data.DecodeInto(out shaderValue); break;
                case "speed": frameSpeed = data.ToFloat(); isOverrideSpeed = true; break;
                default: return false;
            }

            return true;
        }

        public override bool PEGI() {

            pegi.nl();

            "Override Speed".toggle(90,ref isOverrideSpeed).nl();
            if (isOverrideSpeed) {

                "Frame Speed".edit(ref frameSpeed).nl();

                if (frameSpeed == 0)
                    "Frame speed is 0, next frame is unreachable ".writeWarning();
            }



            return false;
        }

        public float GetDelta() {

            float speed = isOverrideSpeed ? frameSpeed : mgmt.frameSpeed;

            var speedSource = mgmt.speedSource;

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
        
        public override string getDefaultTagName()
        {
            return "frame";
        }

        public SpeedAnimationFrame(SpeedAnimationFrame other) {
            if (other != null) {
                other.localPos.Encode().ToString().DecodeInto(out localPos);
                other.LocalScale.Encode().ToString().DecodeInto(out LocalScale);
                other.localRotation.Encode().ToString().DecodeInto(out localRotation);
                other.shaderValue.Encode().ToString().DecodeInto(out shaderValue);
                isOverrideSpeed = other.isOverrideSpeed;
                frameSpeed = other.frameSpeed;
            }
        }

        public SpeedAnimationFrame() {

        }
    }
    
    [Serializable]
    public class AnimatedElement : abstractKeepUnrecognized_STD ,iPEGI, iGotName, iGotIndex {
        [NonSerialized] MaterialPropertyBlock props;
        public SpeedAnimationController mgmt { get { return SpeedAnimationController.inspectedAnimationController; } }
        public SpeedAnimationFrame frame { get { return SpeedAnimationController.inspectedAnimationController.currentFrame; } }
        public static AnimatedElement inspectedAnimatedObject;

        public int index;
        public int GetIndex() { return index; }

        [SerializeField] string _name;
        public string Name { get { return _name; } set { _name = value; } }
        public Transform transform;
        public Renderer rendy;
        public string propertyName;
        public float currentShaderValue;

        public override stdEncoder Encode()
        {
            var cody = new stdEncoder();

            cody.Add("i", index);
            cody.AddText("n", _name);
            cody.AddText("prop", propertyName);

            return cody;
        }

        public override bool Decode(string tag, string data) {
            switch (tag)  {
                case "i": index = data.ToInt(); break;
                case "n": _name = data; break;
                case "prop": propertyName = data; break;
                default: return false;
            }

            return true;
        }

        public Vector3 localPos { get {
                return frame.localPos[index];
            } set { frame.localPos[index] = value; } }
        public Vector3 LocalScale { get { return frame.LocalScale[this]; } set { frame.LocalScale[index] = value; } }
        public Quaternion localRotation { get { return frame.localRotation[this]; } set { frame.localRotation[this] = value; } }
        public float shaderValue { get {
                return frame.shaderValue[index];

            } set {
                frame.shaderValue[index] = value;
            }
        }

        public Vector3 deltaLocalPos { get { return transform.localPosition - localPos; } }
        public Vector3 deltaLocalScale { get { return transform.localScale - LocalScale; } }
        public float angleOfDeltaLocalRotation { get { return  Quaternion.Angle(transform.localRotation, localRotation); } }
        public float deltaShaderValue { get { return currentShaderValue - shaderValue; } }
        


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
           
        }
        
        [SerializeField] bool transformInLocalSpace = true;
        public override bool PEGI() {
            inspectedAnimatedObject = this;

            var key = mgmt.keyElement;

            if ((key == null || key != this) && "Set As Key".Click())
                mgmt.keyElement = this;

            if (this.PEGI_Name().nl() && transform)
                transform.name = Name;

            "Index".edit(50, ref index).nl();
            if ("Object".edit(70, ref transform).nl() && transform)
                Name = transform.name;

            "Renderer".edit(80, ref rendy).nl();
            if (rendy)  {
                "Property".edit(90, ref propertyName).nl();

                if (pegi.edit(ref currentShaderValue, 0, 1).nl())
                    SetCurrentShaderValue(currentShaderValue);
            }

            mgmt.currentFrame.PEGI();

            if (transform)
            {
                transform.PEGI_CopyPaste(ref transformInLocalSpace);

                if (mgmt.transform != transform)
                    transform.PEGI(transformInLocalSpace); //"TF:".edit(() => transform).nl();
            }
   


           



            inspectedAnimatedObject = null;

            return false;
        }
        
        public override string getDefaultTagName()
        {
            return "animElement";
        }
    }
    
    [ExecuteInEditMode]
    public class SpeedAnimationController : ComponentSTD {

        // Elements

        [SerializeField] List<AnimatedElement> elementsUnsorted = new List<AnimatedElement>();
        [NonSerialized] public Countless<AnimatedElement> elements;
        [SerializeField] int keyElementIndex;
        public int indexForNewObject;
        public AnimatedElement keyElement { get { return (elements != null) ? elements[keyElementIndex] : null;  }
            set { keyElementIndex = value.index; } }
        
        // Frames

        [SerializeField]  string std_Data;
        [NonSerialized]  List<SpeedAnimationFrame> frames;
        [SerializeField] int frameIndex;
        public bool SetFrameIndex(int newIndex)
        {
            if (frameIndex != newIndex && newIndex < frames.Count && newIndex >= 0)
            {
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
        [SerializeField] AnimationCurve speedCurve;
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
        [SerializeField] iSTD_Explorer STDexplorer;
        
        public override stdEncoder Encode()
        {
            var cody = new stdEncoder();
            cody.AddText("frames", frames.Encode());
            cody.Add("elm", elementsUnsorted);
            cody.Add("curve", curveSpeed);
            cody.Add("MaxSpeed", maxSpeed);
            cody.Add("src", (int)speedSource);
            cody.Add("Nextind", indexForNewObject);
            cody.Add("KeyElement", keyElementIndex);
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

                    foreach (var v in tmp)
                    {
                        if (elements[v.index] == null)
                        {
                            elementsUnsorted.Add(v);
                            elements[v.index] = v;
                        }
                    }
                    break;


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
                                _callback();
                            Destroy(gameObject);
                        }
                    }
                }
            }
        }
        
        public void Init(System.Action callback)
        {
            _callback = callback;
        }
        
        public static SpeedAnimationController inspectedAnimationController;
        
        public int inspectedElement = -1;
        public bool inspectElements = false;
        float editor_FramePortion;

        public override bool PEGI() {

            bool changed = false;

            if (icon.save.Click()) {
                OnDisable();
                gameObject.UpdatePrefab();

            }
            if (icon.Load.Click().nl()) 
                OnEnable();

            inspectedAnimationController = this;

            pegi.write("Speed From:", 70);
            speedSource = (SpeedSource)pegi.editEnum(speedSource);
            
            if (keyElement != null)
                ("of " + keyElement.Name).nl();
            else  {
                pegi.nl();
                "No Key element".writeWarning();
            }
            pegi.newLine();

            if (currentFrame == null || currentFrame.isOverrideSpeed == false) {
                "Speed".foldout(ref curveSpeed);
                changed |= pegi.edit(ref maxSpeed).nl();
                if (curveSpeed)
                changed |= "Curve:".edit(ref speedCurve).nl();
            }
            
            ("Current: " + (frameIndex+1) + " of " + frames.Count).nl();

            if (frameIndex > 0) {
                if (icon.Undo.ClickUnfocus())
                {
                    changed = true;
                    SetFrameIndex(frameIndex - 1);
                }
            }
            else if (icon.UndoDisabled.Click())
                "First Frame".showNotification();
            
            if (frameIndex < frames.Count - 1) {
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
                if ((playInEditor ? icon.Pause : icon.Play).Click()) {
                    playInEditor = !playInEditor;
                    foreach (var el in elementsUnsorted)
                        el.Set();
                }
            }
            
            if (icon.Add.Click())
            {
                if (frames.Count == 0)
                    frames.Add(new SpeedAnimationFrame(null));
                else {
                    frames.Insert(frameIndex + 1, new SpeedAnimationFrame(currentFrame));
                    SetFrameIndex(frameIndex+1);
                }
            }

            if (icon.Search.Click()) {
               

                foreach (var e in elementsUnsorted) {

                    if (e.Name.SameAs(transform.name))
                    {
                        var ren = transform.GetComponent<Renderer>();
                        if (ren) e.rendy = ren;
                        e.transform = transform;
                        break;
                    }

                    foreach (Transform t in transform) {
                        if (t.name.SameAs(e.Name)) {
                            var ren = t.GetComponent<Renderer>();
                            if (ren) e.rendy = ren;
                            e.transform = t;
                            break;
                        }
                    }
                }
            }

            if (currentFrame != null) {
                if (icon.Delete.Click().nl()) {
                    frames.RemoveAt(frameIndex);
                    SetFrameIndex(frameIndex - 1);
                }

                if (Application.isPlaying || !playInEditor) {
                    var added = elementsUnsorted.PEGI(ref inspectedElement, true, ref changed);
                    if (added != null) {
                        added.Name = "New Element";
                        added.propertyName = "_Portion";
                        added.index = indexForNewObject;
                        indexForNewObject += 1;
                        elements[added.index] = added;
                    } else if (changed) UpdateCountless(); 

                }
                else
                    "Playback in progress".nl();
            }

            pegi.newLine();

            if (playInEditor && !Application.isPlaying && currentFrame!= null)
                    changed |= "Frame".edit(50, ref editor_FramePortion, 0f, 1f).nl();

            changed |= this.PEGI(ref STDexplorer).nl(); 

           

            if (!Application.isPlaying && playInEditor && changed)
                AnimateToPortion(editor_FramePortion);


            inspectedAnimationController = null;

            return changed;
        }
     
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
                elements[el.index] = el;
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
        
        public override string getDefaultTagName()
        {
            return "SpeedAnim";
        }

        public override void Reboot() {

            if (Application.isPlaying)
                frameIndex = 0;

            if (elements == null)
                elements = new Countless<AnimatedElement>();

            if (frames == null)
                frames = new List<SpeedAnimationFrame>();

        }
    }
}