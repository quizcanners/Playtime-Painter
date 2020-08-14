using System;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuizCannersUtilities
{

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration


    public interface ILinkedLerping
    {
        void Portion(LerpData ld);
        void Lerp(LerpData ld, bool canSkipLerp);
    }

    public class LerpData : IPEGI, IGotName, IGotCount, IPEGI_ListInspect
    {
        private float _linkedPortion = 1;
        public string dominantParameter = "None";

        private List<ILinkedLerping> portions = new List<ILinkedLerping>();

        public void AddPortion(float value, ILinkedLerping lerp)
        {
            if (portions.Contains(lerp))
            {
                //QcUnity.ChillLogger.LogErrorOnce(()=> 
                Debug.LogError("Duplicated Portion calculation for {0}".F(lerp.ToString())); //, key: lerp.ToString());
            }
            else
            {
                portions.Add(lerp);
            }

            if (value < MinPortion)
            {
                dominantParameter = lerp.GetNameForInspector();
                MinPortion = value;
            }



        }

        public void LerpAndReset(bool canSkipLerp = false)
        {
            foreach (var portion in portions)
            {
                portion.Lerp(this, canSkipLerp: canSkipLerp);
            }

            Reset();
        }

        public float Portion(bool skipLerp = false) => skipLerp ? 1 : _linkedPortion;

        public float MinPortion
        {
            get { return _linkedPortion; }
            set{_linkedPortion = Mathf.Min(_linkedPortion, value);}
        }
        
        public void Reset()
        {
            _linkedPortion = 1;
            portions.Clear();
            _resets++;
        }

        #region Inspector
        private int _resets;

        public string NameForPEGI
        {
            get { return dominantParameter; }
            set { dominantParameter = value; }
        }


        public int CountForInspector() => _resets;
        
        public bool Inspect()
        {
            var changed = false;

            "Dominant Parameter".edit(ref dominantParameter).nl(ref changed);

            "Reboot calls".edit(ref _resets).nl(ref changed);

            return changed;
        }

        public bool InspectInList(IList list, int ind, ref int edited)
        {
            "Lerp DP: {0} [{1}]".F(dominantParameter, _resets).write();

            if (icon.Refresh.Click("Reset stats"))
            {
                dominantParameter = "None";
                _resets = 0;
            }

            if (icon.Enter.Click())
                edited = ind;

            return false;
        }

        #endregion
    }

    public interface IManageFading
    {
        void FadeAway();
        bool TryFadeIn();
    }

    public static class LinkedLerp
    {

        public enum LerpSpeedMode
        {
            SpeedThreshold = 0,
            Unlimited = 1,
            LerpDisabled = 2,
            UnlinkedSpeed = 3
        }

        #region Abstract Base

        public abstract class BaseLerp : AbstractCfg, ILinkedLerping, IPEGI, IPEGI_ListInspect, IGotDisplayName
        {

            public LerpSpeedMode lerpMode = LerpSpeedMode.SpeedThreshold;

            public virtual bool UsingLinkedThreshold =>
                (lerpMode == LerpSpeedMode.SpeedThreshold && Application.isPlaying);

            public virtual bool Enabled => lerpMode != LerpSpeedMode.LerpDisabled;

           // protected virtual bool EaseInOutImplemented => false;

          //  protected bool easeInOut;

            protected bool defaultSet;
            public float speedLimit = 1;
            protected bool allowChangeParameters = true;

            protected abstract string Name_Internal { get; }
            public virtual string NameForDisplayPEGI()=> Name_Internal;

            #region Encode & Decode

            public override CfgEncoder Encode()
            {

                var cody = new CfgEncoder()
                    .Add_Bool("ch", allowChangeParameters);

                if (allowChangeParameters)
                {

                    //if (EaseInOutImplemented)
                      //  cody.Add_Bool("eio", easeInOut);

                    cody.Add("lm", (int)lerpMode);

                    if (lerpMode == LerpSpeedMode.SpeedThreshold)
                        cody.Add("sp", speedLimit);
                }

                return cody;
            }

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "ch":
                        allowChangeParameters = data.ToBool();
                        break;
                    case "sp":
                        speedLimit = data.ToFloat();
                        break;
                    case "lm":
                        lerpMode = (LerpSpeedMode)data.ToInt();
                        break;
                    default: return false;
                }

                return true;
            }

            #endregion

            public void Lerp(LerpData ld, bool canSkipLerp = false)
            {
                if (!Enabled) return;

                float p;

                switch (lerpMode)
                {
                    case LerpSpeedMode.LerpDisabled:
                        p = 0;
                        break;
                    case LerpSpeedMode.UnlinkedSpeed:
                        p = 1;
                        if (Application.isPlaying)
                            Portion(ref p);

                        if (canSkipLerp)
                            p = 1;

                        break;
                    default:
                        p = ld.Portion(canSkipLerp);
                        break;
                }

                LerpInternal(p);
                defaultSet = true;
            }

            protected abstract bool LerpInternal(float linkedPortion);

            public virtual void Portion(LerpData ld)
            {
                var lp = ld.MinPortion;

                if (UsingLinkedThreshold && Portion(ref lp))
                {
                    ld.AddPortion(lp, this);
                }
                else if (lerpMode == LerpSpeedMode.UnlinkedSpeed)
                {
                    float portion = 1;
                    Portion(ref portion);
                    ld.AddPortion(portion, this);
                }
            }

            protected abstract bool Portion(ref float linkedPortion);

            #region Inspector
            
            public virtual bool InspectInList(IList list, int ind, ref int edited)
            {

                var changed = false;

                if (!allowChangeParameters)
                {
                    Name_Internal.toggleIcon("Will this config contain new parameters", ref allowChangeParameters)
                        .changes(ref changed);
                }
                else
                {
                    if (Application.isPlaying)
                        (Enabled ? icon.Active : icon.InActive).write(Enabled ? "Lerp Possible" : "Lerp Not Possible");

                    switch (lerpMode)
                    {
                        case LerpSpeedMode.SpeedThreshold:
                            (Name_Internal + " Thld").edit(ref speedLimit).changes(ref changed);
                            break;
                        case LerpSpeedMode.UnlinkedSpeed:
                            (Name_Internal + " Speed").edit(ref speedLimit).changes(ref changed);
                            break;
                        default:
                            (Name_Internal + " Mode").editEnum(ref lerpMode).changes(ref changed);
                            break;
                    }
                }

                if (icon.Enter.Click())
                    edited = ind;

                return changed;
            }

            public virtual bool Inspect()
            {

                NameForDisplayPEGI().write();

                var changed = pegi.foldout(icon.Edit, "Will this config contain new parameters", ref allowChangeParameters).nl();

                if (!allowChangeParameters) return changed;

                "Lerp Speed Mode ".editEnum(110, ref lerpMode).changes(ref changed);

                if (Application.isPlaying)
                    (Enabled ? icon.Active : icon.InActive).nl(Enabled ? "Lerp Possible" : "Lerp Not Possible");

                switch (lerpMode)
                {
                    case LerpSpeedMode.SpeedThreshold:
                        ("Max Speed").edit(ref speedLimit).changes(ref changed);
                        break;
                    case LerpSpeedMode.UnlinkedSpeed:
                        ("Speed").edit(ref speedLimit).changes(ref changed);
                        break;
                    //default:
                        //("Mode").editEnum(ref lerpMode).changes(ref changed);
                        //break;
                }

               // if (EaseInOutImplemented)
                  //  "Ease In/Out".toggleIcon(ref easeInOut).nl(ref changed);

                return changed;
            }

            #endregion

        }

        public abstract class BaseLerpGeneric<T> : BaseLerp
        {

            public abstract T TargetValue { get; set; }
            public abstract T CurrentValue { get; set; }

            public T TargetAndCurrentValue
            {
                set
                {
                    TargetValue = value;
                    CurrentValue = value;
                }
            }

            public virtual void Portion(LerpData ld, T targetValue) {
                TargetValue = targetValue;
                Portion(ld);
            }
        }

        public abstract class BaseVector2Lerp : BaseLerpGeneric<Vector2>
        {
            public Vector2 targetValue;
            
            public override Vector2 TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

           // protected override bool EaseInOutImplemented => true;

           // private float _easePortion = 0.1f;

            public override bool UsingLinkedThreshold => base.UsingLinkedThreshold && Enabled;

            protected override bool LerpInternal(float linkedPortion)
            {
                if (CurrentValue != targetValue || !defaultSet)
                    CurrentValue = Vector2.Lerp(CurrentValue, targetValue, linkedPortion);
                else return false;

                return true;
            }

            protected override bool Portion(ref float linkedPortion)
            {

                var magnitude = (CurrentValue - targetValue).magnitude;

                var modSpeed = speedLimit;

               /* if (easeInOut)
                {
                    _easePortion = Mathf.Lerp(_easePortion, magnitude > speedLimit * 0.5f ? 1 : 0.1f, Time.deltaTime * 2);
                    modSpeed *= _easePortion;
                }*/

                return modSpeed.SpeedToMinPortion(magnitude, ref linkedPortion);
            }

            #region Inspector

            public override bool InspectInList(IList list, int ind, ref int edited)
            {
                if (base.InspectInList(list, ind, ref edited))
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

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("b", base.Encode);
                if (allowChangeParameters)
                    cody.Add("t", CurrentValue);

                return cody;
            }

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b":
                        data.Decode_Delegate(base.Decode);
                        break;
                    case "t":
                        targetValue = data.ToVector2();
                        break;
                    default: return false;
                }

                return true;
            }

            #endregion


        }

        public abstract class BaseQuaternionLerp : BaseLerpGeneric<Quaternion>
        {
            protected Quaternion targetValue;

            public override Quaternion TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            public override bool UsingLinkedThreshold => base.UsingLinkedThreshold && Enabled;

            protected override bool LerpInternal(float linkedPortion)
            {
                if (CurrentValue != targetValue || !defaultSet)
                    CurrentValue = Quaternion.Lerp(CurrentValue, targetValue, linkedPortion);
                else return false;

                return true;
            }

            protected override bool Portion(ref float linkedPortion)
            {

                var magnitude = Quaternion.Angle(CurrentValue, targetValue);

                return speedLimit.SpeedToMinPortion(magnitude, ref linkedPortion);
            }

            #region Inspector

            public override bool InspectInList(IList list, int ind, ref int edited)
            {
                if (base.InspectInList(list, ind, ref edited))
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

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("b", base.Encode);
                if (allowChangeParameters)
                    cody.Add("t", targetValue);

                return cody;
            }

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "t": targetValue = data.ToQuaternion(); break;
                    default: return false;
                }

                return true;
            }

            #endregion

            protected BaseQuaternionLerp()
            {
                lerpMode = LerpSpeedMode.SpeedThreshold;
            }
        }

        public abstract class BaseFloatLerp : BaseLerpGeneric<float>
        {

            protected virtual bool CanLerp => true;

            protected override bool LerpInternal(float linkedPortion)
            {
                if (CanLerp && (!defaultSet || CurrentValue != TargetValue))
                    CurrentValue = Mathf.Lerp(CurrentValue, TargetValue, linkedPortion);
                else return false;

                return true;
            }

            protected override bool Portion(ref float linkedPortion) =>
                speedLimit.SpeedToMinPortion(CurrentValue - TargetValue, ref linkedPortion);

            #region Inspect
            
            public override bool Inspect()
            {
                var ret = base.Inspect();
                if (Application.isPlaying)
                    "{0} => {1}".F(CurrentValue, TargetValue).nl();
                return ret;
            }

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode() => new CfgEncoder()
                .Add("b", base.Encode);

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b":
                        data.Decode_Delegate(base.Decode);
                        break;
                    default: return false;
                }

                return true;
            }

            #endregion
        }

        public abstract class BaseMaterialTextureTransition : BaseFloatLerp
        {
            private float _portion;

            private readonly ShaderProperty.FloatValue transitionProperty;
            protected readonly ShaderProperty.TextureValue currentTexturePrTextureValue;
            protected readonly ShaderProperty.TextureValue nextTexturePrTextureValue;

            private enum OnStart
            {
                Nothing = 0,
                ClearTexture = 1,
                LoadCurrent = 2
            }

            private OnStart _onStart = OnStart.Nothing;

            public override float TargetValue
            {
                get { return Mathf.Max(0, _targetTextures.Count - 1); }
                set { }
            }

            public override float CurrentValue
            {
                get { return _portion; }
                set
                {

                    _portion = value;

                    while (_portion >= 1)
                    {
                        _portion -= 1;
                        if (_targetTextures.Count > 1)
                        {
                            RemovePreviousTexture();
                            Current = _targetTextures[0];
                            if (_targetTextures.Count > 1)
                                Next = _targetTextures[1];
                        }
                    }

                    if (Material)
                        Material.Set(transitionProperty, _portion);
                }
            }

            protected readonly List<Texture> _targetTextures = new List<Texture>();

            protected abstract Material Material { get; }

            protected virtual Texture Current
            {
                get { return Material.Get(currentTexturePrTextureValue); }
                set { Material.Set(currentTexturePrTextureValue, value); }
            }

            protected virtual Texture Next
            {
                get { return Material.Get(nextTexturePrTextureValue); }
                set { Material.Set(nextTexturePrTextureValue, value); }
            }

            protected virtual void RemovePreviousTexture() => _targetTextures.RemoveAt(0);

            public virtual Texture TargetTexture
            {
                get { return _targetTextures.TryGetLast(); }

                set
                {
                    if (!value || !Material) return;

                    if (_targetTextures.Count == 0)
                    {
                        _targetTextures.Add(null);
                        _targetTextures.Add(value);
                        Current = null;
                        Next = value;
                    }
                    else
                    {

                        if (value == _targetTextures[0])
                        {
                            if (_targetTextures.Count > 1)
                            {
                                _targetTextures.Swap(0, 1);
                                CurrentValue = Mathf.Max(0, 1 - CurrentValue);
                                Current = Next;
                                Next = value;
                                QcSharp.TryRemoveTill(_targetTextures,2);
                            }
                        }
                        else if (_targetTextures.Count > 1 && value == _targetTextures[1])
                        {
                            QcSharp.TryRemoveTill(_targetTextures, 2);
                        }
                        else
                        {
                            if (_targetTextures.Count == 1)
                            {
                                _targetTextures.Add(value);
                                Next = value;
                            }
                            else
                            {
                                if (_targetTextures[1] == value && _targetTextures.Count == 3)
                                    _targetTextures.RemoveAt(2);
                                else
                                    _targetTextures.ForceSet(2, value);
                            }
                        }
                    }
                }
            }

            protected BaseMaterialTextureTransition()
            {
                transitionProperty = TransitionPortion;
                currentTexturePrTextureValue = CurrentTexture;
                nextTexturePrTextureValue = NextTexture;
            }

            static readonly ShaderProperty.VectorValue ImageProjectionPosition = new ShaderProperty.VectorValue("_imgProjPos");
            static readonly ShaderProperty.TextureValue NextTexture = new ShaderProperty.TextureValue("_Next_MainTex");
            static readonly ShaderProperty.TextureValue CurrentTexture = new ShaderProperty.TextureValue("_MainTex_Current");
            static readonly ShaderProperty.FloatValue TransitionPortion = new ShaderProperty.FloatValue("_Transition");


            #region Inspector

            public override bool Inspect()
            {
                var changed = base.Inspect();

                var tex = Current;
                if (allowChangeParameters)
                {

                    "On Start:".editEnum(60, ref _onStart).nl(ref changed);

                    if ("Texture[{0}]".F(_targetTextures.Count).edit(90, ref tex).nl(ref changed))
                        TargetTexture = tex;

                }
                else TargetTexture.write();

                return changed;
            }

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {

                var cody = new CfgEncoder().Add("b", base.Encode);
                if (allowChangeParameters)
                {
                    cody.Add_IfNotZero("onStart", (int)_onStart);
                    if (_onStart == OnStart.LoadCurrent)
                        cody.Add_Reference("s", _targetTextures.TryGetLast());
                }

                return cody;
            }

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b":
                        data.Decode_Delegate(base.Decode);
                        break;
                    case "s":
                        Texture tmp = null;
                        data.Decode_Reference(ref tmp);
                        TargetTexture = tmp;
                        break;
                    case "clear":
                        _onStart = OnStart.ClearTexture;
                        break;
                    case "onStart":
                        _onStart = (OnStart)data.ToInt();
                        break;
                    default: return false;
                }

                return true;
            }

            public override void Decode(string data)
            {
                _onStart = OnStart.Nothing;
                base.Decode(data);

                if (_onStart == OnStart.ClearTexture)
                {
                    Current = null;
                    Next = null;
                    _targetTextures.Clear();
                }
            }

            #endregion
        }

        public abstract class BaseMaterialAtlasedTextureTransition : BaseMaterialTextureTransition {
            
            Dictionary<Texture, Rect> offsets = new Dictionary<Texture, Rect>();

            void NullOffset(Texture tex)
            {
                if (tex && offsets.ContainsKey(tex))
                    offsets.Remove(tex);
            }
            
            Rect GetRect(Texture tex)
            {
                Rect rect;
                if (tex && offsets.TryGetValue(tex, out rect))
                    return rect;
                return new Rect(0, 0, 1, 1);

            }

            public void SetTarget(Texture tex, Rect offset)
            {
                TargetTexture = tex;
                if (tex)
                    offsets[tex] = offset;
            }

            public override Texture TargetTexture
            {
                get { return base.TargetTexture; }

                set
                {
                    NullOffset(value);
                    base.TargetTexture = value;

                }
            }

            protected override void RemovePreviousTexture()
            {

                NullOffset(_targetTextures[0]);

                base.RemovePreviousTexture();
            }

            protected override Texture Current
            {
                get { return base.Current; }
                set
                {
                    base.Current = value;
                    currentTexturePrTextureValue.Set(Material, GetRect(value));
                }
            }

            protected override Texture Next
            {
                get { return base.Next; }
                set
                {
                    base.Next = value;
                    nextTexturePrTextureValue.Set(Material, GetRect(value));
                }
            }

        }

        public abstract class BaseShaderLerp<T> : BaseLerpGeneric<T>
        {

            public Material material;
            public Renderer rendy;

            protected Material Material => material ? material : rendy.MaterialWhatever();

            protected override string Name_Internal {
                get
                {
                    if (material)
                        return material.name;
                    if (rendy)
                        return rendy.name;
                    return "?";

                }
            }

            protected sealed override bool LerpInternal(float linkedPortion)
            {
                if (LerpSubInternal(linkedPortion))
                    Set();
                else
                    return false;

                return true;
            }

            protected abstract bool LerpSubInternal(float linkedPortion);

            protected void Set() => Set(Material);

            public abstract void Set(Material on);

            protected BaseShaderLerp(float startingSpeed = 1, Material m = null, Renderer renderer = null)
            {
                speedLimit = startingSpeed;
                material = m;
                rendy = renderer;
            }
            
            #region Encode & Decode

            public override CfgEncoder Encode() {
                var cody = new CfgEncoder()
                    .Add("b", base.Encode);

                return cody;
            }

            public override bool Decode(string tg, string data) {
                switch (tg) {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    default: return false;
                }

                return true;
            }

            #endregion

        }

        public abstract class BaseColorLerp : BaseLerpGeneric<Color>
        {
            protected override string Name_Internal => "Base Color";

            public Color targetValue = Color.white;

            public override Color TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            protected override bool Portion(ref float linkedPortion) =>
                speedLimit.SpeedToMinPortion(CurrentValue.DistanceRgba(targetValue), ref linkedPortion);

            protected sealed override bool LerpInternal(float linkedPortion)
            {
                if (Enabled && (targetValue != CurrentValue || !defaultSet))
                    CurrentValue = Color.Lerp(CurrentValue, targetValue, linkedPortion);
                else return false;

                return true;
            }

            #region Encode & Decode

            public override CfgEncoder Encode() => new CfgEncoder()
                .Add("b", base.Encode)
                .Add("col", targetValue);

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b":
                        data.Decode_Delegate(base.Decode);
                        break;
                    case "col":
                        targetValue = data.ToColor();
                        break;
                    default: return false;
                }

                return true;
            }

            #endregion

            #region Inspector
            
            public override bool Inspect()
            {

                var changed = base.Inspect();

                pegi.edit(ref targetValue).nl(ref changed);

                return changed;
            }

            #endregion
        }

        #endregion

        #region Value Types

        public class FloatValue : BaseFloatLerp, IGotName
        {
            private readonly string _name = "Float value";

            public float targetValue;

            private bool minMax;

            private float min;

            private float max = 1;

            public override float CurrentValue { get; set; }

            public override float TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            protected override string Name_Internal => _name;

            #region Inspect
            public string NameForPEGI
            {
                get { return _name; }
                set { }
            }
            
            public override bool InspectInList(IList list, int ind, ref int edited)
            {
                var changed = false;

                if (allowChangeParameters)
                {
                    int width = _name.ApproximateLength();
                    if (minMax)
                        _name.edit(width, ref targetValue, min, max).changes(ref changed);
                    else
                        _name.edit(width, ref targetValue).changes(ref changed);
                }

                if (icon.Enter.Click())
                    edited = ind;

                //base.InspectInList(list, ind, ref edited).changes(ref changed);

                return changed;
            }


            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("trgf", targetValue)
                    .Add_Bool("rng", minMax);
                if (minMax)
                    cody.Add("min", min)
                        .Add("max", max);

                return cody;
            }

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "trgf": targetValue = data.ToFloat(); break;
                    case "rng": minMax = data.ToBool(); break;
                    case "min": min = data.ToFloat(); break;
                    case "max": max = data.ToFloat(); break;
                    default: return base.Decode(tg, data); // For compatibility reasons, should return false
                }

                return true;
            }

            #endregion


            public FloatValue(float startValue = 1, float lerpSpeed = 1, string name = "Float Value")
            {
                _name = name;
                targetValue = startValue;
                CurrentValue = startValue;
                speedLimit = lerpSpeed;
            }

            public FloatValue(float startValue, float lerpSpeed, float min, float max, string name)
            {
                _name = name;
                targetValue = startValue;
                CurrentValue = startValue;
                speedLimit = lerpSpeed;
                minMax = true;
                this.min = min;
                this.max = max;
            }

        }

        public class ColorValue : BaseColorLerp, IGotName
        {
            protected readonly string _name = "Color value";

            protected override string Name_Internal => _name;

            protected Color currentValue;

            public override Color CurrentValue
            {
                get { return currentValue; }
                set { currentValue = value; }
            }

            public string NameForPEGI
            {
                get { return _name; }
                set { }
            }

            public ColorValue()
            {
            }

            public ColorValue(string name)
            {
                _name = name;
            }

            public ColorValue(string name, float speed)
            {
                _name = name;
                this.speedLimit = speed;
            }


        }

        public class Vector3Value : BaseLerpGeneric<Vector3>
        {
            protected readonly string _name = "Vector3 value";
            
            public Vector3 targetValue;

            public Vector3 currentValue;

            public override Vector3 TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            public override bool UsingLinkedThreshold => base.UsingLinkedThreshold && Enabled;

            public override Vector3 CurrentValue { get => currentValue; set{ currentValue = value; } }

            protected override string Name_Internal => _name;

            public Vector3Value()
            {

            }

            public Vector3Value(string name)
            {
                _name = name;
            }

            protected override bool LerpInternal(float linkedPortion)
            {
                if (CurrentValue != targetValue || !defaultSet)
                    CurrentValue = Vector3.Lerp(CurrentValue, targetValue, linkedPortion);
                else return false;

                return true;
            }

            protected override bool Portion(ref float linkedPortion)
            {

                var magnitude = (CurrentValue - targetValue).magnitude;

                var modSpeed = speedLimit;

                return modSpeed.SpeedToMinPortion(magnitude, ref linkedPortion);
            }

            #region Inspector

            public override bool InspectInList(IList list, int ind, ref int edited)
            {
                if (base.InspectInList(list, ind, ref edited))
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

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("b", base.Encode);
                if (allowChangeParameters)
                    cody.Add("t", CurrentValue);

                return cody;
            }

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b":
                        data.Decode_Delegate(base.Decode);
                        break;
                    case "t":
                        targetValue = data.ToVector3();
                        break;
                    default: return false;
                }

                return true;
            }

            #endregion


        }

        public class Vector2Value : BaseVector2Lerp
        {
            protected readonly string _name = "Vector2 value";
            
            public Vector2 currentValue;

            public override bool UsingLinkedThreshold => base.UsingLinkedThreshold && Enabled;

            public override Vector2 CurrentValue { get => currentValue; set { currentValue = value; } }

            protected override string Name_Internal => _name;

            public Vector2Value()
            {

            }

            public Vector2Value(string name)
            {
                _name = name;
            }
        }


        public class ShaderColorValueGlobal : ColorValue {

            protected ShaderProperty.ColorFloat4Value shaderValue; 
            
            public override Color CurrentValue
            {
                get { return currentValue; }
                set { currentValue = value;
                    shaderValue.GlobalValue = value;
                }
            }

            public ShaderColorValueGlobal(string name) {
                shaderValue = new ShaderProperty.ColorFloat4Value(name);
            }

        }
        
        public class QuaternionValue : BaseQuaternionLerp
        {

            Quaternion current = Quaternion.identity;

            private readonly string _name;

            public override Quaternion CurrentValue
            {
                get { return current; }
                set { current = value; }
            }

            protected override string Name_Internal => _name;

            #region Inspect
            
            public override bool InspectInList(IList list, int ind, ref int edited)
            {
                var changed = false;

                if (allowChangeParameters)
                {
                    int width = Name_Internal.ApproximateLength();
                    Name_Internal.edit(width, ref targetValue).changes(ref changed);
                }

                if (icon.Enter.Click())
                    edited = ind;

                return changed;
            }

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("trgf", targetValue);

                return cody;
            }

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "trgf": targetValue = data.ToQuaternion(); break;
                    default: return false;
                }

                return true;
            }

            #endregion

            public QuaternionValue()
            {
            }

            public QuaternionValue(string name)
            {
                _name = name;
            }

            public QuaternionValue(string name, Quaternion startValue)
            {
                _name = name;
                targetValue = startValue;
                CurrentValue = startValue;
            }

            public QuaternionValue(string name, Quaternion startValue, float lerpSpeed)
            {
                _name = name;
                targetValue = startValue;
                CurrentValue = startValue;
                speedLimit = lerpSpeed;
            }


        }

        #endregion

        #region Transform

        public abstract class TransformQuaternionBase : BaseLerpGeneric<Quaternion>
        {

            public override bool Enabled => base.Enabled && transform;

            public Transform transform;

            public Quaternion targetValue;

            public override Quaternion TargetValue { get { return targetValue; } set { targetValue = value; } }

            public TransformQuaternionBase(Transform transform, float nspeed)
            {
                this.transform = transform;
                targetValue = CurrentValue;
                speedLimit = nspeed;
            }

            protected override bool LerpInternal(float portion)
            {
                if (Enabled && CurrentValue != targetValue)
                    CurrentValue = Quaternion.Lerp(CurrentValue, targetValue, portion);
                else return false;

                return true;
            }

            protected override bool Portion(ref float portion) =>
                speedLimit.SpeedToMinPortion( Quaternion.Angle(CurrentValue, targetValue), ref portion);
        }
        
        public abstract class TransformVector3Base : BaseLerpGeneric<Vector3> {

            public override bool Enabled => base.Enabled && transform;

            public Transform transform;

            public Vector3 targetValue;

            public override Vector3 TargetValue { get { return targetValue; } set { targetValue = value; } }

            public TransformVector3Base(Transform transform, float nspeed)
            {
                this.transform = transform;
                speedLimit = nspeed;
            }

            protected override bool LerpInternal(float portion)
            {
                if (Enabled && CurrentValue != targetValue)
                    CurrentValue = Vector3.Lerp(CurrentValue, targetValue, portion);
                else return false;

                return true;
            }

            protected override bool Portion(ref float portion) =>
                speedLimit.SpeedToMinPortion((CurrentValue - targetValue).magnitude, ref portion);
        }

        public class TransformLocalScale : TransformVector3Base
        {
            protected override string Name_Internal => "Local Scale";

            public override Vector3 CurrentValue
            {
                get { return transform.localScale; }
                set { transform.localScale = value; }
            }

            public TransformLocalScale(Transform transform, float nspeed) : base(transform, nspeed)
            {
            }
        }

        public class TransformPosition : TransformVector3Base
        {
            protected override string Name_Internal => "Position";

            public override Vector3 CurrentValue
            {
                get { return transform.position; }
                set { transform.position = value; }
            }

            public TransformPosition(Transform transform, float nspeed) : base(transform, nspeed) { }
        }

        public class TransformLocalPosition : TransformVector3Base
        {
            protected override string Name_Internal => "Local Position " + (transform ? transform.name : "NULL TF");
        
            public override Vector3 CurrentValue
            {
                get { return transform.localPosition; }
                set { transform.localPosition = value; }
            }

            public TransformLocalPosition(Transform transform, float nspeed) : base(transform, nspeed)
            {

            }
        }

        public class TransformLocalRotation : TransformQuaternionBase
        {
            protected override string Name_Internal => "Local Rotation" + (transform ? transform.name : "NULL TF");

            public override Quaternion CurrentValue
            {
                get { return transform.localRotation; }
                set { transform.localRotation = value; }
            }

            public TransformLocalRotation(Transform transform, float nspeed) : base(transform, nspeed) { }
        }


        #endregion

        #region Rect Transform

        public abstract class RectTransformVector2Value : BaseVector2Lerp
        {
            public RectTransform rectTransform;

            public override bool Enabled => base.Enabled && rectTransform;

            protected override string Name_Internal => (rectTransform ? rectTransform.name : "?") + NameSuffix_Internal;

            protected abstract string NameSuffix_Internal { get; }

            public RectTransformVector2Value()
            {
            }

            public RectTransformVector2Value(RectTransform rect, float nspeed) {
                rectTransform = rect;
                speedLimit = nspeed;
            }
        }

        public class RectangleTransformAnchoredPositionValue : RectTransformVector2Value
        {
            protected override string NameSuffix_Internal => " Anchored Position";
            
            public override Vector2 CurrentValue
            {
                get { return rectTransform ? rectTransform.anchoredPosition : targetValue; }
                set
                {
                    if (rectTransform)
                        rectTransform.anchoredPosition = value;
                }
            }

            public RectangleTransformAnchoredPositionValue() { }

            public RectangleTransformAnchoredPositionValue(RectTransform rect, float nspeed) : base(rect, nspeed) { }
        }

        public class RectangleTransformWidthHeight : RectTransformVector2Value
        {
            protected override string NameSuffix_Internal => " Width Height";

            public override Vector2 CurrentValue
            {
                get { return rectTransform ? rectTransform.sizeDelta : targetValue; }
                set { rectTransform.sizeDelta = value; }
            }

            public RectangleTransformWidthHeight()
            {
            }

            public RectangleTransformWidthHeight(RectTransform rect, float speed) : base(rect, speed) { }
        }

        #endregion

        #region Material

        public class MaterialFloat : BaseShaderLerp<float>
        {
            private readonly ShaderProperty.IndexGeneric<float>//ShaderProperty.FloatValue 
                _property;

            protected override string Name_Internal =>
                _property != null ? _property.NameForDisplayPEGI() : "Material Float";

            public override float TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            public override float CurrentValue { 
                get {return _property.latestValue;}
                set{_property.latestValue = value;
                    defaultSet = false;
                }
            }

            protected float targetValue;

            public override void Set(Material mat)
            {
                if (mat)
                    mat.Set(_property);
                else
                    _property.GlobalValue = _property.latestValue;
            }

            public MaterialFloat(string nName, float startingValue, float startingSpeed = 1, Renderer renderer = null,
                Material m = null) : base(startingSpeed, m, renderer)
            {
                _property = new ShaderProperty.FloatValue(nName);
                targetValue = startingValue;
                CurrentValue = startingValue;
            }

            public MaterialFloat(string nName, string featureDirective, float startingValue = 0, float startingSpeed = 1, Renderer renderer = null,
                Material m = null) : base(startingSpeed, m, renderer)
            {
                _property = new ShaderProperty.FloatFeature(nName, featureDirective);
                targetValue = startingValue;
                CurrentValue = startingValue;
            }

            protected override bool Portion(ref float linkedPortion) =>
                speedLimit.SpeedToMinPortion(CurrentValue - targetValue, ref linkedPortion);

            protected override bool LerpSubInternal(float portion)
            {
                if (Enabled && (CurrentValue != targetValue || !defaultSet))
                {
                    _property.latestValue = Mathf.Lerp(CurrentValue, targetValue, portion);
                    return true;
                }

                return false;
            }

            #region Inspector
            
            public override bool Inspect()
            {
                var changed = base.Inspect();

                "Target".edit(70, ref targetValue).nl(ref changed);

                if ("Value".edit(ref _property.latestValue).nl(ref changed))
                    Set();

                return changed;
            }

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {

                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("f", targetValue);

                return cody;
            }

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "f": targetValue = data.ToFloat(); break;

                    default: return false;
                }

                return true;
            }

            #endregion
        }

        public class MaterialColor : BaseShaderLerp<Color>
        {
            private readonly ShaderProperty.IndexGeneric<Color> _property;

            protected override string Name_Internal => _property != null ? _property.NameForDisplayPEGI(): "Material Float";

            public override Color TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            public override Color CurrentValue
            {
                get { return _property.latestValue; }
                set
                {
                    _property.latestValue = value;
                    defaultSet = false;
                }
            }

            public Color targetValue;

            public override void Set(Material mat)
            {
                if (mat)
                    mat.Set(_property);
                else
                    _property.GlobalValue = CurrentValue;
            }

            public MaterialColor(string nName, Color startingValue, float startingSpeed = 1, Material m = null,
                Renderer renderer = null) : base(startingSpeed, m, renderer)
            {
                _property = new ShaderProperty.ColorFloat4Value(nName);
                CurrentValue = startingValue;
            }

            public MaterialColor(string nName, Color startingValue, string shaderFeatureDirective, float startingSpeed = 1, Material m = null,
                Renderer renderer = null) : base(startingSpeed, m, renderer)
            {
                _property = new ShaderProperty.ColorFeature(nName, shaderFeatureDirective);
                CurrentValue = startingValue;
            }

            protected override bool LerpSubInternal(float portion)
            {
                if (CurrentValue != targetValue || !defaultSet)
                {
                    _property.latestValue = Color.Lerp(CurrentValue, targetValue, portion);
                    return true;
                }

                return false;
            }

            protected override bool Portion(ref float portion) =>
                speedLimit.SpeedToMinPortion(CurrentValue.DistanceRgba(targetValue), ref portion);

            public override bool Inspect()
            {
                var changed = base.Inspect().nl();

                pegi.Try_Nested_Inspect(_property).nl(ref changed);

                return changed;
            }

            #region Encode & Decode

            public override CfgEncoder Encode()
            {

                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("c", targetValue);

                return cody;
            }

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "c": targetValue = data.ToColor(); break;

                    default: return false;
                }

                return true;
            }

            #endregion

        }

        public class MaterialVector4 : BaseShaderLerp<Vector4>
        {
            private readonly ShaderProperty.IndexGeneric<Vector4> _property;

            protected override string Name_Internal => _property != null ? _property.NameForDisplayPEGI() : "Material Float4";

            public override Vector4 TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            public override Vector4 CurrentValue
            {
                get { return _property.latestValue; }
                set
                {
                    _property.latestValue = value;
                    defaultSet = false;
                }
            }

            public Vector4 targetValue;

            public override void Set(Material mat)
            {
                if (mat)
                    mat.Set(_property);
                else
                    _property.GlobalValue = CurrentValue;
            }

            public MaterialVector4(string nName, Vector4 startingValue, float startingSpeed = 1, Material m = null,
                Renderer renderer = null) : base(startingSpeed, m, renderer)
            {
                _property = new ShaderProperty.VectorValue(nName);
                CurrentValue = startingValue;
            }

            protected override bool LerpSubInternal(float portion) {
                if (CurrentValue != targetValue || !defaultSet) {
                    _property.latestValue = Vector4.Lerp(CurrentValue, targetValue, portion);
                    return true;
                }

                return false;
            }

            protected override bool Portion(ref float portion) =>
                speedLimit.SpeedToMinPortion((CurrentValue - targetValue).magnitude, ref portion);

            #region Encode & Decode

            public override CfgEncoder Encode()
            {

                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("v4", targetValue);

                return cody;
            }

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b": data.Decode_Delegate(base.Decode); break;
                    case "v4": targetValue = data.ToColor(); break;

                    default: return false;
                }

                return true;
            }

            #endregion

        }


        public class GraphicMaterialTextureTransition : BaseMaterialTextureTransition
        {
            protected override string Name_Internal => "Texture Transition";

            private Graphic _graphic;

            public GraphicMaterialTextureTransition(float nSpeed = 1)
            {
                speedLimit = nSpeed;
            }

            public Graphic Graphic
            {
                set
                {
                    if (value != _graphic)
                    {
                        _graphic = value;
                        if (Application.isPlaying)
                            _graphic.material = Object.Instantiate(_graphic.material);
                    }
                }
            }

            protected override Material Material => _graphic ? _graphic.material : null;
        }

        public class GraphicMaterialAtlasedTextureTransition : BaseMaterialAtlasedTextureTransition
        {
            protected override string Name_Internal => "AtTexture Transition";

            private Graphic _graphic;

            public GraphicMaterialAtlasedTextureTransition(float nSpeed = 1)
            {
                speedLimit = nSpeed;
            }

            public Graphic Graphic
            {
                set
                {
                    if (value != _graphic)
                    {
                        _graphic = value;
                        if (Application.isPlaying)
                            _graphic.material = Object.Instantiate(_graphic.material);
                    }
                }
            }

            protected override Material Material => _graphic ? _graphic.material : null;
        }


        public class RendererMaterialTextureTransition : BaseMaterialTextureTransition
        {
            private Renderer _graphic;

            protected override string Name_Internal => "Renderer Texture Transition";

            public RendererMaterialTextureTransition(Renderer rendy, float nSpeed = 1)
            {
                speedLimit = nSpeed;
                _graphic = rendy;
            }

            public Renderer Renderer
            {
                set
                {
                    if (value != _graphic)
                    {
                        _graphic = value;
                        if (Application.isPlaying) _graphic.material = Object.Instantiate(_graphic.material);
                    }
                }
            }

            protected override Material Material => _graphic ? _graphic.MaterialWhatever() : null;
        }

        #endregion

        #region UIElement Values

        public class GraphicAlpha : BaseFloatLerp
        {

            protected Graphic graphic;

            public Graphic Graphic
            {
                get { return graphic; }
                set
                {
                    graphic = value;
                    if (setZeroOnStart && !defaultSet)
                    {
                        graphic.TrySetAlpha(0);
                        defaultSet = true;
                    }
                }
            }

            protected float targetValue;
            public bool setZeroOnStart = true;

            public override float TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            public override float CurrentValue
            {
                get { return graphic ? graphic.color.a : targetValue; }
                set { graphic.TrySetAlpha(value); }
            }

            protected override string Name_Internal => "Graphic Alpha";

            public GraphicAlpha()
            {
            }

            public GraphicAlpha(Graphic graphic)
            {
                this.graphic = graphic;
            }

            #region Encode & Decode

            public override void Decode(string data)
            {
                base.Decode(data);

                if (setZeroOnStart && !defaultSet)
                    graphic.TrySetAlpha(0);
            }

            public override CfgEncoder Encode() =>
                new CfgEncoder().Add("bb", base.Encode).Add_Bool("zero", setZeroOnStart);

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "bb":
                        data.Decode_Delegate(base.Decode);
                        break;
                    case "zero":
                        setZeroOnStart = data.ToBool();
                        break;
                    default: return base.Decode(tg, data);
                }

                return true;
            }

            #endregion

            #region Inspect
            
            public override bool Inspect()
            {

                var changed = base.Inspect();

                "Set zero On Start".toggleIcon(ref setZeroOnStart).nl();

                return changed;
            }

            #endregion


        }

        public class GraphicColor : BaseColorLerp
        {

            protected override string Name_Internal => "Graphic Color";

            public Graphic graphic;

            public override Color CurrentValue
            {
                get { return graphic ? graphic.color : targetValue; }
                set { graphic.color = value; }
            }

            public GraphicColor()
            {
            }

            public GraphicColor(Graphic graphic)
            {
                this.graphic = graphic;
            }

        }

        #endregion

    }

    public static class LerpUtils
    {

        #region Float
        
        private static float SpeedToPortion(this float speed, float dist) =>
            Math.Abs(dist) > (float.Epsilon * 10) ? Mathf.Clamp01(speed * Time.deltaTime / Mathf.Abs(dist)) : 1;
        
        public static bool SpeedToMinPortion(this float speed, float dist, LerpData ld)
        {

            var nPortion = speed.SpeedToPortion(dist);

            var prt = ld.Portion();

            if (nPortion < prt)  {

                ld.MinPortion = prt;

                return dist > 0;
            }

            return false;
        }

        public static bool SpeedToMinPortion(this float speed, float dist, ref float portion)
        {

            var nPortion = speed.SpeedToPortion(dist);
            if (!(nPortion < portion))
                return (1 - portion) < float.Epsilon && dist > 0;

            portion = nPortion;

            return true;

        }

        public static bool IsLerpingBySpeed(ref float from, float to, float speed)
        {
            if (from == to)
                return false;

            from = Mathf.LerpUnclamped(from, to, speed.SpeedToPortion(Mathf.Abs(from - to)));
            return true;
        }

        public static float LerpBySpeed(float from, float to, float speed)
            => Mathf.LerpUnclamped(from, to, speed.SpeedToPortion(Mathf.Abs(from - to)));

        public static float LerpBySpeed(float from, float to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Mathf.Abs(from - to));
            return Mathf.LerpUnclamped(from, to, portion);
        }

        public static bool LerpAngle_bySpeed(ref float from, float to, float speed)
        {
            var dist = Mathf.Abs(Mathf.DeltaAngle(from, to));
            if (dist <= float.Epsilon) return false;
            var portion = speed.SpeedToPortion(dist);
            from = Mathf.LerpAngle(from, to, portion);
            return true;
        }

        #endregion

        #region Double

        public static bool IsLerpingBySpeed(ref double from, double to, double speed)
        {
            if (from == to)
                return false;

            double diff = to - from;

            double dist = Math.Abs(diff);

            from += diff * QcMath.Clamp01(speed * Time.deltaTime / dist);
            return true;
        }

        public static double LerpBySpeed(double from, double to, double speed)
        {
            if (from == to)
                return from;

            double diff = to - from;

            double dist = Math.Abs(diff);

            return from + diff * QcMath.Clamp01(speed * Time.deltaTime / dist);
        }

        #endregion

        #region Vectors & Color

        public static bool IsLerpingBySpeed(ref Vector2 from, Vector2 to, float speed)
        {
            if (from == to)
                return false;

            from = Vector2.LerpUnclamped(from, to, speed.SpeedToPortion(Vector2.Distance(from, to)));
            return true;
        }
        
        public static Vector2 LerpBySpeed(this Vector2 from, Vector2 to, float speed) =>
            Vector2.LerpUnclamped(from, to, speed.SpeedToPortion(Vector2.Distance(from, to)));

        public static Vector3 LerpBySpeed(this Vector3 from, Vector3 to, float speed) =>
            Vector3.LerpUnclamped(from, to, speed.SpeedToPortion(Vector3.Distance(from, to)));

        public static Vector3 LerpBySpeed_DirectionFirst(this Vector3 from, Vector3 to, float speed) {

            const float precision = float.Epsilon * 10;
            
            var fromMagn = from.magnitude;
            var toMagn = to.magnitude;

            float dist = Vector3.Distance(from, to);
            
            float pathThisFrame = speed * Time.deltaTime;

            if (pathThisFrame >= dist)
                return to;

            if (fromMagn * toMagn < precision)
                return from.LerpBySpeed(to, speed);

            var toNormalized = to.normalized;

            var targetDirection = toNormalized * (fromMagn+toMagn)*0.5f;

            var toTargetDirection = targetDirection - from;

            float rotDiffMagn = toTargetDirection.magnitude;
            
            if (pathThisFrame > rotDiffMagn) {

                pathThisFrame -= rotDiffMagn;

                from = targetDirection;

                var newDiff = to - from;

                from += newDiff.normalized * pathThisFrame;

            }
            else
                from += toTargetDirection * pathThisFrame / rotDiffMagn;


            return from;
        }

        public static Vector4 LerpBySpeed(this Vector4 from, Vector4 to, float speed) =>
            Vector4.LerpUnclamped(from, to, speed.SpeedToPortion(Vector4.Distance(from, to)));

        public static Vector4 LerpBySpeed(this Vector4 from, Vector4 to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(Vector4.Distance(from, to));
            return Vector4.LerpUnclamped(from, to, portion);
        }

        public static Quaternion LerpBySpeed(this Quaternion from, Quaternion to, float speedInDegrees) =>
            Quaternion.LerpUnclamped(from, to, speedInDegrees.SpeedToPortion(Quaternion.Angle(from, to)));

        public static Quaternion LerpBySpeed(this Quaternion from, Quaternion to, float speedInDegrees, out float portion)
        {
            portion = speedInDegrees.SpeedToPortion(Quaternion.Angle(from, to));
            return Quaternion.LerpUnclamped(from, to, portion);
        }

        public static float DistanceRgb(this Color col, Color other)
            =>
                (Mathf.Abs(col.r - other.r) + Mathf.Abs(col.g - other.g) + Mathf.Abs(col.b - other.b));

        public static float DistanceRgba(this Color col, Color other) =>
                ((Mathf.Abs(col.r - other.r) + Mathf.Abs(col.g - other.g) + Mathf.Abs(col.b - other.b)) * 0.33f +
                 Mathf.Abs(col.a - other.a));

        public static float DistanceRgba(this Color col, Color other, ColorMask mask) =>
             (mask.HasFlag(ColorMask.R) ? Mathf.Abs(col.r - other.r) : 0) +
             (mask.HasFlag(ColorMask.G) ? Mathf.Abs(col.g - other.g) : 0) +
             (mask.HasFlag(ColorMask.B) ? Mathf.Abs(col.b - other.b) : 0) +
             (mask.HasFlag(ColorMask.A) ? Mathf.Abs(col.a - other.a) : 0);

        public static Color LerpBySpeed(this Color from, Color to, float speed) =>
            Color.LerpUnclamped(from, to, speed.SpeedToPortion(from.DistanceRgb(to)));

        public static Color LerpRgb(this Color from, Color to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(from.DistanceRgb(to));
            to.a = from.a;
            return Color.LerpUnclamped(from, to, portion);
        }

        public static Color LerpRgba(this Color from, Color to, float speed, out float portion)
        {
            portion = speed.SpeedToPortion(from.DistanceRgba(to));
            return Color.LerpUnclamped(from, to, portion);
        }

        #endregion

        #region Components

        public static bool IsLerpingAlphaBySpeed<T>(this List<T> graphicList, float alpha, float speed) where T : Graphic
        {

            if (graphicList.IsNullOrEmpty()) return false;

            var changing = false;

            foreach (var i in graphicList)
                changing |= i.IsLerpingAlphaBySpeed(alpha, speed);

            return changing;
        }

        public static bool IsLerpingAlphaBySpeed<T>(this T img, float alpha, float speed) where T : Graphic
        {
            if (!img) return false;

            var changing = false;

            var col = img.color;
            col.a = LerpBySpeed(col.a, alpha, speed);

            img.color = col;
            changing |= col.a != alpha;

            return changing;
        }

        public static bool IsLerpingRgbBySpeed<T>(this T img, Color target, float speed) where T : Graphic
        {
            bool changing = false;

            if (img)
            {
                float portion;
                img.color = img.color.LerpRgb(target, speed, out portion);

                changing = portion < 1;
            }

            return changing;
        }

        public static void LerpAlpha_DisableIfZero(this LineRenderer linkRenderer, float target, float speed) {

            var col = linkRenderer.startColor;
            float alpha = col.a;
            if (IsLerpingBySpeed(ref alpha, target, speed))
                linkRenderer.startColor = col.Alpha(alpha);

            col = linkRenderer.endColor;
            float alpha2 = col.a;
            if (IsLerpingBySpeed(ref alpha2, target, speed))
                linkRenderer.endColor = col.Alpha(alpha2);

            bool active = alpha > 0 || alpha2 > 0;
            
            linkRenderer.gameObject.SetActive(active);

        }

        public static bool IsLerpingBySpeed_Volume(this AudioSource src, float target, float speed)
        {
            if (!src)
                return false;

            var vol = src.volume;

            if (IsLerpingBySpeed(ref vol, target, speed))
            {
                src.volume = vol;
                return true;
            }

            return false;
        }

        #endregion

        public static void SkipLerp<T>(this T obj, LerpData ld) where T : ILinkedLerping
        {
            ld.Reset();
            obj.Portion(ld);
            ld.MinPortion = 1;
            obj.Lerp(ld, true);
        }

        public static void SkipLerp<T>(this T obj) where T : ILinkedLerping {
            var ld = new LerpData();
            obj.Portion(ld);
            ld.MinPortion = 1;
            obj.Lerp(ld, true);
        }

        public static void Portion<T>(this T[] list, LerpData ld) where T : ILinkedLerping {

            if (typeof(Object).IsAssignableFrom(typeof(T))) {
                for (int i = list.Length - 1; i >= 0; i--) {

                    var e = list[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.Portion(ld);
                }
            }
            else for (int i = list.Length - 1; i >= 0; i--) {
                var e = list[i];
                if (e != null)
                    e.Portion(ld);
            }
        }
        
        public static void Portion<T>(this List<T> list, LerpData ld) where T : ILinkedLerping {

            if (typeof(Object).IsAssignableFrom(typeof(T))) {
                for (int i = list.Count - 1; i >= 0; i--) {
                    var e = list[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.Portion(ld);
                }

            }
            else for (int i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e != null)
                    e.Portion(ld);
            }

        }

        public static void Lerp<T>(this T[] array, LerpData ld, bool canSkipLerp = false) where T : ILinkedLerping
        {

            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                for (int i = array.Length - 1; i >= 0; i--)
                {

                    var e = array[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.Lerp(ld, canSkipLerp: canSkipLerp);
                }
            }
            else for (int i = array.Length - 1; i >= 0; i--)
            {
                var e = array[i];
                if (e != null)
                    e.Lerp(ld, canSkipLerp: canSkipLerp);
            }
        }

        public static void Lerp<T>(this List<T> list, LerpData ld, bool canSkipLerp = false) where T : ILinkedLerping
        {

            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.Lerp(ld, canSkipLerp: canSkipLerp);
                }

            }
            else for (int i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e != null)
                    e.Lerp(ld, canSkipLerp: canSkipLerp);
            }

        }


        public static void FadeAway<T>(this List<T> list) where T : IManageFading
        {
            if (list == null) return;

            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.FadeAway();
                }

            }
            else for (int i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e != null)
                    e.FadeAway();
            }
        }

        public static bool TryFadeIn<T>(this List<T> list) where T : IManageFading {
            
            if (list == null) return false;

            var fadedIn = false;
            
            if (typeof(Object).IsAssignableFrom(typeof(T))) {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        fadedIn |= e.TryFadeIn();
                }

            } else for (int i = list.Count - 1; i >= 0; i--) {

                var e = list[i];
                if (e != null)
                    fadedIn |= e.TryFadeIn();
            }

            return fadedIn;
        }
    }

}