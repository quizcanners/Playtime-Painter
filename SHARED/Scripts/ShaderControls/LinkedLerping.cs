using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using UnityEngine.UI;

namespace SharedTools_Stuff {

    public interface ILinkedLerping
    {
        void Portion(ref float portion, ref string dominantParameter);
        void Lerp(float portion);
    }

    public interface IManageFading
    {
        void FadeAway();
        bool TryFadeIn();
    }

    public class LinkedLerp
    {

        public enum LerpSpeedMode { Treshold = 0, Unlimited = 1, LerpDisabled = 2 }

        #region Abstract Base
        public abstract class BASE_AnyValue : Abstract_STD, ILinkedLerping, IPEGI, IPEGI_ListInspect {

            public LerpSpeedMode lerpMode = LerpSpeedMode.Treshold;
            public virtual bool UsingTreshold => (lerpMode == LerpSpeedMode.Treshold && Application.isPlaying);
            public virtual bool Enabled => lerpMode != LerpSpeedMode.LerpDisabled;

            protected bool defaultSet = false;
            public float speedTreshold = 1;
            protected bool allowChangeParameters = true;

            protected abstract string Name { get;  } 

            #region Encode & Decode
            public override StdEncoder Encode() {

                var cody = new StdEncoder()
                    .Add_Bool("ch", allowChangeParameters);

                if (allowChangeParameters) {

                    cody.Add("lm",(int)lerpMode);

                    if (lerpMode == LerpSpeedMode.Treshold)
                    cody.Add("sp", speedTreshold);
                }

                return cody;
            }
            public override bool Decode(string tag, string data)
            {
                switch (tag)
                {
                    case "ch": allowChangeParameters = data.ToBool(); break;
                    case "sp": speedTreshold = data.ToFloat(); defaultSet = false; break;
                    case "lm": lerpMode = (LerpSpeedMode)data.ToInt(); break;
                    default: return false;
                }
                return true;
            }
            #endregion

            public abstract void Lerp(float portion);

            public virtual void Portion(ref float portion, ref string dominantParameter) {
                if (UsingTreshold && Portion(ref portion))
                    dominantParameter = Name;
            }

            public abstract bool Portion(ref float portion);
                  
            #region Inspector
#if PEGI
            public virtual bool PEGI_inList(IList list, int ind, ref int edited) {

                var changed = false;

       
                if (!allowChangeParameters)
                    Name.toggleIcon("Will this config contain new parameters", ref allowChangeParameters).changes(ref changed);
                else {

                    if (Application.isPlaying)
                        (Enabled ? icon.Active : icon.InActive).write(Enabled ? "Lerp Possible" : "Lerp Not Possible");

                    if (lerpMode == LerpSpeedMode.Treshold)
                        (Name + " Thld").edit(170, ref speedTreshold).changes(ref changed);
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
                    if (lerpMode == LerpSpeedMode.Treshold)
                        "Lerp Speed for {0}".F(Name).edit(150, ref speedTreshold).nl(ref changed);
                }

                return changed;
            }
#endif
            #endregion

        }

        public abstract class BASE_Vector2Lerp : BASE_AnyValue, IPEGI_ListInspect
        {
            public Vector2 targetValue;

            protected abstract Vector2 CurrentValue { get; set; }
            
            public override bool UsingTreshold => base.UsingTreshold && Enabled;

            public override void Lerp(float portion)
            {
                if (Enabled && (CurrentValue != targetValue || !defaultSet)) {
                    defaultSet = true;
                    CurrentValue = Vector2.Lerp(CurrentValue, targetValue, portion);
                }
            }

            public override bool Portion(ref float portion) =>
                speedTreshold.SpeedToMinPortion((CurrentValue - targetValue).magnitude, ref portion);

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

            public override bool Decode(string tag, string data) {
                switch (tag) {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "t": targetValue = data.ToVector2(); break;
                    default: return false;
                }
                return true;
            }
            #endregion

            public BASE_Vector2Lerp() {
                lerpMode = LerpSpeedMode.LerpDisabled;
            }
        }

        public abstract class BASE_FloatLerp : BASE_AnyValue, IPEGI_ListInspect  {
            
            protected abstract float TargetValue { get; set; }

            public abstract float Value { get;  set; }

            protected virtual bool CanLerp => true;

