using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using UnityEngine.UI;

namespace QuizCannersUtilities {

    public interface ILinkedLerping {
        void Portion(LerpData ld); 
        void Lerp(LerpData ld, bool canTeleport);
    }

    public class LerpData : IPEGI, IGotName, IGotCount, IPEGI_ListInspect {
        public float linkedPortion = 1;
        public float teleportPortion;
        private float _minPortion = 1;
        private int _resets;
        public string dominantParameter = "None";

        public float Portion(bool canTeleport = false) => canTeleport ? Mathf.Max(teleportPortion, linkedPortion) : linkedPortion;

        public float MinPortion { get { return Mathf.Min(_minPortion, linkedPortion); } set { _minPortion = Mathf.Min(_minPortion, value); }  }

        public int CountForInspector => _resets;

        public string NameForPEGI { get { return dominantParameter; } set { dominantParameter = value; } }

        public void Reset() {
            teleportPortion = 0;
            linkedPortion = 1;
            _minPortion = 1;
            _resets++;
        }

        #region Inspector
        #if PEGI
        public bool Inspect() {
            var changed = false;

            "Dominant Parameter".edit(ref dominantParameter).nl(ref changed);

            "Reboot calls".edit(ref _resets).nl(ref changed);

            "teleport portion: {0}".F(teleportPortion).nl();

            "min Portion {0} ".F(_minPortion).nl();

            return changed;
        }

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            "Lerp DP: {0} [{1}]".F(dominantParameter, _resets).write();

            if (icon.Refresh.Click("Reset stats")) {
                dominantParameter = "None";
                _resets = 0;
            }

            if (icon.Enter.Click())
                edited = ind;

