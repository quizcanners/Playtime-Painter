using System;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCannersUtilities {

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    public static class ShaderProperty {

        #region Base Abstract

        public abstract class BaseShaderPropertyIndex : AbstractCfg, IGotDisplayName, IPEGI_ListInspect
        {
            protected int id;
            protected string name;
            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                var bi = obj as BaseShaderPropertyIndex;

                return bi != null ? bi.id == id : name.Equals(obj.ToString());
            }

            public override int GetHashCode() => id;
            
            private void UpdateIndex() => id = Shader.PropertyToID(name);

            public override string ToString() => name;

            public abstract void SetOn(Material mat);

            public abstract void SetOn(MaterialPropertyBlock block);

            public Renderer SetOn(Renderer renderer, int materialIndex = 0)
            {
#if UNITY_2018_3_OR_NEWER
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, materialIndex);
                SetOn(block);
                renderer.SetPropertyBlock(block, materialIndex);
#else
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                SetOn(block);
                renderer.SetPropertyBlock(block);
#endif
                return renderer;
            }

            #region Inspector
            public string NameForDisplayPEGI()=> name;
            
            public bool InspectInList(IList list, int ind, ref int edited)
            {
                "Id: {0}".F(id).write(50);
                name.write_ForCopy();
               
                return false;
            }
           
            #endregion

            #region Encode & Decode
            public override CfgEncoder Encode() => new CfgEncoder()
                .Add_String("n", name)
                //.Add_IfTrue("nm", nonMaterialProperty)
            ;
            
            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "n":  name = data; UpdateIndex();  break;
                   // case "nm": nonMaterialProperty = data.ToBool();  break;
                    default: return false;
                }

                return true;
            }
            #endregion

            #region Constructors
            protected BaseShaderPropertyIndex()
            {
            }

            protected BaseShaderPropertyIndex(string name)
            {
                this.name = name;
                UpdateIndex();
            }
            #endregion
        }

        public static bool Has<T>(this Material mat, T property) where T : BaseShaderPropertyIndex =>
            mat.HasProperty(property.GetHashCode());

        #endregion

        #region Generics
 
        public abstract class IndexGeneric<T> : BaseShaderPropertyIndex {
            
            public T latestValue;

            public abstract T Get(Material mat);

            protected abstract T GlobalValue_Internal { get; set; }

            public T GlobalValue
            {
                get
                {
                    return GlobalValue_Internal; }
                set
                {
                    latestValue = value;
                    GlobalValue_Internal = value;
                }

            }

            public virtual Material SetOn(Material material, T value)
            {
                latestValue = value;

                if (material)
                    SetOn(material);

                return material;
            }

            public virtual Renderer SetOn(Renderer renderer, T value)
            {
                latestValue = value;
                SetOn(renderer);
                return renderer;
            }

            public virtual void SetOn(MaterialPropertyBlock block, T value)
            {
                latestValue = value;
                SetOn(block);
            }

            public void SetGlobal() => GlobalValue = latestValue;

            public void SetGlobal(T value) => GlobalValue = value;

            public T GetGlobal(T value) => GlobalValue;

            protected IndexGeneric()
            {
            }

            protected IndexGeneric(string name) : base(name)
            {
            }

        }

        public abstract class IndexWithShaderFeatureGeneric<T> : IndexGeneric<T> {

            private readonly string _featureDirective;

            private bool _directiveGlobalValue;
            
            protected override T GlobalValue_Internal
            {
                set {
                    if (_directiveGlobalValue == DirectiveEnabledForLastValue)
                        return;

                    _directiveGlobalValue = DirectiveEnabledForLastValue;

                    QcUnity.SetShaderKeyword(_featureDirective, _directiveGlobalValue);
                }
            }

            public override Material SetOn(Material material, T value) {

                var ret =  base.SetOn(material, value);
                
                material.SetShaderKeyword(_featureDirective, DirectiveEnabledForLastValue);

                return ret;
            }

            protected IndexWithShaderFeatureGeneric(string name, string featureDirective) : base(name)
            {

                _featureDirective = featureDirective;

            }

            protected abstract bool DirectiveEnabledForLastValue { get; }

        }

        public static MaterialPropertyBlock Set<T>(this MaterialPropertyBlock block, IndexGeneric<T> property)
        {
            property.SetOn(block);
            return block;
        }

        public static MaterialPropertyBlock Set<T>(this MaterialPropertyBlock block, IndexGeneric<T> property, T value)
        {
            property.SetOn(block, value);
            return block;
        }

        public static Material Set<T>(this Material mat, IndexGeneric<T> property)
        {
            property.SetOn(mat);
            return mat;
        }

        public static Material Set<T>(this Material mat, IndexGeneric<T> property, T value) =>
            property.SetOn(mat, value);

        public static Renderer Set<T>(this Renderer renderer, IndexGeneric<T> property, T value) =>
            property.SetOn(renderer, value);

        public static T Get<T>(this Material mat, IndexGeneric<T> property) => property.Get(mat);

        #endregion

        #region Float

        public class FloatValue : IndexGeneric<float> {

            public override void SetOn(Material material) => material.SetFloat(id, latestValue);

            public override float Get(Material material) => material.GetFloat(id);

            public override void SetOn(MaterialPropertyBlock block) => block.SetFloat(id, latestValue);

            protected override float GlobalValue_Internal
            {
                get { return Shader.GetGlobalFloat(id); }
                set{ Shader.SetGlobalFloat(id, value); }
            }

            public FloatValue()
            {
            }

            public FloatValue(string name) : base(name)
            {
            }

        }

        public class FloatFeature : IndexWithShaderFeatureGeneric<float>
        {

            public override void SetOn(Material material) => material.SetFloat(id, latestValue);

            public override float Get(Material material) => material.GetFloat(id);

            public override void SetOn(MaterialPropertyBlock block) => block.SetFloat(id, latestValue);

            protected override float GlobalValue_Internal
            {
                get { return Shader.GetGlobalFloat(id); }
                set
                {
                    base.GlobalValue_Internal = value;
                    Shader.SetGlobalFloat(id, value);
                }
            }

            protected override bool DirectiveEnabledForLastValue => latestValue > float.Epsilon * 10;

            public FloatFeature(string name, string featureDirective) : base(name, featureDirective) { }
        }

        #endregion

        #region Color

        public class ColorFeature : IndexWithShaderFeatureGeneric<Color>, IPEGI {

            public static readonly ColorFloat4Value tintColor = new ColorFloat4Value("_TintColor");

            public override void SetOn(Material material) => material.SetColor(id, latestValue);
            
            public override Color Get(Material material) => material.GetColor(id);

            public override void SetOn(MaterialPropertyBlock block) => block.SetColor(id, latestValue);
            
            protected override Color GlobalValue_Internal
            {
                get { return Shader.GetGlobalColor(id); }
                set {
                    base.GlobalValue_Internal = value;
                    Shader.SetGlobalColor(id, value);
                }
            }

            protected override bool DirectiveEnabledForLastValue => latestValue.a > 0.01f;

            public bool Inspect()
            {

                var changed = false;

                NameForDisplayPEGI().write(); 

                (DirectiveEnabledForLastValue ? icon.Active: icon.InActive).nl();
                
                if (pegi.edit(ref latestValue).nl(ref changed))
                {
                    GlobalValue = latestValue;
                }

                return changed;
            }

            public ColorFeature(string name, string featureDirective) : base(name, featureDirective) { }
        }

        [Serializable]

        public class ColorFloat4Value : IndexGeneric<Color> {

            public static readonly ColorFloat4Value tintColor = new ColorFloat4Value("_TintColor");

            public bool ConvertToLinear
            {
                get
                {
                    if (!_colorSpaceChecked)
                        CheckColorSpace();
                    return _convertToLinear;
                }
                set
                {
                    _colorSpaceChecked = true;
                    _convertToLinear = value;
                }
            }
            private bool _convertToLinear;
            private bool _colorSpaceChecked;

            private Color ConvertedColor => ConvertToLinear ? latestValue.linear : latestValue;

            public override void SetOn(Material material) => material.SetColor(id, ConvertedColor);

            public override Color Get(Material material) => material.GetColor(id);

            public override void SetOn(MaterialPropertyBlock block) => block.SetColor(id, ConvertedColor);

            protected override Color GlobalValue_Internal
            {
                get { return Shader.GetGlobalColor(id); }
                set { Shader.SetGlobalColor(id, ConvertedColor); }
            }

            void CheckColorSpace()
            {
                _colorSpaceChecked = true;
                #if UNITY_EDITOR
                ConvertToLinear = PlayerSettings.colorSpace == ColorSpace.Linear;
                #endif
            }

            public ColorFloat4Value()
            {
                latestValue = Color.grey;
            }

            public ColorFloat4Value(string name) : base(name)
            {
                latestValue = Color.grey;
            }
            
            public ColorFloat4Value(string name, bool convertToLinear) : base(name)
            {
                latestValue = Color.grey;
                ConvertToLinear = convertToLinear;
            }

            public ColorFloat4Value(string name, Color startingColor, bool convertToLinear) : base(name)
            {
                latestValue = startingColor;
                ConvertToLinear = convertToLinear;
            }

            public ColorFloat4Value(string name, Color startingColor) : base(name)
            {
                latestValue = startingColor;
            }

        }

        #endregion

        #region Vector

        public class VectorValue : IndexGeneric<Vector4>
        {

            public override void SetOn(Material material) => material.SetVector(id, latestValue);

            public override void SetOn(MaterialPropertyBlock block) => block.SetVector(id, latestValue);

            public override Vector4 Get(Material mat) => mat.GetVector(id);

            protected override Vector4 GlobalValue_Internal
            {
                get { return Shader.GetGlobalVector(id); }
                set { Shader.SetGlobalVector(id, value); }
            }

            public VectorValue()
            {
            }

            public VectorValue(string name) : base(name)
            {
            }

        }

        #endregion

        #region Matrix

        public class MatrixValue : IndexGeneric<Matrix4x4>
        {
            public override void SetOn(Material material) => material.SetMatrix(id, latestValue);

            public override void SetOn(MaterialPropertyBlock block) => block.SetMatrix(id, latestValue);

            public override Matrix4x4 Get(Material mat) => mat.GetMatrix(id);

            protected override Matrix4x4 GlobalValue_Internal
            {
                get { return Shader.GetGlobalMatrix(id); }
                set { Shader.SetGlobalMatrix(id, value); }
            }
            
            public MatrixValue()
            {
            }

            public MatrixValue(string name) : base(name)
            {
            }
        }

        #endregion

        #region Texture
        
        public class TextureValue : IndexGeneric<Texture>
        {
            public static readonly TextureValue mainTexture = new TextureValue("_MainTex");

            public override Texture Get(Material mat) => mat.GetTexture(id);

            public override void SetOn(Material material) => material.SetTexture(id, latestValue);

            public override void SetOn(MaterialPropertyBlock block) => block.SetTexture(id, latestValue);

            protected override Texture GlobalValue_Internal
            {
                get { return Shader.GetGlobalTexture(id); }
                set
                {
                    Shader.SetGlobalTexture(id, value);

                    if (_screenFillAspect!=null)
                        Set_ScreenFillAspect();

                }
            }

            #region Texture Specific

            [NonSerialized]
            private List<string> _usageTags = new List<string>();

            public TextureValue AddUsageTag(string value)
            {
                if (!_usageTags.Contains(value)) _usageTags.Add(value);
                return this;
            }

            public bool HasUsageTag(string tag) => _usageTags.Contains(tag);

            public Vector2 GetOffset(Material mat) => mat ? mat.GetTextureOffset(id) : Vector2.zero;

            public Vector2 GetTiling(Material mat) => mat ? mat.GetTextureScale(id) : Vector2.one;

            public void Set(Material mat, Rect value)
            {
                if (mat) {
                    mat.SetTextureOffset(id, value.min);
                    mat.SetTextureScale(id, value.size);
                }
            }

            public void SetOffset(Material mat, Vector2 value)
            {
                if (mat)
                    mat.SetTextureOffset(id, value);
            }

            public void SetTiling(Material mat, Vector2 value)
            {
                if (mat)
                    mat.SetTextureScale(id, value);
            }

            private const string FILL_ASPECT_RATION_SUFFIX = "_ScreenFillAspect";
            private  VectorValue _screenFillAspect;
            private VectorValue GetScreenFillAspect() {
            
                if (_screenFillAspect == null)
                    _screenFillAspect = new VectorValue(name + FILL_ASPECT_RATION_SUFFIX);
                return _screenFillAspect;
                
            }
            public void Set_ScreenFillAspect()
            {
                if (!latestValue)
                {
                   // QcUtils.ChillLogger.LogErrorOnce(name+"noTex", ()=>"{0} was not set. Can't Update {1} ".F(name, FILL_ASPECT_RATION_SUFFIX));
                    return;
                }
                
                float screenAspect = pegi.GameView.AspectRatio;
                float texAspect = ((float)latestValue.width) / latestValue.height;

                Vector4 aspectCorrection = new Vector4(1,1, 1f/latestValue.width, 1f/latestValue.height);

                if (screenAspect > texAspect)
                    aspectCorrection.y = (texAspect / screenAspect);
                else
                    aspectCorrection.x = (screenAspect / texAspect);

                

                GetScreenFillAspect().GlobalValue = aspectCorrection;
            } 

            #endregion

            #region Constructors
            public TextureValue()
            {
            }

            public TextureValue(string name, string tag, bool set_ScreenFillAspect = false) : base(name)
            {
                AddUsageTag(tag);

                if (set_ScreenFillAspect)
                    GetScreenFillAspect();
            }

            public TextureValue(string name, bool set_ScreenFillAspect = false) : base(name)
            {
                if (set_ScreenFillAspect)
                    GetScreenFillAspect();
            }

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode() => new CfgEncoder()
                .Add("b", base.Encode())
                .Add("tgs", _usageTags);

            public override bool Decode(string tg, string data)
            {
                switch (tg)
                {
                    case "b":
                        data.Decode_Delegate(base.Decode);
                        break;
                    case "tgs":
                        data.Decode_List(out _usageTags);
                        break;
                    default: return false;
                }

                return true;
            }

            #endregion
        }

        public static Vector2 GetOffset(this Material mat, TextureValue property) => property.GetOffset(mat);

        public static Vector2 GetTiling(this Material mat, TextureValue property) => property.GetTiling(mat);

        public static void SetOffset(this Material mat, TextureValue property, Vector2 value) =>
            property.SetOffset(mat, value);

        public static void SetTiling(this Material mat, TextureValue property, Vector2 value) =>
            property.SetTiling(mat, value);

        public static List<TextureValue> MyGetTextureProperties_Editor(this Material m)
        {
            #if UNITY_EDITOR
            {
                var lst = new List<TextureValue>();
                foreach (var n in m.GetProperties(MaterialProperty.PropType.Texture))
                    lst.Add(new TextureValue(n));

                return lst;
            }
            #else
            return new List<TextureValue>();
            #endif
        }

        #endregion

        #region Keyword

        public class ShaderKeyword : IPEGI {

            private string _name;

            private bool lastValue;

            public bool Enabled {
                get { return lastValue; }
                set { lastValue = value; QcUnity.SetShaderKeyword(_name, value); }
            }

            public ShaderKeyword(string name) {
                _name = name;
            }

            public bool Inspect()
            {
                var changed = false;
                if (_name.toggleIcon(ref lastValue).changes(ref changed))
                    Enabled = lastValue;

                return changed;
            }
        }

        #endregion

    }

    #region Shader Tags
    public class ShaderTag : IGotDisplayName
    {
        public readonly string tag;
        public string NameForDisplayPEGI()=> tag;
        public bool Has(Material mat) => mat.HasTag(tag);
        public string Get(Material mat, bool searchFallBacks = false, string defaultValue = "") => mat.GetTag(tag, searchFallBacks, defaultValue);

        public string Get(Material mat, ShaderProperty.BaseShaderPropertyIndex property,
            bool searchFallBacks = false) =>
            Get(mat, property.NameForDisplayPEGI(), searchFallBacks);

        public string Get(Material mat, string prefix, bool searchFallBacks = false) =>
            mat.GetTag(prefix + tag, searchFallBacks);

        public ShaderTag(string nTag)
        {
            tag = nTag;
        }

        public List<Material> GetTaggedMaterialsFromAssets()
        {
            var mats = new List<Material>();

#if UNITY_EDITOR

            var tmpMats = QcUnity.FindAssetsByType<Material>();

            foreach (var mat in tmpMats)
                if (Has(mat))
                    mats.AddIfNew(mat);
            

#endif

            return mats;
        }

    }

    public class ShaderTagValue : IGotDisplayName
    {
        private readonly ShaderTag tag;
        public string NameForDisplayPEGI()=> value;
        private readonly string value;

        public bool Has(Material mat, bool searchFallBacks = false) =>
            value.Equals(tag.Get(mat, searchFallBacks));

        public bool Has(Material mat, ShaderProperty.BaseShaderPropertyIndex property, bool searchFallBacks = false) =>
            mat && value.Equals(tag.Get(mat, property, searchFallBacks));

        public bool Equals(string tg) => value.Equals(tg);
        
        public ShaderTagValue(string newValue, ShaderTag nTag)
        {
            value = newValue;
            tag = nTag;
        }
    }

    public static class ShaderTags {
        
        public static readonly ShaderTag ShaderTip = new ShaderTag("ShaderTip");

        public static readonly ShaderTag Queue = new ShaderTag("Queue");

        public static class Queues 
        {
            public static readonly ShaderTagValue Background = new ShaderTagValue("Background", Queue);
            public static readonly ShaderTagValue Geometry = new ShaderTagValue("Geometry", Queue);
            public static readonly ShaderTagValue AlphaTest = new ShaderTagValue("Geometry", Queue);
            public static readonly ShaderTagValue Transparent = new ShaderTagValue("Transparent", Queue);
            public static readonly ShaderTagValue Overlay = new ShaderTagValue("Overlay", Queue);
        }
    }

    public static class ShaderTagExtensions
    {
        public static string Get(this Material mat, ShaderTag tag, bool searchFallBacks = false, string defaultValue = "") =>
            tag.Get(mat, searchFallBacks, defaultValue);

        public static string Get(this Material mat, ShaderProperty.BaseShaderPropertyIndex propertyPrefix,
            ShaderTag tag, bool searchFallBacks = false) =>
            tag.Get(mat, propertyPrefix, searchFallBacks);

        public static string Get(this Material mat, string prefix, ShaderTag tag, bool searchFallBacks = false) =>
            tag.Get(mat, prefix, searchFallBacks);

        public static bool Has(this Material mat, ShaderTag tag) => tag.Has(mat);

        public static bool Has(this Material mat, ShaderTagValue val, bool searchFallBacks = false) =>
            val.Has(mat, searchFallBacks);

        public static bool Has(this Material mat, ShaderProperty.BaseShaderPropertyIndex propertyPrefix,
            ShaderTagValue val, bool searchFallBacks = false) =>
            val.Has(mat, propertyPrefix, searchFallBacks);
    }
    #endregion


    /*
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(TextureValue))]
    public class TextureValueDrawer : PropertyDrawer {

        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label) {
            if (prop.Inspect("latestValue", pos, label))
                prop.GetValue<TextureValue>().SetGlobal();
        }

    }


    [CustomPropertyDrawer(typeof(ColorValue))]
    public class ColorValueDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            if (prop.Inspect("latestValue", pos, label))
                prop.GetValue<ColorValue>().SetGlobal();
        }

    }

#endif
    */
}