            public override void Lerp(float portion) {
                if (CanLerp && (!defaultSet || Value != TargetValue)) {
                    Value = Mathf.Lerp(Value, TargetValue, portion);
                    defaultSet = true;
                }
            }

            public override bool Portion(ref float portion) =>
                speedTreshold.SpeedToMinPortion(Value - TargetValue, ref portion);
            
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
            public override StdEncoder Encode()
            {
                var cody = new StdEncoder()
                      .Add("b", base.Encode);
                return cody;
            }

            public override bool Decode(string tag, string data)
            {
                switch (tag)
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

            enum onStart {Nothing = 0, ClearTexture = 1, LoadCurrent = 2 }

            onStart _onStart = onStart.Nothing;

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

                    Material.SetFloat(transitionPropertyName, portion);
                }
            }

            public string transitionPropertyName = CustomShaderParameters.transitionPortion;
            public string currentTexturePropertyName = CustomShaderParameters.currentTexture;
            public string nextTexturePropertyName = CustomShaderParameters.nextTexture;

            List<Texture> targetTextures = new List<Texture>();

            public abstract Material Material { get; }

            Texture Current { get { return Material.GetTexture(currentTexturePropertyName); } set { Material.SetTexture(currentTexturePropertyName, value); } }
            Texture Next { get { return Material.GetTexture(nextTexturePropertyName); } set { Material.SetTexture(nextTexturePropertyName, value); } }

            public Texture TargetTexture
            {
                get
                {
                    return targetTextures.TryGetLast();
                }

                set
                {

                    if (value != null && Material) {

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
                    if (_onStart == onStart.LoadCurrent)
                        cody.Add_Reference("s", targetTextures.TryGetLast());
                }
                return cody;
            }

            public override bool Decode(string tag, string data)
            {
                switch (tag)
                {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "s":
                        Texture tmp = null;
                        data.Decode_Reference(ref tmp);
                        TargetTexture = tmp;
                        break;
                    case "clear": _onStart = onStart.ClearTexture; break;
                    case "onStart": _onStart = (onStart)data.ToInt(); break;
                    default: return false;
                }

                return true;
            }

            public override void Decode(string data)
            {
                _onStart = onStart.Nothing;
                base.Decode(data);

                if (_onStart == onStart.ClearTexture) {
                    Current = null;
                    Next = null;
                }
            }

            public BASE_MaterialTextureTransition()
            {
            }

            public BASE_MaterialTextureTransition(string transitionPropName, string curTexPropName, string nextTexPropName)
            {
                transitionPropertyName = transitionPropName;
                currentTexturePropertyName = curTexPropName;
                nextTexturePropertyName = nextTexPropName;
            }


            #endregion
        }
        
        public abstract class BASE_ShaderValue : BASE_AnyValue, IGotName {

            protected string name;
            private Material mat;
            private Renderer rendy;

            protected Material Material => mat ? mat : rendy.MaterialWhaever(); 

            protected override string Name => name;
            public string NameForPEGI { get { return name;  } set { name = value;  } }

            public override sealed void Lerp(float portion) {
                if (Lerp_Internal(portion))
                    Set();
            }

            protected abstract bool Lerp_Internal(float portion);
            
            protected void Set() => Set(Material);
  
            public abstract void Set(Material on);

            public BASE_ShaderValue(string nname, float startingSpeed = 1, Material m = null, Renderer renderer = null) {
                name = nname;
                speedTreshold = startingSpeed;
                mat = m;
                rendy = renderer;
            }
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

            public override Vector3 Value { get => _transform.localScale; set => _transform.localScale = value; }

            public Transform_LocalScale(Transform transform, float nspeed) : base(transform, nspeed) { }
        }

        public class Transform_Position : Transform_LocalPosition
        {
            protected override string Name => base.Name;

            public override Vector3 Value { get => _transform.position; set => _transform.position = value; }

            public Transform_Position(Transform transform, float nspeed) : base(transform, nspeed) { }
        }

        public class Transform_LocalPosition : BASE_AnyValue
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
                speedTreshold = nspeed;
            }

            public override void Lerp(float portion) {
                if (Enabled && Value != targetValue)
                    Value = Vector3.Lerp(Value, targetValue, portion);
            }

