using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using UnityEngine.UI;

namespace SharedTools_Stuff {

    public interface IlinkedLerping
    {
        void Portion(ref float portion, ref string dominantParameter);
        void Lerp(float portion);
    }

    public class LinkedLerp
    {

        #region Abstract Base
        public abstract class BASE_AnyValue : Abstract_STD, IlinkedLerping, IPEGI, IPEGI_ListInspect {

            protected bool defaultSet = false;

            public float speed = 1;
            protected bool allowChangeParameters = true;

            protected abstract string Name { get;  } 

            #region Encode & Decode
            public override StdEncoder Encode()
            {

                var cody = new StdEncoder()
                    .Add_Bool("ch", allowChangeParameters);

                if (allowChangeParameters)
                    cody.Add("sp", speed);

                return cody;
            }

            public override bool Decode(string tag, string data)
            {
                switch (tag)
                {
                    case "ch": allowChangeParameters = data.ToBool(); break;
                    case "sp": speed = data.ToFloat(); defaultSet = false; break;
                    default: return false;
                }
                return true;
            }
            #endregion

            public abstract void Lerp(float portion);
            public abstract void Portion(ref float portion, ref string dominantParameter);

            #region Inspector
#if PEGI
            public virtual bool PEGI_inList(IList list, int ind, ref int edited) {

                var changed = false;
                
                if (!allowChangeParameters)
                    Name.toggleIcon("Will this config contain new parameters", ref allowChangeParameters, true);
                else
                    Name.edit(80, ref speed);

                if (icon.Enter.Click())
                    edited = ind;

                return changed;
            }

            public virtual bool Inspect() {

                var changed = "Edit".toggleIcon("Will this config contain new parameters", ref allowChangeParameters, true);

                if (allowChangeParameters)
                    "Lerp Speed for {0}".F(Name).edit(120, ref speed).nl(ref changed);

                pegi.nl();

                return changed;
            }
#endif
            #endregion

        }

        public abstract class BASE_Vector2Lerp : BASE_AnyValue, IPEGI_ListInspect
        {
            public Vector2 targetValue;

            protected abstract Vector2 CurrentValue { get; set; }

            public override bool IsDefault => !enabled;

            protected virtual bool CanLerp => true;

            bool lerpFinished = false;

            bool enabled = false;

            public override void Lerp(float portion)
            {
                if ( enabled && CanLerp &&  !lerpFinished && (CurrentValue != targetValue || !defaultSet)) {
                    defaultSet = true;
                    CurrentValue = Vector2.Lerp(CurrentValue, targetValue, portion);

                    if (portion == 1)
                        lerpFinished = true;

                }
            }

            public override void Portion(ref float portion, ref string dominantParameter) {
                if ( enabled && CanLerp && speed.SpeedToMinPortion((CurrentValue - targetValue).magnitude, ref portion))
                    dominantParameter = Name;
            }

            #region Inspector
#if PEGI
            public override bool PEGI_inList(IList list, int ind, ref int edited)
            {
                var changes = false;

                if (!enabled) {

                    if ("{0} Disabled".F(Name).toggleIcon(ref enabled, "{0} Enabled".F(Name)).changes(ref changes))
                        targetValue = CurrentValue;

                    if (icon.Enter.Click())
                        edited = ind;
                }
                else
                    changes |= base.PEGI_inList(list, ind, ref edited);

                if (changes)
                    lerpFinished = false;

                return false;
            }

            public override bool Inspect()
            {
                pegi.nl();

                var changed = false;

                if ("Enable".toggleIcon(ref enabled, true).changes(ref changed))
                    targetValue = CurrentValue;

                if (enabled) {

                    (CanLerp ? icon.Active : icon.InActive).nl(CanLerp ? "Can Lerp" : "Can't lerp");

                    "Target".edit(ref targetValue).nl(ref changed);

                    base.Inspect().nl(ref changed);
                }

                if (changed)
                    lerpFinished = false;

                return changed;
            }
#endif
#endregion

            #region Encode & Decode
            public override StdEncoder Encode()
            {
                var cody = new StdEncoder()
                      .Add("b", base.Encode)
                      .Add_Bool("e", enabled);
                if (enabled && allowChangeParameters)
                    cody.Add("t", CurrentValue);

                return cody;
            }

            public override bool Decode(string tag, string data)
            {
                switch (tag)
                {
                    case "e": enabled = data.ToBool(); break;
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "t": targetValue = data.ToVector2(); lerpFinished = false; break;
                    default: return false;
                }
                return true;
            }
            #endregion
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

            public override void Portion(ref float portion, ref string dominantParameter)
            {
                if (CanLerp && speed.SpeedToMinPortion(Value - TargetValue, ref portion))
                    dominantParameter = Name;
            }