            return false;
        }
        #endif
        #endregion
    }

    public interface IManageFading
    {
        void FadeAway();
        bool TryFadeIn();
    }

    public class LinkedLerp
    {

        public enum LerpSpeedMode { SpeedTreshold = 0, Unlimited = 1, LerpDisabled = 2, UnlinkedSpeed = 3 }

        #region Abstract Base
        public abstract class BaseAnyValue : AbstractStd, ILinkedLerping, IPEGI, IPEGI_ListInspect {

            public LerpSpeedMode lerpMode = LerpSpeedMode.SpeedTreshold;
            public virtual bool UsingLinkedThreshold => (lerpMode == LerpSpeedMode.SpeedTreshold && Application.isPlaying);
            public virtual bool Enabled => lerpMode != LerpSpeedMode.LerpDisabled;

            protected virtual bool EaseInOutImplemented => false;

            protected bool easeInOut;

            protected bool defaultSet;
            public float speedLimit = 1;
            protected bool allowChangeParameters = true;

            protected abstract string Name { get;  } 

            #region Encode & Decode
            public override StdEncoder Encode() {

                var cody = new StdEncoder()
                    .Add_Bool("ch", allowChangeParameters);

                if (allowChangeParameters) {

                    if (EaseInOutImplemented)
                        cody.Add_Bool("eio", easeInOut);

                    cody.Add("lm",(int)lerpMode);

                    if (lerpMode == LerpSpeedMode.SpeedTreshold)
                    cody.Add("sp", speedLimit);
                }

                return cody;
            }

            public override bool Decode(string tg, string data) {
                switch (tg) {
                    case "ch": allowChangeParameters = data.ToBool(); break;
                    case "sp": speedLimit = data.ToFloat(); break;
                    case "lm": lerpMode = (LerpSpeedMode)data.ToInt(); break;
                    case "eio": easeInOut = data.ToBool(); break;
                    default: return false;
                }
                return true;
            }
            #endregion

            public void Lerp(LerpData ld, bool canTeleport = false) {
                if (Enabled) {

                    float p;

                    switch (lerpMode) {
                        case LerpSpeedMode.LerpDisabled: p = 0; break;
                        case LerpSpeedMode.UnlinkedSpeed: p = 1;
                            if (Application.isPlaying)
                                Portion(ref p);

                            if (canTeleport)
                                p = Mathf.Max(p, ld.teleportPortion);

                            break;
                        default: p = ld.Portion(canTeleport); break;
                    }

                    Lerp_Internal(p);
                    defaultSet = true;
                }
                }

            public abstract bool Lerp_Internal(float linkedPortion);

            public virtual void Portion(LerpData ld) {
                if (UsingLinkedThreshold && Portion(ref ld.linkedPortion))
                    ld.dominantParameter = Name;
                else if (lerpMode == LerpSpeedMode.UnlinkedSpeed) {
                    float portion = 1;
                    Portion(ref portion);
                    ld.MinPortion = portion;
                }
            }

            public abstract bool Portion(ref float linkedPortion);
                  
            #region Inspector
#if PEGI
            public virtual bool PEGI_inList(IList list, int ind, ref int edited) {

                var changed = false;

       
                if (!allowChangeParameters)
                    Name.toggleIcon("Will this config contain new parameters", ref allowChangeParameters).changes(ref changed);
                else {

                    if (Application.isPlaying)
                        (Enabled ? icon.Active : icon.InActive).write(Enabled ? "Lerp Possible" : "Lerp Not Possible");

                    if (lerpMode == LerpSpeedMode.SpeedTreshold)
                        (Name + " Thld").edit(170, ref speedLimit).changes(ref changed);
                    else if (lerpMode == LerpSpeedMode.UnlinkedSpeed)
                        (Name + " Speed").edit(170, ref speedLimit).changes(ref changed);
                    else (Name + " Mode").editEnum(120, ref lerpMode).changes(ref changed);
                }

                if (icon.Enter.Click())
                    edited = ind;

                return changed;
            }

            public virtual bool Inspect() {

                var changed = "Edit".toggleIcon("Will this config contain new parameters", ref allowChangeParameters).nl();

                if (allowChangeParameters) {
                    
                    "Lerp Speed Mode ".editEnum(110, ref lerpMode).nl(ref changed);
                    if (lerpMode == LerpSpeedMode.SpeedTreshold || lerpMode == LerpSpeedMode.UnlinkedSpeed)
                        "Lerp Speed for {0}".F(Name).edit(150, ref speedLimit).nl(ref changed);

                    if (EaseInOutImplemented)
                        "Ease In/Out".toggleIcon(ref easeInOut).nl(ref changed);
                }

                return changed;
            }
#endif
            #endregion

        }

        public abstract class BaseVector2Lerp : BaseAnyValue, IPEGI_ListInspect
        {
            protected Vector2 targetValue;

            protected override bool EaseInOutImplemented => true;

            private float _easePortion = 0.1f;

            protected abstract Vector2 CurrentValue { get; set; }
            
            public override bool UsingLinkedThreshold => base.UsingLinkedThreshold && Enabled;

            public override bool Lerp_Internal(float linkedPortion)
            {
                if (CurrentValue != targetValue || !defaultSet) 
                    CurrentValue = Vector2.Lerp(CurrentValue, targetValue, linkedPortion);
                else return false;

                return true;
            }

            public override bool Portion(ref float linkedPortion) {

                var magn = (CurrentValue - targetValue).magnitude;

                var modSpeed = speedLimit;
                
                if (easeInOut) {
                    _easePortion = Mathf.Lerp(_easePortion, magn > speedLimit*0.5f ? 1 : 0.1f, Time.deltaTime*2);
                    modSpeed *= _easePortion;
                }

               return modSpeed.SpeedToMinPortion(magn, ref linkedPortion);
            }
            #region Inspector
                #if PEGI

            public override bool PEGI_inList(IList list, int ind, ref int edited)
            {
                if (base.PEGI_inList(list, ind, ref edited))
                {
                    targetValue = CurrentValue;
                    return true;
                }
                return false;
            }

            public override bool Inspect()
            {
                pegi.nl();

                var changed = false;

                if (base.Inspect().nl(ref changed))
                    targetValue = CurrentValue;

                if (lerpMode != LerpSpeedMode.LerpDisabled) 
                    "Target".edit(ref targetValue).nl(ref changed);
                

                return changed;
            }
#endif
            #endregion

            #region Encode & Decode
            public override StdEncoder Encode()
            {
                var cody = new StdEncoder()
                      .Add("b", base.Encode);
                if (allowChangeParameters)
                    cody.Add("t", CurrentValue);

                return cody;
            }

            public override bool Decode(string tg, string data) {
                switch (tg) {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "t": targetValue = data.ToVector2(); break;
                    default: return false;
                }
                return true;
            }
            #endregion

            public BaseVector2Lerp() {
                lerpMode = LerpSpeedMode.LerpDisabled;
            }
        }

        public abstract class BASE_FloatLerp : BaseAnyValue, IPEGI_ListInspect  {
            
            protected abstract float TargetValue { get; set; }

            public abstract float Value { get;  set; }

            protected virtual bool CanLerp => true;

            public override bool Lerp_Internal(float linkedPortion) {
                if (CanLerp && (!defaultSet || Value != TargetValue)) 
                    Value = Mathf.Lerp(Value, TargetValue, linkedPortion);
                else return false;

                return true;
            }

            public override bool Portion(ref float linkedPortion) =>
                speedLimit.SpeedToMinPortion(Value - TargetValue, ref linkedPortion);
            
            #region Inspect
            #if PEGI
            public override bool Inspect() {
                var ret = base.Inspect();
                if (Application.isPlaying)
                    "{0} => {1}".F(Value, TargetValue).nl();
                return ret;
            }
            #endif
            #endregion

            #region Encode & Decode
            public override StdEncoder Encode() => new StdEncoder()
                .Add("b", base.Encode);
        
            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    default: return false;
                }
                return true;
            }
            #endregion
        }
        
        public abstract class BASE_MaterialTextureTransition : BASE_FloatLerp {
            float portion = 0;
            
            public int transitionPropertyName;
            public int currentTexturePropertyName;
            public int nextTexturePropertyName;

            enum OnStart {Nothing = 0, ClearTexture = 1, LoadCurrent = 2 }

            OnStart _onStart = OnStart.Nothing;

            protected override float TargetValue {
                get { return Mathf.Max(0, targetTextures.Count - 1); } set { } }

            public override float Value
            {
                get { return portion; }
                set
                {
                    portion = value;

                    while (portion >= 1) {
                        portion -= 1;
                        if (targetTextures.Count > 1) {
                            targetTextures.RemoveAt(0);
                            Current = targetTextures[0];
                            if (targetTextures.Count > 1)
                                Next = targetTextures[1];
                        }
                    }

                    Material?.SetFloat(transitionPropertyName, portion);
                }
            }
            
            List<Texture> targetTextures = new List<Texture>();

            public abstract Material Material { get; }

            Texture Current { get { return Material?.GetTexture(currentTexturePropertyName); } set { Material?.SetTexture(currentTexturePropertyName, value); } }
            Texture Next { get { return Material.GetTexture(nextTexturePropertyName); } set { Material.SetTexture(nextTexturePropertyName, value); } }

            public Texture TargetTexture
            {
                get
                {
                    return targetTextures.TryGetLast();
                }

                set
                {

                    if (value && Material) {

                        if (targetTextures.Count == 0) {
                            targetTextures.Add(null);
                            targetTextures.Add(value);
                            Current = null;
                            Next = value;
                        } else {

                            if (value == targetTextures[0])
                            {
                                if (targetTextures.Count > 1)
                                {
                                    targetTextures.Swap(0, 1);
                                    Value = Mathf.Max(0, 1 - Value);
                                    Current = Next;
                                    Next = value;
                                    targetTextures.TryRemoveTill(2);
                                }
                            }
                            else
                            if (targetTextures.Count >1 && value == targetTextures[1])
                            {
                                targetTextures.TryRemoveTill(2);
                            }
                            else 
                            {
                                if (targetTextures.Count == 1) {
                                    targetTextures.Add(value);
                                    Next = value;
                                }
                                else {
                                    if (targetTextures[1] == value && targetTextures.Count == 3)
                                        targetTextures.RemoveAt(2);
                                    else
                                        targetTextures.ForceSet(2, value);
                                }
                            }
                        }
                    }
                }
            }

            public BASE_MaterialTextureTransition()
            {
                transitionPropertyName = Shader.PropertyToID(CustomShaderParameters.TransitionPortion);
                currentTexturePropertyName = Shader.PropertyToID(CustomShaderParameters.currentTexture);
                nextTexturePropertyName = Shader.PropertyToID(CustomShaderParameters.NextTexture);
            }

            #region Inspector
#if PEGI
            public override bool Inspect()
            {
                var changed = base.Inspect();

                var tex = Current;
                if (allowChangeParameters) {

                    "On Start:".editEnum(60, ref _onStart).nl(ref changed);

                    if ("Texture[{0}]".F(targetTextures.Count).edit(90, ref tex).nl(ref changed))
                        TargetTexture = tex;
                    
                }
                else TargetTexture.write();

                return changed;
            }
            #endif
            #endregion

            #region Encode & Decode
            public override StdEncoder Encode() {

                var cody = new StdEncoder().Add("b", base.Encode);
                if (allowChangeParameters) {
                    cody.Add_IfNotZero("onStart", (int)_onStart);
                    if (_onStart == OnStart.LoadCurrent)
                        cody.Add_Reference("s", targetTextures.TryGetLast());
                }
                return cody;
            }

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "s":
                        Texture tmp = null;
                        data.Decode_Reference(ref tmp);
                        TargetTexture = tmp;
                        break;
                    case "clear": _onStart = OnStart.ClearTexture; break;
                    case "onStart": _onStart = (OnStart)data.ToInt(); break;
                    default: return false;
                }

                return true;
            }

            public override void Decode(string data)
            {
                _onStart = OnStart.Nothing;
                base.Decode(data);

                if (_onStart == OnStart.ClearTexture) {
                    Current = null;
                    Next = null;
                    targetTextures.Clear();
                }
            }

            #endregion
        }
        
        public abstract class BASE_ShaderValue : BaseAnyValue, IGotName {
            
            public Material material;
            public Renderer rendy;
            protected string _name;

            protected Material Material => material ? material : rendy.MaterialWhatever(); 

            protected override string Name => _name;
            public string NameForPEGI { get { return _name;  } set { _name = value;  } }

            public override sealed bool Lerp_Internal(float linkedPortion) {
                if (Lerp_SubInternal(linkedPortion))
                    Set();
                else
                    return false;

                return true;
            }

            protected abstract bool Lerp_SubInternal(float linkedPortion);
            
            protected void Set() => Set(Material);
  
            public abstract void Set(Material on);

            public BASE_ShaderValue(float startingSpeed = 1, Material m = null, Renderer renderer = null) {
                speedLimit = startingSpeed;
                material = m;
                rendy = renderer;
            }
        }

        public abstract class BASE_ColorValue : BaseAnyValue {
            protected override string Name => "Color";
            public Color targetValue = Color.white;
            public abstract Color Value { get; set; }

            public override bool Portion(ref float linkedPortion) =>
              speedLimit.SpeedToMinPortion(Value.DistanceRGBA(targetValue), ref linkedPortion);

            public sealed override bool Lerp_Internal(float linkedPortion) {
                if (Enabled && (targetValue != Value || !defaultSet)) 
                    Value = Color.Lerp(Value, targetValue, linkedPortion);
                else return false;

                return true;
            }

            #region Encode & Decode

            public override StdEncoder Encode() => new StdEncoder()
                .Add("b", base.Encode)
                .Add("col", targetValue);

            public override bool Decode(string tg, string data) {
                switch (tg) {
                    case "b":   data.Decode_Delegate(base.Decode); break;
                    case "col": targetValue = data.ToColor(); break;
                    default: return false;
                }
                return true;
            }

            #endregion

            #region Inspector
            #if PEGI
            public override bool Inspect() {

                var changed = base.Inspect();

                pegi.edit(ref targetValue).nl(ref changed);
                   

                return changed;
            }
            #endif
            #endregion
        }
        #endregion

        #region Value Types
        public class FloatValue : BASE_FloatLerp, IGotName
        {
            readonly string _name = "Float value"; 
            float _value;
            public float targetValue;
            public override float Value { get { return _value; } set { _value = value; } }
            protected override float TargetValue { get { return targetValue; } set { targetValue = value; } }

            protected override string Name => _name;

            public string NameForPEGI { get { return _name; } set { } }

            public FloatValue() { }

            public FloatValue(string name) {
                _name = name;
            }

        }
        #endregion

        #region Transform
        public class Transform_LocalScale : Transform_LocalPosition
        {
            protected override string Name => base.Name;

            public override Vector3 Value { get { return _transform.localScale; } set { _transform.localScale = value; } }

            public Transform_LocalScale(Transform transform, float nspeed) : base(transform, nspeed) { }
        }

        public class Transform_Position : Transform_LocalPosition
        {
            protected override string Name => base.Name;

            public override Vector3 Value { get { return _transform.position; } set { _transform.position = value; } }

            public Transform_Position(Transform transform, float nspeed) : base(transform, nspeed) { }
        }

        public class Transform_LocalPosition : BaseAnyValue
        {
            protected override string Name => "Local Position";
            public Transform _transform;
            public Vector3 targetValue;

            public override bool Enabled => base.Enabled && _transform; 

            public virtual Vector3 Value
            {
                get { return _transform.localPosition; }
                set { _transform.localPosition = value; }
            }

            public Transform_LocalPosition(Transform transform, float nspeed)
            {
                _transform = transform;
                speedLimit = nspeed;
            }

            public override bool Lerp_Internal(float portion) {
                if (Enabled && Value != targetValue)
                    Value = Vector3.Lerp(Value, targetValue, portion);
                else return false;

                return true;
            }

            public override bool Portion(ref float portion) =>
                speedLimit.SpeedToMinPortion((Value - targetValue).magnitude, ref portion);
                
        }
        #endregion

        #region Rect Transform
        public class RectangleTransform_AnchoredPositionValue : BaseVector2Lerp, IPEGI
        {
            public RectTransform rectTransform;

            public override bool Enabled => base.Enabled && rectTransform;

            protected override string Name => "Anchored Position";

            protected override Vector2 CurrentValue
            {
                get { return rectTransform ? rectTransform.anchoredPosition : targetValue; }
                set
                {
                    if (rectTransform)
                        rectTransform.anchoredPosition = value;
                }
            }

            public RectangleTransform_AnchoredPositionValue(RectTransform rect, float nspeed)
            {
                rectTransform = rect;
                speedLimit = nspeed;
            }
        }

        public class RectangleTransform_WidthHeight : RectangleTransform_AnchoredPositionValue
        {

            protected override string Name => "Width Height";

            protected override Vector2 CurrentValue
            {
                get { return rectTransform ? rectTransform.sizeDelta : targetValue; }
                set
                {
                    rectTransform.sizeDelta = value;
                }
            }

            public RectangleTransform_WidthHeight(RectTransform rect, float speed) : base(rect, speed)
            { }
        }
        #endregion

        #region Material
        public class MaterialFloat : BASE_ShaderValue {

            ShaderProperty.FloatValue property;
            public float Value { get { return property.lastValue; } set { property.lastValue = value; defaultSet = false; } }

            public float targetValue;

            public override void Set(Material mat = null) {
                if (mat)
                    mat.Set(property);
                else
                    property.GlobalValue = property.lastValue;
            }

            public MaterialFloat(string nname, float startingValue, float startingSpeed = 1, Renderer renderer = null, Material m = null) : base(startingSpeed, m, renderer)
            {
                property = new ShaderProperty.FloatValue(nname);

                property.GlobalValue = startingValue;
            }
            
            public override bool Portion(ref float linkedPortion) =>
                speedLimit.SpeedToMinPortion(property.lastValue - targetValue, ref linkedPortion);

            protected override bool Lerp_SubInternal(float portion) {
                if (Enabled && (property.lastValue != targetValue || !defaultSet)) {
                    property.lastValue = Mathf.Lerp(property.lastValue, targetValue, portion);
                    return true;
                }
                return false;
            }

            #region Inspector
            #if PEGI

            public override bool Inspect()
            {
                var changed = base.Inspect();

                "Target".edit(70, ref targetValue).nl(ref changed);

                if ("Value".edit(ref property.lastValue).nl(ref changed))
                    Set();

                return changed;
            }

            #endif
            #endregion


        }
        
        public class MaterialColor : BASE_ShaderValue {

            ShaderProperty.ColorValue property;

            public Color Value { get { return property.lastValue; } set { property.lastValue = value; defaultSet = false; } }

            public Color targetValue;

            public override void Set(Material mat) {
                if (mat)
                    mat.Set(property);
                else
                    property.GlobalValue = property.lastValue;
            }

            public MaterialColor(string nname, Color startingValue, float startingSpeed = 1, Material m = null, Renderer renderer = null) : base(startingSpeed, m, renderer)
            {
                property = new ShaderProperty.ColorValue(nname);

                property.lastValue = startingValue;
            }

            protected override bool Lerp_SubInternal(float portion) {
                if (property.lastValue != targetValue || !defaultSet)  {
                    property.lastValue = Color.Lerp(property.lastValue, targetValue, portion);
                    return true;
                }
                return false;
            }

            public override bool Portion(ref float portion) =>
                speedLimit.SpeedToMinPortion(property.lastValue.DistanceRGBA(targetValue), ref portion);

        }
        
        public class GraphicMaterialTextureTransition : BASE_MaterialTextureTransition
        {
            protected override string Name => "Texture Transition";

            Graphic graphic;

            public GraphicMaterialTextureTransition(float nspeed = 1) : base()
            {
                speedLimit = nspeed;
            }

            public Graphic Graphic
            {
                set
                {
                    if (value != graphic)
                    {
                        graphic = value;
                        if (Application.isPlaying)
                            graphic.material = Object.Instantiate(graphic.material);
                    }
                }
            }

            public override Material Material => graphic?.material;
        }

        public class RendererMaterialTextureTransition : BASE_MaterialTextureTransition
        {
            Renderer graphic;

            protected override string Name => "Renderer Texture Transition";

            public RendererMaterialTextureTransition(Renderer rendy, float nspeed = 1) : base()
            {
                speedLimit = nspeed;
                graphic = rendy;
            }

            public Renderer Renderer
            {
                set
                {
                    if (value != graphic)
                    {
                        graphic = value; if (Application.isPlaying) graphic.material = UnityEngine.Object.Instantiate(graphic.material);
                    }
                }
            }

            public override Material Material => graphic?.MaterialWhatever();
        }
        #endregion

        #region UIElement Values
        public class GraphicAlpha : BASE_FloatLerp {

            protected Graphic _graphic;
            public Graphic Graphic { get { return _graphic;  } set { _graphic = value; if (setZeroOnStart && !defaultSet) { _graphic.TrySetAlpha(0); defaultSet = true; } } }
            public float targetValue = 0;
            public bool setZeroOnStart = true;

            protected override float TargetValue { get { return targetValue; }
                set { targetValue = value; } }

            public override float Value { get { return _graphic ? _graphic.color.a : targetValue; } set { _graphic.TrySetAlpha(value); } }

            protected override string Name => "Graphic Alpha";

            public GraphicAlpha() { }

            public GraphicAlpha (Graphic graphic) {
                _graphic = graphic;
            }

            #region Encode & Decode

            public override void Decode(string data)
            {
                base.Decode(data);

                if (setZeroOnStart && !defaultSet)
                    _graphic.TrySetAlpha(0);
            }

            public override StdEncoder Encode() => new StdEncoder().Add("bb", base.Encode).Add_Bool("zero", setZeroOnStart);

            public override bool Decode(string tg, string data) {
                switch (tg) {
                    case "bb": data.Decode_Delegate(base.Decode); break;
                    case "zero": setZeroOnStart = data.ToBool(); break;
                    default: return base.Decode(tg, data);
                } 
                return true;
            }

            #endregion

            #region Inspect
#if PEGI
            public override bool Inspect() {

                var changed = base.Inspect();

                "Set zero On Start".toggleIcon(ref setZeroOnStart).nl();

                return changed;
            }
            #endif
            #endregion


        }

        public class GraphicColor : BASE_ColorValue {

            protected override string Name => "Graphic Color";

            public Graphic _graphic;
            public override Color Value { get { return _graphic ? _graphic.color : targetValue; } set { _graphic.color = value; } }

            public GraphicColor() { }

            public GraphicColor(Graphic graphic)
            {
                _graphic = graphic;
            }



        }

        #endregion

    }

    public static class LinkedLerpingExtensions
    {

        public static void Portion<T>(this T[] list, LerpData ld) where T : ILinkedLerping
        {
            foreach (var e in list)
                if (!e.IsNullOrDestroyed_Obj())
                    e.Portion(ld);
        }

        public static void Lerp<T>(this T[] list, LerpData ld, bool canTeleport = false) where T : ILinkedLerping
        {
            foreach (var e in list)
                e.NullIfDestroyed()?.Lerp(ld, canTeleport);
        }

        public static void Portion<T>(this List<T> list, LerpData ld) where T : ILinkedLerping {

            foreach (var e in list)
                    if (!e.IsNullOrDestroyed_Obj())
                      e.Portion(ld);

        }

        public static void Lerp<T>(this List<T> list, LerpData ld, bool canTeleport = false) where T : ILinkedLerping {
            foreach (var e in list)
                    e.NullIfDestroyed()?.Lerp(ld, canTeleport);
        }

        public static void FadeAway<T>(this List<T> list) where T : IManageFading
        {
            if (list == null) return;
            
            foreach (var e in list)
                e.NullIfDestroyed()?.FadeAway();
        }

        public static bool TryFadeIn<T>(this List<T> list) where T : IManageFading {

        
            if (list == null) return false;
            
            var fadedIn = false;

            
            foreach (var e in list)
                if (!e.IsNullOrDestroyed_Obj()) fadedIn |= e.TryFadeIn();

            return fadedIn;
        }
    }

    public static class CustomShaderParameters
    {

        public const string ImageProjectionPosition = "_imgProjPos";
        public const string NextTexture = "_Next_MainTex";
        public const string currentTexture = "_MainTex_Current";
        public const string TransitionPortion = "_Transition";


#if PEGI
        public static void Inspect()
        {
            "Image projection position".write_ForCopy(ImageProjectionPosition); pegi.nl();

            "Next Texture".write_ForCopy(NextTexture); pegi.nl();

            "Transition portion".write_ForCopy(TransitionPortion); pegi.nl();
        }
#endif
    }

}