            public override bool Portion(ref float portion) =>
                speedTreshold.SpeedToMinPortion((Value - targetValue).magnitude, ref portion);
                
        }
        #endregion

        #region Rect Transform
        public class RectangleTransform_AnchoredPositionValue : BASE_Vector2Lerp, IPEGI
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
                speedTreshold = nspeed;
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

            public float value;
            public float targetValue;

            public override void Set(Material mat) {
                if (mat)
                    mat.SetFloat(name, value);
                else
                    Shader.SetGlobalFloat(name, value);
            }

            public MaterialFloat(string nname, float startingValue, float startingSpeed = 1, Renderer renderer = null, Material m = null) : base(nname, startingSpeed, m, renderer)
            {
                value = startingValue;
            }
            
            public override bool Portion(ref float portion) =>
                speedTreshold.SpeedToMinPortion(value - targetValue, ref portion);

            protected override bool Lerp_Internal(float portion) {
                if (Enabled && (value != targetValue || !defaultSet)) {
                    value = Mathf.Lerp(value, targetValue, portion);
                    return true;
                }
                return false;
            }

        }
        
        public class MaterialColor : BASE_ShaderValue
        {

            public Color value;
            public Color targetValue;


            public override void Set(Material mat) {
                if (mat)
                    mat.SetColor(name, value);
                else
                    Shader.SetGlobalColor(name, value);
            }


            public MaterialColor(string nname, Color startingValue, float startingSpeed = 1, Material m = null, Renderer renderer = null) : base(nname, startingSpeed, m, renderer)
            {
                value = startingValue;
            }

            protected override bool Lerp_Internal(float portion) {
                if (value != targetValue || !defaultSet)  {
                    value = Color.Lerp(value, targetValue, portion);
                    return true;
                }
                return false;
            }

            public override bool Portion(ref float portion) =>
                speedTreshold.SpeedToMinPortion(value.DistanceRGBA(targetValue), ref portion);

        }
        
        public class GraphicMaterialTextureTransition : BASE_MaterialTextureTransition
        {
            protected override string Name => "Texture Transition";

            Graphic graphic;

            public GraphicMaterialTextureTransition(string transitionPropName, string curTexPropName, string nextTexPropName) : base(transitionPropName, curTexPropName, nextTexPropName)
            {
            }

            public GraphicMaterialTextureTransition(float nspeed = 1) : base()
            {
                speedTreshold = nspeed;
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

            public RendererMaterialTextureTransition(string transitionPropName, string curTexPropName, string nextTexPropName) : base(transitionPropName, curTexPropName, nextTexPropName)
            {

            }

            public RendererMaterialTextureTransition(Renderer rendy, float nspeed = 1) : base()
            {
                speedTreshold = nspeed;
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

            public override Material Material => graphic?.MaterialWhaever();
        }
        #endregion

        public class GraphicAlpha : BASE_FloatLerp {

            public Graphic graphic;
            public float targetValue = 0;

            protected override float TargetValue { get { return targetValue; }
                set { targetValue = value; } }

            public override float Value { get { return graphic ? graphic.color.a : targetValue; } set { graphic.TrySetAlpha(value); } }

            protected override string Name => "Graphic Alpha";

            public GraphicAlpha() { }

            public GraphicAlpha (Graphic _graphic) {
                graphic = _graphic;
            }

        }

    }

    public static class LinkedLerpingExtensions
    {
        public static string Portion<T>(this List<T> list, ref float portion) where T : ILinkedLerping
        {
            string dom = "None (weird)";

            foreach (var e in list)
                    e.NullIfDestroyed()?.Portion(ref portion, ref dom);

            return dom;
        }

        public static void Portion<T>(this List<T> list, ref float portion, ref string dominantValue) where T : ILinkedLerping
        {
            foreach (var e in list)
                    e.NullIfDestroyed()?.Portion(ref portion, ref dominantValue);
        }

        public static void Lerp<T>(this List<T> list, float portion) where T : ILinkedLerping
        {
            foreach (var e in list)
                    e.NullIfDestroyed()?.Lerp(portion);
        }

        public static void FadeAway<T>(this List<T> list) where T : IManageFading {
            if (list != null)
                foreach (var e in list)
                    e.NullIfDestroyed()?.FadeAway();
        }

        public static bool TryFadeIn<T>(this List<T> list) where T : IManageFading {

            bool fadedIn = false;

            if (list != null)
                foreach (var e in list)
                    if (!e.IsNullOrDestroyed()) fadedIn |= e.TryFadeIn();

            return fadedIn;
        }
    }
    
}