            #region Inspect
            #if PEGI
            public override bool Inspect() {
                var ret = base.Inspect();
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
        
        public abstract class BASE_MaterialTextureTransition : BASE_FloatLerp
        {
            float portion = 0;

            protected override float TargetValue { get { return Mathf.Max(0, targetTextures.Count - 1); } set { } }

            public override float Value
            {
                get { return portion; }
                set
                {
                    portion = value;

                    while (portion >= 1) {
                        portion -= 1;
                        targetTextures.RemoveAt(0);
                        current = targetTextures[0];
                        if (targetTextures.Count > 1)
                            next = targetTextures[1];
                    }

                    material.SetFloat(transitionPropertyName, portion);
                }
            }

            public string transitionPropertyName = CustomShaderParameters.transitionPortion;
            public string currentTexturePropertyName = CustomShaderParameters.currentTexture;
            public string nextTexturePropertyName = CustomShaderParameters.nextTexture;

            List<Texture> targetTextures = new List<Texture>();

            public abstract Material material { get; }

            Texture current { get { return material.GetTexture(currentTexturePropertyName); } set { material.SetTexture(currentTexturePropertyName, value); } }
            Texture next { get { return material.GetTexture(nextTexturePropertyName); } set { material.SetTexture(nextTexturePropertyName, value); } }

            public Texture TargetTexture
            {
                get
                {
                    return targetTextures.TryGetLast();
                }

                set
                {

                    if (value != null && material)
                    {

                        if (targetTextures.Count == 0)
                        {
                            targetTextures.Add(value);
                            current = value;
                        }
                        else
                        {
                            //>0

                            if (value == targetTextures[0])
                            {
                                if (targetTextures.Count > 1)
                                {
                                    targetTextures.Swap(0, 1);
                                    Value = Mathf.Max(0, 1 - Value);
                                    current = next;
                                    next = value;
                                    targetTextures.TryRemoveTill(2);
                                }
                            }
                            else
                            {
                                if (targetTextures.Count == 1) {
                                    targetTextures.Add(value);
                                    next = value;
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

                var tex = current;
                if (allowChangeParameters)
                {
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
                if (allowChangeParameters)
                    cody.Add_Reference("s", targetTextures.TryGetLast());

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
                    default: return false;
                }

                return true;
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
        
        public abstract class BASE_ShaderValue : IlinkedLerping {

            protected string name;
            public float speed;
            protected bool defaultSet;
            protected Material mat;
            protected Renderer rendy;

            public abstract void Set(Renderer on);

            public abstract void Set();

            public abstract void Set(Material on);

            public BASE_ShaderValue(string nname, float startingSpeed = 1, Material m = null, Renderer renderer = null)
            {
                name = nname;
                speed = startingSpeed;
                mat = m;
                rendy = renderer;
            }

            public abstract void Portion(ref float portion, ref string dominantParameter);

            public abstract void Lerp(float portion);

        }


        #endregion

        #region Value Types
        public class FloatValue : BASE_FloatLerp
        {
            string _name = "Float value"; 
            float _value;
            public float targetValue;
            public override float Value { get { return _value; } set { _value = value; } }
            protected override float TargetValue { get { return targetValue; } set { targetValue = value; } }

            protected override string Name => _name;

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

        public class Transform_LocalPosition
        {
            protected virtual string Name => "Local Position";
            public Transform _transform;
            public Vector3 targetValue;
            public float speed;

            public virtual Vector3 Value
            {
                get { return _transform.localPosition; }
                set { _transform.localPosition = value; }
            }

            public Transform_LocalPosition(Transform transform, float nspeed)
            {
                _transform = transform;
                speed = nspeed;
            }

            public void Lerp(float portion)
            {
                if (!_transform)
                    return;

                if (Value != targetValue)
                    Value = Vector3.Lerp(Value, targetValue, portion);
            }

            public void Portion(ref float portion, ref string dominantParameter)
            {
                if (!_transform)
                    return;
                if (speed.SpeedToMinPortion((Value - targetValue).magnitude, ref portion))
                    dominantParameter = Name;
            }

        }
        #endregion

        #region Rect Transform
        public class RectangleTransform_AnchoredPositionValue : BASE_Vector2Lerp, IPEGI
        {
            public RectTransform rectTransform;

            protected override bool CanLerp => rectTransform;

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
                speed = nspeed;
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
        public class MaterialFloat : BASE_ShaderValue
        {

            public float value;
            public float targetValue;

            public override void Set(Renderer on)
            {
                if (Application.isPlaying)
                    on.material.SetFloat(name, value);
                else
                    on.sharedMaterial.SetFloat(name, value);
            }

            public override void Set()
            {
                if (mat)
                    Set(mat);
                else
                    Shader.SetGlobalFloat(name, value);
            }

            public override void Set(Material on) => on.SetFloat(name, value);

            public MaterialFloat(string nname, float startingValue, float startingSpeed = 1, Material m = null, Renderer renderer = null) : base(nname, startingSpeed, m, renderer)
            {
                value = startingValue;
            }

            public void LerpBySpeedTo(float dvalue, Material mat = null) => LerpBySpeedTo(dvalue, speed, mat);

            public void LerpBySpeedTo(float dvalue, float nspeed, Material mat = null)
            {
                speed = nspeed;
                targetValue = dvalue;

                if (!defaultSet || dvalue != value)
                {
                    value = MyMath.Lerp_bySpeed(value, dvalue, speed);
                    if (mat)
                        Set(mat);
                    else
                        Set();
                    defaultSet = true;
                }
            }

            public override void Portion(ref float portion, ref string dominantParameter)
            {

                if (speed.SpeedToMinPortion(value - targetValue, ref portion))
                    dominantParameter = name;

            }

            public void Lerp(float portion, Renderer rendy)
            {
                if (value != targetValue || !defaultSet)
                {
                    value = Mathf.Lerp(value, targetValue, portion);
                    Set(rendy);
                }
            }

            public override void Lerp(float portion)
            {
                if (value != targetValue || !defaultSet)
                {
                    value = Mathf.Lerp(value, targetValue, portion);
                    Set();
                }
            }

        }
        
        public class MaterialColor : BASE_ShaderValue
        {

            public Color value;
            public Color targetValue;

            public override void Set(Renderer on)
            {
                if (Application.isPlaying)
                    on.material.SetColor(name, value);
                else
                    on.sharedMaterial.SetColor(name, value);
            }

            public override void Set()
            {
                if (mat)
                    Set(mat);
                else
                    Shader.SetGlobalColor(name, value);
            }

            override public void Set(Material on) => on.SetColor(name, value);

            public MaterialColor(string nname, Color startingValue, float startingSpeed = 1, Material m = null, Renderer renderer = null) : base(nname, startingSpeed, m, renderer)
            {
                value = startingValue;
            }

            public override void Portion(ref float portion, ref string dominantParameter)
            {
                if (speed.SpeedToMinPortion(value.DistanceRGBA(targetValue), ref portion))
                    dominantParameter = name;
            }

            public override void Lerp(float portion)
            {
                if (value != targetValue || !defaultSet)
                {
                    value = Color.Lerp(value, targetValue, portion);
                    Set();
                }
            }

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
                speed = nspeed;
            }

            public Graphic Graphic
            {
                set
                {
                    if (value != graphic)
                    {
                        graphic = value; if (Application.isPlaying)
                            graphic.material = UnityEngine.Object.Instantiate(graphic.material);
                    }
                }
            }

            public override Material material => graphic?.material;
        }

        public class RendererMaterialTextureTransition : BASE_MaterialTextureTransition
        {

            Renderer graphic;

            protected override string Name => "Renderer Texture Transition";

            public RendererMaterialTextureTransition(string transitionPropName, string curTexPropName, string nextTexPropName) : base(transitionPropName, curTexPropName, nextTexPropName)
            {

            }

            public RendererMaterialTextureTransition(float nspeed = 1) : base()
            {
                speed = nspeed;
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

            public override Material material => graphic?.MaterialWhaever();
        }
        #endregion

        public class GraphicAlpha : BASE_FloatLerp {

            public Graphic graphic;
            public float targetValue = 0;

            protected override float TargetValue { get { return targetValue; }
                set { targetValue = value; } }

            public override float Value { get { return graphic ? graphic.color.a : targetValue; } set { graphic.TrySetAlpha(value); } }

            protected override string Name => "Graphic Alpha";
        }

    }

    public static class LinkedLerpingExtensions
    {

        public static string Portion<T>(this List<T> list, ref float portion) where T : IlinkedLerping
        {
            string dom = "None (weird)";

            foreach (var e in list)
                if (e != null)
                    e.Portion(ref portion, ref dom);

            return dom;
        }

        public static void Portion<T>(this List<T> list, ref float portion, ref string dominantValue) where T : IlinkedLerping
        {
            foreach (var e in list)
                if (e != null)
                    e.Portion(ref portion, ref dominantValue);
        }

        public static void Lerp<T>(this List<T> list, float portion) where T : IlinkedLerping
        {
            foreach (var e in list)
                if (e != null)
                    e.Lerp(portion);
        }
    }
    
}